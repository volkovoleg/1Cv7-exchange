using System;
using System.Linq;
using System.Threading;
using ExchangeWith1C.Data;
using ExchangeWith1C.Data.Enum;
using ExchangeWith1C.Entity;
using ExchangeWith1C.Helpers;
using ExchangeWith1C.Models;
using ExchangeWith1C.Utils;
using ExchangeWith1C.Xml;

namespace ExchangeWith1C
{
    public class Exchange
    {
        private readonly DatabaseService _databaseService;
        private readonly string workingDirectory;
        private readonly string proceedDirectory;
        private readonly string errorDirectory;

        public Exchange(string workingDirectory, string proceedDirectory, string errorDirectory)
        {
            _databaseService = new DatabaseService();
            this.workingDirectory = workingDirectory;
            this.proceedDirectory = proceedDirectory;
            this.errorDirectory = errorDirectory;
        }

        /// <summary>
        ///     Взять самую раннюю запись из очереди
        /// </summary>
        /// <returns></returns>
        public ExchangeRecord GetExchangeRecord()
        {
            return _databaseService.GetExchangeRecord();
        }

        /// <summary>
        ///     От 1С принимается файл Ostatki, который отдает все долги по клиентам, и остатки, свободные/зарезервированные
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// Папка для обмена
        /// <param name="proceedDirectory"></param>
        /// Папка, куда складываються результаты обмена
        /// <param name="ostatkiName"></param>
        /// Название файла
        public void ProceedRestAndDebt(String ostatkiName, ExchangeRecord record)
        {
            if ((record.SourceType == SourceType.Rest) && (record.ExchangeState == ExchangeState.Ready))
            {
                if (FileUtils.IsExistByPartOfName(workingDirectory, ostatkiName))
                {
                    OstatkiParsed a = XmlParser.ParseOstatki(workingDirectory + ostatkiName);
                    if (NullUtils.IsNullObject(a))
                    {
                        ErrorUtils.markError(record, "Не получается обработать остатки с 1С");
                        return;
                    }
                    FileUtils.Move(workingDirectory + ostatkiName, proceedDirectory + ostatkiName);
                    _databaseService.SaveRestAndDebt(a);
                    record.ExchangeState = ExchangeState.Done;
                    _databaseService.UpdateExchangeRecord(record);
                }
            }
        }


        /// <summary>
        ///     От 1С принимаеться выгрузка с номерами и именами товаров, а так же с их ценовыми колонками
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// Папка для обмена
        /// <param name="proceedDirectory"></param>
        /// Папка, куда складываються результаты обмена
        /// <param name="priceName"></param>
        /// Название файла
        public void ProceedPrice(String priceName, ExchangeRecord record)
        {
            if ((record.SourceType == SourceType.Price) && (record.ExchangeState == ExchangeState.Ready))
            {
                if (FileUtils.IsExistByPartOfName(workingDirectory, priceName))
                {
                    PriceParsed a = XmlParser.ParsePrice(workingDirectory + priceName);
                    if (NullUtils.IsNullObject(a))
                    {
                        ErrorUtils.markError(record, "Не получается обработать выгрузку ценовых колонок из 1С");
                        return;
                    }
                    FileUtils.Move(workingDirectory + priceName, proceedDirectory + priceName);
                    _databaseService.SaveColumnsAndGoods(a);
                    record.ExchangeState = ExchangeState.Done;
                    _databaseService.UpdateExchangeRecord(record);
                }
            }
        }

        /// <summary>
        ///     Формируется заказ, для введения в 1С. Результатом идет xml с указанием номера 1С
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="proceedDirectory"></param>
        /// <param name="record"></param>
        public void ProceedOrderNew(ExchangeRecord record)
        {
            if ((record.SourceType == SourceType.OrderCreate) && (record.ExchangeState == ExchangeState.Ready))
            {
                OrderNew orderToXml = _databaseService.GetNewOrder(record);
                String fileName = XmlCreator.CreateOrderXml(workingDirectory, orderToXml);
                if(ExchangeOrErrorCycle(record))
                {
                    return;
                }
                FileUtils.Move(workingDirectory + fileName, proceedDirectory + fileName);
                string responceFileName = FileUtils.Get1CFileByPartOfName(workingDirectory, "from1CtoIS");
                OrderNewResponce orderNewResponce =
                    XmlParser.CreateOrderResponse(workingDirectory + responceFileName);
                if (NullUtils.IsNullObject(orderNewResponce))
                {
                    ErrorUtils.markError(record, "Не получается обработать ответ 1С на создание заказа");
                    return;
                }
                _databaseService.SaveOrderNewResponce(orderNewResponce);
                record.ExchangeState = ExchangeState.Done;
                _databaseService.UpdateExchangeRecord(record);
                FileUtils.Move(workingDirectory + responceFileName, proceedDirectory + responceFileName);
            }
        }

