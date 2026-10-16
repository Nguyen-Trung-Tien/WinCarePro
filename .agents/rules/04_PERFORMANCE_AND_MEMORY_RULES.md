# ⚡ 04. Performance & Memory Management Rules

> [⬅️ 03. Concurrency & Threading](03_CONCURRENCY_AND_THREADING_RULES.md) • [🏠 Rules Index](README.md) • **Rule 04** • [Next Rule: 05. C# 13 Coding Standards ➡️](05_CODING_STANDARDS_AND_CONVENTIONS.md)

---

## 🏎️ 1. Startup & Resource Consumption Budgets

All production builds of WinCare Pro MUST strictly conform to the following quantitative limits:

| Performance Metric | Threshold Budget | Measurement Criteria |
| :--- | :--- | :--- |
| **Cold Start** | $\le 350\text{ ms}$ | Process invocation to initial primary frame render |
| **Foreground RAM** | $\le 85\text{ MB}$ | Active Dashboard view with live telemetry streaming |
| **System Tray Background RAM** | $\le 15\text{ MB}$ | Post-invocation of `TrimProcessMemory()` |
| **Idle CPU Utilization** | $\le 0.3\%$ | Steady-state background telemetry monitoring |
| **Animation Refresh Rate** | $120\text{ FPS}$ (or display maximum) | Windows Composition Layer frame dispatching |

---

## 🧹 2. Memory Compaction Pipeline (`TrimProcessMemory`)

When minimized to the Windows System Tray, the application **MUST** execute the memory compaction sequence:

```csharp
public static void TrimProcessMemory()
{
    try
    {
        // 1. Force Gen2 GC collection and heap compaction
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);

        // 2. Release physical working set via Win32 API
        using var process = Process.GetCurrentProcess();
        NativeMethods.EmptyWorkingSet(process.Handle);
    }
    catch (Exception ex)
    {
        CrashLogger.LogWarning("TrimProcessMemory failed", ex);
    }
}
```

---

## 🚰 3. XAML Memory Leak Elimination

1. **Mandatory Event Unsubscription:**
   - Event subscriptions attached to long-lived objects (Singleton Services, `Window`, `DispatcherTimer`, `NetworkChange.NetworkAddressChanged`) **MUST** be detached (`-=`) during the View's `Unloaded` event or ViewModel `Dispose()` method.
2. **Utilize `WeakReferenceMessenger`:**
   - Inter-component and cross-ViewModel communication MUST use weak references via `CommunityToolkit.Mvvm` to prevent subscriber retention by GC roots.
3. **Deterministic Resource Disposal (`IDisposable`):**
   - All types implementing `IDisposable` (e.g. `SqliteConnection`, `FileStream`, `Process`, `BitmapImage`) MUST be wrapped in `using var` blocks or explicitly disposed.

---

## 🖼️ 4. I/O Caching & Shell Icon Optimization (`IconCacheService`)

- **Zero Redundant Disk I/O:** NEVER invoke `SHGetFileInfo` or `ExtractIcon` repeatedly for the same executable or file type.
- **In-Memory Cache:** Cache resolved icons using a `ConcurrentDictionary<string, ImageSource>` with LRU bounds to guarantee $O(1)$ retrieval for process and startup application lists.

---

## 📊 5. Telemetry Layout Jitter Prevention (`Tabular Numerals`)

To prevent text bounds vibration (Layout Jitter / Stutter) during real-time updates of CPU%, RAM%, and network throughput:
- **ALWAYS** declare `Typography.NumeralAlignment="Tabular"` on all telemetry TextBlock elements.

---

## 🎨 6. Composition 3D Visuals & Reduced Motion Governance

1. **Animation Teardown on Navigation:** SpriteVisual scanning sweeps (`Start3DScanEffect`) MUST be halted and disposed (`Stop3DScanEffect`) in the View's `OnNavigatedFrom` handler and `MainPage.CleanupActivePage()`. Running GPU composition loops in background views is STRICTLY FORBIDDEN.
2. **Reduced Motion Compliance:** Prior to initiating 3D sweeps or micro-interactions, verify `ReducedMotionHelper.AreAnimationsEnabled` (evaluating both Windows Accessibility settings and user preference `EnableAnimations`). When Reduced Motion is active, transitions MUST execute instantaneously or via static alpha crossfades.

---

<div align="center">
  <sub>[🏠 Rules Index](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>
