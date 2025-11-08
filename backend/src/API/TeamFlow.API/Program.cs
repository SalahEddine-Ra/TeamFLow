using DotNetEnv;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TeamFlowAPI.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
Env.Load(".env.local");

// Add controllers
builder.Services.AddControllers();

// Configure DbContext
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("❌ Connection string is not configured. Check your .env.local file.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// FluentValidation
builder.Services.AddControllers()
    .AddFluentValidation(fv =>
    {
        fv.RegisterValidatorsFromAssemblyContaining<Program>();
        fv.AutomaticValidationEnabled = true;
    });
    


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