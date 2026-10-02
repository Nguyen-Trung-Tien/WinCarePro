using System;
using System.IO;
using System.Runtime.InteropServices;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Core.Helpers;

/// <summary>
/// Provides safe file and directory deletion capabilities with Windows Recycle Bin support (Undoable),
/// verified strictly against SafePathGuard before any deletion attempt.
/// </summary>
public static class SafeFileRecycler
{
    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOERRORUI = 0x0400;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

    /// <summary>
    /// Safely deletes a file or directory. By default, moves it to the Windows Recycle Bin for safety.
    /// </summary>
    /// <param name="path">The full path of the file or directory to delete.</param>
    /// <param name="sendToRecycleBin">True to move to Recycle Bin (default), false to permanently delete.</param>
    /// <returns>OperationResult indicating success or failure with error details.</returns>
    public static OperationResult Delete(string path, bool sendToRecycleBin = true)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return OperationResult.Fail("Path cannot be null or empty.");
        }

        try
        {
            string fullPath = Path.GetFullPath(path);

            if (!SafePathGuard.IsSafeToDelete(fullPath))
            {
                CrashLogger.LogMessage("SafeFileRecycler", $"Path is protected by SafePathGuard: {fullPath}");
                return OperationResult.Fail($"Path is protected against deletion: {fullPath}");
            }

            bool isFile = File.Exists(fullPath);
            bool isDir = Directory.Exists(fullPath);

            if (!isFile && !isDir)
            {
                return OperationResult.Ok(); // Already gone
            }

            if (sendToRecycleBin)
            {
                // SHFileOperation requires double null-terminated string
                var fileOp = new SHFILEOPSTRUCT
                {
                    wFunc = FO_DELETE,
                    pFrom = fullPath + '\0' + '\0',
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI
                };

                int result = SHFileOperation(ref fileOp);
                if (result == 0 && !fileOp.fAnyOperationsAborted)
                {
                    return OperationResult.Ok();
                }

                // If SHFileOperation failed or was not supported on volume, fall back to permanent deletion if safe
                CrashLogger.LogMessage("SafeFileRecycler", $"SHFileOperation returned {result} for {fullPath}. Falling back to standard deletion.");
            }

            if (isFile)
            {
                File.Delete(fullPath);
            }
            else if (isDir)
            {
                Directory.Delete(fullPath, recursive: true);
            }

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException($"SafeFileRecycler.Delete({path})", ex);
            return OperationResult.Fail(ex.Message, ex);
        }
    }
}
