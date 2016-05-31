
using System;

namespace ExchangeWith1C.Models
{
    public class OrderNewItem
    {
        public String Goodcode1C { get; set; }
        /// <summary>
        /// Количество
        /// </summary>
        public String Count { get; set; }
        /// <summary>
        ///Прайс
        /// </summary>
        public String Price { get; set; }
    }
}
