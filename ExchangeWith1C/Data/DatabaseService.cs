using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using ExchangeWith1C.Data.Enum;
using ExchangeWith1C.Entity;
using ExchangeWith1C.Helpers;
using ExchangeWith1C.Models;
using ExchangeWith1C.Utils;

namespace ExchangeWith1C.Data
{
    public class DatabaseService
    {
        public readonly developer_database _context;
        private int CENTRAL_ID = 0;

        public DatabaseService()
        {
            _context = new developer_database();
        }

        /// <summary>
        ///     Возвращает запись из очереди, представленную к обработке
        /// </summary>
        /// <returns></returns>
        public ExchangeRecord GetExchangeRecord()
        {
            ExchangeRecord record = null;
            Exchange1C exchange1C =
                _context.Exchange1C.FirstOrDefault(x => x.ExchangeState == (int) ExchangeState.Error);

            if (exchange1C != null)
            {
                //Тут мы узнаем что обмен запущен с недоконченными в прошлый раз элементом очереди
                record = new ExchangeRecord
                {
                    Id = exchange1C.Id,
                    ExchangeState = (ExchangeState) exchange1C.ExchangeState,
                    SourceType = (SourceType) exchange1C.Source,
                    DateTime = exchange1C.Date,
                    ItemId = NullUtils.validateInt(exchange1C.ItemId)
                };
            }
            else
            {
                // Предоставление готового к обработке элемента очереди
                exchange1C = _context.Exchange1C.FirstOrDefault(x => x.ExchangeState == (int) ExchangeState.Ready);
                if (exchange1C != null)
                {
                    record = new ExchangeRecord
                    {
                        Id = exchange1C.Id,
                        ExchangeState = (ExchangeState) exchange1C.ExchangeState,
                        SourceType = (SourceType) exchange1C.Source,
                        DateTime = exchange1C.Date,
                        ItemId = NullUtils.validateInt(exchange1C.ItemId)
                    };
                }
            }
            return record;
        }

        public Client GetClient(ExchangeRecord record)
        {
            var clientPartnerNew = _context.Partners.FirstOrDefault(x => x.Id == record.ItemId);
            //КЛиент новый, посему у него всего 1 пользователь в начале
            var clientUserNew = _context.Users.FirstOrDefault(x => x.PartnerId == clientPartnerNew.Id);
            var suppliers = _context.Suppliers.FirstOrDefault(x => x.ClientUserId == clientUserNew.Id);
            var userSummplier = _context.Users.FirstOrDefault(x =>x.Id==suppliers.SupplierUserId);
            var price = _context.PriceColumns.FirstOrDefault(x => x.Id == clientPartnerNew.PriceColumnId);
            // не надо на пустоту проверить (во всех таких методах)???
            var clientToXml = new Client {Name = clientPartnerNew.CompanyName, ClientCode = clientPartnerNew.Id.ToString(), ManagerFio = userSummplier.Fio, PriceColumn =price.Name };
            return clientToXml;
        }

        public OrderBild GetOrderBild(ExchangeRecord record)
        {
            var order = _context.Orders.FirstOrDefault(x => x.Id == record.ItemId);
            var supplier = _context.Suppliers.FirstOrDefault(x => x.Id == order.SuppliersId);
            var clientUser = _context.Users.FirstOrDefault(x => x.Id == supplier.ClientUserId);
            var client = _context.Partners.FirstOrDefault(x => x.Id == clientUser.PartnerId);
            List<OrderItems> orderItemsList = _context.OrderItems.Where(x => x.OrderId == order.Id).ToList();
            var goods = _context.Goods.ToList();
            var orderBuild = new OrderBild();
            orderBuild.Idis = order.Id.ToString();
            orderBuild.Id1C = order.Code1C;
            orderBuild.Creationdate = TimeUtils.convertDateTimeString(order.CreationDate);
            orderBuild.Orderitems = new List<OrderNewItem>();
            foreach (var orderItem in orderItemsList)
            {
                var price = GetPersonalPrice(client.Id, orderItem.GoodId);
                if (price == 0)
                {
                    price = (float)orderItem.Price;
                }
                var code1c = goods.FirstOrDefault(x => x.Id == orderItem.GoodId).Code1C;
                orderBuild.Orderitems.Add(new OrderNewItem { Price = price.ToString("##.0"), Goodcode1C = code1c, Count = orderItem.Count.ToString() });
            }
            return orderBuild;
        }

