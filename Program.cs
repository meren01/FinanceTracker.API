using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FinanceTracker.API.Data;
using FinanceTracker.API.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Controller ve Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

// AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT Authentication
var jwtSection = configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection.GetValue<string>("Key"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Dev ortamýnda true olursa sorun çýkabilir
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection.GetValue<string>("Issuer"),
        ValidAudience = jwtSection.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Authorization
builder.Services.AddAuthorization();

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173") // React URL'leri
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp"); // CORS burada olmalý

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
