using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GreyMD
{
    public class UdpClientManager
    {
        //接收数据事件
        public Action<string> recvMessageEvent = null;
        //发送结果事件
        public Action<int> sendResultEvent = null;
        //本地监听端口
        public int localPort = 0;
        //组播地址
        public string MultiCastHost = "";

        private UdpClient udpClient = null;

        public UdpClientManager(int localPort, string MultiCastHost)
        {
            if (localPort < 0 || localPort > 65535)
                throw new ArgumentOutOfRangeException("localPort is out of range");
            if (string.IsNullOrEmpty(MultiCastHost))
                throw new ArgumentNullException("message cant not null");

            this.localPort = localPort;
            this.MultiCastHost = MultiCastHost;
        }

        public void Start()
        {
            while (true)
            {
                try
                {
                    udpClient = new UdpClient(localPort, AddressFamily.InterNetwork);//指定本地监听port
                    udpClient.JoinMulticastGroup(IPAddress.Parse(MultiCastHost));
                    ReceiveMessage();
                    break;
                }
                catch (Exception)
                {
                    Console.WriteLine("aaaa");
                    Thread.Sleep(100);
                    
                }
            }
        }

        private async void ReceiveMessage()
        {
            while (true)
            {
                if (udpClient == null)
                    return;

                try
                {
                    UdpReceiveResult udpReceiveResult = await udpClient.ReceiveAsync();
                    Console.WriteLine("Got {}", udpReceiveResult.Buffer.Length);
                    string message = Encoding.UTF8.GetString(udpReceiveResult.Buffer);
                    if (recvMessageEvent != null)
                        recvMessageEvent(message);
                }
                catch (Exception ex)
                {
                }
            }
        }

        public async void SendMessageByMulticast(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("message cant not null");
            if (udpClient == null)
                throw new ArgumentNullException("udpClient cant not null");

            byte[] buffer = Encoding.UTF8.GetBytes(message);
            int len = 0;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    len = await udpClient.SendAsync(buffer, buffer.Length, new IPEndPoint(IPAddress.Parse(MultiCastHost), localPort));
                }
                catch (Exception)
                {
                    len = 0;
                }

                if (len <= 0)
                    Thread.Sleep(100);
                else
                    break;
            }

            if (sendResultEvent != null)
                sendResultEvent(len);
        }

        public void CloseUdpCliend()
        {
            if (udpClient == null)
                throw new ArgumentNullException("udpClient cant not null");

            try
            {
                udpClient.Client.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }
            udpClient.Close();
            udpClient = null;
        }
    }

}
