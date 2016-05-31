using System;
using System.Globalization;

namespace ExchangeWith1C.Utils
{
    public class TimeUtils
    {
        public static string CurrentDateTimeString()
        {
            var format = "yyyyMMddHHmmss";
            CultureInfo ci = CultureInfo.InvariantCulture;
            var time = DateTime.Now;
            return time.ToString(format, ci);
        }

        public static string convertDateTimeString(DateTime dateTime)
        {
            var format = "yyyyMMddHHmmss";
            CultureInfo ci = CultureInfo.InvariantCulture;
            var time = dateTime;
            return time.ToString(format, ci);
        }
        public static string convertDateString(DateTime dateTime)
        {
            var format = "dd.MM.yyyy";
            CultureInfo ci = CultureInfo.InvariantCulture;
            var time = dateTime;
            return time.ToString(format, ci);
        }
    }
}
