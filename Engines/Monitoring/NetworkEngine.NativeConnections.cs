using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public partial class NetworkEngine
{
    private const int AF_INET = 2;   // IPv4
    private const int AF_INET6 = 23; // IPv6

    private enum TCP_TABLE_CLASS
    {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL
    }

    private enum UDP_TABLE_CLASS
    {
        UDP_TABLE_BASIC,
        UDP_TABLE_OWNER_PID,
        UDP_TABLE_OWNER_MODULE
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int pdwSize,
        bool bOrder,
        int ulAf,
        TCP_TABLE_CLASS tableClass,
        uint reserved = 0);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedUdpTable(
        IntPtr pUdpTable,
        ref int pdwSize,
        bool bOrder,
        int ulAf,
        UDP_TABLE_CLASS tableClass,
        uint reserved = 0);

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID
    {
        public uint state;
        public uint localAddr;
        public byte localPort1;
        public byte localPort2;
        public byte localPort3;
        public byte localPort4;
        public uint remoteAddr;
        public byte remotePort1;
        public byte remotePort2;
        public byte remotePort3;
        public byte remotePort4;
        public uint owningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_UDPROW_OWNER_PID
    {
        public uint localAddr;
        public byte localPort1;
        public byte localPort2;
        public byte localPort3;
        public byte localPort4;
        public uint owningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCP6ROW_OWNER_PID
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ucLocalAddr;
        public uint dwLocalScopeId;
        public byte localPort1;
        public byte localPort2;
        public byte localPort3;
        public byte localPort4;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ucRemoteAddr;
        public uint dwRemoteScopeId;
        public byte remotePort1;
        public byte remotePort2;
        public byte remotePort3;
        public byte remotePort4;
        public uint dwState;
        public uint dwOwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_UDP6ROW_OWNER_PID
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ucLocalAddr;
        public uint dwLocalScopeId;
        public byte localPort1;
        public byte localPort2;
        public byte localPort3;
        public byte localPort4;
        public uint dwOwningPid;
    }

    private static string MapTcpState(uint state) => state switch
    {
        1 => "CLOSED",
        2 => "LISTENING",
        3 => "SYN_SENT",
        4 => "SYN_RCVD",
        5 => "ESTABLISHED",
        6 => "FIN_WAIT_1",
        7 => "FIN_WAIT_2",
        8 => "CLOSE_WAIT",
        9 => "CLOSING",
        10 => "LAST_ACK",
        11 => "TIME_WAIT",
        12 => "DELETE_TCB",
        _ => "UNKNOWN"
    };

    private static ushort GetPort(byte p1, byte p2) => (ushort)((p1 << 8) | p2);

    private static Dictionary<int, string> GetRunningProcessNames()
    {
        var procDict = new Dictionary<int, string>();
        try
        {
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    procDict[p.Id] = p.ProcessName;
                }
                catch { }
                finally
                {
                    p.Dispose();
                }
            }
        }
        catch { }
        return procDict;
    }

    public List<ActiveConnectionInfo> GetActiveConnectionsNative()
    {
        var list = new List<ActiveConnectionInfo>();
        var procDict = GetRunningProcessNames();

        // 1. TCP IPv4
        ReadTcp4Connections(list, procDict);

        // 2. TCP IPv6
        ReadTcp6Connections(list, procDict);

        // 3. UDP IPv4
        ReadUdp4Connections(list, procDict);

        // 4. UDP IPv6
        ReadUdp6Connections(list, procDict);

        return list;
    }

    private void ReadTcp4Connections(List<ActiveConnectionInfo> list, Dictionary<int, string> procDict)
    {
        int bufferSize = 0;
        uint ret = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
        if (bufferSize == 0) return;

        IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            ret = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
            if (ret != 0) return;

            int numEntries = Marshal.ReadInt32(tcpTablePtr);
            IntPtr rowPtr = IntPtr.Add(tcpTablePtr, 4);
            int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

            for (int i = 0; i < numEntries; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                int pid = (int)row.owningPid;
                procDict.TryGetValue(pid, out string? procName);
                procName ??= pid == 0 ? "System Idle Process" : (pid == 4 ? "System" : "Unknown");

                string localIp = new IPAddress(row.localAddr).ToString();
                int localPort = GetPort(row.localPort1, row.localPort2);
                string remoteIp = new IPAddress(row.remoteAddr).ToString();
                int remotePort = GetPort(row.remotePort1, row.remotePort2);

                list.Add(new ActiveConnectionInfo
                {
                    Protocol = "TCP",
                    LocalAddress = $"{localIp}:{localPort}",
                    ForeignAddress = $"{remoteIp}:{remotePort}",
                    State = MapTcpState(row.state),
                    ProcessName = procName,
                    Pid = pid
                });

                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }
        }
        catch (Exception ex)
        {
            Log($"ReadTcp4Connections native error: {ex.Message}");
        }
        finally
        {
            Marshal.FreeHGlobal(tcpTablePtr);
        }
    }

    private void ReadTcp6Connections(List<ActiveConnectionInfo> list, Dictionary<int, string> procDict)
    {
        int bufferSize = 0;
        uint ret = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
        if (bufferSize == 0) return;

        IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            ret = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, true, AF_INET6, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
            if (ret != 0) return;

            int numEntries = Marshal.ReadInt32(tcpTablePtr);
            IntPtr rowPtr = IntPtr.Add(tcpTablePtr, 4);
            int rowSize = Marshal.SizeOf<MIB_TCP6ROW_OWNER_PID>();

            for (int i = 0; i < numEntries; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCP6ROW_OWNER_PID>(rowPtr);
                int pid = (int)row.dwOwningPid;
                procDict.TryGetValue(pid, out string? procName);
                procName ??= pid == 0 ? "System Idle Process" : (pid == 4 ? "System" : "Unknown");

                string localIp = new IPAddress(row.ucLocalAddr, row.dwLocalScopeId).ToString();
                int localPort = GetPort(row.localPort1, row.localPort2);
                string remoteIp = new IPAddress(row.ucRemoteAddr, row.dwRemoteScopeId).ToString();
                int remotePort = GetPort(row.remotePort1, row.remotePort2);

                list.Add(new ActiveConnectionInfo
                {
                    Protocol = "TCP",
                    LocalAddress = $"[{localIp}]:{localPort}",
                    ForeignAddress = $"[{remoteIp}]:{remotePort}",
                    State = MapTcpState(row.dwState),
                    ProcessName = procName,
                    Pid = pid
                });

                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }
        }
        catch (Exception ex)
        {
            Log($"ReadTcp6Connections native error: {ex.Message}");
        }
        finally
        {
            Marshal.FreeHGlobal(tcpTablePtr);
        }
    }

    private void ReadUdp4Connections(List<ActiveConnectionInfo> list, Dictionary<int, string> procDict)
    {
        int bufferSize = 0;
        uint ret = GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, AF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID);
        if (bufferSize == 0) return;

        IntPtr udpTablePtr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            ret = GetExtendedUdpTable(udpTablePtr, ref bufferSize, true, AF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID);
            if (ret != 0) return;

            int numEntries = Marshal.ReadInt32(udpTablePtr);
            IntPtr rowPtr = IntPtr.Add(udpTablePtr, 4);
            int rowSize = Marshal.SizeOf<MIB_UDPROW_OWNER_PID>();

            for (int i = 0; i < numEntries; i++)
            {
                var row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(rowPtr);
                int pid = (int)row.owningPid;
                procDict.TryGetValue(pid, out string? procName);
                procName ??= pid == 0 ? "System Idle Process" : (pid == 4 ? "System" : "Unknown");

                string localIp = new IPAddress(row.localAddr).ToString();
                int localPort = GetPort(row.localPort1, row.localPort2);

                list.Add(new ActiveConnectionInfo
                {
                    Protocol = "UDP",
                    LocalAddress = $"{localIp}:{localPort}",
                    ForeignAddress = "*:*",
                    State = "-",
                    ProcessName = procName,
                    Pid = pid
                });

                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }
        }
        catch (Exception ex)
        {
            Log($"ReadUdp4Connections native error: {ex.Message}");
        }
        finally
        {
            Marshal.FreeHGlobal(udpTablePtr);
        }
    }

    private void ReadUdp6Connections(List<ActiveConnectionInfo> list, Dictionary<int, string> procDict)
    {
        int bufferSize = 0;
        uint ret = GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, AF_INET6, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID);
        if (bufferSize == 0) return;

        IntPtr udpTablePtr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            ret = GetExtendedUdpTable(udpTablePtr, ref bufferSize, true, AF_INET6, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID);
            if (ret != 0) return;

            int numEntries = Marshal.ReadInt32(udpTablePtr);
            IntPtr rowPtr = IntPtr.Add(udpTablePtr, 4);
            int rowSize = Marshal.SizeOf<MIB_UDP6ROW_OWNER_PID>();

            for (int i = 0; i < numEntries; i++)
            {
                var row = Marshal.PtrToStructure<MIB_UDP6ROW_OWNER_PID>(rowPtr);
                int pid = (int)row.dwOwningPid;
                procDict.TryGetValue(pid, out string? procName);
                procName ??= pid == 0 ? "System Idle Process" : (pid == 4 ? "System" : "Unknown");

                string localIp = new IPAddress(row.ucLocalAddr, row.dwLocalScopeId).ToString();
                int localPort = GetPort(row.localPort1, row.localPort2);

                list.Add(new ActiveConnectionInfo
                {
                    Protocol = "UDP",
                    LocalAddress = $"[{localIp}]:{localPort}",
                    ForeignAddress = "*:*",
                    State = "-",
                    ProcessName = procName,
                    Pid = pid
                });

                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }
        }
        catch (Exception ex)
        {
            Log($"ReadUdp6Connections native error: {ex.Message}");
        }
        finally
        {
            Marshal.FreeHGlobal(udpTablePtr);
        }
    }
}
