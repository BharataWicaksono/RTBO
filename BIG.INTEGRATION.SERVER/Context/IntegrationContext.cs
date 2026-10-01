using BIG.NETCORE.Database.SqlServer;
using BIG.NETCORE.Integration;
using BIG.NETCORE.Logger;
using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BIG.INTEGRATION.SERVER.Context
{
    public partial class IntegrationContext
    {
        public DbContext ctxBO, ctxFO;
        private int QueueCount = 0;
        public int Queue { get; set; }
        public string MIDDLE_PROCESS { get; set; }
        public string FO_PROCESS { get; set; }
        private LogWriter _logWriter;
        public IntegrationContext(LogWriter logWriter)
        {
            _logWriter = logWriter;

            ctxBO = new DbContext("BO", "FO", Helper.BOConnectionString.Value, Helper.FOConnectionString.Value, AllSetting.Timestamp, AllSetting.intradays[0], DatabaseType.SQLSERVER, DatabaseType.POSTGRESQL);
            ctxBO.OnProcessQueueExtension += CtxBO_OnProcessQueueExtension;
            ctxBO.GetLogMessage += Ctx_GetLogMessage;
            ctxBO.GetProcessingName += CtxBO_GetProcessingName;
            ctxBO.OnQueueIncrement += Ctx_OnQueueIncrement;
            ctxBO.OnQueueDecrement += Ctx_OnQueueDecrement;
            ctxBO.OnQueueCompleted += Ctx_OnQueueCompleted;
            ctxBO.StartQueue();
            ctxBO.StartIntradayWorker();

            ctxFO = new DbContext("FO", "BO", Helper.FOConnectionString.Value, Helper.BOConnectionString.Value, AllSetting.Timestamp, AllSetting.intradays[0], DatabaseType.POSTGRESQL, DatabaseType.SQLSERVER);
            ctxFO.OnProcessQueueExtension += CtxFO_OnProcessQueueExtension;
            ctxFO.GetLogMessage += Ctx_GetLogMessage;
            ctxFO.GetProcessingName += CtxFO_GetProcessingName;
            ctxFO.OnQueueIncrement += Ctx_OnQueueIncrement;
            ctxFO.OnQueueDecrement += Ctx_OnQueueDecrement;
            ctxFO.OnQueueCompleted += Ctx_OnQueueCompleted;
            ctxFO.StartQueue();
            ctxFO.StartIntradayWorker();
        }
        private void Ctx_OnQueueIncrement()
        {
            Queue = Interlocked.Increment(ref QueueCount);
            //_lblQueue.SetText($"Queue : {Interlocked.Increment(ref QueueCount).ToString()}");
        }
        private void Ctx_OnQueueDecrement()
        {
            //_lblQueue.SetText($"Queue : {Interlocked.Decrement(ref QueueCount).ToString()}");
            Queue = Interlocked.Decrement(ref QueueCount);
        }
        private void CtxBO_GetProcessingName(string DataType)
        {
            //if (string.IsNullOrEmpty(DataType) == false)
            MIDDLE_PROCESS = DataType;
            SetActionText();
        }
        private void CtxFO_GetProcessingName(string DataType)
        {
            //if (string.IsNullOrEmpty(DataType) == false)
            FO_PROCESS = DataType;
            SetActionText();
        }
        private void Ctx_GetLogMessage(bool IsMandatory, string Message, string Content, string Stacktrace)
        {
            DebugMode(IsMandatory, Message, Content, Stacktrace);
        }
        public void DebugMode(bool auto, string Message, string Content, string StackTrace = null)
        {
            if ((auto || AllSetting.setting.DEBUG) && _logWriter != null)
            {
                if (string.IsNullOrEmpty(StackTrace))
                    _logWriter.AddLogContent(LogType.INFO, Message, Content);
                else
                    _logWriter.AddLogContent(LogType.ERROR, Message, Content, StackTrace);
            }
        }
        public void SetActionText()
        {
            //if (string.IsNullOrEmpty(LastDataTypeBO) && string.IsNullOrEmpty(LastDataTypeRT)) _lblAction.SetText("Action : ");
            //else _lblAction.SetText("Processing " + (string.IsNullOrEmpty(LastDataTypeBO) ? "" : " BO:" + LastDataTypeBO) + (string.IsNullOrEmpty(LastDataTypeRT) ? "" : " FO:" + LastDataTypeRT));            
        }
        public void StopQueue()
        {
            ctxBO.StopQueue();
            ctxFO.StopQueue();
        }
        public void KillQueue()
        {
            ctxBO.KillQueue();
            ctxFO.KillQueue();
        }
        /// <summary>
        /// event di panggil setiap selesai satu queue selesai di proses
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="o"></param>
        private void Ctx_OnQueueCompleted(object obj, DataTypeBase o)
        {
            try
            {
                var jArray = JArray.Parse(o.Content);
                switch (Helper.GetEnumByName<TimestampName>(o.Name))
                {
                    case TimestampName.FO_CASHWITHDRAW:
                        //for (int i = 0; i < jArray.Count; i++)
                        //    SendEmail(jArray[0]["ClientID"].ToString(), double.Parse(jArray[0]["Col1"].ToString()));
                        break;
                    case TimestampName.FO_STOCKWITHDRAW:
                        //for (int i = 0; i < jArray.Count; i++)
                        //    SendEmail(jArray[0]["ClientID"].ToString(), jArray[0]["StockID"].ToString(), double.Parse(jArray[0]["Col1"].ToString()), double.Parse(jArray[0]["Col2"].ToString()));
                        break;
                }
            }
            catch (Exception e)
            {
                _logWriter.AddLog(LogType.ERROR, e.Message, e.StackTrace);
            }
        }
        #region BO->FO
        public void CopyToFo(OpCode opCode, TimestampName Name, DatabaseType DestType, string Content)
        {
            DataTypeBase o = new DataTypeBase();
            o.Name = Helper.GetEnumName<TimestampName>(Name);
            o.OpCode = opCode;
            o.DestType = DestType;
            o.Content = Content;
            ctxBO.EnqueueData(o);
        }
        private void CtxBO_OnProcessQueueExtension(object obj, DataTypeBase o)
        {
            switch (Helper.GetEnumByName<TimestampName>(o.Name))
            {
                case TimestampName.BO_BANKINSTRUCTION:
                    ProcessFrontOfficeBankInstruction(o); break;
                case TimestampName.BO_BRIINSTRUCTION:
                    ProcessFrontOfficeBRIInstruction(o); break;
            }
        }
        private async void ProcessFrontOfficeBankInstruction(DataTypeBase o)
        {
            try
            {
                //RemoteTradingBankInstruction a = o as RemoteTradingBankInstruction;

                //if (AllSetting.setting.DEBUG)
                //    _logWriter.AddLog(LogType.INFO, "--> API: " + a.Name + " | BankInstructionNID: " + a.BankInstructionNID);

                //ApiClient apiClient = AllSetting.apiClients.FirstOrDefault(f => f.NAME == "BANKINSTRUCTION");
                //if (apiClient == null) return;

                //string Uri = apiClient.URI; 
                //using (var client = new HttpClient())
                //{
                //    client.Timeout = TimeSpan.FromSeconds(5);
                //    client.BaseAddress = new Uri(Uri);
                //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

                //    HttpResponseMessage response = new HttpResponseMessage();
                //    StringBuilder sb = new StringBuilder();
                //    sb.Append("BankInstructionNID=").Append(a.BankInstructionNID);
                //    response = await client.GetAsync(apiClient.ENDPOINT + "?" + Helper.GetQueryString(sb.ToString())).ConfigureAwait(true);
                //    if (response.IsSuccessStatusCode)
                //    {
                //        string responseObj = await response.Content.ReadAsStringAsync();
                //        //BankInstructionResponse result = JsonConvert.DeserializeObject<BankInstructionResponse>(responseObj);
                //        _logWriter.AddLog(LogType.INFO, "API --> BankInstructionNID : " + a.BankInstructionNID + " (" + responseObj + ")");
                //    }
                //}
            }
            catch (HttpRequestException e)
            {
                _logWriter.AddLog(LogType.ERROR, e.Message, e.StackTrace);
            }
            catch (Exception e)
            {
                _logWriter.AddLog(LogType.ERROR, e.Message, e.StackTrace);
            }
            _logWriter.AddLog(LogType.INFO, $"Completed BO->API Copy [{Helper.GetEnumName<TimestampName>(TimestampName.BO_BANKINSTRUCTION)}]");
        }
        private async void ProcessFrontOfficeBRIInstruction(DataTypeBase o)
        {
            try
            {
                //RemoteTradingBRIInstruction a = o as RemoteTradingBRIInstruction;

                //if (AllSetting.setting.DEBUG)
                //    _logWriter.AddLog(LogType.INFO, "--> API: " + a.Name + " | Type: " + a.Type + " | Date: " + a.Date.ToString("yyyyMMdd"));

                //ApiClient apiClient = AllSetting.apiClients.FirstOrDefault(f => f.NAME == "BRIINSTRUCTION");
                //if (apiClient == null) return;


                //string Uri = apiClient.URI; //"http://10.101.32.33:10011/bri/generate/transfer/{type}/{date}";
                //using (var client = new HttpClient())
                //{
                //    client.Timeout = TimeSpan.FromSeconds(5);
                //    client.BaseAddress = new Uri(Uri);
                //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //    HttpResponseMessage response = new HttpResponseMessage();

                //    response = await client.PostAsync(apiClient.ENDPOINT + "/" + a.Type + "/" + a.Date.ToString("yyyyMMdd"), null).ConfigureAwait(true);
                //    if (response.IsSuccessStatusCode)
                //    {
                //        string responseObj = await response.Content.ReadAsStringAsync();
                //        //BankInstructionResponse result = JsonConvert.DeserializeObject<BankInstructionResponse>(responseObj);
                //        _logWriter.AddLog(LogType.INFO, "API --> Type : " + a.Type + " | Date: " + a.Date.ToString("yyyyMMdd") + " (" + responseObj + ")");
                //    }
                //}
            }
            catch (HttpRequestException e)
            {
                _logWriter.AddLog(LogType.ERROR, e.Message, e.StackTrace);
            }
            catch (Exception e)
            {
                _logWriter.AddLog(LogType.ERROR, e.Message, e.StackTrace);
            }
            _logWriter.AddLog(LogType.INFO, $"Completed BO->API Copy [{Helper.GetEnumName<TimestampName>(TimestampName.BO_BRIINSTRUCTION)}]");
        }
        #endregion
        #region FO->BO        
        private void CtxFO_OnProcessQueueExtension(object obj, DataTypeBase o)
        {
            switch (Helper.GetEnumByName<TimestampName>(o.Name))
            {
                case TimestampName.FO_ORDER:
                    ProcessBackOfficeOrder(o); break;
                    //case TimestampName.FO_TRADE:
                    //    ProcessBackOfficeTrade(o); break;               
            }
        }
        private async void ProcessBackOfficeOrder(DataTypeBase o)
        {
            try
            {
                var jArray = JArray.Parse(o.Content);

                if (jArray.Count == 0) return;
                if (AllSetting.setting.DEBUG)
                    _logWriter.AddLog(LogType.INFO, "--> API: " + o.Name + " | ID: " + jArray[0][0]?.ToString());

                ApiClient? apiClient = AllSetting.apiClients.FirstOrDefault(f => f.NAME == "BO");
                if (apiClient == null) return;

                string Uri = apiClient.URI;
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    client.BaseAddress = new Uri(Uri);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

                    HttpResponseMessage response = new HttpResponseMessage();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("ID=").Append(jArray[0][0]?.ToString());
                    response = await client.GetAsync(apiClient.ENDPOINT + "?" + Helper.GetQueryString(sb.ToString())).ConfigureAwait(true);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseObj = await response.Content.ReadAsStringAsync();
                        //BankInstructionResponse result = JsonConvert.DeserializeObject<BankInstructionResponse>(responseObj);
                        _logWriter.AddLog(LogType.INFO, "API --> ID : " + jArray[0][0]?.ToString() + " (" + responseObj + ")");
                    }
                }
            }
            catch (Exception e)
            {
                _logWriter.AddLog(LogType.ERROR, e.Message, e.StackTrace);
            }
            _logWriter.AddLog(LogType.INFO, $"Completed FO -> API Copy [{Helper.GetEnumName<TimestampName>(TimestampName.FO_ORDER)}]");
        }
        #endregion       
        #region EMAIL
        public void SendEmail(string ClientID, string StockID, double Price, double Volume)
        {
            try
            {
                //List<string> CC = new List<string>();
                //CC.AddRange(AllSetting.email.CC.Split('|'));
                //var fromAddress = new MailAddress(AllSetting.email.FROM, "");
                //var toAddress = new MailAddress(AllSetting.email.TO, AllSetting.email.TO);
                //var smtp = new SmtpClient
                //{
                //    Host = AllSetting.email.SMTP,
                //    Port = AllSetting.email.PORT,
                //    DeliveryMethod = SmtpDeliveryMethod.Network,
                //    Credentials = new NetworkCredential(AllSetting.email.USER, AllSetting.email.PASSWORD, AllSetting.email.DOMAIN)
                //};
                //StringBuilder sb = new StringBuilder();
                //sb.AppendLine("<html>Share Withdrawal for Margin Customer")
                //        .AppendLine("<br><br>")
                //        .AppendLine("Client ID			            : ").Append(ClientID).AppendLine("<br>")
                //        .AppendLine("Share Name			            : ").Append(StockID).AppendLine("<br>")
                //        .AppendLine("MarketPrice		            : ").Append(Price).AppendLine("<br>")
                //        .AppendLine("Withdrawal qty		            : ").Append(Volume).AppendLine("<br>")
                //        .AppendLine("<br><br>")
                //        .AppendLine("Mohon untuk melakukan proses lebih lanjut bagi nasabah tersebut")
                //        .AppendLine("<br><br>")
                //        .AppendLine("Terima Kasih</html>");
                //using (var message = new MailMessage(fromAddress, toAddress)
                //{
                //    Subject = "Share Withdrawal From FrontOffice",
                //    Body = sb.ToString(),
                //    IsBodyHtml = true
                //})
                //{
                //    for (int i = 0; i < CC.Count; i++)
                //        message.CC.Add(new MailAddress(CC[i]));
                //    smtp.Send(message);
                //        //}
                //    }
                //    catch (Exception e)
                //    {
                //        _logWriter.AddLog(LogType.ERROR, e.Message, e.StackTrace);
                //    }
                //}
                //public void SendEmail(string ClientID, double Amount)
                //{
                //    try
                //    {
                //        List<string> CC = new List<string>();
                //        CC.AddRange(AllSetting.email.CC.Split('|'));
                //        var fromAddress = new MailAddress(AllSetting.email.FROM, "");
                //        var toAddress = new MailAddress(AllSetting.email.TO, AllSetting.email.TO);
                //        var smtp = new SmtpClient
                //        {
                //            Host = AllSetting.email.SMTP,
                //            Port = AllSetting.email.PORT,
                //            DeliveryMethod = SmtpDeliveryMethod.Network,
                //            Credentials = new NetworkCredential(AllSetting.email.USER, AllSetting.email.PASSWORD, AllSetting.email.DOMAIN)
                //        };

                //        StringBuilder sb = new StringBuilder();

                //        sb.AppendLine("<html>Fund Withdrawal for Margin Customer")
                //            .AppendLine("<br><br>")
                //            .AppendLine("Client ID			            : ").Append(ClientID).AppendLine("<br>")
                //            .AppendLine("Withdrawal Amount	            : Rp ").Append(Amount).AppendLine("<br>")
                //            .AppendLine("<br><br>")
                //            .AppendLine("Mohon untuk melakukan proses lebih lanjut bagi nasabah tersebut")
                //            .AppendLine("<br><br>")
                //            .AppendLine("Terima Kasih</html>");
                //        using (var message2 = new MailMessage(fromAddress, toAddress)
                //        {
                //            Subject = "Fund Withdrawal From FrontOffice",
                //            Body = sb.ToString(),
                //            IsBodyHtml = true
                //        })
                //        {
                //            for (int i = 0; i < CC.Count; i++)
                //                message2.CC.Add(new MailAddress(CC[i]));
                //            smtp.Send(message2);
                //        }
            }
            catch (Exception e)
            {
                _logWriter.AddLog(LogType.ERROR, e.Message, e.StackTrace);
            }
        }
        #endregion
        public DateTime? GetApplicationDate()
        {
            DateTime appDate;
            try
            {
                using (SqlWrapper sql = new SqlWrapper(Helper.BOConnectionString.Value))
                {
                    sql.PrepareStoredProcedure("GetApplicationDate", 0);
                    if (DateTime.TryParse(sql.ExecuteScalar().ToString(), out appDate))
                        return appDate;
                    else
                        return null;
                }
            }
            catch (Exception e)
            {
                _logWriter.AddLog(LogType.ERROR, e.Message, e.StackTrace);
                return null;
            }
        }
    }
}
