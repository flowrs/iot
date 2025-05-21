namespace DeviceSimulator.Models;

public class Device
{
    public string DeviceId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ReportingInterval { get; set; } = 5;
} 