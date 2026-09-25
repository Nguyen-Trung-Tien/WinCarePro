using System;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Services.Contracts;

/// <summary>
/// Service interface for automated 1-click remediation and smart system fixing.
/// </summary>
public interface ISmartFixService
{
    Task ExecuteFixAsync(string actionKey, Action<SmartFixProgress>? progressCallback = null, CancellationToken cancellationToken = default);
}
