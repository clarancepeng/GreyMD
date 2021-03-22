using System;
using System.Collections.Generic;
using System.Text;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.IO;

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
        private int subSecurityId;
        private bool addListener = false;
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
            /*
            tradeDatas.Clear();
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            tradeDatas.Add(new TradeData());
            */
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
            //var config = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location))
            //.AddJsonFile("appsetting.json").Build();
        }

        MulticastUdpClient udpClientWrapper;

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClientWrapper.UdpMessageReceived -= OnUdpMessageReceived;
            }
            catch
            {

            }
            bookCache.Clean();
            // Create address objects
            int port = Int32.Parse(txtPort.Text);
            IPAddress multicastIPaddress = IPAddress.Parse(txtRemoteIP.Text);
            IPAddress localIPaddress = IPAddress.Any;

            // Create MulticastUdpClient
            udpClientWrapper = new MulticastUdpClient(multicastIPaddress, port, localIPaddress);
            udpClientWrapper.UdpMessageReceived += OnUdpMessageReceived;

            subSecurityId = Int32.Parse(txtSecurityCode.Text);
            AddToLog("MarketData Client started");
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

        /// <summary>
        /// UDP Message received event
        /// </summary>
        void OnUdpMessageReceived(object sender, MulticastUdpClient.UdpMessageReceivedEventArgs e)
        {
            //BitConverter.ToInt32(e.Buffer, 0);
            short pktSize = BitConverter.ToInt16(e.Buffer, 0);
            int seqNum = BitConverter.ToInt32(e.Buffer, 4);
            long sendTime = BitConverter.ToInt64(e.Buffer, 8);
            
            int msgLen = e.Buffer.Length;
            short msgSize = BitConverter.ToInt16(e.Buffer, 16);
            short msgType = BitConverter.ToInt16(e.Buffer, 18);
            int securityCode = BitConverter.ToInt32(e.Buffer, 20);

            if(subSecurityId != securityCode)
            {
                return;
            }

            DateTime now = DateTime.Now;
            switch (msgType)
            {
                case 53:
                    byte updateType = e.Buffer[24];
                    byte noEntries = e.Buffer[27];
                    if (updateType == 1)
                    {
                        for (var i = 0; i < noEntries; i++)
                        {
                            int offset = 28 + 24 * i;
                            long qty = BitConverter.ToInt64(e.Buffer, offset);
                            int price = BitConverter.ToInt32(e.Buffer, offset + 8);
                            int orderCount = BitConverter.ToInt32(e.Buffer, offset + 12);
                            short side = BitConverter.ToInt16(e.Buffer, offset + 16);
                            byte priceLevel = e.Buffer[offset + 18];
                            byte updateAction = e.Buffer[offset + 19];
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
                        try {
                            for (var i = 0; i < noEntries; i++)
                            {
                                int offset = 28 + 24 * i;
                                long qty = BitConverter.ToInt64(e.Buffer, offset);
                                int price = BitConverter.ToInt32(e.Buffer, offset + 8);
                                int orderCount = BitConverter.ToInt32(e.Buffer, offset + 12);
                                short side = BitConverter.ToInt16(e.Buffer, offset + 16);
                                byte priceLevel = e.Buffer[offset + 18];
                                byte updateAction = e.Buffer[offset + 19];
                                if (side == 0)
                                {
                                    bookCache.UpdateBid(securityCode, orderCount, price, qty, priceLevel, updateAction, aggOrderBooks);
                                } else
                                {
                                    bookCache.UpdateAsk(securityCode, orderCount, price, qty, priceLevel, updateAction, aggOrderBooks);
                                }

                            }
                            // UpdateBid(int securityCode, int orders, int price, int qty, byte priceLevel, byte updateAction, ObservableCollection < AggOrderBook > aggOrderBooks)
                        } catch
                        {
                            AddToLog("Except when handle Aggregate OrderBook");
                        }
                    }
                    
                    AddToLog("Received message: AggregateOrderBook[securityCode=" + securityCode + ", noEntries=" + noEntries + "] on " + now.ToString("HH:mm:ss.fff"));
                    
                    break;
                case 50:
                    {
                        try
                        {
                            int tradeID = BitConverter.ToInt32(e.Buffer, 24);
                            double price = BitConverter.ToInt32(e.Buffer, 28) / 1000.0;
                            int quantity = BitConverter.ToInt32(e.Buffer, 32);
                            long tradeTime = BitConverter.ToInt64(e.Buffer, 40) / 1_000_000 + 28_800_000;
                            if (tradeTime > 0)
                            {
                            
                                    DateTimeOffset dtOffset = DateTimeOffset.FromUnixTimeMilliseconds(tradeTime);
                                    string txtTrade = dtOffset.DateTime.ToString("HH:mm:ss.fff") + " " + price.ToString("0.000") + " " + formatQty(quantity);
                                    AddTrade(txtTrade);
                            

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
                        byte itemCount = e.Buffer[24];
                        short side2 = BitConverter.ToInt16(e.Buffer, 25);
                        char bqMoreFlag = (char)e.Buffer[27];
                        for (var n = 0; n < itemCount; n++)
                        {
                            int offset = 28 + 4 * n;
                            short item = BitConverter.ToInt16(e.Buffer, offset);
                            char type = (char)e.Buffer[offset + 2];
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
                    } catch
                    {
                        AddToLog("Exception when handle Broker Queue.");
                        string hex = "";
                        for (int i = 0; i < msgLen; i++)
                        {
                            if (i % 16 == 0)
                            {
                                hex += "\n";
                            }
                            int d = e.Buffer[i];
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
            /*
            string hex="";
            for (int i =0; i < msgLen; i++)
            {
                if (i % 16 == 0)
                {
                    hex += "\n";
                }
                int d = e.Buffer[i];
                if(d < 16)
                {
                    hex += "0";
                }
                hex += d.ToString("x") + " ";
                
                
            }
            */
            
            // string receivedText = ASCIIEncoding.Unicode.GetString(e.Buffer);
            //AddToLog("Received message: " + BitConverter.IsLittleEndian + " " + sod + " " + msgLen + "\n" + hex); //receivedText);
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
                txtTrade.Text = s + Environment.NewLine + txtTrade.Text;
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