        /// <summary>
        ///     Создается запрос в 1С, показывающий, что заказ и нужные его элементы собраны
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="proceedDirectory"></param>
        /// <param name="record"></param>
        public void ProceedOrderBuild(ExchangeRecord record)
        {
            if ((record.SourceType == SourceType.OrderBuild) && (record.ExchangeState == ExchangeState.Ready))
            {
                OrderBild orderBild = _databaseService.GetOrderBild(record);
                String fileName = XmlCreator.BildOrderXml(workingDirectory, orderBild);
                if (ExchangeOrErrorCycle(record))
                {
                    return;
                }
                FileUtils.Move(workingDirectory + fileName, proceedDirectory + fileName);
                string responceFileName = FileUtils.Get1CFileByPartOfName(workingDirectory, "from1CtoIS");
                OrderBuildResponce orderBuildResponce = XmlParser.OrderBuildResponse(workingDirectory + responceFileName);
                if (NullUtils.IsNullObject(orderBuildResponce))
                {
                    ErrorUtils.markError(record, "Не получается обработать ответ 1С на сборку заказа");
                    return;
                }
                if (orderBuildResponce.result.Equals("ERROR_REALIZED"))
                {
                    ErrorUtils.markError(record, "В заказе на сборку обнаружены ошибки");
                    return;
                }
                _databaseService.SaveOrderBuildResponce(orderBuildResponce);
                record.ExchangeState = ExchangeState.Done;
                _databaseService.UpdateExchangeRecord(record);
                FileUtils.Move(workingDirectory + responceFileName, proceedDirectory + responceFileName);
            }
        }

        /// <summary>
        ///     Создание запроса в 1С об удалении заказа. Возвращается результат такой же, как и при заведении нового заказа
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="proceedDirectory"></param>
        /// <param name="record"></param>
        public void ProceedOrderDelete(ExchangeRecord record)
        {
            if ((record.SourceType == SourceType.OrderDelete) && (record.ExchangeState == ExchangeState.Ready))
            {
                OrderDelete orderDelete = _databaseService.GetOrderToDelete(record);
                String fileName = XmlCreator.DeleteOrderXml(workingDirectory, orderDelete);
                if (ExchangeOrErrorCycle(record))
                {
                    return;
                }
                FileUtils.Move(workingDirectory + fileName, proceedDirectory + fileName);
                string responceFileName = FileUtils.Get1CFileByPartOfName(workingDirectory, "from1CtoIS");
                //Возвращаемая при удалении xml такая же как и после успешного заведения
                OrderNewResponce orderDeleteResponce = XmlParser.CreateOrderResponse(workingDirectory + responceFileName);
                if (NullUtils.IsNullObject(orderDeleteResponce))
                {
                    ErrorUtils.markError(record, "Ошибка от 1С при попытке удаления заказа");
                    return;
                }
                _databaseService.SaveOrderDeleteStatus(orderDeleteResponce);
                record.ExchangeState = ExchangeState.Done;
                _databaseService.UpdateExchangeRecord(record);
                FileUtils.Move(workingDirectory + responceFileName, proceedDirectory + responceFileName);
            }
        }

        /// <summary>
        ///     Создает запрос в 1С о создании нового клиента и получает номер
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="proceedDirectory"></param>
        /// <param name="record"></param>
        public void ProceedClient(ExchangeRecord record)
        {
            if ((record.SourceType == SourceType.Client) && (record.ExchangeState == ExchangeState.Ready))
            {
                Client client = _databaseService.GetClient(record);
                String fileName = XmlCreator.CreateClientXml(workingDirectory, client);
                if (ExchangeOrErrorCycle(record))
                {
                    return;
                }
                FileUtils.Move(workingDirectory + fileName, proceedDirectory + fileName);
                string responceFileName = FileUtils.Get1CFileByPartOfName(workingDirectory, "from1CtoIS");
                ClientResponce clientNewResponce = XmlParser.ClientResponse(workingDirectory + responceFileName);
                if (NullUtils.IsNullObject(clientNewResponce))
                {
                    ErrorUtils.markError(record, "Не удается внести нового клиента в 1С");
                    return;
                }
                _databaseService.SaveClientNewResponce(clientNewResponce);
                record.ExchangeState = ExchangeState.Done;
                _databaseService.UpdateExchangeRecord(record);
                FileUtils.Move(workingDirectory + responceFileName, proceedDirectory + responceFileName);
            }
        }

