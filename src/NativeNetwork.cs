using System.Runtime.InteropServices;

namespace AirMirror;

internal static class NativeNetwork
{
    private const uint ErrorInsufficientBuffer = 122;
    private const uint TcpStateEstablished = 5;
    private const int AddressFamilyInterNetwork = 2;
    private const int AddressFamilyInterNetworkV6 = 23;
    private const int TcpTableOwnerPidAll = 5;

    internal static bool? HasRemoteTcpClient(int processId)
    {
        var ipv4Result = HasRemoteIpv4TcpClient(processId);
        var ipv6Result = HasRemoteIpv6TcpClient(processId);

        if (ipv4Result is null && ipv6Result is null)
        {
            return null;
        }

        return ipv4Result == true || ipv6Result == true;
    }

    private static bool? HasRemoteIpv4TcpClient(int processId)
    {
        var bufferSize = 0;
        var result = GetExtendedTcpTable(
            IntPtr.Zero,
            ref bufferSize,
            true,
            AddressFamilyInterNetwork,
            TcpTableOwnerPidAll,
            0);
        if (result != ErrorInsufficientBuffer || bufferSize <= 0)
        {
            return null;
        }

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetExtendedTcpTable(
                buffer,
                ref bufferSize,
                true,
                AddressFamilyInterNetwork,
                TcpTableOwnerPidAll,
                0);
            if (result != 0)
            {
                return null;
            }

            var rowCount = Marshal.ReadInt32(buffer);
            var rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();
            var rowAddress = IntPtr.Add(buffer, sizeof(int));
            for (var index = 0; index < rowCount; index++)
            {
                var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowAddress);
                if (row.OwningPid == processId &&
                    row.State == TcpStateEstablished &&
                    !IsIpv4LoopbackOrUnspecified(row.RemoteAddress))
                {
                    return true;
                }

                rowAddress = IntPtr.Add(rowAddress, rowSize);
            }

            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool? HasRemoteIpv6TcpClient(int processId)
    {
        var bufferSize = 0;
        var result = GetExtendedTcpTable(
            IntPtr.Zero,
            ref bufferSize,
            true,
            AddressFamilyInterNetworkV6,
            TcpTableOwnerPidAll,
            0);
        if (result != ErrorInsufficientBuffer || bufferSize <= 0)
        {
            return null;
        }

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetExtendedTcpTable(
                buffer,
                ref bufferSize,
                true,
                AddressFamilyInterNetworkV6,
                TcpTableOwnerPidAll,
                0);
            if (result != 0)
            {
                return null;
            }

            var rowCount = Marshal.ReadInt32(buffer);
            var rowSize = Marshal.SizeOf<MibTcp6RowOwnerPid>();
            var rowAddress = IntPtr.Add(buffer, sizeof(int));
            for (var index = 0; index < rowCount; index++)
            {
                var row = Marshal.PtrToStructure<MibTcp6RowOwnerPid>(rowAddress);
                if (row.OwningPid == processId &&
                    row.State == TcpStateEstablished &&
                    !IsIpv6LoopbackOrUnspecified(row.RemoteAddress))
                {
                    return true;
                }

                rowAddress = IntPtr.Add(rowAddress, rowSize);
            }

            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool IsIpv4LoopbackOrUnspecified(uint address)
    {
        return address == 0 ||
               (address & 0xFF) == 127 ||
               ((address >> 24) & 0xFF) == 127;
    }

    private static bool IsIpv6LoopbackOrUnspecified(byte[] address)
    {
        return address.All(value => value == 0) ||
               address.Take(15).All(value => value == 0) && address[15] == 1;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpRowOwnerPid
    {
        internal uint State;
        internal uint LocalAddress;
        internal uint LocalPort;
        internal uint RemoteAddress;
        internal uint RemotePort;
        internal int OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcp6RowOwnerPid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal byte[] LocalAddress;

        internal uint LocalScopeId;
        internal uint LocalPort;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal byte[] RemoteAddress;

        internal uint RemoteScopeId;
        internal uint RemotePort;
        internal uint State;
        internal int OwningPid;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr table,
        ref int tableLength,
        bool sort,
        int ipVersion,
        int tableClass,
        uint reserved);
}
