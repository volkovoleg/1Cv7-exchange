using System;

namespace ExchangeWith1C.Models
{
    /// <summary>
    /// Остатки и резервы
    /// </summary>
    public class Rest
    {
        public int ReservedCount { get; set; }
        public int FreeCount { get; set; }
        public String Good1CCode { get; set; }
    }
}
