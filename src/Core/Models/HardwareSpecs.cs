using System;

namespace WinCarePro.Models;

public class HardwareSpecs
{
    public string CpuModel { get; set; } = "Loading...";
    public int CpuCores { get; set; }
    public int CpuThreads { get; set; }
    public string CpuSpeed { get; set; } = "";
    
    public double RamCapacityGb { get; set; }
    public string RamSpeed { get; set; } = "";
    
    public string GpuModel { get; set; } = "Loading...";
    public string GpuVram { get; set; } = "";
    public string GpuDriverVersion { get; set; } = "";
    
    public string MotherboardManufacturer { get; set; } = "";
    public string MotherboardModel { get; set; } = "";
    public string BiosVersion { get; set; } = "";
    
    public string StorageInfo { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string SystemUptime { get; set; } = "";
}
