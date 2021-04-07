using System;
using System.Net.Sockets;
using System.Text;

namespace GreyMD
{
    class TcpMdClient
    {
        private TcpClient tcpClient;
        private readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public void Initialize(string ip, int port)
        {
            try
            {
                tcpClient = new TcpClient(ip, port);

                if (tcpClient.Connected)
                    _log.Info("Connected to: {0}:{1}", ip, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Initialize(ip, port);
            }
        }

        public void Receive()
        {
            var buffer = new byte[2048];
            var ns = tcpClient.GetStream();
            ns.BeginRead(buffer, 0, buffer.Length, EndRead, buffer);
        }

        public void EndRead(IAsyncResult result)
        {
            try
            {
                var buffer = (byte[])result.AsyncState;
                var ns = tcpClient.GetStream();
                var bytesAvailable = ns.EndRead(result);

                if (TcpMDReceived != null)
                {
                    TcpMDReceived(this, new TcpMDReceivedEventArgs() { Buffer = buffer, Length = bytesAvailable });
                }
                Receive();
            }
            catch(Exception ex)
            {
                _log.Error("{}", ex.Message);
            }
        }

        public void Send(byte[] data)
        {
            var ns = tcpClient.GetStream();
            ns.BeginWrite(data, 0, data.Length, EndSend, data);
        }

        public void EndSend(IAsyncResult result)
        {
            var bytes = (byte[])result.AsyncState;
            _log.Info("Sent  {0} bytes to server.", bytes.Length);
            Console.WriteLine("Sent: {0}", Encoding.ASCII.GetString(bytes));
        }

        /// <summary>
        /// Event handler which will be invoked when TCP message is received
        /// </summary>
        public event EventHandler<TcpMDReceivedEventArgs> TcpMDReceived;

        /// <summary>
        /// Arguments for TcpMessageReceived event handler
        /// </summary>
        public class TcpMDReceivedEventArgs : EventArgs
        {
            public byte[] Buffer { get; set; }
            public int Length { get; set; }
        }
    }
}
