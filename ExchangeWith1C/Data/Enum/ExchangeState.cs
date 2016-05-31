using System;

namespace ExchangeWith1C.Data.Enum
{
    /// <summary>
    /// Статусы в очереди на обмен.
    /// </summary>
    [Flags]
    public enum ExchangeState
    {
        Ready=0,
        Error=1,
        Done=2
    }
}
