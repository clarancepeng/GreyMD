using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GreyMD
{
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
}
