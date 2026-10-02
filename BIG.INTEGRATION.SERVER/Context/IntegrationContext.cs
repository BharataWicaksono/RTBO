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
                
            }
        }        
        #endregion
        #region FO->BO        
        private void CtxFO_OnProcessQueueExtension(object obj, DataTypeBase o)
        {
            switch (Helper.GetEnumByName<TimestampName>(o.Name))
            {
                              
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
