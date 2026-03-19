using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using server_dotnet.Converters;
using server_dotnet.Data;
using Yitter.IdGenerator;

var builder = WebApplication.CreateBuilder(args);

// Initialize Yitter IdGenerator (雪花ID生成器)
var snowflakeBaseTimeStr = builder.Configuration["SnowflakeId:BaseTime"] ?? "2026-03-15 00:00:00";
var snowflakeBaseTime = DateTime.ParseExact(snowflakeBaseTimeStr, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
var idGeneratorOptions = new IdGeneratorOptions(1)
{
    BaseTime = new DateTime(snowflakeBaseTime.Ticks, DateTimeKind.Utc)
};
YitIdHelper.SetIdGenerator(idGeneratorOptions);

// Configure JSON to serialize long values as strings and DateTime in yyyy-MM-dd HH:mm:ss format
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new LongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableLongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new DateTimeOffsetConverter());
    });
builder.Services.AddOpenApi();

// Configure MySQL database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(5, 7, 0))));

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "markdown-notes-secret-key-2024";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Map routes
app.MapControllers();

// Root endpoint
app.MapGet("/", () => new { message = "Markdown Notes API Server", version = "1.0.0" });

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    Console.WriteLine("数据库初始化成功");
}

app.Run();
