using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SQS.API.Data;
using SQS.API.Hubs;
using SQS.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ════════════════════════════════════════════════════════════════
// 1. DATABASE — EF Core + MySQL (Pomelo)
// ════════════════════════════════════════════════════════════════
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(connectionString)
);

// ════════════════════════════════════════════════════════════════
// 2. AUTHENTICATION — JWT Bearer
// ════════════════════════════════════════════════════════════════
var jwtConfig = builder.Configuration.GetSection("Jwt");
var jwtKey    = jwtConfig["Key"]      ?? throw new InvalidOperationException("JWT Key not configured.");
var jwtIssuer = jwtConfig["Issuer"]   ?? "SQS.API";
var jwtAud    = jwtConfig["Audience"] ?? "SQS.Clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtIssuer,
        ValidAudience            = jwtAud,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew                = TimeSpan.Zero
    };

    // Hỗ trợ SignalR gửi token qua query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path        = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ════════════════════════════════════════════════════════════════
// 3. CORS — cho phép ReactJS và Flutter gọi API
// ════════════════════════════════════════════════════════════════
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SqsPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()  // Bắt buộc cho SignalR WebSocket
    );
});

// ════════════════════════════════════════════════════════════════
// 4. SIGNALR — Real-time Queue Updates
// ════════════════════════════════════════════════════════════════
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval    = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// ════════════════════════════════════════════════════════════════
// 5. APPLICATION SERVICES
// ════════════════════════════════════════════════════════════════

// Business Logic Services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SequenceService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<QueueNotificationService>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Serialize enum thành string (VD: "Waiting" thay vì 0)
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title       = "SQS API",
        Version     = "v1",
        Description = "Smart Queue System — Hệ thống xếp hàng tự động (IoT Project)"
    });

    // JWT auth trong Swagger UI
    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Nhập: 'Bearer {token}'",
        Name        = "Authorization",
        In          = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT"
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ════════════════════════════════════════════════════════════════
// 6. BUILD & PIPELINE
// ════════════════════════════════════════════════════════════════
var app = builder.Build();

// Auto-migrate khi khởi động (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SQS API v1"));
}

app.UseHttpsRedirection();
app.UseCors("SqsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ═ SignalR Hub endpoint ═════════════════════════════════════════════════
app.MapHub<QueueHub>("/hubs/queue");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status  = "healthy",
    service = "SQS API",
    time    = DateTime.UtcNow
}));

app.Run();
