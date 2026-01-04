using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace TjdHelperWinUI.Tools
{
    public static class NetworkPortHelper
    {
        #region TCP
        public static List<NetworkPortModel> GetAllTcpPorts()
        {
            var list = new List<NetworkPortModel>();

            IntPtr buffer = IntPtr.Zero;
            int size = 0;

            GetExtendedTcpTable(IntPtr.Zero, ref size, true, 2, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
            buffer = Marshal.AllocHGlobal(size);

            try
            {
                if (GetExtendedTcpTable(buffer, ref size, true, 2, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0) == 0)
                {
                    int count = Marshal.ReadInt32(buffer);
                    IntPtr rowPtr = IntPtr.Add(buffer, 4);

                    for (int i = 0; i < count; i++)
                    {
                        var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);

                        list.Add(new NetworkPortModel
                        {
                            LocalAddress = new IPAddress(row.localAddr).ToString(),
                            LocalPort = (int)((row.localPort >> 8) & 0xFF | (row.localPort << 8) & 0xFF00),
                            PID = row.owningPid,
                            RemoteAddress = new IPAddress(row.remoteAddr).ToString(),
                            RemotePort = (int)((row.remotePort >> 8) & 0xFF | (row.remotePort << 8) & 0xFF00),
                            ProcessName = GetProcessName(row.owningPid).ProcessName,
                            State = GetTcpState(row.state)
                        });

                        rowPtr = IntPtr.Add(rowPtr, Marshal.SizeOf<MIB_TCPROW_OWNER_PID>());
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return list;
        }
        #endregion

        #region UDP
        public static List<NetworkPortModel> GetAllUdpPorts()
        {
            var list = new List<NetworkPortModel>();

            IntPtr buffer = IntPtr.Zero;
            int size = 0;

            GetExtendedUdpTable(IntPtr.Zero, ref size, true, 2, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
            buffer = Marshal.AllocHGlobal(size);

            try
            {
                if (GetExtendedUdpTable(buffer, ref size, true, 2, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0) == 0)
                {
                    int count = Marshal.ReadInt32(buffer);
                    IntPtr rowPtr = IntPtr.Add(buffer, 4);

                    for (int i = 0; i < count; i++)
                    {
                        var row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(rowPtr);

                        list.Add(new NetworkPortModel
                        {
                            LocalAddress = new IPAddress(row.localAddr).ToString(),
                            LocalPort = (int)((row.localPort >> 8) & 0xFF | (row.localPort << 8) & 0xFF00),
                            PID = row.owningPid,
                            ProcessName = GetProcessName(row.owningPid).ProcessName
                        });

                        rowPtr = IntPtr.Add(rowPtr, Marshal.SizeOf<MIB_UDPROW_OWNER_PID>());
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return list;
        }
        #endregion

        #region Win32

        [DllImport("iphlpapi.dll")]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref int dwOutBufLen,
            bool sort,
            int ipVersion,
            TCP_TABLE_CLASS tblClass,
            uint reserved);

        [DllImport("iphlpapi.dll")]
        private static extern uint GetExtendedUdpTable(
            IntPtr pUdpTable,
            ref int dwOutBufLen,
            bool sort,
            int ipVersion,
            UDP_TABLE_CLASS tblClass,
            uint reserved);

        private enum TCP_TABLE_CLASS
        {
            TCP_TABLE_OWNER_PID_ALL = 5
        }

        private enum UDP_TABLE_CLASS
        {
            UDP_TABLE_OWNER_PID = 1
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddr;
            public uint localPort;
            public uint remoteAddr;
            public uint remotePort;
            public int owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPROW_OWNER_PID
        {
            public uint localAddr;
            public uint localPort;
            public int owningPid;
        }

        private static int ntohs(ushort net)
        {
            return (net << 8) | (net >> 8);
        }

        private static Process GetProcessName(int pid)
        {
            try
            {
                return Process.GetProcessById(pid);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        private static string GetTcpState(uint state)
        {
            return state switch
            {
                1 => "CLOSED",
                2 => "LISTENING",
                3 => "SYN_SENT",
                4 => "SYN_RECEIVED",
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
        }
    }

    #region 网络端口模型
    public class NetworkPortModel
    {
        public string LocalAddress { get; set; }

        public int LocalPort { get; set; }

        public string RemoteAddress { get; set; }

        public int RemotePort { get; set; }

        public string ProcessName { get; set; }

        public int PID { get; set; }

        public string State { get; set; }
    }

    #endregion
}