        /// <summary>
        /// Региистрация в 1С всех принятых на сборку позиций с учетом изменений
        /// </summary>
        public void SaveOrderBuildResponce(OrderBuildResponce orderBuildResponce)
        {
            var order = _context.Orders.FirstOrDefault(x => x.Id == orderBuildResponce.idIs);
            if (order != null && orderBuildResponce.result.Equals("SUCCESS"))
            {
                order.Status = (int)OrderState.OrderBuild;
            }
            Update(order);
        }

        /// <summary>
        /// Получение всей информации по новому заказу для последующей сериализации в xml
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public OrderNew GetNewOrder(ExchangeRecord record)
        {
            var order = _context.Orders.FirstOrDefault(x => x.Id == record.ItemId);
            var supplier = _context.Suppliers.FirstOrDefault(x => x.Id == order.SuppliersId);
            var clientUser = _context.Users.FirstOrDefault(x => x.Id == supplier.ClientUserId);
            var client = _context.Partners.FirstOrDefault(x => x.Id == clientUser.PartnerId);
            var priceColumn = _context.PriceColumns.FirstOrDefault(x => x.Id == client.PriceColumnId);
            var delivery = _context.Deliveries.FirstOrDefault(x => x.OrderId == order.Id);
            List<OrderItems> orderItemsList = _context.OrderItems.Where(x => x.OrderId == order.Id).ToList();
            var goods = _context.Goods.ToList();
            String oneCString = GetOneCString(order, delivery, orderItemsList, goods);
            var ordernew = new OrderNew();
            ordernew.Idis = order.Id.ToString();
            ordernew.Idclientis = client.Id.ToString();
            ordernew.Idclient1C = client.Num1C;
            ordernew.Pricecolumn = priceColumn.Name;
            ordernew.Firm = order.FirmId.ToString();
            ordernew.Creationdate = TimeUtils.convertDateTimeString(order.CreationDate);
            ordernew.Address = oneCString;
            ordernew.Orderitems=new List<OrderNewItem>();
            foreach (var orderItem in orderItemsList)
            {
               var price=GetPersonalPrice(client.Id, orderItem.GoodId);
                if (price == 0)
                {
                    price = (float) orderItem.Price;
                }
                var code1c = goods.FirstOrDefault(x => x.Id == orderItem.GoodId).Code1C;
                ordernew.Orderitems.Add(new OrderNewItem{Price = price.ToString("##.0"),Goodcode1C = code1c,Count = orderItem.Count.ToString()});  
            }
            return ordernew;
        }

        /// <summary>
        /// Взять нужный заказ для его последующего удаления ????????
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public OrderDelete GetOrderToDelete(ExchangeRecord record)
        {
            var order = _context.Orders.FirstOrDefault(x => x.Id == record.ItemId);
            var supplier = _context.Suppliers.FirstOrDefault(x => x.Id == order.SuppliersId);
            var clientUser = _context.Users.FirstOrDefault(x => x.Id == supplier.ClientUserId);
            var client = _context.Partners.FirstOrDefault(x => x.Id == clientUser.PartnerId);
            var priceColumn = _context.PriceColumns.FirstOrDefault(x => x.Id == client.PriceColumnId);
            var delivery = _context.Deliveries.FirstOrDefault(x => x.OrderId == order.Id);
            List<OrderItems> orderItemsList = _context.OrderItems.Where(x => x.OrderId == order.Id).ToList();
            var goods = _context.Goods.ToList();
            String oneCString = GetOneCString(order, delivery, orderItemsList, goods);
            var orderToDel = new OrderDelete();
            orderToDel.Idis = order.Id.ToString();
            orderToDel.Id1C = order.Code1C;
            orderToDel.Idclientis = client.Id.ToString();
            orderToDel.Idclient1C = client.Num1C;
            orderToDel.Pricecolumn = priceColumn.Name;
            orderToDel.Firm = order.FirmId.ToString();
            orderToDel.Creationdate = TimeUtils.convertDateTimeString(order.CreationDate);
            orderToDel.Address = oneCString;
            return orderToDel;
        }



        /// <summary>
        /// Ставиться флаг удаления на заказе. Объект тот же что и при успешном заведении заказа
        /// </summary>
        /// <param name="responce"></param>
        public void SaveOrderDeleteStatus(OrderNewResponce responce)
        {
            var order = _context.Orders.FirstOrDefault(x => x.Id == responce.idIs);
            if (order != null)
            {
                order.Status = (int)OrderState.Deleted;
            }
            Update(order); 
        }

