using System;
using System.Collections.Generic;

namespace ExchangeWith1C.Models
{
    public class OrderBild
    {
        public String Idis { get; set; }
        public String Id1C { get; set; }
        public String Creationdate { get; set; }
        public List<OrderNewItem> Orderitems;
    }
}
