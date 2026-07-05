using System;
using System.IO.Ports;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.SignalR.Client;

namespace SQS.WinForms;

public partial class MainForm : Form
{
    private HubConnection? _hubConnection;
    private SerialPort? _serialPort;
    private readonly HttpClient _httpClient;
    
    // UI Elements
    private Label lblCurrentTicket = null!;
    private Label lblStatus = null!;
    private Label lblQueueCount = null!;
    private Button btnCallNext = null!;
    private Button btnComplete = null!;
    private Button btnSkip = null!;
    private Button btnConnectCom = null!;
    private ComboBox cbxComPorts = null!;
    
    private int _currentTicketId = 0;
    private int _counterId = 1; // Giả sử nhân viên này ngồi quầy 1

    public MainForm()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginForm.JwtToken);
        
        SetupUI();
    }

    private void SetupUI()
    {
        this.Text = $"Quầy {_counterId} - Nhân viên: {LoginForm.LoggedInUserName}";
        this.Size = new System.Drawing.Size(600, 500);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Current Ticket Display
        Label lblTitle = new Label { Text = "ĐANG PHỤC VỤ", Location = new System.Drawing.Point(50, 30), Size = new System.Drawing.Size(500, 20), TextAlign = System.Drawing.ContentAlignment.MiddleCenter, Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold) };
        lblCurrentTicket = new Label { Text = "--", Location = new System.Drawing.Point(50, 60), Size = new System.Drawing.Size(500, 80), TextAlign = System.Drawing.ContentAlignment.MiddleCenter, Font = new System.Drawing.Font("Segoe UI", 48, System.Drawing.FontStyle.Bold), ForeColor = System.Drawing.Color.DodgerBlue };
        
        // Queue status
        lblQueueCount = new Label { Text = "Đang chờ: Đang tải...", Location = new System.Drawing.Point(50, 150), Size = new System.Drawing.Size(500, 20), TextAlign = System.Drawing.ContentAlignment.MiddleCenter };

        // Buttons
        btnCallNext = new Button { Text = "Gọi số tiếp theo", Location = new System.Drawing.Point(100, 200), Size = new System.Drawing.Size(400, 50), Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold), BackColor = System.Drawing.Color.MediumSeaGreen, ForeColor = System.Drawing.Color.White, FlatStyle = FlatStyle.Flat };
        btnCallNext.Click += BtnCallNext_Click;

        btnComplete = new Button { Text = "Hoàn thành", Location = new System.Drawing.Point(100, 270), Size = new System.Drawing.Size(190, 40), BackColor = System.Drawing.Color.SteelBlue, ForeColor = System.Drawing.Color.White, FlatStyle = FlatStyle.Flat, Enabled = false };
        btnComplete.Click += BtnComplete_Click;

        btnSkip = new Button { Text = "Bỏ qua", Location = new System.Drawing.Point(310, 270), Size = new System.Drawing.Size(190, 40), BackColor = System.Drawing.Color.Gray, ForeColor = System.Drawing.Color.White, FlatStyle = FlatStyle.Flat, Enabled = false };
        btnSkip.Click += BtnSkip_Click;

        // COM Port Section
        GroupBox grpCom = new GroupBox { Text = "Kết nối Arduino (Nút cứng)", Location = new System.Drawing.Point(100, 340), Size = new System.Drawing.Size(400, 80) };
        cbxComPorts = new ComboBox { Location = new System.Drawing.Point(20, 30), Size = new System.Drawing.Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        cbxComPorts.Items.AddRange(SerialPort.GetPortNames());
        if (cbxComPorts.Items.Count > 0) cbxComPorts.SelectedIndex = 0;

        btnConnectCom = new Button { Text = "Kết nối COM", Location = new System.Drawing.Point(160, 28), Size = new System.Drawing.Size(120, 30) };
        btnConnectCom.Click += BtnConnectCom_Click;

        grpCom.Controls.Add(cbxComPorts);
        grpCom.Controls.Add(btnConnectCom);

        lblStatus = new Label { Text = "Trạng thái: Đang kết nối server...", Location = new System.Drawing.Point(10, 430), Size = new System.Drawing.Size(560, 20), ForeColor = System.Drawing.Color.Gray };

        this.Controls.Add(lblTitle);
        this.Controls.Add(lblCurrentTicket);
        this.Controls.Add(lblQueueCount);
        this.Controls.Add(btnCallNext);
        this.Controls.Add(btnComplete);
        this.Controls.Add(btnSkip);
        this.Controls.Add(grpCom);
        this.Controls.Add(lblStatus);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadMyQueueAsync();
        await SetupSignalRAsync();
    }

    private async Task SetupSignalRAsync()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/hubs/queue")
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<JsonElement>("QueueUpdated", (payload) =>
            {
                // Refresh queue when there's an update
                Invoke(new Action(async () => await LoadMyQueueAsync()));
            });

            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("JoinGroup", $"staff-{_counterId}");
            
            lblStatus.Text = "Trạng thái: Đã kết nối Server (SignalR).";
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Lỗi SignalR: " + ex.Message;
        }
    }

    private async Task LoadMyQueueAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"http://localhost:5000/api/staff/my-queue?counterId={_counterId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(content);
                var waitingCount = doc.RootElement.GetProperty("waitingCount").GetInt32();
                
                lblQueueCount.Text = $"Đang chờ trong hàng đợi: {waitingCount} người";
                btnCallNext.Enabled = waitingCount > 0 || _currentTicketId == 0;
            }
        }
        catch { }
    }

    private async void BtnCallNext_Click(object? sender, EventArgs e)
    {
        btnCallNext.Enabled = false;
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(new { counterId = _counterId }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("http://localhost:5000/api/staff/call-next", content);
            
            if (response.IsSuccessStatusCode)
            {
                var resultString = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(resultString);
                
                _currentTicketId = doc.RootElement.GetProperty("ticketId").GetInt32();
                var ticketNum = doc.RootElement.GetProperty("ticketNumber").GetString();
                
                lblCurrentTicket.Text = ticketNum;
                btnComplete.Enabled = true;
                btnSkip.Enabled = true;
                
                SendToArduino($"CALL:{ticketNum}");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Lỗi gọi số: " + error, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi kết nối: " + ex.Message);
        }
        finally
        {
            await LoadMyQueueAsync();
        }
    }

    private async void BtnComplete_Click(object? sender, EventArgs e)
    {
        if (_currentTicketId == 0) return;
        try
        {
            await _httpClient.PostAsync($"http://localhost:5000/api/staff/complete/{_currentTicketId}", null);
            ResetCurrentTicket();
        }
        catch { }
    }

    private async void BtnSkip_Click(object? sender, EventArgs e)
    {
        if (_currentTicketId == 0) return;
        try
        {
            await _httpClient.PostAsync($"http://localhost:5000/api/staff/skip/{_currentTicketId}", null);
            ResetCurrentTicket();
        }
        catch { }
    }

    private void ResetCurrentTicket()
    {
        _currentTicketId = 0;
        lblCurrentTicket.Text = "--";
        btnComplete.Enabled = false;
        btnSkip.Enabled = false;
        LoadMyQueueAsync().Wait();
    }

    // ── COM PORT LOGIC ────────────────────────────────────────────────

    private void BtnConnectCom_Click(object? sender, EventArgs e)
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();
            btnConnectCom.Text = "Kết nối COM";
            lblStatus.Text = "Đã ngắt kết nối Arduino.";
            return;
        }

        if (cbxComPorts.SelectedItem == null)
        {
            MessageBox.Show("Vui lòng chọn cổng COM!");
            return;
        }

        try
        {
            _serialPort = new SerialPort(cbxComPorts.SelectedItem.ToString()!, 9600);
            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();
            btnConnectCom.Text = "Ngắt kết nối";
            lblStatus.Text = $"Đã kết nối {_serialPort.PortName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi kết nối COM: " + ex.Message);
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var data = _serialPort!.ReadLine().Trim();
            if (data == "BTN:NEXT")
            {
                // Invoke trên UI thread
                this.Invoke(new Action(() => 
                {
                    if (btnCallNext.Enabled)
                        BtnCallNext_Click(null, EventArgs.Empty);
                }));
            }
        }
        catch { }
    }

    private void SendToArduino(string command)
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            try
            {
                _serialPort.WriteLine(command);
            }
            catch { }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_serialPort != null && _serialPort.IsOpen)
            _serialPort.Close();
        
        base.OnFormClosing(e);
    }
}
