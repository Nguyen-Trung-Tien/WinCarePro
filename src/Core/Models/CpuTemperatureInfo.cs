using System;

namespace WinCarePro.Models;

public class CpuTemperatureInfo
{
    public double TemperatureCelsius { get; set; }
    public string SensorName { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
