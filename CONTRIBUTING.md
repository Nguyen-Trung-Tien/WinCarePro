# Contributing to WinCare Pro

Thank you for your interest in contributing to WinCare Pro! We welcome contributions that help optimize Windows systems, clean junk files, and improve general UI aesthetics.

## Coding Style Guidelines
- **Framework**: WinUI 3 / Windows App SDK (.NET 10).
- **Architecture**: MVVM pattern. ViewModels should not reference UI components directly.
- **XAML Styling**: Utilize the design tokens defined in [App.xaml](file:///d:/WinCare/App.xaml) (e.g., `ModernCardStyle`, `PrimaryAccentGradient`).
- **Dependencies**: Keep external references minimal. Prefer native APIs or WMI queries for performance metrics.

## Development Workflow
1. Fork the repository and create your feature branch: `git checkout -b feature/amazing-feature`.
2. Ensure the code compiles cleanly by running `dotnet build -c Release`.
3. Verify changes by adding tests in [WinCarePro.Tests](file:///d:/WinCare/WinCarePro.Tests).
4. Commit your changes with clear, descriptive commit messages.

## Issues and PRs
- File a detailed bug report specifying the OS version and hardware context.
- When opening a Pull Request, link it to the relevant issue and describe your verification steps.
