# WinCare Pro Architecture

This document details the system design, data flows, and module dependencies for WinCare Pro.

## Architectural Overview

WinCare Pro is built on top of WinUI 3 (Windows App SDK) using the MVVM (Model-View-ViewModel) pattern and Dependency Injection.

```mermaid
graph TD
    UI[WinUI Views: DashboardPage, NotificationPage, etc.] --> VM[ViewModels: DashboardViewModel, ProcessViewModel, etc.]
    VM --> Service[Services / DI Container: IJunkCleanerService, etc.]
    Service --> Engine[Hardware/Software Engines: SoftwareUpdaterEngine, DiskEngine, etc.]
    Engine --> DB[(SQLite Database: DbManager)]
    Engine --> OS[Windows API / WMI Performance Counters]
```

## Core Modules

### 1. Presentation Layer (Views & ViewModels)
- **Views**: Written in XAML with modern styles, using `x:Bind` for compiled data binding.
- **ViewModels**: Inherit from `ViewModelBase` and implement `INotifyPropertyChanged`. Handles formatting and background async tasks.

### 2. Service & Dependency Injection
Managed in `App.xaml.cs` via `Microsoft.Extensions.DependencyInjection`:
- All core scanning and action engines are registered as **Singletons**.
- ViewModels are registered as **Transients** for clean life cycles.

### 3. Database Layer (`DbManager`)
SQLite-backed database (`wincaredb.db`) containing tables:
- `Users`: Stores user profiles and app configurations.
- `Logs`: Historical audit trail of cleanups, fixes, and optimizations.
- `Notifications`: System alert items.
- `Reports`: File references to generated diagnostic reports.

---

## Technical Flow Diagrams

### Performance Telemetry Collection Flow
```mermaid
sequenceDiagram
    participant VM as DashboardViewModel
    participant WMI as Windows Management Instrumentation
    participant PerformanceCounter as System Perf Counters
    
    loop Every 1 Second
        VM->>WMI: Query GPU Load (Win32_PerfFormattedData_GPUPerformanceCounters)
        WMI-->>VM: GPU % Load
        VM->>PerformanceCounter: Query Disk Time / CPU Temp
        PerformanceCounter-->>VM: Disk & Temp values
        VM->>VM: Update Observable Properties
    end
```
