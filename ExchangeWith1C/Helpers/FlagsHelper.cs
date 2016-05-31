namespace ExchangeWith1C.Helpers
{
    public class FlagsHelper
    {
        /// <summary>
        ///     Проверяем есть ли такой флаг в списке флагов
        /// </summary>
        /// <typeparam name="T">Используемый Enum</typeparam>
        /// <param name="flags">Список флагов</param>
        /// <param name="flag">Флаг</param>
        public static bool IsSet<T>(T flags, T flag) where T : struct
        {
            var flagsValue = (int) (object) flags;
            var flagValue = (int) (object) flag;

            return (flagsValue & flagValue) != 0;
        }

        /// <summary>
        ///     Установить флаг
        /// </summary>
        /// <typeparam name="T">Используемый Enum</typeparam>
        /// <param name="flags">Список флагов</param>
        /// <param name="flag">Флаг</param>
        public static void Set<T>(ref T flags, T flag) where T : struct
        {
            var flagsValue = (int) (object) flags;
            var flagValue = (int) (object) flag;

            flags = (T) (object) (flagsValue | flagValue);
        }

        /// <summary>
        ///     Установить уникальный флаг
        /// </summary>
        /// <typeparam name="T">Используемый Enum</typeparam>
        /// <param name="flags">Список флагов</param>
        /// <param name="flag">Флаг</param>
        public static void Unset<T>(ref T flags, T flag) where T : struct
        {
            var flagsValue = (int) (object) flags;
            var flagValue = (int) (object) flag;

            flags = (T) (object) (flagsValue & (~flagValue));
        }
    }
}
