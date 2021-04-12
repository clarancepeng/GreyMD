using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GreyMD
{
    public class TradeData : INotifyPropertyChanged
    {
        private string price;
        private string quantity;
        private string tradeTime;

        public string Price
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

        public string Quantity
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

        public string TradeTime
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
}