        /// <summary>
        /// Сохранить номер 1С у заказа
        /// </summary>
        /// <param name="orderNewResponce"></param>
        public void SaveOrderNewResponce(OrderNewResponce orderNewResponce)
        {
            var order = _context.Orders.FirstOrDefault(x => x.Id == orderNewResponce.idIs);
            if (order != null)
            {
                order.Code1C = orderNewResponce.id1C;
                order.Status = (int) OrderState.OneCConfirmed;
            }
            Update(order);
        }

        /// <summary>
        /// СОхранить номер 1С у клиента
        /// </summary>
        /// <param name="clientResponce"></param>
        public void SaveClientNewResponce(ClientResponce clientResponce)
        {
            var client = _context.Partners.FirstOrDefault(x => x.Id == clientResponce.idIs);
            if (client != null)
            {
                client.Num1C = clientResponce.id1C;
            }
            Update(client);
        }

        /// <summary>
        ///     Обновить статус записи в очереди
        /// </summary>
        /// <param name="exchangeRecord"></param>
        public void UpdateExchangeRecord(ExchangeRecord exchangeRecord)
        {
            var exchange = new Exchange1C
            {
                Id = exchangeRecord.Id,
                Source = (int) exchangeRecord.SourceType,
                ExchangeState = (int) exchangeRecord.ExchangeState,
                Date = exchangeRecord.DateTime,
                ItemId=NullUtils.validateInt(exchangeRecord.ItemId)
            };
            Update(exchange);
        }

        /// <summary>
        ///     Удалить запись из основной очереди и переместить в штрафную
        /// </summary>
        /// <param name="exchangeRecord"></param>
        public void MoveToBrokenExchange(ExchangeRecord exchangeRecord)
        {
            var exchange= _context.Exchange1C.FirstOrDefault(x => x.Id == exchangeRecord.Id);
            _context.Exchange1C.Remove(exchange);
            var brokenExchange = new Exchange1CBroken
            {
                Source = exchange.Source,
                Date = exchange.Date,
                Error = exchangeRecord.ErrorMessage,
                ItemId = exchangeRecord.ItemId
            };
           // _context.Exchange1CBroken.Attach(brokenExchange);
           // _context.SaveChanges();
            InsertOrUpdate(brokenExchange);
        }

        /// <summary>
        ///     Сохраняет долги у партнеров\клиентов и обновляет остатки. Закрывает таск в очереди
        /// </summary>
        /// <param name="ostatkiParsed"></param>
        public void SaveRestAndDebt(OstatkiParsed ostatkiParsed)
        {
            List<Partners> partners = _context.Partners.ToList();
            List<ClientDebt> debts = ostatkiParsed.ClientDebts.ToList();
            foreach (Partners partner in partners)
            {
                ClientDebt debt = debts.FirstOrDefault(x => x.Client1CId == partner.Num1C);
                if (debt != null)
                {
                    partner.DebtBank = debt.BankDebt;
                    partner.DebtCash = debt.CashDebt;
                }
                Update(partner);
            }
            List<Rest> rests = ostatkiParsed.Rests.ToList();
            List<Goods> goods = _context.Goods.ToList();
            List<PartnerRests> partnerRests = _context.PartnerRests.Where(x => x.PartnerId == CENTRAL_ID).ToList();
            foreach (Goods good in goods)
            {
                Rest rest = rests.FirstOrDefault(x => x.Good1CCode == good.Code1C.Trim());
                if (rest != null)
                {
                    PartnerRests partnerRest = partnerRests.FirstOrDefault(x => x.GoodId == good.Id);
                    if (partnerRest == null)
                    {
                        partnerRest = new PartnerRests
                        {
                            PartnerId = CENTRAL_ID,
                            Count = rest.FreeCount,
                            GoodId = good.Id,
                            PartnerGoodName = good.Code
                        };
                    }
                    else
                    {
                        partnerRest.Count = rest.FreeCount;
                    }
                    InsertOrUpdate(partnerRest);
                }
            }
        }

