using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GreyMD
{
    /// <summary>
    /// Multicast UdpClient wrapper with send and receive capabilities.
    /// Usage: pass local and remote multicast IPs and port to constructor.
    /// Use Send method to send data,
    /// subscribe to Received event to get notified about received data.
    /// </summary>
    public class TcpMarketDataclient
    {
        Socket tcpClient;
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);
        private readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        // State object for receiving data from remote device.

        public class StateObject
        {
            // Client socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 2048;
            public int receiveSize = 0;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
        }
        public TcpMarketDataclient(IPAddress ipAddress, int port, IPAddress localIPaddress = null)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);
                // Create a TCP/IP socket.
                tcpClient = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
                // Connect to the remote endpoint.
                tcpClient.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), tcpClient);
                connectDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }


        public void Close()
        {
            tcpClient.Shutdown(SocketShutdown.Both);
            tcpClient.Close();
        }

        private void ConnectCallback(IAsyncResult ar)
        {

            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;
                // Complete the connection.
                client.EndConnect(ar);
                _log.Info("Socket connected to {0}", client.RemoteEndPoint.ToString());
                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }



        public void Receive()
        {
            try
            {
                StateObject state = new StateObject();
                // state.workSocket = tcpClient;
                // Begin receiving the data from the remote device.
                // tcpClient.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                tcpClient.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                _log.Error(e.ToString());
            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                // Read data from the remote device.
                int bytesRead = tcpClient.EndReceive(ar);
                if (bytesRead > 0)
                {
                    // Get the rest of the data.
                    if (TcpMessageReceived != null)
                    {
                        TcpMessageReceived(this, new TcpMessageReceivedEventArgs() { Buffer = state.buffer, Length = bytesRead });
                    }
                    
                }
                tcpClient.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
               _log.Error(e.ToString());
            }

        }



        public void Send(byte[] data)
        {
            // Begin sending the data to the remote device.
            tcpClient.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), tcpClient);
        }



        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;
                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                _log.Info("Sent {0} bytes to server.", bytesSent);
                // Signal that all bytes have been sent.
                sendDone.Set();

            }
            catch (Exception e)
            {
                _log.Error(e.ToString());
            }

        }

        /// <summary>
        /// Event handler which will be invoked when TCP message is received
        /// </summary>
        public event EventHandler<TcpMessageReceivedEventArgs> TcpMessageReceived;

        /// <summary>
        /// Arguments for TcpMessageReceived event handler
        /// </summary>
        public class TcpMessageReceivedEventArgs : EventArgs
        {
            public byte[] Buffer { get; set; }
            public int Length { get; set; }
        }
    }
}

