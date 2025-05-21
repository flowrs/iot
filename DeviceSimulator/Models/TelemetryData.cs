namespace DeviceSimulator.Models;

public class TelemetryData
{
    public DateTime Timestamp { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
} 