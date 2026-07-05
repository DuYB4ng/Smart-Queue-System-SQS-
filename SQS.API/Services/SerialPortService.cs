using System.IO.Ports;

namespace SQS.API.Services;

/// <summary>
/// Quản lý kết nối Serial Port (COM) với Arduino mô phỏng trên Proteus.
///
/// PROTOCOL:
///   Backend → Arduino: "CALL:001\n"   → LCD hiển thị + Buzzer kêu
///                      "RESET\n"      → Xóa màn hình LCD
///                      "MSG:text\n"   → Hiển thị text bất kỳ
///
///   Arduino → Backend: "ACK:OK\n"     → Xác nhận nhận lệnh
///                      "BTN:NEXT\n"   → Nút bấm vật lý (gọi số tiếp)
///                      "BTN:RESET\n"  → Nút reset
///
/// Đăng ký dưới dạng Singleton để giữ kết nối COM suốt vòng đời app.
/// </summary>
public class SerialPortService : IDisposable
{
    private SerialPort?  _port;
    private readonly ILogger<SerialPortService> _logger;
    private readonly IConfiguration _config;
    private bool _isEnabled;
    private bool _disposed;

    // Event phát ra khi nhận được tín hiệu từ Arduino
    public event EventHandler<ArduinoMessageEventArgs>? MessageReceived;

    public bool IsConnected => _port?.IsOpen ?? false;
    public string PortName   => _config["SerialPort:PortName"] ?? "COM4";

    public SerialPortService(ILogger<SerialPortService> logger, IConfiguration config)
    {
        _logger    = logger;
        _config    = config;
        _isEnabled = bool.Parse(config["SerialPort:Enabled"] ?? "false");
    }

    // ── CONNECT ───────────────────────────────────────────────────

    /// <summary>
    /// Mở kết nối với cổng COM.
    /// Tự động bỏ qua nếu SerialPort:Enabled = false trong config.
    /// </summary>
    public bool Connect()
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("SerialPort disabled (SerialPort:Enabled=false). Bỏ qua kết nối COM.");
            return false;
        }

        try
        {
            var portName  = _config["SerialPort:PortName"] ?? "COM4";
            var baudRate  = int.Parse(_config["SerialPort:BaudRate"] ?? "9600");

            _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout  = 1000,
                WriteTimeout = 1000,
                NewLine      = "\n",
                Encoding     = System.Text.Encoding.ASCII,
            };

            _port.DataReceived += OnDataReceived;
            _port.ErrorReceived += OnErrorReceived;
            _port.Open();

            _logger.LogInformation("✅ Đã kết nối COM port: {Port} @ {Baud} baud", portName, baudRate);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️  Không thể mở COM port: {Message}. Chạy tiếp không có phần cứng.", ex.Message);
            return false;
        }
    }

    // ── DISCONNECT ────────────────────────────────────────────────

    public void Disconnect()
    {
        if (_port?.IsOpen == true)
        {
            _port.Close();
            _logger.LogInformation("Đã đóng COM port.");
        }
    }

    // ── SEND COMMANDS ─────────────────────────────────────────────

    /// <summary>
    /// Gửi lệnh hiển thị số thứ tự lên LCD Arduino.
    /// Gửi: "CALL:001\n"
    /// </summary>
    public async Task SendCallNumberAsync(string ticketNumber)
    {
        await SendRawAsync($"CALL:{ticketNumber}");
        _logger.LogInformation("📤 COM → Arduino: CALL:{Number}", ticketNumber);
    }

    /// <summary>Xóa màn hình LCD Arduino.</summary>
    public async Task SendResetAsync()
    {
        await SendRawAsync("RESET");
        _logger.LogInformation("📤 COM → Arduino: RESET");
    }

    /// <summary>Gửi text tùy ý lên LCD (tối đa 16 ký tự/dòng).</summary>
    public async Task SendMessageAsync(string message)
    {
        var safe = message.Length > 32 ? message[..32] : message;
        await SendRawAsync($"MSG:{safe}");
    }

    // ── INTERNAL ──────────────────────────────────────────────────

    private Task SendRawAsync(string command)
    {
        if (_port is null || !_port.IsOpen)
        {
            _logger.LogDebug("COM port chưa kết nối — bỏ qua lệnh: {Cmd}", command);
            return Task.CompletedTask;
        }

        return Task.Run(() =>
        {
            try
            {
                _port.WriteLine(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi lệnh COM: {Cmd}", command);
            }
        });
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var raw = _port!.ReadLine().Trim();
            _logger.LogDebug("📥 Arduino → COM: {Data}", raw);

            var args = ParseArduinoMessage(raw);
            if (args is not null)
                MessageReceived?.Invoke(this, args);
        }
        catch (TimeoutException) { /* Bình thường nếu không có data */ }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi đọc COM port.");
        }
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        _logger.LogWarning("COM port error: {Type}", e.EventType);
    }

    private static ArduinoMessageEventArgs? ParseArduinoMessage(string raw)
    {
        if (raw.StartsWith("ACK:"))
            return new ArduinoMessageEventArgs(ArduinoMessageType.Ack, raw[4..]);

        if (raw.StartsWith("BTN:"))
            return new ArduinoMessageEventArgs(ArduinoMessageType.Button, raw[4..]);

        return new ArduinoMessageEventArgs(ArduinoMessageType.Unknown, raw);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _port?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

// ── EVENT ARGS ────────────────────────────────────────────────────

public class ArduinoMessageEventArgs : EventArgs
{
    public ArduinoMessageType Type    { get; }
    public string             Payload { get; }
    public DateTime           ReceivedAt { get; } = DateTime.UtcNow;

    public ArduinoMessageEventArgs(ArduinoMessageType type, string payload)
    {
        Type    = type;
        Payload = payload;
    }
}

public enum ArduinoMessageType
{
    Ack,      // "ACK:OK" — Arduino xác nhận
    Button,   // "BTN:NEXT" hoặc "BTN:RESET"
    Unknown
}
