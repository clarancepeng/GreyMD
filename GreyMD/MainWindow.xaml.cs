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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;

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
        }
        public MainWindow()
        {
            InitializeComponent();
            clean();
            orderBookGrid.DataContext = aggOrderBooks;
            // tradeView.ItemsSource = tradeDatas;
            brokerQueueGrid.DataContext = brokerQueueDatas;
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
            // Create address objects
            int port = Int32.Parse(txtPort.Text);
            IPAddress multicastIPaddress = IPAddress.Parse(txtRemoteIP.Text);
            IPAddress localIPaddress = IPAddress.Any;

            // Create MulticastUdpClient
            udpClientWrapper = new MulticastUdpClient(multicastIPaddress, port, localIPaddress);
            udpClientWrapper.UdpMessageReceived += OnUdpMessageReceived;

            subSecurityId = Int16.Parse(txtSecurityCode.Text);
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
                    byte noEntries = e.Buffer[27];
                    AddToLog("Received message: AggregateOrderBook[securityCode=" + securityCode + ", noEntries=" + noEntries + "] on " + now.ToString("HH:mm:ss.fff"));
                    for(var i = 0; i < noEntries; i++)
                    {
                        int offset = 28 + 24 * i;
                        long qty = BitConverter.ToInt64(e.Buffer, offset);
                        int price = BitConverter.ToInt32(e.Buffer, offset + 8);
                        int orderCount = BitConverter.ToInt32(e.Buffer, offset + 12);
                        short side = BitConverter.ToInt16(e.Buffer, offset + 16);
                        byte priceLevel = e.Buffer[offset + 18];
                        byte updateAction = e.Buffer[offset + 19];
                        AggOrderBook book = aggOrderBooks[priceLevel - 1];
                        if (side == 1)
                        {
                            //AddToLog("买" + priceLevel + " " + (price / 1000.0) + " " + qty + "(" + orderCount + ")");
                            book.BidPx = price / 1000.0;
                            book.BidQty = formatQty(qty);
                            book.BidOrders = "(" + orderCount + ")";
                        } else
                        {
                            // AddToLog("卖" + priceLevel + " " + (price / 1000.0) + " " + qty + "(" + orderCount + ")");
                            book.OfferPx = price / 1000.0;
                            book.OfferQty = formatQty(qty);
                            book.OfferOrders = "(" + orderCount + ")";
                        }
                        
                        
                    }
                    break;
                case 50:
                    {
                        int tradeID = BitConverter.ToInt32(e.Buffer, 24);
                        double price = BitConverter.ToInt32(e.Buffer, 28) / 1000.0;
                        int quantity = BitConverter.ToInt32(e.Buffer, 32);
                        long tradeTime = BitConverter.ToInt64(e.Buffer, 40) / 1_000_000 + 28_800_000;
                        if (tradeTime > 0)
                        {
                            /*
                            TradeData tradeData = new TradeData();
                            tradeData.Price = price;
                            tradeData.Quantity = formatQty(quantity);
                            DateTimeOffset dtOffset = DateTimeOffset.FromUnixTimeMilliseconds(tradeTime);
                            tradeData.TradeTime = dtOffset.DateTime.ToString("HH:mm:ss.SSS");
                            tradeDatas.Add(tradeData);
                            */
                            /*
                            for(var i = 19; i > 1; i++)
                            {
                                TradeData t1 = tradeDatas[i-1];
                                TradeData t2 = tradeDatas[i];
                                t2.TradeTime = t1.TradeTime;
                                t2.Quantity = t1.Quantity;
                                t2.Price = t1.Price;
                            }
                            


                            TradeData t0 = new TradeData(); // tradeDatas[0];
                            t0.Price = price;
                            t0.Quantity = formatQty(quantity);
                            DateTimeOffset dtOffset = DateTimeOffset.FromUnixTimeMilliseconds(tradeTime);
                            t0.TradeTime = dtOffset.DateTime.ToString("HH:mm:ss.SSS");
                            tradeDatas.Add(t0);
                            AddToLog(t0.TradeTime + " " + (price / 1000.0) + " " + t0.Quantity + " " + tradeDatas.Count());
                            */
                            DateTimeOffset dtOffset = DateTimeOffset.FromUnixTimeMilliseconds(tradeTime);
                            string txtTrade = dtOffset.DateTime.ToString("HH:mm:ss.fff") + " " + price.ToString("0.000") + " " + formatQty(quantity);
                            AddTrade(txtTrade);
                            

                        }
                    }
                    
                    break;
                case 54:
                    byte itemCount = e.Buffer[24];
                    short side2 = BitConverter.ToInt16(e.Buffer, 25);
                    char bqMoreFlag = (char)e.Buffer[27];
                    for(var n = 0; n < itemCount; n++)
                    {
                        int offset = 28 + 4 * n;
                        short item = BitConverter.ToInt16(e.Buffer, offset);
                        char type = (char)e.Buffer[offset + 2];
                        int row = n / 4;
                        int col = n % 4;
                        BrokerQueuRow rowData = brokerQueueDatas[row];
                        if(side2 == 1)
                        {
                            switch(col)
                            {
                                case 0:
                                    rowData.BidBQ1 = item;
                                    break;
                                case 1:
                                    rowData.BidBQ2 = item;
                                    break;
                                case 2:
                                    rowData.BidBQ3 = item;
                                    break;
                                case 3:
                                    rowData.BidBQ4 = item;
                                    break;
                            }
                        } else
                        {
                            switch (col)
                            {
                                case 0:
                                    rowData.OfferBQ1 = item;
                                    break;
                                case 1:
                                    rowData.OfferBQ2 = item;
                                    break;
                                case 2:
                                    rowData.OfferBQ3 = item;
                                    break;
                                case 3:
                                    rowData.OfferBQ4 = item;
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
                                        rowData.BidBQ1 = 0;
                                        break;
                                    case 1:
                                        rowData.BidBQ2 = 0;
                                        break;
                                    case 2:
                                        rowData.BidBQ3 = 0;
                                        break;
                                    case 3:
                                        rowData.BidBQ4 = 0;
                                        break;
                                }
                            }
                            else
                            {
                                switch (col)
                                {
                                    case 0:
                                        rowData.OfferBQ1 = 0;
                                        break;
                                    case 1:
                                        rowData.OfferBQ2 = 0;
                                        break;
                                    case 2:
                                        rowData.OfferBQ3 = 0;
                                        break;
                                    case 3:
                                        rowData.OfferBQ4 = 0;
                                        break;
                                }
                            }
                        }

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


    public class BrokerQueuRow : INotifyPropertyChanged
    {
        private short bidBQ1;
        private short bidBQ2;
        private short bidBQ3;
        private short bidBQ4;

        private short offerBQ1;
        private short offerBQ2;
        private short offerBQ3;
        private short offerBQ4;

        public short BidBQ1
        {
            get { return bidBQ1; }
            set
            {
                if (bidBQ1 != value)
                {
                    bidBQ1 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("BidBQ1"));
                }
            }
        }

        public short BidBQ2
        {
            get { return bidBQ2; }
            set
            {
                if (bidBQ2 != value)
                {
                    bidBQ2 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("BidBQ2"));
                }
            }
        }

        public short BidBQ3
        {
            get { return bidBQ3; }
            set
            {
                if (bidBQ3 != value)
                {
                    bidBQ3 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("BidBQ3"));
                }
            }
        }

        public short BidBQ4
        {
            get { return bidBQ4; }
            set
            {
                if (bidBQ4 != value)
                {
                    bidBQ4 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("BidBQ4"));
                }
            }
        }


        public short OfferBQ1
        {
            get { return offerBQ1; }
            set
            {
                if (offerBQ1 != value)
                {
                    offerBQ1 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("OfferBQ1"));
                }
            }
        }

        public short OfferBQ2
        {
            get { return offerBQ2; }
            set
            {
                if (offerBQ2 != value)
                {
                    offerBQ2 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("OfferBQ2"));
                }
            }
        }

        public short OfferBQ3
        {
            get { return offerBQ3; }
            set
            {
                if (offerBQ3 != value)
                {
                    offerBQ3 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("OfferBQ3"));
                }
            }
        }

        public short OfferBQ4
        {
            get { return offerBQ4; }
            set
            {
                if (offerBQ4 != value)
                {
                    offerBQ4 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("OfferBQ4"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
    public class TradeData : INotifyPropertyChanged
    {
        private double price;
        private String quantity;
        private String tradeTime;

        public double Price
        {
            get { return price; }
            set
            {
                if (price != value)
                {
                    price = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Price"));
                }
            }
        }

        public String Quantity
        {
            get { return quantity; }
            set
            {
                if (quantity != value)
                {
                    quantity = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Quantity"));
                }
            }
        }

        public String TradeTime
        {
            get { return tradeTime; }
            set
            {
                if (tradeTime != value)
                {
                    tradeTime = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("TradeTime"));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }

    public class AggOrderBook : INotifyPropertyChanged
    {
        private double bidPx;
        private String bidQty;
        private String bidOrdes;
        private double offerPx;
        private String offerQty;
        private String offerOrdes;

        public AggOrderBook(String level)
        {
            this.Level = level;
        }
        public double BidPx
        {
            get { return bidPx; }
            set
            {
                if (bidPx != value)
                {
                    bidPx = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("BidPx"));
                }
            }
        }

        public String BidQty
        {
            get { return bidQty; }
            set
            {
                if (bidQty != value)
                {
                    bidQty = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("BidQty"));
                }
            }
        }


        public String BidOrders
        {
            get { return bidOrdes; }
            set
            {
                if (bidOrdes != value)
                {
                    bidOrdes = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("BidOrders"));
                }
            }
        }

        public String Level { get; set; }

        // public double OfferPx { get; set; }
        public double OfferPx
        {
            get { return offerPx; }
            set
            {
                if (offerPx != value)
                {
                    offerPx = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("OfferPx"));
                }
            }
        }

        public String OfferQty
        {
            get { return offerQty; }
            set
            {
                if (offerQty != value)
                {
                    offerQty = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("OfferQty"));
                }
            }
        }


        public String OfferOrders
        {
            get { return offerOrdes; }
            set
            {
                if (offerOrdes != value)
                {
                    offerOrdes = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("OfferOrders"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }


}
