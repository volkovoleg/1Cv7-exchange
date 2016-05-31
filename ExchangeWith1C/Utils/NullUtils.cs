using System;

namespace ExchangeWith1C.Helpers
{
    public class NullUtils
    {
        public static Boolean IsNullObject(Object o)
        {
            if (o == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static int validateInt(Object o)
        {
            if (o == null)
            {
                return 0;
            }
            else
            {
                return (int) o;
            }
        }

        public static float validateFloat(Object o)
        {
            if (o == null)
            {
                return 0;
            }
            else
            {
                return (float)o;
            }
        }
    }
}
