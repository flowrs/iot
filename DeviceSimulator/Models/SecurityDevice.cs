using System.Text.Json.Serialization;

namespace DeviceSimulator.Models;

public class SecurityDevice
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public DeviceStatus Status { get; set; } = DeviceStatus.Online;

    [JsonPropertyName("batteryLevel")]
    public double BatteryLevel { get; set; } = 100.0;

    [JsonPropertyName("signalStrength")]
    public int SignalStrength { get; set; } = 4; // 1-5 bars

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("humidity")]
    public double Humidity { get; set; }

    [JsonPropertyName("motionDetected")]
    public bool MotionDetected { get; set; }

    [JsonPropertyName("doorOpen")]
    public bool DoorOpen { get; set; }

    [JsonPropertyName("lastMaintenance")]
    public DateTime LastMaintenance { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("cellularData")]
    public CellularData CellularData { get; set; } = new();

    [JsonPropertyName("firmwareVersion")]
    public string FirmwareVersion { get; set; } = "1.0.0";

    [JsonPropertyName("motionSensitivity")]
    public int MotionSensitivity { get; set; } = 5; // 1-10

    [JsonPropertyName("alertThresholds")]
    public Dictionary<string, double> AlertThresholds { get; set; } = new()
    {
        { "temperatureHigh", 30.0 },
        { "temperatureLow", 10.0 },
        { "humidityHigh", 80.0 },
        { "humidityLow", 20.0 },
        { "batteryLow", 20.0 }
    };

    [JsonPropertyName("reportingInterval")]
    public int ReportingInterval { get; set; } = 5; // seconds

    public void ResetToDefaults()
    {
        Status = DeviceStatus.Online;
        BatteryLevel = 100.0;
        SignalStrength = 4;
        MotionSensitivity = 5;
        ReportingInterval = 5;
        AlertThresholds = new Dictionary<string, double>
        {
            { "temperatureHigh", 30.0 },
            { "temperatureLow", 10.0 },
            { "humidityHigh", 80.0 },
            { "humidityLow", 20.0 },
            { "batteryLow", 20.0 }
        };
        LastMaintenance = DateTime.UtcNow;
    }
}

public enum DeviceStatus
{
    Online,
    Offline,
    Maintenance,
    Alert
}

public class CellularData
{
    [JsonPropertyName("carrier")]
    public string Carrier { get; set; } = "T-Mobile";

    [JsonPropertyName("dataUsageMB")]
    public double DataUsageMB { get; set; }

    [JsonPropertyName("planLimitMB")]
    public double PlanLimitMB { get; set; } = 1024; // 1GB plan

    [JsonPropertyName("lastSync")]
    public DateTime LastSync { get; set; } = DateTime.UtcNow;
} 