using SQS.API.Services;

namespace SQS.API.BackgroundServices;

/// <summary>
/// Background service khởi động SerialPort khi app bắt đầu,
/// lắng nghe tín hiệu từ Arduino và phản ứng tự động.
///
/// Kịch bản chính:
///   Arduino gửi "BTN:NEXT\n" (nhấn nút vật lý)
///   → Background service nhận event
///   → Gọi API gọi số tiếp theo tại quầy mặc định
/// </summary>
public class ArduinoListenerService : BackgroundService
{
    private readonly SerialPortService _serial;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ArduinoListenerService> _logger;
    private readonly IConfiguration _config;

    public ArduinoListenerService(
        SerialPortService     serial,
        IServiceScopeFactory  scopeFactory,
        ILogger<ArduinoListenerService> logger,
        IConfiguration        config)
    {
        _serial       = serial;
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _config       = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ArduinoListenerService khởi động...");

        // Thử kết nối COM khi app start
        var connected = _serial.Connect();

        if (connected)
        {
            // Đăng ký nhận event từ Arduino
            _serial.MessageReceived += OnArduinoMessageReceived;
            _logger.LogInformation("Đang lắng nghe tín hiệu từ Arduino trên {Port}...", _serial.PortName);
        }
        else
        {
            _logger.LogInformation("Chạy ở chế độ không có phần cứng (COM port không kết nối).");
        }

        // Giữ background service sống cho đến khi app shutdown
        await Task.Delay(Timeout.Infinite, stoppingToken);

        // Cleanup khi shutdown
        _serial.MessageReceived -= OnArduinoMessageReceived;
        _serial.Disconnect();
    }

    private async void OnArduinoMessageReceived(object? sender, ArduinoMessageEventArgs e)
    {
        _logger.LogDebug("Arduino event: Type={Type}, Payload={Payload}", e.Type, e.Payload);

        switch (e.Type)
        {
            case ArduinoMessageType.Button when e.Payload == "NEXT":
                await HandleButtonNextAsync();
                break;

            case ArduinoMessageType.Button when e.Payload == "RESET":
                await _serial.SendResetAsync();
                break;

            case ArduinoMessageType.Ack:
                _logger.LogDebug("Arduino ACK: {Payload}", e.Payload);
                break;
        }
    }

    /// <summary>
    /// Khi Arduino gửi BTN:NEXT → gọi API call-next cho quầy mặc định.
    /// CounterId mặc định lấy từ config hoặc = 1.
    /// </summary>
    private async Task HandleButtonNextAsync()
    {
        try
        {
            // Default counterId từ config (quầy nào có Arduino kết nối)
            var defaultCounterId = int.Parse(_config["SerialPort:DefaultCounterId"] ?? "1");
            var defaultStaffId   = int.Parse(_config["SerialPort:DefaultStaffId"] ?? "1");

            _logger.LogInformation(
                "BTN:NEXT nhận được → Gọi số tại quầy {CounterId}", defaultCounterId);

            using var scope   = _scopeFactory.CreateScope();
            var ticketService = scope.ServiceProvider.GetRequiredService<TicketService>();

            var result = await ticketService.CallNextAsync(defaultStaffId, defaultCounterId);

            // Gửi số vừa gọi ra LCD
            await _serial.SendCallNumberAsync(result.TicketNumber);

            _logger.LogInformation("BTN:NEXT → Gọi thành công số {Number}", result.TicketNumber);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Không còn ai"))
        {
            _logger.LogInformation("BTN:NEXT → Hàng đợi trống.");
            await _serial.SendMessageAsync("HANG DOI TRONG");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xử lý BTN:NEXT từ Arduino.");
        }
    }
}
