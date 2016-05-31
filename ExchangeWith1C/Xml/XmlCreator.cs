using System;
using System.Linq;
using System.Xml.Linq;
using ExchangeWith1C.Models;
using ExchangeWith1C.Utils;

namespace ExchangeWith1C.Xml
{
    internal class XmlCreator
    {
       /// <summary>
       /// Создает xml клиента
       /// </summary>
       /// <param name="directoryPath"></param>
       /// <param name="client"></param>
       /// <returns></returns>
        public static String CreateClientXml(String directoryPath, Client client)
        {
            var fileName = "fromISto1C_" + TimeUtils.CurrentDateTimeString() + ".xml";
            var document = new XDocument(
                new XDeclaration("1.0", "WINDOWS-1251", "yes"),
                new XElement("Document", new XAttribute("type", "All"),
                    new XElement("Clients",
                        new XElement("Client", new XAttribute("priceColumn", client.PriceColumn),
                            new XAttribute("managerFIO", client.ManagerFio),
                            new XAttribute("name", client.Name),
                            new XAttribute("idIS", client.ClientCode),
                            new XAttribute("id1C", ""))
                        )
                    )
                );
            document.Save(directoryPath + FileUtils.TEMP);
            FileUtils.Rename(directoryPath+FileUtils.TEMP,directoryPath+fileName);
           return fileName;
        }

        /// <summary>
        /// Создает xml нового заказа, до обработки в 1С
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="orderNew"></param>
        public static String CreateOrderXml(String directoryPath, OrderNew orderNew)
        {
            var fileName = "fromISto1C_" + TimeUtils.CurrentDateTimeString() + ".xml";
            var document = new XDocument(
                new XDeclaration("1.0", "WINDOWS-1251", "yes"),
                new XElement("Document", new XAttribute("type", "Orders"),
                    new XElement("Orders",
                        new XElement("Order",
                            new XAttribute("idIS", orderNew.Idis),
                            new XAttribute("idClient1c", orderNew.Idclient1C),
                            new XAttribute("idClientIS", orderNew.Idclientis),
                            new XAttribute("creationDate", orderNew.Creationdate),
                            new XAttribute("address", orderNew.Address),
                            new XAttribute("firm", orderNew.Firm),
                            new XAttribute("priceColumn", orderNew.Pricecolumn),
                            orderNew.Orderitems.Select(x => new XElement("OrderItem",
                                new XAttribute("goodCode1C", x.Goodcode1C),
                                new XAttribute("count", x.Count),
                                new XAttribute("price", x.Price)
                                ))
                            )
                        )
                    )
                );
            document.Save(directoryPath + fileName);
            return fileName;
        }

        /// <summary>
        /// Отправка подтвержденного в 1С заказа из сборки в отгрузку
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="orderNew"></param>
        public static String BildOrderXml(String directoryPath, OrderBild orderBild)
        {
            var fileName = "fromISto1C_" + TimeUtils.CurrentDateTimeString() + ".xml";
            var document = new XDocument(
                new XDeclaration("1.0", "WINDOWS-1251", "yes"),
                new XElement("Document", new XAttribute("type", "OrderBuild"),
                    new XElement("Orders",
                        new XElement("Order",
                            new XAttribute("idIS", orderBild.Idis),
                            new XAttribute("id1C", orderBild.Id1C),
                            new XAttribute("creationDate", orderBild.Creationdate),
                            orderBild.Orderitems.Select(x => new XElement("OrderItem",
                                new XAttribute("goodCode1C", x.Goodcode1C),
                                new XAttribute("count", x.Count),
                                new XAttribute("price", x.Price)
                                ))
                            )
                        )
                    )
                );
            document.Save(directoryPath + fileName);
            return fileName;
        }

        /// <summary>
        /// Удаляет заказ, с отметкой в 1С
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="orderNew"></param>
        public static String DeleteOrderXml(String directoryPath, OrderDelete orderDel)
        {
            var fileName = "fromISto1C_" + TimeUtils.CurrentDateTimeString() + ".xml";
            var document = new XDocument(
                new XDeclaration("1.0", "WINDOWS-1251", "yes"),
                new XElement("Document", new XAttribute("type", "Orders"),
                    new XElement("Orders",
                        new XElement("Order",
                            new XAttribute("idIS", orderDel.Idis),
                            new XAttribute("id1C",orderDel.Id1C),
                            new XAttribute("idClient1c", orderDel.Idclient1C),
                            new XAttribute("idClientIS", orderDel.Idclientis),
                            new XAttribute("creationDate", orderDel.Creationdate),
                            new XAttribute("address", orderDel.Address),
                            new XAttribute("firm", orderDel.Firm),
                            new XAttribute("priceColumn", orderDel.Pricecolumn))
                        )
                    )
                );
            document.Save(directoryPath + fileName);
            return fileName;
        }
    }
}
