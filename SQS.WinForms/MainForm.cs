using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.IO.Ports;

namespace SQS.WinForms;

public partial class MainForm : Form
{
    private readonly HttpClient _httpClient;
    private SerialPort? _serialPort;
    
    private TextBox txtGuestName = null!;
    private ComboBox cbxServices = null!;
    private Button btnGetTicket = null!;
    private Label lblStatus = null!;
    private Button btnConnectCom = null!;
    private ComboBox cbxComPorts = null!;
    
    // Store mapping from Service Name -> Service ID
    private Dictionary<string, int> _serviceMap = new();

    public MainForm()
    {
        _httpClient = new HttpClient();
        SetupUI();
    }

    private void SetupUI()
    {
        this.Text = "KIOSK - KHÁCH HÀNG LẤY SỐ";
        this.Size = new System.Drawing.Size(600, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        Label lblTitle = new Label { Text = "SMART QUEUE SYSTEM", Location = new System.Drawing.Point(50, 30), Size = new System.Drawing.Size(500, 30), TextAlign = System.Drawing.ContentAlignment.MiddleCenter, Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold) };
        Label lblSubTitle = new Label { Text = "Vui lòng nhập thông tin để lấy số", Location = new System.Drawing.Point(50, 60), Size = new System.Drawing.Size(500, 20), TextAlign = System.Drawing.ContentAlignment.MiddleCenter };
        
        Label lblName = new Label { Text = "Tên của bạn:", Location = new System.Drawing.Point(100, 110), Size = new System.Drawing.Size(400, 20) };
        txtGuestName = new TextBox { Location = new System.Drawing.Point(100, 130), Size = new System.Drawing.Size(400, 30), Font = new System.Drawing.Font("Segoe UI", 12) };

        Label lblService = new Label { Text = "Chọn dịch vụ:", Location = new System.Drawing.Point(100, 180), Size = new System.Drawing.Size(400, 20) };
        cbxServices = new ComboBox { Location = new System.Drawing.Point(100, 200), Size = new System.Drawing.Size(400, 30), Font = new System.Drawing.Font("Segoe UI", 12), DropDownStyle = ComboBoxStyle.DropDownList };
        
        btnGetTicket = new Button { Text = "IN SỐ THỨ TỰ", Location = new System.Drawing.Point(100, 260), Size = new System.Drawing.Size(400, 50), Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold), BackColor = System.Drawing.Color.DodgerBlue, ForeColor = System.Drawing.Color.White, FlatStyle = FlatStyle.Flat };
        btnGetTicket.Click += BtnGetTicket_Click;

        // Kết nối Arduino để hiển thị thông báo
        GroupBox grpCom = new GroupBox { Text = "Bảng Điện Tử Arduino", Location = new System.Drawing.Point(100, 340), Size = new System.Drawing.Size(400, 70) };
        cbxComPorts = new ComboBox { Location = new System.Drawing.Point(20, 30), Size = new System.Drawing.Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        cbxComPorts.Items.Add("COM2");
        cbxComPorts.SelectedIndex = 0;

        btnConnectCom = new Button { Text = "Kết nối", Location = new System.Drawing.Point(160, 28), Size = new System.Drawing.Size(100, 30) };
        btnConnectCom.Click += BtnConnectCom_Click;
        grpCom.Controls.Add(cbxComPorts);
        grpCom.Controls.Add(btnConnectCom);

        lblStatus = new Label { Text = "Đang tải danh sách dịch vụ...", Location = new System.Drawing.Point(10, 430), Size = new System.Drawing.Size(560, 20), ForeColor = System.Drawing.Color.Gray };

        this.Controls.Add(lblTitle);
        this.Controls.Add(lblSubTitle);
        this.Controls.Add(lblName);
        this.Controls.Add(txtGuestName);
        this.Controls.Add(lblService);
        this.Controls.Add(cbxServices);
        this.Controls.Add(btnGetTicket);
        this.Controls.Add(grpCom);
        this.Controls.Add(lblStatus);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            var response = await _httpClient.GetAsync("http://localhost:5000/api/services");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(content);
                
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    int id = el.GetProperty("id").GetInt32();
                    string name = el.GetProperty("name").GetString() ?? "";
                    
                    _serviceMap[name] = id;
                    cbxServices.Items.Add(name);
                }

                if (cbxServices.Items.Count > 0) cbxServices.SelectedIndex = 0;
                lblStatus.Text = "Hệ thống sẵn sàng phục vụ!";
                
                // Tự động kết nối COM2
                BtnConnectCom_Click(null, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Lỗi kết nối Server: " + ex.Message;
        }
    }

    private async void BtnGetTicket_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtGuestName.Text))
        {
            MessageBox.Show("Vui lòng nhập tên của bạn!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (cbxServices.SelectedItem == null) return;

        btnGetTicket.Enabled = false;
        string selectedServiceName = cbxServices.SelectedItem.ToString()!;
        int serviceId = _serviceMap[selectedServiceName];

        try
        {
            var payload = new { serviceId = serviceId, guestName = txtGuestName.Text.Trim() };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:5000/api/tickets/guest", content);
            
            if (response.IsSuccessStatusCode)
            {
                var resultString = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(resultString);
                
                string ticketNum = doc.RootElement.GetProperty("ticketNumber").GetString() ?? "";
                string serviceName = doc.RootElement.GetProperty("serviceName").GetString() ?? "";
                int estimatedWait = doc.RootElement.GetProperty("estimatedWait").GetInt32();
                
                // Show Ticket info
                string msg = $"LẤY SỐ THÀNH CÔNG!\n\nSố của bạn: {ticketNum}\nDịch vụ: {serviceName}\nĐang chờ trước bạn: {estimatedWait} người.";
                MessageBox.Show(msg, "In Số Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                txtGuestName.Text = "";
                SendToArduino($"MSG:So cua ban: {ticketNum}");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Lỗi lấy số: " + error, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi kết nối: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnGetTicket.Enabled = true;
        }
    }

    // ── COM PORT LOGIC (Physical Button Trigger) ──────────────────────

    private void BtnConnectCom_Click(object? sender, EventArgs e)
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();
            btnConnectCom.Text = "Kết nối";
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
            // Không còn dùng nút bấm trên Arduino, chỉ để trống.
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
