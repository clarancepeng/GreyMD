using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace GreyMD
{

    class PriceQty
    {
        public int Price { get; set; }
        public long Qty { get; set; }

        public int Orders { get; set; }
        public byte PriceLevel { get; set; }
    }
    class AggOrderBookCache
    {
        SortedDictionary<int, PriceQty> bidPriceQtys;
        SortedDictionary<int, PriceQty> askPriceQtys;

        public AggOrderBookCache()
        {
            bidPriceQtys = new SortedDictionary<int, PriceQty>();
            askPriceQtys = new SortedDictionary<int, PriceQty>();
        }

        public void UpdateBid(int securityCode, int orders, int price, long qty, byte priceLevel, byte updateAction, ObservableCollection<AggOrderBook> aggOrderBooks)
        {
            Console.WriteLine("SecurityCode={0}, orders={1}, price={2}, qty={3}, priceLevel={4}, updateAction={5}", securityCode, orders, price, qty, priceLevel, updateAction);
            bool updateOne = false;
            switch (updateAction)
            {
                case 0:
                    bidPriceQtys.Add(price, new PriceQty() { Price = price, PriceLevel = priceLevel, Qty = qty, Orders = orders });
                break;
                case 1:
                    updateOne = true;
                    if (bidPriceQtys.ContainsKey(price))
                    {
                        PriceQty pq = bidPriceQtys[price];
                        pq.Qty = qty;
                        pq.Orders = orders;
                        pq.PriceLevel = priceLevel;
                    }
                    else
                    {
                        bidPriceQtys.Add(price, new PriceQty() { Price = price, PriceLevel = priceLevel, Qty = qty, Orders = orders });
                    }
                    break;
                case 2:
                    bidPriceQtys.Remove(price);
                break;
                case 74:
                    bidPriceQtys.Clear();
                break;
            }
            if (bidPriceQtys.Count > 0)
            {
                int[] prx = SpreadTableUtils.getBidPxLevel10(securityCode, bidPriceQtys.Last().Key);
                if (prx != null)
                {
                    for (int n = 0; n < prx.Length; n++)
                    {
                        int px = prx[n];
                        aggOrderBooks[n].BidPx = px / 1000.0;
                        if (updateOne)
                        {
                            if (px == price)
                            {
                                aggOrderBooks[n].BidQty = FormatQty(qty);
                                aggOrderBooks[n].BidOrders = "(" + orders + ")";
                                break;
                            }
                        }
                        else
                        {
                            if (bidPriceQtys.ContainsKey(px))
                            {
                                PriceQty priceQty = bidPriceQtys[px];
                                aggOrderBooks[n].BidQty = FormatQty(priceQty.Qty);
                                aggOrderBooks[n].BidOrders = "(" + priceQty.Orders + ")";
                            }
                            else
                            {
                                aggOrderBooks[n].BidQty = "0";
                                aggOrderBooks[n].BidOrders = "(0)";
                            }
                        }
                    }
                }
            }
            else
            {
                for (int n = 0; n < 10; n++)
                {
                    aggOrderBooks[n].BidPx = 0;
                    aggOrderBooks[n].BidQty = FormatQty(0);
                    aggOrderBooks[n].BidOrders = "(" + 0 + ")";
                }
            }
        }

        string FormatQty(long orderQty)
        {
            if (orderQty < 1000)
            {
                return orderQty.ToString();
            }
            else if (orderQty < 1000000)
            {
                return (orderQty / 1000.0).ToString() + "K";
            }
            else
            {
                return (orderQty / 1000000.0).ToString() + "M";
            }
        }

        public void UpdateAsk(int securityCode, int orders, int price, long qty, byte priceLevel, byte updateAction, ObservableCollection<AggOrderBook> aggOrderBooks)
        {
            Console.WriteLine("SecurityCode={0}, orders={1}, price={2}, qty={3}, priceLevel={4}, updateAction={5}", securityCode, orders, price, qty, priceLevel, updateAction);
            bool updateOne = false;
            switch (updateAction)
            {
                case 0:
                    askPriceQtys.Add(price, new PriceQty() { Price = price, PriceLevel = priceLevel, Qty = qty, Orders = orders });
                    break;
                case 1:
                    updateOne = true;
                    if (askPriceQtys.ContainsKey(price))
                    {
                        PriceQty pq = askPriceQtys[price];
                        pq.Qty = qty;
                        pq.Orders = orders;
                        pq.PriceLevel = priceLevel;
                    }
                    else
                    {
                        askPriceQtys.Add(price, new PriceQty() { Price = price, PriceLevel = priceLevel, Qty = qty, Orders = orders });
                    }
                    break;
                case 2:
                    askPriceQtys.Remove(price);
                    break;
                case 74:
                    askPriceQtys.Clear();
                    break;
            }
            if(askPriceQtys.Count > 0)
            {
                int[] prx = SpreadTableUtils.getAskPxLevel10(securityCode, askPriceQtys.First().Key);
                if(prx != null)
                {
                    for(int n = 0; n < prx.Length; n++)
                    {
                        int px = prx[n];
                        aggOrderBooks[n].OfferPx = px / 1000.0;
                        if (updateOne)
                        {
                            if (px == price)
                            {
                                aggOrderBooks[n].OfferQty = FormatQty(qty);
                                aggOrderBooks[n].OfferOrders = "(" + orders + ")";
                                break;
                            }
                        }
                        else
                        {
                            if(askPriceQtys.ContainsKey(px))
                            {
                                PriceQty priceQty = askPriceQtys[px];
                                aggOrderBooks[n].OfferQty = FormatQty(priceQty.Qty);
                                aggOrderBooks[n].OfferOrders = "(" + priceQty.Orders + ")";
                            } 
                            else
                            {
                                aggOrderBooks[n].OfferQty = "0";
                                aggOrderBooks[n].OfferOrders = "(0)";
                            }
                        }
                    }
                }
            } 
            else
            {
                for(int n = 0; n < 10; n++)
                {
                    aggOrderBooks[n].OfferPx = 0;
                    aggOrderBooks[n].OfferQty = FormatQty(0);
                    aggOrderBooks[n].OfferOrders = "(" + 0 + ")";
                }
            }
        }

        public void Clean()
        {
            bidPriceQtys.Clear();
            askPriceQtys.Clear();
        }

    }
}
