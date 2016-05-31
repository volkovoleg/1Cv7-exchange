using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ExchangeWith1C.Models;

namespace ExchangeWith1C.Xml
{
    public class XmlParser
    {
        /// <summary>
        /// Достает информацию из xml по всем остаткам и резервам, а так же информацию по клиентским долгам кассе\банку
        /// </summary>
        public static OstatkiParsed ParseOstatki(String absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                return null;
            }
            else
            {
                var document = XDocument.Load(absolutePath);
                var rests = document.Root.Descendants("Rest").Select(x => new Rest
                {
                    ReservedCount = Convert.ToInt32(x.Attribute("reservedCount").Value),
                    FreeCount = Convert.ToInt32(x.Attribute("freeCount").Value),
                    Good1CCode = x.Attribute("goodCode1C").Value
                });
                var clients = document.Root.Descendants("Client").Select(x => new ClientDebt
                {
                    CashDebt = ConvertMouneyFrom1C(x.Attribute("CashDebt").Value),
                    BankDebt = ConvertMouneyFrom1C(x.Attribute("BankDebt").Value),
                    ClientId = Convert.ToInt32(x.Attribute("idIS").Value),
                    Client1CId = x.Attribute("id1C").Value
                });
                return new OstatkiParsed {Rests = rests, ClientDebts = clients};
            }
        }

        /// <summary>
        /// Парсит все ценовые колонки к товарам
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
        public static PriceParsed ParsePrice(String absolutePath)
        {
            var document = XDocument.Load(absolutePath);
            var rests = document.Root.Descendants("Rest").Select(x => new GoodPrice
            {
                PriceName = x.Attribute("prices").Value.Substring(0, x.Attribute("prices").Value.IndexOf("=")).Trim(),
                PriceCost =
                    ConvertMouneyFrom1C(
                        x.Attribute("prices").Value.Substring(x.Attribute("prices").Value.IndexOf("=") + 1).Trim()),
                Brand = x.Attribute("brand").Value.Trim(),
                GoodCode = x.Attribute("goodCodeIS").Value.Trim(),
                GoodName = x.Attribute("goodNameIC").Value.Trim(),
                GoodCode1C = x.Attribute("goodCode1C").Value.Trim()
            });
            return new PriceParsed { Prices = rests };
        }

        /// <summary>
        /// Ответ 1с на регистрацию клиента
        /// </summary>
        /// <param name="absolutePath"></param>
        public static ClientResponce ClientResponse(String absolutePath)
        {
            var document = XDocument.Load(absolutePath);
            XElement responce = document.Descendants("Client").First();
            var clientNewResp = new ClientResponce
            {
                id1C = responce.Attribute("id1C").Value,
                idIs = Convert.ToInt32(responce.Attribute("idIS").Value)
            };
            return clientNewResp;
        }

        /// <summary>
        /// Ответ 1с на проведение заказа
        /// </summary>
        /// <param name="absolutePath"></param>
        public static OrderNewResponce CreateOrderResponse(String absolutePath)
        {
            var document = XDocument.Load(absolutePath);
            XElement responce = document.Descendants("Order").First();
            var orderNewResp = new OrderNewResponce
            {
                id1C = responce.Attribute("id1C").Value,
                idIs = Convert.ToInt32(responce.Attribute("idIS").Value)
            };
            return orderNewResp;
        }

        /// <summary>
        /// Ответ 1С на сборку заказа
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
        public static OrderBuildResponce OrderBuildResponse(String absolutePath)
        {
            var document = XDocument.Load(absolutePath);
            XElement responce = document.Descendants("OrderResult").First();
            var orderBuildResp = new OrderBuildResponce()
            {
                id1C = responce.Attribute("id1C").Value,
                idIs = Convert.ToInt32(responce.Attribute("idIS").Value),
                result = responce.Attribute("result").Value
            };
            return orderBuildResp;
        }

        private static float ConvertMouneyFrom1C(String mouney1C)
        {
            return float.Parse(mouney1C);
        }
    }
}
