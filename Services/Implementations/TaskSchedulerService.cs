using System;
using System.Threading;
using System.Threading.Tasks;

namespace WinCarePro.Services.Implementations;

public class TaskSchedulerService
{
    private static TaskSchedulerService? _instance;
    public static TaskSchedulerService Instance => _instance ??= new TaskSchedulerService();

    private readonly SemaphoreSlim _diskSemaphore = new(2, 2);      // Disk Scan: max 2 threads
    private readonly SemaphoreSlim _junkSemaphore = new(4, 4);      // Junk Scan: max 4 threads
    private readonly SemaphoreSlim _registrySemaphore = new(1, 1);  // Registry Scan: max 1 thread
    private readonly SemaphoreSlim _generalSemaphore = new(3, 3);   // General tasks: max 3 threads

    private TaskSchedulerService() { }

    public async Task<T> RunTaskAsync<T>(string category, Func<CancellationToken, Task<T>> taskFunc, CancellationToken token)
    {
        var semaphore = GetSemaphoreForCategory(category);
        await semaphore.WaitAsync(token);
        try
        {
            token.ThrowIfCancellationRequested();
            return await taskFunc(token);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task RunTaskAsync(string category, Func<CancellationToken, Task> taskFunc, CancellationToken token)
    {
        var semaphore = GetSemaphoreForCategory(category);
        await semaphore.WaitAsync(token);
        try
        {
            token.ThrowIfCancellationRequested();
            await taskFunc(token);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private SemaphoreSlim GetSemaphoreForCategory(string category)
    {
        return category.ToLower() switch
        {
            "disk" => _diskSemaphore,
            "junk" => _junkSemaphore,
            "registry" => _registrySemaphore,
            _ => _generalSemaphore
        };
    }
}
