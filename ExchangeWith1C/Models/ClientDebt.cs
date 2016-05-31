using System;

namespace ExchangeWith1C.Models
{
    /// <summary>
    /// Описание долгов клиента и его связка с 1С
    /// </summary>
    public class ClientDebt
    {
        public float CashDebt { get; set; }
        public float BankDebt { get; set; }
        public int ClientId { get; set; }
        public String Client1CId { get; set; }
    }
}
