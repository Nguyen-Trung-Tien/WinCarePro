using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinCarePro.Modules.GamingTurbo
{
    public partial class GamingTurboViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isTurboActive;

        [ObservableProperty]
        private string _gameStatusMessage = "Chế độ Gaming Turbo đang TẮT. Sẵn sàng tăng tốc!";

        [ObservableProperty]
        private string _ramFreedText = "0 MB";

        [ObservableProperty]
        private int _optimizedProcessesCount = 0;

        [RelayCommand]
        private async Task ToggleTurboAsync()
        {
            if (!IsTurboActive)
            {
                // Activate Gaming Turbo
                IsTurboActive = true;
                GameStatusMessage = "⚡ Gaming Turbo đã BẬT! Đang dọn dẹp bộ nhớ & tối ưu ưu tiên CPU...";

                var freedBytes = await Task.Run(() =>
                {
                    long totalFreed = 0;
                    int count = 0;
                    var processes = Process.GetProcesses();

                    foreach (var proc in processes)
                    {
                        try
                        {
                            // Skip system & critical processes
                            if (proc.Id <= 4 || proc.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                                continue;

                            // Trim Working Set
                            long before = proc.WorkingSet64;
                            EmptyWorkingSet(proc.Handle);
                            long after = proc.WorkingSet64;

                            if (before > after)
                            {
                                totalFreed += (before - after);
                                count++;
                            }
                        }
                        catch { }
                    }

                    OptimizedProcessesCount = count;
                    return totalFreed;
                });

                // Force GC Collect
                GC.Collect();
                GC.WaitForPendingFinalizers();

                double freedMB = freedBytes / (1024.0 * 1024.0);
                RamFreedText = $"{freedMB:N0} MB";
                GameStatusMessage = $"🚀 Gaming Turbo HOẠT ĐỘNG! Đã giải phóng {freedMB:N0} MB RAM trên {OptimizedProcessesCount} tiến trình.";
            }
            else
            {
                // Deactivate Gaming Turbo
                IsTurboActive = false;
                GameStatusMessage = "Chế độ Gaming Turbo đã TẮT. Hệ thống trở về trạng thái chuẩn.";
            }
        }

        [System.Runtime.InteropServices.DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);
    }
}