        //Сохраняем товары из выгрузки, а так же все прайсы в представленные Центральным колонки
        public void SaveColumnsAndGoods(PriceParsed priceParsed)
        {
            List<Goods> goods = _context.Goods.ToList();
            //Добавление новых товаров из 1С
            IEnumerable<string> goodCodes = priceParsed.Prices.Select(x => x.GoodCode).Distinct();
            foreach (string goodCode in goodCodes)
            {
                if (!goods.Any(x => x.Code == goodCode))
                {
                    GoodPrice examplegoodPrice = priceParsed.Prices.FirstOrDefault(x => x.GoodCode == goodCode);
                    var newGood = new Goods
                    {
                        Code = examplegoodPrice.GoodCode,
                        Code1C = examplegoodPrice.GoodCode1C,
                        CategoryId = 0,
                        IsDeleted = false,
                        ImageUrl = "",
                        Barcode = 0,
                        GroupCount = 0,
                        PR = "",
                        Volume = 0,
                        Weight = 0
                    };
                    InsertOrUpdate(newGood);
                }
            }
            List<PriceColumns> centralColumns = _context.PriceColumns.Where(x => x.PartnerId == CENTRAL_ID).ToList();
            //Зануление всех прайсов Центрального магазина
            IEnumerable<PriceColumnItem> allCentralPrices =
                _context.PriceColumnItem.ToList().Where(x => centralColumns.Any(y => y.Id == x.PriceColumnId));
            allCentralPrices.ToList().ForEach(x => x.Price = 0);
            //Только колонки, которые присутсвуют у Центрального магазина и в выгрузке 1С
            List<PriceColumns> actualColumns =
                centralColumns.Where(x => priceParsed.Prices.Any(y => y.PriceName == x.Name)).ToList();
            //Только те товары, которые представлены в выгрузке из 1С
            var actualGoods = _context.Goods.ToList();
            foreach (PriceColumns actualColumn in actualColumns)
            {
                List<GoodPrice> prices = priceParsed.Prices.Where(x => x.PriceName == actualColumn.Name).ToList();
                //Все прайсы, содержащиеся в одной колонке
                foreach (GoodPrice price1C in prices)
                {
                    var good = actualGoods.FirstOrDefault(x=>x.Code.Equals(price1C.GoodCode));
                    PriceColumnItem price = _context.PriceColumnItem.FirstOrDefault(
                        x => x.GoodId==good.Id && x.PriceColumnId == actualColumn.Id);
                    //Если такого прайса в базе нет, то создаем его
                    if (price == null)
                    {
                        var priceToSave = new PriceColumnItem
                        {
                            GoodId = good.Id,
                            PriceColumnId = actualColumn.Id,
                            Price = (decimal) price1C.PriceCost
                        };
                        InsertOrUpdate(priceToSave);
                    }
                        //Если такой прайс в базе есть, то обновляем
                    else
                    {
                        price.Price = (decimal) price1C.PriceCost;
                        Update(price);
                    }
                }
            }
            _context.SaveChanges();
        }

        /// <summary>
        ///     Если нет id, то добавляет новый элемент в таблицу, если есть - обновляет
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public void InsertOrUpdate<T>(T entity) where T : class
        {
            DbEntityEntry<T> entry = _context.Entry(entity);
            object pkey = _context.Set<T>().Create().GetType().GetProperty("Id").GetValue(entity);
            if (!Equals(pkey, 0))
            {
                DbSet<T> set = _context.Set<T>();
                T attachedEntity = set.Find(pkey); // access the key
                if (attachedEntity != null)
                {
                    DbEntityEntry<T> attachedEntry = _context.Entry(attachedEntity);
                    attachedEntry.CurrentValues.SetValues(entity);
                }
                else
                {
                    entry.State = EntityState.Modified; // attach the entity
                }
                _context.SaveChanges();
            }
            else if (Equals(pkey, 0))
            {
                _context.Set<T>().Add(entity);
                try
                {
                    _context.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException e)
                {
                    throw e;
                }

                
            }
        }

        /// <summary>
        ///     При условии заполненности всех полей - обновляет запись в таблице
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public void Update<T>(T entity) where T : class
        {
            DbEntityEntry<T> entry = _context.Entry(entity);
            object pkey = _context.Set<T>().Create().GetType().GetProperty("Id").GetValue(entity);
            if (entry.State == EntityState.Detached)
            {
                DbSet<T> set = _context.Set<T>();
                T attachedEntity = set.Find(pkey); // access the key
                if (attachedEntity != null)
                {
                    DbEntityEntry<T> attachedEntry = _context.Entry(attachedEntity);
                    attachedEntry.CurrentValues.SetValues(entity);
                }
                else
                {
                    entry.State = EntityState.Modified; // attach the entity
                }
            }
            _context.SaveChanges();
        }

        /// <summary>
        /// Удаляет сущность
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public void Delete<T>(T entity) where T : class
        {
            DbEntityEntry<T> entry = _context.Entry(entity);
            entry.State = EntityState.Deleted;
            _context.SaveChanges();
        }

