using Microsoft.Extensions.Configuration;

namespace DeviceSimulator;

public class Configuration
{
    public string DeviceId { get; set; } = string.Empty;
    public string IoTHubConnectionString { get; set; } = string.Empty;
    public string EventHubConnectionString { get; set; } = string.Empty;
    public string EventHubName { get; set; } = string.Empty;
    public int TelemetryIntervalSeconds { get; set; } = 5;

    public static Configuration Load()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        return new Configuration
        {
            DeviceId = configuration["DeviceId"] ?? "simulated-device-1",
            IoTHubConnectionString = configuration["IoTHub:ConnectionString"] ?? string.Empty,
            EventHubConnectionString = configuration["EventHub:ConnectionString"] ?? string.Empty,
            EventHubName = configuration["EventHub:Name"] ?? string.Empty,
            TelemetryIntervalSeconds = int.Parse(configuration["TelemetryIntervalSeconds"] ?? "5")
        };
    }
} 