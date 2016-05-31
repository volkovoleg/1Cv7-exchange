using System;
using System.Collections.Generic;

namespace ExchangeWith1C.Models
{
    public class OrderNew
    {
        public String Idis { get; set; }
        public String Idclient1C { get; set; }
        public String Idclientis { get; set; }
        public String Creationdate { get; set; }
        public String Address { get; set; }
        public String Firm { get; set; }
        public String Pricecolumn { get; set; }

        public List<OrderNewItem> Orderitems;
    }
}
