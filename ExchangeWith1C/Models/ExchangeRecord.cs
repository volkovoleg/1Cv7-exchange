using System;
using ExchangeWith1C.Data.Enum;

namespace ExchangeWith1C.Models
{
    public class ExchangeRecord
    {
        public int Id { get; set; }
        public SourceType SourceType { get; set; }
        public ExchangeState ExchangeState { get; set; }
        public DateTime DateTime { get; set; }
        public int ItemId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
