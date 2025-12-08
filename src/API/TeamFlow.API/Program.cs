using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamFlowAPI.Infrastructure.Database;
using TeamFlowAPI.Models.Entities;
using TeamFlowAPI.Services;
using TeamFlowAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
var root = Directory.GetCurrentDirectory();
var dotenv = Path.Combine(root, "../../../.env.local");
if (File.Exists(dotenv))
{
    Env.Load(dotenv);
}
else
{
    Env.Load(".env.local");
}

// Add controllers
builder.Services.AddControllers();

// Configure DbContext
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string is not configured. Check your .env.local file.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Token Validation Service
builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();

// Refresh Token Service
builder.Services.AddScoped<IRefreshTokensService, RefreshTokenService>();

builder.Services.AddScoped<IAccessTokensService, AccessTokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<PasswordService>();

//ip validation service
builder.Services.AddHttpClient();                     // enables IHttpClientFactory
builder.Services.AddScoped<IIpValidationService, IpValidationService>();

// optional – set a timeout for the client used by IpValidationService
builder.Services.AddHttpClient<IpValidationService>(c => c.Timeout = TimeSpan.FromSeconds(5));
// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    


var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();