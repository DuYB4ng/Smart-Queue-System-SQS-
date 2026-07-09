using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace SQS.WinForms;

public partial class MainForm : Form
{
    private readonly HttpClient _httpClient;

    private Panel pnlServiceButtons = null!;
    private Label lblStatus = null!;
    private Label lblTitle = null!;
    private Label lblSubTitle = null!;

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
        this.Size = new Size(820, 700);
        this.MinimumSize = new Size(820, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(245, 247, 250);

        // ── Tiêu đề ─────────────────────────────────────────────────────
        lblTitle = new Label
        {
            Text = "SMART QUEUE SYSTEM",
            Location = new Point(40, 30),
            Size = new Size(730, 50),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 26, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 64, 175)
        };

        lblSubTitle = new Label
        {
            Text = "Chọn dịch vụ để lấy số thứ tự",
            Location = new Point(40, 82),
            Size = new Size(730, 28),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 13, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        // ── Separator line ───────────────────────────────────────────────
        Panel separator = new Panel
        {
            Location = new Point(40, 120),
            Size = new Size(730, 2),
            BackColor = Color.FromArgb(226, 232, 240)
        };

        // ── Panel chứa các nút dịch vụ ──────────────────────────────────
        pnlServiceButtons = new Panel
        {
            Location = new Point(40, 132),
            Size = new Size(730, 490),
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        // ── Label trạng thái ────────────────────────────────────────────
        lblStatus = new Label
        {
            Text = "⏳ Đang tải danh sách dịch vụ...",
            Location = new Point(10, 640),
            Size = new Size(790, 24),
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(100, 116, 139),
            TextAlign = ContentAlignment.MiddleCenter
        };

        this.Controls.Add(lblTitle);
        this.Controls.Add(lblSubTitle);
        this.Controls.Add(separator);
        this.Controls.Add(pnlServiceButtons);
        this.Controls.Add(lblStatus);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadServicesAsync();
    }

    private async System.Threading.Tasks.Task LoadServicesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("http://localhost:5000/api/services");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(content);

                var services = new List<(int Id, string Name, string? Code)>();
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    int id = el.GetProperty("id").GetInt32();
                    string name = el.GetProperty("name").GetString() ?? "";
                    string? code = el.TryGetProperty("code", out var cp) ? cp.GetString() : null;
                    _serviceMap[name] = id;
                    services.Add((id, name, code));
                }

