﻿using System.Net.Sockets;
using System.Net;
using System.Xml.Linq;
using com.Lavaver.WorldBackup.Core;
using com.Lavaver.WorldBackup.Global;

namespace com.Lavaver.WorldBackup
{
    internal class NTPC
    {
        static string? ntpServer;

        public static DateTime Run()
        {
                var configXml = XDocument.Load(GlobalString.SoftwareConfigLocation());
                var serveraddress = configXml.Root.Element("NTP-Server")?.Value;

                if (!string.IsNullOrEmpty(serveraddress))
                {
                    ntpServer = serveraddress;
                    LogConsole.Log("NTP-C Info", $"已读取 NTP 时间来源为 {serveraddress}", ConsoleColor.Blue);
                }
                else
                {
                    ntpServer = "time.windows.com";
                    LogConsole.Log("NTP-C Warning", "元素为空或异常，已将 NTP 时间来源重定向至 time.windows.com", ConsoleColor.Yellow);
                }

                // NTP协议数据包长度为48字节
                byte[] ntpData = new byte[48];

                // 设置协议头，其中第一个字节为协议版本号
                ntpData[0] = 0x1B;

                // NTP服务器IPEndPoint，端口号为123
                IPEndPoint ep = new IPEndPoint(Dns.GetHostAddresses(ntpServer)[0], 123);

                // 创建UDP客户端
                using (var udpClient = new UdpClient())
                {
                    // 发送NTP协议数据包到指定服务器
                    udpClient.Connect(ep);
                    udpClient.Send(ntpData, ntpData.Length);

                    // 接收返回的数据包
                    byte[] receivedData = udpClient.Receive(ref ep);

                    // 根据NTP协议解析时间
                    const byte offsetTransmitTime = 40;
                    ulong intpart = 0;
                    ulong fractpart = 0;

                    for (int i = 0; i <= 3; i++)
                        intpart = 256 * intpart + receivedData[offsetTransmitTime + i];

                    for (int i = 4; i <= 7; i++)
                        fractpart = 256 * fractpart + receivedData[offsetTransmitTime + i];

                    ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);

                    // NTP时间从1900年1月1日开始
                    DateTime networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((long)milliseconds);

                    LogConsole.Log("NTP-C Info", "已成功地获取时间", ConsoleColor.Blue);

                    return networkDateTime.ToLocalTime();
                }

        }
    }
}
