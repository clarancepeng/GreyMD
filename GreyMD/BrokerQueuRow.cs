using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GreyMD
{
    public class BrokerQueuRow : INotifyPropertyChanged
    {
        private string bidBQ1;
        private string bidBQ2;
        private string bidBQ3;
        private string bidBQ4;

        private string offerBQ1;
        private string offerBQ2;
        private string offerBQ3;
        private string offerBQ4;

        public string BidBQ1
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

        public string BidBQ2
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

        public string BidBQ3
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

        public string BidBQ4
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


        public string OfferBQ1
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

        public string OfferBQ2
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

        public string OfferBQ3
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

        public string OfferBQ4
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
}
