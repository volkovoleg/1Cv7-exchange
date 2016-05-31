

using System;
using System.Globalization;
using System.Threading;
using ExchangeWith1C.Models;
using ExchangeWith1C.Utils;

namespace ExchangeWith1C
{
    internal class Program
    {
        public static String path = @"Z:\";
        public static String proceedPath = @"Z:\proceed\";
        public static String errorPath = @"Z:\errors\";
        //public static String path = @"C:\Work\1CTest\";
        //public static String proceedPath = @"C:\Work\1CTest\proceed\";
        //public static String errorPath = @"C:\Work\1CTest\errors\";
        public static String ostatkiName = "Ostatki.xml";
        public static String priceName = "Price.xml";

        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var exchange = new Exchange(path, proceedPath, errorPath);
            while (true)
            {
                Thread.Sleep(5000);
                ExchangeRecord exchangeRecord = exchange.GetExchangeRecord();
                if (exchangeRecord != null)
                {
                    try
                    {
                        exchange.ProceedOrderNew(exchangeRecord);
                        exchange.ProceedOrderBuild(exchangeRecord);
                        exchange.ProceedOrderDelete(exchangeRecord);
                        exchange.ProceedRestAndDebt(ostatkiName, exchangeRecord);
                        exchange.ProceedPrice(priceName, exchangeRecord);
                        exchange.ProceedClient(exchangeRecord);
                    }
                    catch (Exception exception)
                    {
                        exchange.SaveLog(DateTime.Now, "Exchange 1C", exception.Message);
                        ErrorUtils.markError(exchangeRecord, exception.Message);
                    }
                    exchange.ProcessingError(exchangeRecord);
                }
            }
        }
    }
}