        /// <summary>
        ///     ВОзвращает либо 0 либо существующее значение персонального прайса для товара конкртеного клиента
        /// Почему по коду, вообще непонятно. 
        /// </summary>
        /// <param name="partnerId"></param>
        /// <param name="goodCode"></param>
        /// <returns></returns>
        private float GetPersonalPrice(int partnerId, int goodId)
        {
            float price = 0;
            PricesPersonal pricePersonal =
                _context.PricesPersonal.FirstOrDefault(x => x.PartnerId == partnerId && x.GoodId == goodId);
            if (pricePersonal != null)
            {
                DateTime startTime = pricePersonal.StartDate;
                if (pricePersonal.EndDate != null)
                {
                    var endDate = (DateTime) pricePersonal.EndDate;
                    if (DateTime.Now.CompareTo(endDate) < 0 && (DateTime.Now.CompareTo(startTime) > 0))
                    {
                        price = (float) pricePersonal.Price;
                    }
                }
                else
                {
                    if (DateTime.Now.CompareTo(startTime) > 0)
                    {
                        price = (float) pricePersonal.Price;
                    }
                }
            }
            return price;
        }

        /// <summary>
        ///СОздание и отсылка совершенно уебищной информационной строки для отображения в 1С
        /// </summary>
        /// <param name="order"></param>
        /// <param name="deliveryInfo"></param>
        /// <param name="orderNewItems"></param>
        /// <param name="goods"></param>
        /// <returns></returns>
        private String GetOneCString(Orders order, Deliveries deliveryInfo, List<OrderItems> orderNewItems,
            List<Goods> goods)
        {
            float weight = 0;
            float volume = 0;
            foreach (OrderItems orderNewItem in orderNewItems)
            {
                float goodWeight =
                    NullUtils.validateFloat(goods.FirstOrDefault(x => x.Id == orderNewItem.GoodId).Weight);
                float goodVolume =
                    NullUtils.validateFloat(goods.FirstOrDefault(x => x.Id == orderNewItem.GoodId).Volume);
                int count = Convert.ToInt32(orderNewItem.Count);
                weight += goodWeight*count;
                volume += goodVolume * count;
            }
            var sb = new StringBuilder();
            if (order.PayType == 0)
            {
                sb.Append("Безналичный расчет. ");
            }
            else if (order.PayType == 1)
            {
                sb.Append("Наличный расчет. ");
            }

            sb.Append("ДАТА ОТГРУЗКИ: " + TimeUtils.convertDateString(order.ShipmentDate) + ". ");
            if (order.DeliveryId == 0)
                sb.Append("Самовывоз. ");

            else if(order.DeliveryId>0)
            {
                sb.Append("Доставка автотранспортом компании. ");
                if (!deliveryInfo.TransCompany.Equals(""))
                {
                    sb.Append("ТРАНСПОРТНАЯ КОМПАНИЯ: " + deliveryInfo.TransCompany + ". ");
                }
                sb.Append("ВЕС: " + weight + ". ");
                sb.Append("ОБЪЕМ: " + volume + ". ");
                if (!deliveryInfo.TransContactName.Equals(""))
                {
                    sb.Append("КОНТАКТ. ТК: " + deliveryInfo.TransContactName + ". ");
                }

                if (!deliveryInfo.TransAdress.Equals(""))
                {
                    sb.Append("АДРЕС ТК: " + deliveryInfo.TransAdress + ". ");
                }

                if (!deliveryInfo.TransPhone.Equals(""))
                {
                    sb.Append("ТЕЛ. ТК: " + deliveryInfo.TransPhone + ". ");
                }

                if (!deliveryInfo.ReciverAdress.Equals(""))
                {
                    sb.Append("АДРЕС ДОСТ.: " + deliveryInfo.ReciverAdress + ". ");
                }

                if (!deliveryInfo.ReciverContactName.Equals(""))
                {
                    sb.Append("КОНТАКТ. ДОСТ.: " + deliveryInfo.ReciverContactName + ". ");
                }

                if (!deliveryInfo.ReciverPhone.Equals(""))
                {
                    sb.Append("ТЕЛ. ДОСТ.: " + deliveryInfo.ReciverPhone + ". ");
                }

                if (!deliveryInfo.Comment.Equals(""))
                {
                    sb.Append(deliveryInfo.Comment);
                }
            }
            return sb.ToString();
        }
    }
}
