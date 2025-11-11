using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TeamFlowAPI.Infrastructure.Database;
using TeamFlowAPI.Models.Entities;
using TeamFlowAPI.Services;
using TeamFlowAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
Env.Load(".env.local");

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

builder.Services.AddScoped<IIpValidationService, IpValidationService>();

// Token Validation Service
builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();

// Refresh Token Service (if not already registered)
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

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