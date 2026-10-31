# ⚡ WinCare Pro — Concurrency & WinUI 3 Threading Guide

## Inviolable Rules
1. **Never use Sync-over-Async:** Avoid `.Result`, `.GetAwaiter().GetResult()`, `.Wait()`. They cause immediate threadpool starvation and WinUI Dispatcher deadlocks.
2. **Never declare `async void`:** Except strictly in WinUI XAML event handlers (`RoutedEventHandler`). Background tasks and commands must return `async Task`.
3. **Always Marshal to UI Thread:** UI collections (`ObservableCollection<T>`) and bound properties must be updated on the WinUI 3 `DispatcherQueue` via `ViewModelBase.RunOnUI(() => { ... })`.
4. **Propagate CancellationToken:** Heavy async routines must accept `CancellationToken ct = default` and invoke `ct.ThrowIfCancellationRequested()`.
5. **Hygiene in `finally`:** Always clear `IsBusy = false; OperationState = OperationState.Idle;` inside `finally` blocks.

