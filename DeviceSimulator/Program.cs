using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using DeviceSimulator.Models;
using DeviceSimulator.Services;
using DeviceSimulator;

var config = Configuration.Load();
var deviceId = config.DeviceId;

using var deviceClient = DeviceClient.CreateFromConnectionString(config.IoTHubConnectionString);
await deviceClient.OpenAsync();

var device = new SecurityDevice
{
    DeviceId = deviceId,
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

var deviceService = new DeviceService(
    deviceClient,
    device,
    message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}")
);

Console.WriteLine($"Starting device simulator for {deviceId}...");
Console.WriteLine("Press Ctrl+C to exit.");

try
{
    await deviceService.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    deviceService.Stop();
    await deviceClient.CloseAsync();
}

public partial class Program { }
