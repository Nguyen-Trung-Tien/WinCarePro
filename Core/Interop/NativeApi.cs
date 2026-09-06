using System;
using System.Runtime.InteropServices;

namespace WinCarePro.Core.Interop;

/// <summary>
/// Centralized Win32 P/Invoke declarations for system telemetry, memory, and process management.
/// All native structs and imports are consolidated here to eliminate duplication and ensure
/// a single source of truth for interop layout correctness.
/// </summary>
public static class NativeApi
{
    #region Memory Status

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        /// <summary>
        /// Creates a properly initialized MEMORYSTATUSEX with dwLength pre-set.
        /// </summary>
        public static MEMORYSTATUSEX Create()
        {
            var status = new MEMORYSTATUSEX();
            status.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
            return status;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    #endregion

    #region System Times (CPU Telemetry)

    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        /// <summary>
        /// Converts the FILETIME to a 64-bit unsigned integer for arithmetic.
        /// </summary>
        public readonly ulong ToUInt64() => ((ulong)dwHighDateTime << 32) | dwLowDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    #endregion

    #region Disk Space

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetDiskFreeSpaceEx(
        string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);

    #endregion

    #region Process Memory Management

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    /// <summary>Required for EmptyWorkingSet.</summary>
    public const uint PROCESS_SET_QUOTA = 0x0100;

    /// <summary>Required for querying process information.</summary>
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Samples current system memory usage.
    /// Returns (memoryLoadPercent, availablePhysicalBytes, totalPhysicalBytes).
    /// </summary>
    public static (uint loadPercent, ulong availableBytes, ulong totalBytes) GetMemoryStatus()
    {
        var status = MEMORYSTATUSEX.Create();
        if (GlobalMemoryStatusEx(ref status))
        {
            return (status.dwMemoryLoad, status.ullAvailPhys, status.ullTotalPhys);
        }
        return (0, 0, 0);
    }

    /// <summary>
    /// Calculates CPU usage percentage between two FILETIME snapshots.
    /// Returns -1 if no previous snapshot exists.
    /// </summary>
    public static double CalculateCpuPercent(
        FILETIME idleTime, FILETIME kernelTime, FILETIME userTime,
        ref FILETIME prevIdle, ref FILETIME prevKernel, ref FILETIME prevUser,
        ref bool hasPrevious)
    {
        if (!hasPrevious)
        {
            prevIdle = idleTime;
            prevKernel = kernelTime;
            prevUser = userTime;
            hasPrevious = true;
            return -1;
        }

        ulong idleDiff = idleTime.ToUInt64() - prevIdle.ToUInt64();
        ulong kernelDiff = kernelTime.ToUInt64() - prevKernel.ToUInt64();
        ulong userDiff = userTime.ToUInt64() - prevUser.ToUInt64();

        prevIdle = idleTime;
        prevKernel = kernelTime;
        prevUser = userTime;

        ulong totalDiff = kernelDiff + userDiff;
        if (totalDiff == 0 || totalDiff < idleDiff) return 0;

        return Math.Clamp((double)(totalDiff - idleDiff) * 100.0 / totalDiff, 0, 100);
    }

    #endregion
}
