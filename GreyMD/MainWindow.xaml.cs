using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Generic;
using System.Threading;

namespace GreyMD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<AggOrderBook> aggOrderBooks = new ObservableCollection<AggOrderBook>();
        ObservableCollection<TradeData> tradeDatas = new ObservableCollection<TradeData>();
        ObservableCollection<BrokerQueuRow> brokerQueueDatas = new ObservableCollection<BrokerQueuRow>();
        AggOrderBookCache bookCache = new AggOrderBookCache();
        private Queue<TcpMdClient.TcpMDReceivedEventArgs> tcpMdQueue = new Queue<TcpMdClient.TcpMDReceivedEventArgs>(5000);
        private Thread tcpHandle;
        private int subSecurityId;
        private readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private MulticastUdpClient udpClientWrapper;
        // private TcpMarketDataclient tcpMarketDataclient;
        private TcpMdClient tcpMdClient;
        private byte[] tempBuf;
        private byte[] bufData;
        private int bufPos;
        private int bufLen;
        private int mdUser;
        private string mdPassword;
        private bool isRunning = true;
        public class ProtocolClass
        {
            public int Value { get; set; }
            public string DisplayValue { get; set; }

            public override string ToString()
            {
                return DisplayValue;
            }
        }
        public ObservableCollection<ProtocolClass> ProtocolCollection
        {
            get
            {
                return new ObservableCollection<ProtocolClass>
            {
                new ProtocolClass{DisplayValue = "TCP", Value = 1},
                new ProtocolClass{DisplayValue = "UDP", Value = 2},
            };
            }
        }

        public void clean()
        {
            
            aggOrderBooks.Clear();
            aggOrderBooks.Add(new AggOrderBook(" 1"));
            aggOrderBooks.Add(new AggOrderBook(" 2"));
            aggOrderBooks.Add(new AggOrderBook(" 3"));
            aggOrderBooks.Add(new AggOrderBook(" 4"));
            aggOrderBooks.Add(new AggOrderBook(" 5"));
            aggOrderBooks.Add(new AggOrderBook(" 6"));
            aggOrderBooks.Add(new AggOrderBook(" 7"));
            aggOrderBooks.Add(new AggOrderBook(" 8"));
            aggOrderBooks.Add(new AggOrderBook(" 9"));
            aggOrderBooks.Add(new AggOrderBook("10"));

            tradeDatas.Clear();
            for(int i = 0; i < 100; i++)
            {
                tradeDatas.Add(new TradeData());
            }
            brokerQueueDatas.Clear();
            brokerQueueDatas.Add(new BrokerQueuRow());
            brokerQueueDatas.Add(new BrokerQueuRow());
            brokerQueueDatas.Add(new BrokerQueuRow());
            brokerQueueDatas.Add(new BrokerQueuRow());
            brokerQueueDatas.Add(new BrokerQueuRow());
            brokerQueueDatas.Add(new BrokerQueuRow());
            brokerQueueDatas.Add(new BrokerQueuRow());
            brokerQueueDatas.Add(new BrokerQueuRow());
            brokerQueueDatas.Add(new BrokerQueuRow());
            brokerQueueDatas.Add(new BrokerQueuRow());
            bookCache.Clean();
        }
        public MainWindow()
        {
            InitializeComponent();
            clean();
            orderBookGrid.DataContext = aggOrderBooks;
            // tradeView.ItemsSource = tradeDatas;
            brokerQueueGrid.DataContext = brokerQueueDatas;
            tradeDetailGrid.DataContext = tradeDatas;
            Process currentProcess = Process.GetCurrentProcess();
            int pid = currentProcess.Id;
            _log.Info($" ============= Main ================== pid: {pid}, process name: {currentProcess.ProcessName}");
            string subIP = ConfigurationManager.AppSettings["SubscribeIP"];
            string subPort = ConfigurationManager.AppSettings["SubscribePort"];
            string subSecurityCode = ConfigurationManager.AppSettings["SubscribeSecurityCode"];
            mdUser = Int32.Parse(ConfigurationManager.AppSettings["MdUser"]);
            mdPassword = ConfigurationManager.AppSettings["MdPassword"];
            _log.Info("Subscribe IP={}", subIP);
            AddToLog("Subscribe IP=" + subIP);
            _log.Info("Subscribe Port={}", subPort);
            AddToLog("Subscribe Port=" + subPort);
            _log.Info("Subscribe Securitycode={}", subSecurityCode);
            AddToLog("Subscribe SecurityCode=" + subSecurityCode);
            txtRemoteIP.Text = subIP;
            txtPort.Text = subPort;
            txtSecurityCode.Text = subSecurityCode;
            txProtocol.ItemsSource = ProtocolCollection;
            txProtocol.SelectedIndex = 0;
            tempBuf = new byte[2048];
            bufData = new byte[4096];
            bufPos = 0;
            bufLen = 0;
            
            /*
            tcpHandle = new Thread(() =>
            {
                while(isRunning)
                {
                    TcpMdClient.TcpMDReceivedEventArgs msgEvent;
                    try
                    {
                        tcpMdQueue.TryDequeue(out msgEvent);
                        if (msgEvent != null)
                        {
                            OnTcpMessage(msgEvent);
                        }
                    } catch(Exception e)
                    {
                        _log.Error("Error: {}", e.Message);
                    }
                }
            });
            tcpHandle.Start();
            */
        }

        private void copy(byte[] source, byte[] target, int pos, int len)
        {
            for(int i = 0; i < len; i++)
            {
                target[pos + i] = source[i];
            }
        }

        private void Move(byte[] source, byte[] target, int pos, int len)
        {
            for (int i = 0; i < len; i++)
            {
                target[i] = source[pos + i];
            }
        }
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (udpClientWrapper != null)
                {
                    udpClientWrapper.UdpMessageReceived -= OnUdpMessageReceived;
                    udpClientWrapper = null;
                }
                /*
                if(tcpMarketDataclient != null)
                {
                    tcpMarketDataclient.TcpMessageReceived -= OnTcpMessageReceived;
                    tcpMarketDataclient = null;
                }*/
                if(tcpMdClient != null)
                {
                    tcpMdClient.TcpMDReceived -= OnTcpMessageReceived;
                }
            }
            catch
            {

            }
            bookCache.Clean();

            for (int n = 0; n < 10; n++)
            {
                AggOrderBook book = aggOrderBooks[n];
                book.BidPx = 0;
                book.BidQty = "0";
                book.BidOrders = "(" + 0 + ")";
                book.OfferPx = 0;
                book.OfferQty = "0";
                book.OfferOrders = "(" + 0 + ")";
            }
            // Create address objects
            int port = Int32.Parse(txtPort.Text);
            IPAddress remoteIPaddress = IPAddress.Parse(txtRemoteIP.Text);
            IPAddress localIPaddress = IPAddress.Any;
            ProtocolClass protocol = (ProtocolClass)txProtocol.SelectedValue;
            _log.Info("Subscribe {0} on {1}://{2}@{3}", txtSecurityCode.Text, protocol.DisplayValue.ToLower(), txtRemoteIP.Text, txtPort.Text);
            // Create MulticastUdpClient
            if(protocol.Value == 1)
            {
                tcpMdClient = new TcpMdClient();
                tcpMdClient.Initialize(txtRemoteIP.Text, port);
                tcpMdClient.TcpMDReceived += OnTcpMessageReceived;
                tcpMdClient.Receive();
                byte[] logonBytes = new byte[92];
                copy(BitConverter.GetBytes((ushort)92), logonBytes, 0, 2);
                logonBytes[2] = 1;
                copy(BitConverter.GetBytes(0), logonBytes, 4, 4);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();
                copy(BitConverter.GetBytes(unixTimeMilliseconds), logonBytes, 8, 8);
                copy(BitConverter.GetBytes((ushort)76), logonBytes, 16, 2);
                copy(BitConverter.GetBytes((ushort)101), logonBytes, 18, 2);

                long userId = mdUser;
                copy(BitConverter.GetBytes(userId), logonBytes, 20, 8);
                string password = mdPassword;
                copy(Encoding.ASCII.GetBytes(password), logonBytes, 28, password.Length);
                tcpMdClient.Send(logonBytes);
                AddToLog("Logon to MarketData Server: " + userId + "");
            } else
            {
                udpClientWrapper = new MulticastUdpClient(remoteIPaddress, port, localIPaddress);
                udpClientWrapper.UdpMessageReceived += OnUdpMessageReceived;
            }

            subSecurityId = Int32.Parse(txtSecurityCode.Text);
            AddToLog("MarketData Client started");
            _log.Info("MarketData Client started");
            NLog.LogManager.Flush();
        }

        int i = 1;
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            // Generate some message bytes
            string msgString = String.Format("Message from {0} pid {1} #{2}",
                GetLocalIPAddress(),
                System.Diagnostics.Process.GetCurrentProcess().Id,
                i.ToString());
            i++;
            byte[] buffer = Encoding.Unicode.GetBytes(msgString);

            // Send
            udpClientWrapper.SendMulticast(buffer);
            AddToLog("Sent message: " + msgString);
        }

        string formatQty(long orderQty)
        {
            if(orderQty < 1000)
            {
                return orderQty.ToString();
            } else if(orderQty < 1000000)
            {
                return (orderQty / 1000.0).ToString() + "K";
            } else
            {
                return (orderQty / 1000000.0).ToString() + "M";
            }
        }

        string formatBroker(short broker)
        {
            if(broker == 0)
            {
                return "0";
            } else
            {
                return "" + broker;
            }
        }

        void OnTcpMessageReceived(object sender, TcpMdClient.TcpMDReceivedEventArgs e)
        {
            _log.Info("Get MarketData Message[len={0}]", e.Length);
            // tcpMdQueue.Enqueue(e);
            OnTcpMessage(e);
        }

        void OnTcpMessage(TcpMdClient.TcpMDReceivedEventArgs e)
        {
            try
            {
                if (e.Length == 0)
                {
                    return;
                }
                _log.Info("Got {} bytes", e.Length);
                
                int totalLen = 0;
                if (bufLen == 0)
                {
                    //_log.Info("11111, Lenght={0}, bufLen={1}", e.Length, e.Buffer.Length);
                    e.Buffer.CopyTo(bufData, 0);
                    totalLen = e.Length;
                   //_log.Info("2222 without data left last time. ");
                }
                else
                {
                    //_log.Info("33333 left bufLen={}", bufLen);
                    //byte[] workBuf = new byte[8192];
                    //copy(tempBuf, workBuf, 0, bufLen);
                    //copy(e.Buffer, workBuf, bufLen, e.Length);
                    //bufData = workBuf;
                    totalLen = bufLen + e.Length;
                    copy(tempBuf, bufData, 0, bufLen);
                    copy(e.Buffer, bufData, bufLen, e.Length);
                    // tempBuf.CopyTo(bufData, 0);
                    // e.Buffer.CopyTo(bufData, bufLen);
                    //_log.Info("44444 totalLen={}", totalLen);
                }
                bufPos = 0;
                while (true)
                {
                    //_log.Info("55555");
                    short pktSize = BitConverter.ToInt16(bufData, bufPos);
                    if(pktSize == 0)
                    {
                        bufLen = 0;
                        _log.Error("aaaaaaa");
                        break;
                    }
                    int leaves = totalLen - bufPos;
                    //_log.Info("66666 pktSize={}, leaves={}, bufPos={}", pktSize, leaves, bufPos);
                    if (pktSize > leaves)
                    {
                        //_log.Info("77777");
                        Move(bufData, tempBuf, bufPos, leaves);
                        bufLen = leaves;
                        bufPos = 0;
                        //_log.Info("88888");
                        break;
                    }
                    else
                    {
                        //_log.Info("99999 - pktSize={}, leaves={}, bufPos={}, totalLen={}", pktSize, leaves, bufPos, totalLen);
                        byte[] oneBuf = new byte[pktSize];
                        Move(bufData, oneBuf, bufPos, pktSize);
                        //_log.Info("====11111");
                        OnMarketData(oneBuf, pktSize);
                        //_log.Info("====2222");
                        bufPos += pktSize;
                        if(bufPos >= totalLen)
                        {
                            bufLen = 0;
                            if(bufPos > totalLen)
                            {
                                _log.Error("bbbbb");
                            }
                            break;
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                _log.Error("Err: {}", ex.Message);
            }

        }

        void OnMarketData(byte[] bufData, int msgLen)
        {
            short pktSize = BitConverter.ToInt16(bufData, 0);
            int seqNum = BitConverter.ToInt32(bufData, 4);
            long sendTime = BitConverter.ToInt64(bufData, 8);
            if(msgLen == 16)
            {
                _log.Info("Got Heart Beat");
                return;
            }
            short msgSize = BitConverter.ToInt16(bufData, 16);
            short msgType = BitConverter.ToInt16(bufData, 18);
            int securityCode = BitConverter.ToInt32(bufData, 20);

            _log.Info("*** Got pktSize = {}, seqNum={}, msgType={}, securityCode={}", pktSize, seqNum, msgType, securityCode);
            if (subSecurityId != securityCode)
            {
                return;
            }

            DateTime now = DateTime.Now;
            switch (msgType)
            {
                case 53:
                    byte updateType = bufData[24];
                    byte noEntries = bufData[27];
                    if (updateType == 1)
                    {
                        for (var i = 0; i < noEntries; i++)
                        {
                            int offset = 28 + 24 * i;
                            long qty = BitConverter.ToInt64(bufData, offset);
                            int price = BitConverter.ToInt32(bufData, offset + 8);
                            int orderCount = BitConverter.ToInt32(bufData, offset + 12);
                            short side = BitConverter.ToInt16(bufData, offset + 16);
                            byte priceLevel = bufData[offset + 18];
                            byte updateAction = bufData[offset + 19];
                            // donothing

                            if (updateAction == 74)
                            {
                                for (int n = 0; n < 10; n++)
                                {
                                    AggOrderBook book = aggOrderBooks[n];
                                    if (side == 0)
                                    {
                                        //AddToLog("买" + priceLevel + " " + (price / 1000.0) + " " + qty + "(" + orderCount + ")");
                                        //AddToLog("Buy " + n + " Clean");
                                        book.BidPx = 0;
                                        book.BidQty = formatQty(0);
                                        book.BidOrders = "(" + 0 + ")";
                                    }
                                    else
                                    {
                                        // AddToLog("卖" + priceLevel + " " + (price / 1000.0) + " " + qty + "(" + orderCount + ")");
                                        //AddToLog("Sell " + n + " Clean");
                                        book.OfferPx = 0;
                                        book.OfferQty = formatQty(0);
                                        book.OfferOrders = "(" + 0 + ")";
                                    }
                                }
                            }
                            else
                            {
                                if (side == 0)
                                {
                                    bookCache.UpdateBid(securityCode, orderCount, price, qty, priceLevel, updateAction, aggOrderBooks);
                                }
                                else
                                {
                                    bookCache.UpdateAsk(securityCode, orderCount, price, qty, priceLevel, updateAction, aggOrderBooks);
                                }
                                /*
                                AggOrderBook book = aggOrderBooks[i % 10];
                                if (side == 0)
                                {
                                    //AddToLog("买" + priceLevel + " " + (price / 1000.0) + " " + qty + "(" + orderCount + ")");
                                    book.BidPx = price / 1000.0;
                                    book.BidQty = formatQty(qty);
                                    book.BidOrders = "(" + orderCount + ")";
                                }
                                else
                                {
                                    // AddToLog("卖" + priceLevel + " " + (price / 1000.0) + " " + qty + "(" + orderCount + ")");
                                    book.OfferPx = price / 1000.0;
                                    book.OfferQty = formatQty(qty);
                                    book.OfferOrders = "(" + orderCount + ")";
                                }*/

                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            for (var i = 0; i < noEntries; i++)
                            {
                                int offset = 28 + 24 * i;
                                long qty = BitConverter.ToInt64(bufData, offset);
                                int price = BitConverter.ToInt32(bufData, offset + 8);
                                int orderCount = BitConverter.ToInt32(bufData, offset + 12);
                                short side = BitConverter.ToInt16(bufData, offset + 16);
                                byte priceLevel = bufData[offset + 18];
                                byte updateAction = bufData[offset + 19];
                                if (side == 0)
                                {
                                    bookCache.UpdateBid(securityCode, orderCount, price, qty, priceLevel, updateAction, aggOrderBooks);
                                }
                                else
                                {
                                    bookCache.UpdateAsk(securityCode, orderCount, price, qty, priceLevel, updateAction, aggOrderBooks);
                                }

                            }
                            // UpdateBid(int securityCode, int orders, int price, int qty, byte priceLevel, byte updateAction, ObservableCollection < AggOrderBook > aggOrderBooks)
                        }
                        catch
                        {
                            AddToLog("Except when handle Aggregate OrderBook");
                        }
                    }

                    // AddToLog("Received message: AggregateOrderBook[securityCode=" + securityCode + ", noEntries=" + noEntries + "] on " + now.ToString("HH:mm:ss.fff"));

                    break;
                case 50:
                    {
                        try
                        {
                            int tradeID = BitConverter.ToInt32(bufData, 24);
                            double price = BitConverter.ToInt32(bufData, 28) / 1000.0;
                            int quantity = BitConverter.ToInt32(bufData, 32);
                            long tradeTime = BitConverter.ToInt64(bufData, 40) / 1_000_000 + 28_800_000;
                            if (tradeTime > 0)
                            {

                                DateTimeOffset dtOffset = DateTimeOffset.FromUnixTimeMilliseconds(tradeTime);
                                // string txtTrade = dtOffset.DateTime.ToString("HH:mm:ss.fff") + " " + price.ToString("0.000") + " " + formatQty(quantity);
                                //AddTrade(txtTrade);
                                // tradeDatas.Add(new TradeData() { Price = price==0?"": price.ToString(), Quantity= formatQty(quantity), TradeTime= dtOffset.DateTime.ToString("HH:mm:ss.fff") }) ;
                                // tradeDatas.RemoveAt(99);
                                // tradeDatas.Insert(0, new TradeData() { Price = price == 0 ? "" : price.ToString(), Quantity = formatQty(quantity), TradeTime = dtOffset.DateTime.ToString("HH:mm:ss.fff") });
                                for(int n = 99; n > 0; n--)
                                {
                                    TradeData t2 = tradeDatas[n];
                                    TradeData t1 = tradeDatas[n-1];
                                    t2.Price = t1.Price;
                                    t2.Quantity = t1.Quantity;
                                    t2.TradeTime = t1.TradeTime;
                                }
                                TradeData t = tradeDatas[0];
                                t.Price = price == 0 ? "" : price.ToString();
                                t.Quantity = formatQty(quantity);
                                t.TradeTime = dtOffset.DateTime.ToString("HH:mm:ss.fff");
                            }
                        }
                        catch
                        {
                            AddToLog("Except when handle Trade");
                        }
                    }

                    break;
                case 54:
                    try
                    {
                        byte itemCount = bufData[24];
                        short side2 = BitConverter.ToInt16(bufData, 25);
                        char bqMoreFlag = (char)bufData[27];
                        for (var n = 0; n < itemCount; n++)
                        {
                            int offset = 28 + 4 * n;
                            short item = BitConverter.ToInt16(bufData, offset);
                            char type = (char)bufData[offset + 2];
                            int row = n / 4;
                            int col = n % 4;
                            BrokerQueuRow rowData = brokerQueueDatas[row];
                            if (side2 == 1)
                            {
                                switch (col)
                                {
                                    case 0:
                                        rowData.BidBQ1 = formatBroker(item);
                                        break;
                                    case 1:
                                        rowData.BidBQ2 = formatBroker(item);
                                        break;
                                    case 2:
                                        rowData.BidBQ3 = formatBroker(item);
                                        break;
                                    case 3:
                                        rowData.BidBQ4 = formatBroker(item);
                                        break;
                                }
                            }
                            else
                            {
                                switch (col)
                                {
                                    case 0:
                                        rowData.OfferBQ1 = formatBroker(item);
                                        break;
                                    case 1:
                                        rowData.OfferBQ2 = formatBroker(item);
                                        break;
                                    case 2:
                                        rowData.OfferBQ3 = formatBroker(item);
                                        break;
                                    case 3:
                                        rowData.OfferBQ4 = formatBroker(item);
                                        break;
                                }
                            }
                        }

                        if (itemCount < 40)
                        {

                            for (var n = itemCount; n < 40; n++)
                            {
                                int row = n / 4;
                                int col = n % 4;
                                BrokerQueuRow rowData = brokerQueueDatas[row];
                                if (side2 == 1)
                                {
                                    switch (col)
                                    {
                                        case 0:
                                            rowData.BidBQ1 = "";
                                            break;
                                        case 1:
                                            rowData.BidBQ2 = "";
                                            break;
                                        case 2:
                                            rowData.BidBQ3 = "";
                                            break;
                                        case 3:
                                            rowData.BidBQ4 = "";
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (col)
                                    {
                                        case 0:
                                            rowData.OfferBQ1 = "";
                                            break;
                                        case 1:
                                            rowData.OfferBQ2 = "";
                                            break;
                                        case 2:
                                            rowData.OfferBQ3 = "";
                                            break;
                                        case 3:
                                            rowData.OfferBQ4 = "";
                                            break;
                                    }
                                }
                            }

                        }
                    }
                    catch
                    {
                        AddToLog("Exception when handle Broker Queue.");
                        string hex = "";
                        for (int i = 0; i < msgLen; i++)
                        {
                            if (i % 16 == 0)
                            {
                                hex += "\n";
                            }
                            int d = bufData[i];
                            if (d < 16)
                            {
                                hex += "0";
                            }
                            hex += d.ToString("x") + " ";


                        }
                        AddToLog("Hex Data: \n" + hex);
                    }
                    break;
            }
        }
        /// <summary>
        /// UDP Message received event
        /// </summary>
        void OnUdpMessageReceived(object sender, MulticastUdpClient.UdpMessageReceivedEventArgs e)
        {
            OnMarketData(e.Buffer, e.Buffer.Length);
        }

        /// <summary>
        /// Write the information to log
        /// </summary>
        void AddToLog(string s)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                txtLog.Text = s + Environment.NewLine + txtLog.Text;
                /*
                txtLog.Text += Environment.NewLine;
                txtLog.Text += s;
                */
            }), null);
        }

        void AddTrade(string s)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                // txtTrade.Text = s + Environment.NewLine + txtTrade.Text;
                /*
                txtLog.Text += Environment.NewLine;
                txtLog.Text += s;
                */
            }), null);
        }

        // http://stackoverflow.com/questions/6803073/get-local-ip-address
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        private void orderBookGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            //var drv = e.Row.Item as DataRowView;
            // e.Row.
            if(e.Row.GetIndex() % 2 == 0)
            {
                e.Row.Background = new SolidColorBrush(Colors.LightGray);
            } else
            {
                e.Row.Background = new SolidColorBrush(Colors.Cyan);
            }
        }
    }
}
