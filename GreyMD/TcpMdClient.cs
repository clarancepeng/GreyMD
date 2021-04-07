using System;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace GreyMD
{
    class TcpMdClient
    {
        private TcpClient tcpClient;
        private readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private System.Timers.Timer aTimer;
        private long lastReceived = 0;
        private bool isConnect = false;
        private bool isHBTimerStart;
        public void Initialize(string ip, int port)
        {
            try
            {
                tcpClient = new TcpClient(ip, port);
                if (tcpClient.Connected) {
                    isConnect = true;
                    _log.Info("Connected to: {0}:{1}", ip, port);
                    if (!isHBTimerStart)
                    {
                        isHBTimerStart = true;
                        StartHeartBeatTimer();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Initialize(ip, port);
            }
        }

        private void StartHeartBeatTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(60_000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnHeartBeatTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private void copy(byte[] source, byte[] target, int pos, int len)
        {
            for (int i = 0; i < len; i++)
            {
                target[pos + i] = source[i];
            }
        }
        private void generateAndSendHB()
        {
            byte[] heartBeatBytes = new byte[16];
            copy(BitConverter.GetBytes((ushort)16), heartBeatBytes, 0, 2);
            // heartBeatBytes[2] = 0;
            // heartBeatBytes[3] = 0;
            // copy(BitConverter.GetBytes(0), heartBeatBytes, 4, 4);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();
            copy(BitConverter.GetBytes(unixTimeMilliseconds), heartBeatBytes, 8, 8);
            Send(heartBeatBytes);
        }
        private void OnHeartBeatTimedEvent(Object source, ElapsedEventArgs e)
        {
            if(!isConnect)
            {
                return;
            }
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long nowTime = now.ToUnixTimeMilliseconds();
            if (nowTime - lastReceived >= 60_000)
            {
                lastReceived = nowTime;
                generateAndSendHB();
                _log.Info("The HeartBeat Sent at {0:HH:mm:ss.fff}", e.SignalTime);
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
                DateTimeOffset now = DateTimeOffset.UtcNow;
                lastReceived = now.ToUnixTimeMilliseconds();
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
