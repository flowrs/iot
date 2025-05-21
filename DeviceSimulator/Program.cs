using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter.OpenTelemetryProtocol;
using DeviceSimulator.Models;
using DeviceSimulator.Services;
using DeviceSimulator;
using Azure.Monitor.OpenTelemetry.AspNetCore;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Add services
        services.AddSingleton<LoggingService>();
        services.AddSingleton<MetricsService>();
        
        // Register device client and security device
        services.AddSingleton(provider =>
        {
            var config = Configuration.Load();
            return DeviceClient.CreateFromConnectionString(config.IoTHubConnectionString);
        });
        
        services.AddSingleton(provider =>
        {
            var config = Configuration.Load();
            return new SecurityDevice
            {
                DeviceId = config.DeviceId,
                Status = DeviceStatus.Online,
                BatteryLevel = 100.0,
                SignalStrength = 4,
                Temperature = 22.0,
                Humidity = 45.0,
                MotionDetected = false,
                DoorOpen = false,
                LastMaintenance = DateTime.UtcNow,
                FirmwareVersion = "1.0.0"
            };
        });
        
        services.AddHostedService<DeviceService>();

        // Configure Azure Monitor OpenTelemetry Distro (Application Insights)
        services.AddOpenTelemetry()
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = hostContext.Configuration["ApplicationInsights:ConnectionString"];
            })
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddSource("DeviceSimulator");
            })
            .WithMetrics(metricsProviderBuilder =>
            {
                metricsProviderBuilder.AddMeter("DeviceSimulator.Metrics");
            });
    });

var host = builder.Build();
var config = Configuration.Load();
var deviceId = config.DeviceId;

var logger = host.Services.GetRequiredService<ILogger<DeviceService>>();
var loggingService = host.Services.GetRequiredService<LoggingService>();
var metricsService = host.Services.GetRequiredService<MetricsService>();
var deviceClient = host.Services.GetRequiredService<DeviceClient>();
var device = host.Services.GetRequiredService<SecurityDevice>();

await deviceClient.OpenAsync();

logger.LogInformation("Starting device simulator for {DeviceId}...", deviceId);
loggingService.LogDeviceStartup(deviceId);

try
{
    // Simulate some initial logging
    loggingService.SimulateErrors();
    loggingService.SimulateWarnings();
    loggingService.SimulateSecurityWarnings();
    loggingService.SimulatePerformanceWarnings();
    loggingService.LogDeviceMetrics(deviceId, device.BatteryLevel, device.SignalStrength, device.Temperature);

    // Start the host
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogError(ex, "Error running device simulator");
}
finally
{
    await deviceClient.CloseAsync();
}

public partial class Program { }
