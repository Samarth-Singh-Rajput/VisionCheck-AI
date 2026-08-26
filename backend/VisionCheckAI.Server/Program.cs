using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VisionCheckAI.Server.Data;
using VisionCheckAI.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers & JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 2. Database Context (SQLite)
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "visioncheck.db");
builder.Services.AddDbContext<VisionCheckDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// 3. Register Custom Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInferenceService, PyTorchInferenceService>();

// 4. Configure CORS to allow Blazor WebAssembly client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Authorization");
    });
});

// 5. Configure JWT Authentication
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "VisionCheckAI_SuperSecretKey_ForCourseProject_2026!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "VisionCheckAI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "VisionCheckAI.Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

// 6. Swagger API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure Database & Directory standard setup on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VisionCheckDbContext>();
    db.Database.EnsureCreated();
}

var uploadsDir = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "uploads");
Directory.CreateDirectory(uploadsDir);

// Configure Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazorClient");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