        /// <summary>
        ///     Тут отлавливаются все пришедшие ошибки
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="errorDirectory"></param>
        /// <param name="record"></param>
        public void ProcessingError(ExchangeRecord record)
        {
            if (record.ExchangeState == ExchangeState.Error)
            {
                //Флаг, перемещать ли в поломанную очередь или просто закрыть в обычной очереди
                bool moveToBrokenExchange = true;
                //Базовый файл, сигнализирующий об ошибке в обмене
                if (FileUtils.IsExistByPartOfName(errorDirectory, "last_error.txt"))
                {
                    string fileName = FileUtils.Get1CFileByPartOfName(errorDirectory, "last_error.txt");
                    if (fileName != null)
                    {
                        FileUtils.Delete(errorDirectory + fileName);
                    }
                }
                //Файл указывает на невозможность зарезервировать позиции
                if (FileUtils.IsExistByPartOfName(errorDirectory, "ERROR_ORDER_BAD_RESTS"))
                {
                    string fileName = FileUtils.Get1CFileByPartOfName(errorDirectory, "ERROR_ORDER_BAD_RESTS");
                    if (fileName != null)
                    {
                        FileUtils.Delete(errorDirectory + fileName);
                    }
                    Orders order = _databaseService._context.Orders.FirstOrDefault(x => x.Id == record.ItemId);
                    order.Status = (int) OrderState.Saved;
                    _databaseService.Update(order);
                    //
                    //Послать менеджеру сообщение

                    moveToBrokenExchange = false;
                }
                //Файл указывает на то, что клиент заблокирован и его заказ не может быть проведен
                if (FileUtils.IsExistByPartOfName(errorDirectory, "Error_Client_is_block"))
                {
                    string fileName = FileUtils.Get1CFileByPartOfName(errorDirectory, "Error_Client_is_block");
                    if (fileName != null)
                    {
                        FileUtils.Delete(errorDirectory+fileName);
                    }
                    Orders order = _databaseService._context.Orders.FirstOrDefault(x => x.Id == record.ItemId);
                    order.Status = (int) OrderState.Saved;
                    _databaseService.Update(order);
                    //
                    //Послать менеджеру сообщение

                    moveToBrokenExchange = false;
                }
                Thread.Sleep(5000);
                //Вычищение всех файлов приходящих в 1С
                while (FileUtils.IsExistByPartOfName(workingDirectory, "fromISto1C"))
                {
                    string fileName = FileUtils.Get1CFileByPartOfName(workingDirectory, "fromISto1C");
                    if (fileName != null)
                    {
                        FileUtils.Move(workingDirectory + fileName, proceedDirectory + fileName);
                    }
                }
                //Вычищение всех файлов приходящих от 1С
                while (FileUtils.IsExistByPartOfName(workingDirectory, "from1CtoIS"))
                {
                    string fileName = FileUtils.Get1CFileByPartOfName(workingDirectory, "from1CtoIS");
                    if (fileName != null)
                    {
                        FileUtils.Move(workingDirectory + fileName, proceedDirectory + fileName);
                    }
                }
                if (moveToBrokenExchange)
                {
                    _databaseService.MoveToBrokenExchange(record);  
                }
                else if(!moveToBrokenExchange)
                {
                    record.ExchangeState = ExchangeState.Done;
                    _databaseService.UpdateExchangeRecord(record);  
                }           
            }
        }

        /// <summary>
        ///     Проверка на ответные действия от 1С. Это либо xml с ответом, либо файлы, сигнализирующие об ошибке. Показывает, срабатывать обработке ошибок или нет.
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        private Boolean ExchangeOrErrorCycle(ExchangeRecord record)
        {
            bool markError = false;
            bool filesExist = false;
            while (!filesExist)
            {
                if (FileUtils.IsExistByPartOfName(errorDirectory, "last_error"))
                {
                    ErrorUtils.markError(record, "Появился файл last_error, 1С не может принять заказ");
                    markError = true;
                    filesExist = true;
                }
                if (FileUtils.IsExistByPartOfName(errorDirectory, "ERROR_ORDER_BAD_RESTS"))
                {
                    ErrorUtils.markError(record,
                        "Заказа не может быть проведен из-за невозможности в 1С зарезервировать товар");
                    markError = true;
                    filesExist = true;
                }
                if (FileUtils.IsExistByPartOfName(errorDirectory, "Error_Client_is_block"))
                {
                    ErrorUtils.markError(record, "Заказа не может быть проведен из-за того, что клиент заблокирован");
                    markError = true;
                    filesExist = true;
                }
                if (FileUtils.IsExistByPartOfName(workingDirectory, "from1CtoIS"))
                {
                    filesExist = true;
                }
            }
            return markError;
        }

        public void SaveLog(DateTime dateTime, string logTitle, string logMessage)
        {
            var log = new Log {DateAndTime = dateTime, LogTitle = logTitle, LogMessage = logMessage};
            _databaseService.InsertOrUpdate(log);
        }
    }
}
    
    