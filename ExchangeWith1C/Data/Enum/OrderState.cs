using System;

namespace ExchangeWith1C.Data.Enum
{
    [Flags]
    public enum OrderState
    {
        Saved=0,
        Reserved=1,
        Processing=3,
        OneCConfirmed=4,
        OrderBuild=5,
        Deleted=6
    }
}
