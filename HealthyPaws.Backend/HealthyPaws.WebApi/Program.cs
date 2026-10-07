using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.WebApi.Filters;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.Json.Serialization;
using DotNetEnv;
using HealthyPaws.Services;
using Microsoft.Extensions.Configuration;

Env.Load("./.env");

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", false);

// Add services to the container.
builder.Services.ConfigureAuthorization(builder.Configuration);
builder.Services.AddControllers(config =>
{
    config.ConfigureControllerAuthorization();
    config.Filters.Add<ExceptionFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDatabase(Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? throw new ArgumentException("ConnectionStrings__Default is not defined"));
//await builder.Services.MigrateDatabaseAsync();

builder.Services.AddMapster();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();

builder.Services.ConfigureServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
