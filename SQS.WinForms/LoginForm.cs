using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace SQS.WinForms;

public partial class LoginForm : Form
{
    private TextBox txtEmail = null!;
    private TextBox txtPassword = null!;
    private Button btnLogin = null!;
    private Label lblStatus = null!;

    public static string JwtToken { get; private set; } = string.Empty;
    public static int LoggedInUserId { get; private set; }
    public static string LoggedInUserName { get; private set; } = string.Empty;
    public static string LoggedInRole { get; private set; } = string.Empty;

    public LoginForm()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        this.Text = "Đăng Nhập - SQS Desktop";
        this.Size = new System.Drawing.Size(400, 300);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        Label lblTitle = new Label { Text = "Smart Queue System", Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold), Location = new System.Drawing.Point(50, 20), Size = new System.Drawing.Size(300, 40), TextAlign = System.Drawing.ContentAlignment.MiddleCenter };
        
        Label lblEmail = new Label { Text = "Email:", Location = new System.Drawing.Point(50, 80), Size = new System.Drawing.Size(100, 20) };
        txtEmail = new TextBox { Location = new System.Drawing.Point(50, 100), Size = new System.Drawing.Size(280, 25), Text = "staff1@sqs.com" };
        
        Label lblPassword = new Label { Text = "Mật khẩu:", Location = new System.Drawing.Point(50, 130), Size = new System.Drawing.Size(100, 20) };
        txtPassword = new TextBox { Location = new System.Drawing.Point(50, 150), Size = new System.Drawing.Size(280, 25), PasswordChar = '*', Text = "123456" };
        
        btnLogin = new Button { Text = "Đăng Nhập", Location = new System.Drawing.Point(50, 190), Size = new System.Drawing.Size(280, 35), BackColor = System.Drawing.Color.DodgerBlue, ForeColor = System.Drawing.Color.White, FlatStyle = FlatStyle.Flat };
        btnLogin.Click += BtnLogin_Click;

        lblStatus = new Label { Text = "", Location = new System.Drawing.Point(50, 230), Size = new System.Drawing.Size(280, 20), ForeColor = System.Drawing.Color.Red, TextAlign = System.Drawing.ContentAlignment.MiddleCenter };

        this.Controls.Add(lblTitle);
        this.Controls.Add(lblEmail);
        this.Controls.Add(txtEmail);
        this.Controls.Add(lblPassword);
        this.Controls.Add(txtPassword);
        this.Controls.Add(btnLogin);
        this.Controls.Add(lblStatus);
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblStatus.Text = "Đang đăng nhập...";
        btnLogin.Enabled = false;

        try
        {
            using var client = new HttpClient();
            var payload = new { Email = txtEmail.Text.Trim(), Password = txtPassword.Text };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://localhost:5000/api/auth/login", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var doc = JsonDocument.Parse(responseString);
                JwtToken = doc.RootElement.GetProperty("token").GetString() ?? "";
                
                var user = doc.RootElement.GetProperty("user");
                LoggedInUserId = user.GetProperty("id").GetInt32();
                LoggedInUserName = user.GetProperty("name").GetString() ?? "";
                LoggedInRole = user.GetProperty("role").GetString() ?? "";

                if (LoggedInRole != "Staff" && LoggedInRole != "Admin")
                {
                    lblStatus.Text = "Ứng dụng này chỉ dành cho Staff/Admin.";
                    btnLogin.Enabled = true;
                    return;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                lblStatus.Text = "Đăng nhập thất bại. Sai email hoặc mật khẩu.";
                btnLogin.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Lỗi kết nối tới Server.";
            btnLogin.Enabled = true;
        }
    }
}
