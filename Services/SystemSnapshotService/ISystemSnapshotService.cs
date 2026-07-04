using System.Threading;
using System.Threading.Tasks;

namespace WinCarePro.Services.Contracts;

public interface ISystemSnapshotService
{
    Task<string> CreateSnapshotAsync(string description, CancellationToken token = default);
    Task<bool> RestoreSnapshotAsync(string snapshotId, CancellationToken token = default);
    Task<bool> DeleteSnapshotAsync(string snapshotId);
}
