using ExchangeWith1C.Data.Enum;
using ExchangeWith1C.Models;

namespace ExchangeWith1C.Utils
{
    public class ErrorUtils
    {
        public static ExchangeRecord markError(ExchangeRecord record, string errorMessage)
        {
            record.ErrorMessage = errorMessage;
            record.ExchangeState = ExchangeState.Error;
            //
            //Послать администратору сообщение
            return record;
        }
    }
}