                BuildServiceButtons(services);
                lblStatus.Text = "✅ Hệ thống sẵn sàng phục vụ!";
                lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
            }
            else
            {
                lblStatus.Text = "❌ Không tải được danh sách dịch vụ.";
                lblStatus.ForeColor = Color.Crimson;
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = "❌ Lỗi kết nối Server: " + ex.Message;
            lblStatus.ForeColor = Color.Crimson;

            // Hiển thị nút dịch vụ mẫu để test UI khi không có server
            BuildServiceButtons(new List<(int, string, string?)>
            {
                (1, "Đăng ký học phần", "DK"),
                (2, "Nộp hồ sơ", "HS"),
                (3, "Thanh toán học phí", "HP"),
                (4, "Tư vấn tuyển sinh", "TV"),
                (5, "Nhận bằng / Giấy tờ", "GB"),
            });
        }
    }

    private void BuildServiceButtons(List<(int Id, string Name, string? Code)> services)
    {
        pnlServiceButtons.Controls.Clear();

        // Màu sắc cho các nút
        Color[] palette = new Color[]
        {
            Color.FromArgb(37, 99, 235),   // blue
            Color.FromArgb(5, 150, 105),   // green
            Color.FromArgb(217, 119, 6),   // amber
            Color.FromArgb(220, 38, 38),   // red
            Color.FromArgb(124, 58, 237),  // violet
            Color.FromArgb(14, 165, 233),  // sky
        };

        int btnWidth = 330;
        int btnHeight = 100;
        int hGap = 20;
        int vGap = 16;
        int cols = 2;
        int panelWidth = pnlServiceButtons.Width;

        for (int i = 0; i < services.Count; i++)
        {
            var svc = services[i];
            int col = i % cols;
            int row = i / cols;

            int x = col * (btnWidth + hGap);
            int y = row * (btnHeight + vGap);

            // Center nếu số lẻ và đây là nút cuối
            if (services.Count % 2 != 0 && i == services.Count - 1)
                x = (panelWidth - btnWidth) / 2;

            Color btnColor = palette[i % palette.Length];

            Button btn = new Button
            {
                Text = (svc.Code != null ? $"[{svc.Code}]\n" : "") + svc.Name,
                Location = new Point(x, y),
                Size = new Size(btnWidth, btnHeight),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = btnColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = svc.Id,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(btnColor, 0.1f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(btnColor, 0.2f);

            // Bo góc thông qua Paint event
            btn.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            };

            btn.Click += ServiceButton_Click;
            pnlServiceButtons.Controls.Add(btn);
        }

        // Điều chỉnh size form theo số dịch vụ
        int rows = (int)Math.Ceiling(services.Count / (double)cols);
        int neededHeight = 145 + rows * (btnHeight + vGap) + 70;
        this.Size = new Size(this.Width, Math.Min(neededHeight, 750));
        lblStatus.Location = new Point(10, this.ClientSize.Height - 38);
        lblStatus.Size = new Size(this.ClientSize.Width - 20, 24);
    }

    private async void ServiceButton_Click(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        int serviceId = (int)btn.Tag!;
        string serviceName = btn.Text.Contains('\n') ? btn.Text.Split('\n')[1] : btn.Text;

        // Vô hiệu hóa tất cả nút trong khi xử lý
        foreach (Control c in pnlServiceButtons.Controls)
            c.Enabled = false;

        lblStatus.Text = "⏳ Đang lấy số thứ tự...";
        lblStatus.ForeColor = Color.FromArgb(100, 116, 139);

        try
        {
            var payload = new { serviceId = serviceId, guestName = "Khách" };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:5000/api/tickets/guest", content);

            if (response.IsSuccessStatusCode)
            {
                var resultString = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(resultString);

                string ticketNum = doc.RootElement.GetProperty("ticketNumber").GetString() ?? "???";
                string svcName = doc.RootElement.TryGetProperty("serviceName", out var sn) ? sn.GetString() ?? serviceName : serviceName;
                int estimatedWait = doc.RootElement.TryGetProperty("estimatedWait", out var ew) ? ew.GetInt32() : 0;

                // Hiển thị form xác nhận lấy số
                ShowTicketResult(ticketNum, svcName, estimatedWait);

                lblStatus.Text = "✅ Lấy số thành công!";
                lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Lỗi lấy số: " + error, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "❌ Có lỗi xảy ra khi lấy số.";
                lblStatus.ForeColor = Color.Crimson;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi kết nối: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "❌ Lỗi kết nối máy chủ.";
            lblStatus.ForeColor = Color.Crimson;
        }
        finally
        {
            foreach (Control c in pnlServiceButtons.Controls)
                c.Enabled = true;
        }
    }

    private void ShowTicketResult(string ticketNumber, string serviceName, int waitCount)
    {
        // Phát âm thanh máy in (Tùy chọn: dùng tiếng Beep ngắn liên tục mô phỏng)
        System.Threading.Tasks.Task.Run(() => {
            for(int i=0; i<3; i++) { Console.Beep(4000, 50); System.Threading.Thread.Sleep(50); }
        });

        using var dlg = new Form
        {
            Text = "PHIẾU LẤY SỐ",
            Size = new Size(350, 550),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.None, // Bỏ viền để giống tờ giấy
            BackColor = Color.White,
            ShowInTaskbar = false
        };

        // Panel viền đen đứt nét tạo cảm giác phiếu in
        var pnlReceipt = new Panel
        {
            Size = new Size(330, 530),
            Location = new Point(10, 10),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };
        dlg.Controls.Add(pnlReceipt);

        var lblHeader = new Label
        {
            Text = "TRUNG TÂM HÀNH CHÍNH\nSMART QUEUE SYSTEM",
            Font = new Font("Courier New", 14, FontStyle.Bold),
            Size = new Size(310, 50),
            Location = new Point(10, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblDivider1 = new Label { Text = "--------------------------------", Font = new Font("Courier New", 12), Location = new Point(10, 75), Size = new Size(310, 20), TextAlign = ContentAlignment.MiddleCenter };

        var lblService = new Label
        {
            Text = serviceName.ToUpper(),
            Font = new Font("Courier New", 12, FontStyle.Bold),
            Size = new Size(310, 30),
            Location = new Point(10, 105),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblNum = new Label
        {
            Text = ticketNumber,
            Font = new Font("Arial", 60, FontStyle.Bold),
            Size = new Size(310, 100),
            Location = new Point(10, 140),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblDivider2 = new Label { Text = "--------------------------------", Font = new Font("Courier New", 12), Location = new Point(10, 250), Size = new Size(310, 20), TextAlign = ContentAlignment.MiddleCenter };

        var lblTime = new Label
        {
            Text = $"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm}",
            Font = new Font("Courier New", 10),
            Size = new Size(310, 25),
            Location = new Point(10, 280),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblWait = new Label
        {
            Text = $"Đang chờ trước bạn: {waitCount} người",
            Font = new Font("Courier New", 10, FontStyle.Bold),
            Size = new Size(310, 25),
            Location = new Point(10, 310),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblFooter = new Label
        {
            Text = "Vui lòng chú ý lắng nghe\nthông báo từ hệ thống loa.",
            Font = new Font("Courier New", 10, FontStyle.Italic),
            Size = new Size(310, 50),
            Location = new Point(10, 350),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var btnClose = new Button
        {
            Text = "XÉ PHIẾU (ĐÓNG)",
            Font = new Font("Arial", 12, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(290, 50),
            Location = new Point(20, 450),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (s, e) => dlg.Close();

        pnlReceipt.Controls.AddRange(new Control[] { 
            lblHeader, lblDivider1, lblService, lblNum, lblDivider2, lblTime, lblWait, lblFooter, btnClose 
        });

        // Hiệu ứng trượt lên (Slide up) mô phỏng máy in
        System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 15 };
        int targetY = (this.ClientSize.Height - dlg.Height) / 2;
        dlg.StartPosition = FormStartPosition.Manual;
        dlg.Location = new Point(this.Location.X + (this.Width - dlg.Width) / 2, this.Location.Y + this.Height);

        t.Tick += (s, e) => {
            if (dlg.Location.Y > this.Location.Y + targetY) {
                dlg.Location = new Point(dlg.Location.X, dlg.Location.Y - 20);
            } else {
                t.Stop();
            }
        };

        dlg.Load += (s, e) => t.Start();
        dlg.ShowDialog(this);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _httpClient.Dispose();
        base.OnFormClosing(e);
    }
}
