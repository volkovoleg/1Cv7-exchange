namespace ExchangeWith1C.Data.Enum
{
    /// <summary>
    /// Тип заложенного в очередь объекта
    /// </summary>
    public enum SourceType
    {
        OrderCreate=0,
        OrderBuild=1,
        OrderDelete=2,
        Client=3,
        Rest=4,
        Price=5
    }
}
