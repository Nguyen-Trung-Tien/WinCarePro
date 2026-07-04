using System.Collections.Generic;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Services.Contracts;

public interface ILockingAppService
{
    Task<List<LockingAppInfo>> GetLockingAppsAsync();
    Task CloseAppsAsync(IEnumerable<LockingAppInfo> apps, System.Func<string, Task<bool>> confirmForceClose);
}
