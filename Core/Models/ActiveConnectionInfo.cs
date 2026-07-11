using System;
using WinCarePro.Services;

namespace WinCarePro.Models;

public class ActiveConnectionInfo
{
    public string Protocol { get; set; } = "";
    public string LocalAddress { get; set; } = "";
    public string ForeignAddress { get; set; } = "";
    public string State { get; set; } = "";
    public string DisplayState => State.T();
    public string ProcessName { get; set; } = "";
    public int Pid { get; set; }
}
