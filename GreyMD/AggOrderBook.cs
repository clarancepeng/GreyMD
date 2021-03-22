using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GreyMD
{
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
