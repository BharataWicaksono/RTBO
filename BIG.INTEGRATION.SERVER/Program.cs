// See https://aka.ms/new-console-template for more information
using BIG.NETCORE.Integration;
using BIG.INTEGRATION.SERVER;
using BIG.INTEGRATION.SERVER.Context;
using BIG.NETCORE.Logger;
using BIG.NETCORE.WebServer;
using Newtonsoft.Json;
//using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Text;
using BIG.NETCORE.Database.SqlServer;


bool running = true, _isNewStart = true, _isLateSOD = false;
DateTime prevDate;
DateTime? applicationDate;
ILogger[] logger;
LogWriter logWriter;
IntegrationContext context;
Thread tmrThread, logThread, tickThread;
StringBuilder sbTableHome = new StringBuilder();
bool IsBoDbConnected = false;
bool IsFoDbConnected = false;
int[] ports;
string ip = string.Empty;
string ext = "BIG";
HttpServerListener httpServer = null;

prevDate = DateTime.Now;
Helper.SetLogFullName(prevDate);
Helper.SetLogErrorFullName(prevDate);

AllSetting.LoadFileSetting();
Helper.BIGConnectionString = AllSetting.connections.Find(f => f.NAME == Helper.BIGCS);
Helper.BOConnectionString = AllSetting.connections.Find(f => f.NAME == Helper.BOCS);
Helper.FOConnectionString = AllSetting.connections.Find(f => f.NAME == Helper.FOCS);

if (File.Exists(Path.Combine(Helper.STARTUP_PATH, Helper.FILE_SETTING)) == false)
{
    Console.WriteLine("BIG Connection String not Found, Please check " + Helper.STARTUP_PATH + "!");
    Console.ReadLine();
    return;
}
//Console.WriteLine("Test Connection " + Helper.BIGCS);
if (Helper.isSqlServerConnect(Helper.BIGConnectionString.Value) == false)
{
    Console.WriteLine("BIG Connection fail to connect, Please check " + Helper.BOCS + "!");
    Console.ReadLine();
    return;
}

Exception exception = AllSetting.LoadSetting();
if (exception != null)
{
    Console.WriteLine(exception.Message + Environment.NewLine + exception.StackTrace);
    return;
}

ports = Array.ConvertAll(AllSetting.setting.PORTS.Split(";"), int.Parse);

logger = new ILogger[AllSetting.setting.LOGFILE ? 3 : 1];
logger[0] = new ConsoleLogger();
if (AllSetting.setting.LOGFILE)
{
    logger[1] = new FileLogger(Helper.Log_FullName);
    logger[2] = new FileLogger(Helper.LogError_FullName, FileLogType.ERROR_ONLY);
}

logWriter = new LogWriter(logger, false);
logWriter.LOG_FILE = AllSetting.setting.LOGFILE;
logWriter.LOG_DATABASE = AllSetting.setting.LOGDATABASE;
logWriter.CONNECTION_STRING = Helper.BIGConnectionString.Value;
logWriter.PROCEDURE_DATABASE = Helper.ADD_LOGMESSAGE;

context = new IntegrationContext(logWriter);

if (AllSetting.setting.LOGDATABASE)
{
    if (Helper.BIGConnectionString != null)
    {
        logWriter.ClearLog(Helper.BIG_HOUSEKEEPING_LOG);
    }
}

#region WebServer
if (ports.Length > 0)
{
    string BigServerName = "BIG";
    httpServer = new BIG.NETCORE.WebServer.HttpServerListener(ports[0], BigServerName);
    //httpServer = new BIG.NETCORE.WebServer.HttpServerListener(18766, BigServerName);
    ip = httpServer.GetIPAddress();
    httpServer.SetFileExtension(AllSetting.setting.PUBLISH, ext);
    httpServer.SetConnectionString(Helper.BIGConnectionString.Value);
    httpServer.OnReceived += HttpServer_OnReceived;
    httpServer.OnErrorReceived += HttpServer_OnErrorReceived;

    void HttpServer_OnErrorReceived(string Uri, HttpListenerContext HttpContext, string Message, string Stacktrace)
    {
        logWriter.AddLog(LogType.ERROR, $"Request {BigServerName} {Message}");
        httpServer.Response(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new Response { Message = "Request params not Found" })), HttpContext, HttpContext.Request, string.Empty);
    }

    void HttpServer_OnReceived(string Uri, HttpListenerContext HttpContext, Dictionary<string, string> Params)
    {
        try
        {
            if (Uri.ToUpper() == "SERVER")
            {
                if (Params == null) return;
                string Key = Params["Key"];
                string Instruction = Params["Instruction"];
                if (Key == "BIG")
                {
                    switch (Instruction.ToUpper())
                    {
                        case "RESTART":
                            Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " | request incoming to Server to -> " + Instruction);
                            Restart(HttpContext, HttpContext.Request);
                            break;
                        case "SHUTDOWN":
                            Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " | request incoming to Server to -> " + Instruction);
                            Shudown(HttpContext, HttpContext.Request);
                            break;
                        case "STATUS":
                            httpServer.Response(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new ResponseStatus { MIDDLEDB = IsBoDbConnected, MID_CURRENT_PROCESS = context.MIDDLE_PROCESS, FODB = IsFoDbConnected, FO_CURRENT_PROCESS = context.FO_PROCESS, QUEUE = context.Queue })), HttpContext, HttpContext.Request, string.Empty);
                            break;
                        case "NETWORK":
                            httpServer.Response(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new ResponseNewtwork { Ip = ip, Ports = AllSetting.setting.PORTS })), HttpContext, HttpContext.Request, string.Empty);
                            break;
                        default:
                            Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " | request incoming to Server to -> " + Instruction);
                            logWriter.AddLog(LogType.ERROR, $"Request {BigServerName} for Instruction [{Instruction}] not Found!");
                            httpServer.Response(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new Response { Message = "Request params not Found" })), HttpContext, HttpContext.Request, string.Empty);
                            break;
                    }
                }
                else if (Key == "BO")
                {
                    if (Params.ContainsKey("SelectedId"))
                    {
                        string Id = Params["SelectedId"];
                        context.ctxBO.CopyByEnum(OpCode.Selected, Instruction.ToUpper(), Id);
                    }
                    else
                        context.ctxBO.CopyByEnum(OpCode.Manual, Instruction.ToUpper());
                }
                else if (Key == "FO")
                {
                    if (Params.ContainsKey("SelectedId"))
                    {
                        string Id = Params["SelectedId"];
                        context.ctxFO.CopyByEnum(OpCode.Selected, Instruction.ToUpper(), Id);
                    }
                    else
                        context.ctxFO.CopyByEnum(OpCode.Manual, Instruction.ToUpper());
                }
                else
                {
                    logWriter.AddLog(LogType.ERROR, $"Request {BigServerName} for Key [{Key}] not Found!");
                    httpServer.Response(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new Response { Message = "Request params not Found" })), HttpContext, HttpContext.Request, string.Empty);
                }
            }
            else
            {
                //Console.WriteLine( " SERVER : " +DateTime.Now.ToString("dd MMM yyyy HH:mm:ss") + " | request incoming for file -> " + Uri);
                httpServer.ReadFileHtml(Uri, HttpContext, HttpContext.Request.HttpMethod, Params);
            }
        }
        catch (Exception e)
        {
            logWriter.AddLog(LogType.ERROR, e.Message, e.StackTrace);
        }
    }
    httpServer.StartServer();
}
#endregion

#region WebServer 
if (ports.Length > 1)
{
    string BigServerName2 = "BIG2";
    //BIG.INTEGRATION.SERVER.HttpServerListener httpServer2 = new BIG.INTEGRATION.SERVER.HttpServerListener(18765, BigServerName2);
    BIG.NETCORE.WebServer.HttpServerListener httpServer2 = new BIG.NETCORE.WebServer.HttpServerListener(ports[1], BigServerName2);
    httpServer2.SetConnectionString(Helper.BIGConnectionString.Value);
    httpServer2.SetFileExtension(AllSetting.setting.PUBLISH, ext);
    httpServer2.OnReceived += HttpServer2_OnReceived;
    httpServer2.OnErrorReceived += HttpServer2_OnErrorReceived;
    void HttpServer2_OnErrorReceived(string Uri, HttpListenerContext HttpContext, string Message, string Stacktrace)
    {
        logWriter.AddLog(LogType.ERROR, $"Request {BigServerName2} {Message}");
        httpServer2.Response(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new Response { Message = "Request params not Found" })), HttpContext, HttpContext.Request, string.Empty);
    }
    void HttpServer2_OnReceived(string Uri, HttpListenerContext HttpContext, Dictionary<string, string> Params)
    {
        try
        {
            if (Uri.ToUpper() == "SERVER")
            {
                if (Params == null) return;
                string Key = Params["Key"];
                string Instruction = Params["Instruction"];
                if (Key == "BIG")
                {
                    switch (Instruction.ToUpper())
                    {
                        case "STATUS":
                            httpServer2.Response(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new ResponseStatus { MIDDLEDB = IsBoDbConnected, MID_CURRENT_PROCESS = context.MIDDLE_PROCESS, FODB = IsFoDbConnected, FO_CURRENT_PROCESS = context.FO_PROCESS, QUEUE = context.Queue })), HttpContext, HttpContext.Request, string.Empty);
                            break;
                        case "LOGMESSAGE":
                            httpServer2.Response(Encoding.ASCII.GetBytes(sbTableHome.ToString()), HttpContext, HttpContext.Request, string.Empty);
                            break;
                        default:
                            Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " | request incoming to Server to -> " + Instruction);
                            logWriter.AddLog(LogType.ERROR, $"Request {BigServerName2} for Instruction [{Instruction}] not Found!");
                            httpServer2.Response(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new Response { Message = "Request params not Found" })), HttpContext, HttpContext.Request, string.Empty);
                            break;
                    }
                }
                else
                {
                    logWriter.AddLog(LogType.ERROR, $"Request {BigServerName2} for Key [{Key}] not Found!");
                    httpServer2.Response(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new Response { Message = "Request params not Found" })), HttpContext, HttpContext.Request, string.Empty);
                }
            }
            else
            {
                httpServer2.ReadFileHtml(Uri, HttpContext, HttpContext.Request.HttpMethod, Params);
            }
        }
        catch (Exception e)
        {
            logWriter.AddLog(LogType.ERROR, e.Message, e.StackTrace);
        }
    }
    httpServer2.StartServer(string.Empty, true);
}
#endregion

logWriter.AddLog(LogType.INFO, "Application Started...");
running = true;
tmrThread = new Thread(async () =>
{
    while (running)
    {
        //Create Log File
        if (AllSetting.schedulers.Find(f => f.NAME == Helper.RESTART).AUTO == false && prevDate.Date < DateTime.Now.Date)
        {
            prevDate = DateTime.Now.Date;
            if (AllSetting.setting.LOGFILE)
            {
                (logger[1] as FileLogger).LogNewFile(Helper.Log_FullName);
                (logger[2] as FileLogger).LogNewFile(Helper.LogError_FullName);
            }
        }
        else if (prevDate.Date > DateTime.Now.Date)//handle jika ada rubah2 tanggal PC
        {
            prevDate = DateTime.Now.Date;
            if (!System.IO.File.Exists(Helper.Log_FullName))
            {
                if (AllSetting.setting.LOGFILE)
                {
                    (logger[1] as FileLogger).LogNewFile(Helper.Log_FullName);
                    (logger[2] as FileLogger).LogNewFile(Helper.LogError_FullName);
                }
            }
        }

        //SCHEDULER
        if (prevDate < DateTime.Now.Date)
        {
            prevDate = DateTime.Now;
            _isNewStart = false;
            for (int i = 0; i < AllSetting.schedulers.Count; i++)
                AllSetting.schedulers[i].Executed = false;
        }

        TimeSpan now = DateTime.Now.TimeOfDay;
        for (int i = 0; i < AllSetting.schedulers.Count; i++)
        {
            if (AllSetting.schedulers[i].AUTO && AllSetting.schedulers[i].SCHEDULE <= now && !AllSetting.schedulers[i].Executed)
            {
                if (_isNewStart) AllSetting.schedulers[i].Executed = true;
                else
                {
                    AllSetting.schedulers[i].Executed = true;
                    List<TimestampBuffer> Scheduler = AllSetting.Timestamp.FindAll(f => f.SchedulerID == AllSetting.schedulers[i].ID && f.ScheduleOrder > 0).OrderBy(f => f.ScheduleOrder).ToList();
                    if (Scheduler.Any())
                    {
                        if (AllSetting.schedulers[i].SOURCE.ToUpper() == "BO" && AllSetting.schedulers[i].NAME.ToUpper() == "SOD")
                        {
                            applicationDate = context.GetApplicationDate();
                            if (applicationDate != null && applicationDate < DateTime.Now.Date)
                                _isLateSOD = true;
                        }

                        foreach (TimestampBuffer s in Scheduler)
                        {
                            if (s.Key == "BO")
                                context.ctxBO.CopyByEnum(OpCode.Schedule, s.Name.ToUpper());
                            else if (s.Key == "FO")
                                context.ctxFO.CopyByEnum(OpCode.Schedule, s.Name.ToUpper());
                        }
                    }
                }
            }
        }

        if (_isLateSOD)//SOD telat, schedule SOD sudah jalan maka kirim ClientPosition ke depan jika applicationDate sudah hari ini 
        {
            applicationDate = context.GetApplicationDate();
            if (applicationDate != null && applicationDate >= DateTime.Now.Date)
            {
                _isLateSOD = false;
                Scheduler Sod = AllSetting.schedulers.Find(f => f.NAME == "SOD");
                if (Sod != null)
                {
                    List<TimestampBuffer> Resend = AllSetting.Timestamp.FindAll(f => f.SchedulerID == Sod.ID && f.ScheduleOrder > 0 && f.Resend == true).OrderBy(f => f.ScheduleOrder).ToList();
                    if (Resend.Any())
                    {
                        foreach (TimestampBuffer s in Resend)
                        {
                            context.ctxBO.CopyByEnum(OpCode.Schedule, s.Name.ToUpper());
                        }
                    }
                }
            }
        }
       
        _isNewStart = false;
        Exception error = AllSetting.SaveAllTimestamp();
        if (error != null)
            logWriter.AddLog(LogType.ERROR, error.Message, error.StackTrace);

        Thread.Sleep(AllSetting.intradays[0].MILISECONDS);
    }
});

logThread = new Thread(() =>
{
    while (running)
    {
        logWriter.ProcessLogQueue();
        Thread.Sleep(1000);
    }
});

tickThread = new Thread(() =>
{
    string LastDate = DateTime.Now.Date.ToString("yyyy-MM-dd");
    //Console.WriteLine(LastDate);
    while (running)
    {
        IsBoDbConnected = Helper.isSqlServerConnect(Helper.BOConnectionString.Value);

        IsFoDbConnected = Helper.isPostgresSqlConnect(Helper.FOConnectionString.Value);

        using (SqlWrapper sql = new SqlWrapper(Helper.BIGConnectionString.Value))
        {
            int i = 1;            
            sql.PrepareStoredProcedure("[GETLAST_LOGMESSAGE]", 0);
            sql.AddParameter("@Date", System.Data.DbType.String, LastDate);
            SqlDataReader r = sql.ExecuteReader();
            if (r.HasRows)
            {
                sbTableHome.Clear();

                while (r.Read())
                {
                    sbTableHome.AppendLine("<tr>");

                    sbTableHome.Append("<td>").Append("<div id=\"Date").Append(i).Append("\" style=\"text-align:left;color:").Append(r["DataType"].ToString() == "ERROR" ? "red;" : "black;").Append("\">").Append(r["Date"].ToString()).AppendLine("</div></td>");
                    sbTableHome.Append("<td>").Append("<div id=\"DataType").Append(i).Append("\" style=\"text-align:left;color:").Append(r["DataType"].ToString() == "ERROR" ? "red;" : "black;").Append("\">").Append(r["DataType"].ToString()).AppendLine("</div></td>");
                    sbTableHome.Append("<td>").Append("<div id=\"Message").Append(i).Append("\" style=\"text-align:left;color:").Append(r["DataType"].ToString() == "ERROR" ? "red;" : "black;").Append("\">").Append(r["Message"].ToString()).AppendLine("</div></td>");

                    sbTableHome.AppendLine("</tr>");
                    i++;

                    LastDate = r.GetDateTime(r.GetOrdinal("Date")).ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }
        }
        Thread.Sleep(5000);
    }
});

logThread.Start();
tmrThread.Start();
tickThread.Start();
Console.ReadLine();

void Restart(HttpListenerContext HttpContext, HttpListenerRequest request)
{
    if (httpServer == null)
        return;

    byte[] buffer = Encoding.UTF8.GetBytes("Server Restarting");
    httpServer.Response(buffer, HttpContext, request, string.Empty);
    Console.WriteLine("Restarting application...");
    System.Diagnostics.Process.Start(AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName);
    Environment.Exit(0);
}
void Shudown(HttpListenerContext HttpContext, HttpListenerRequest request)
{
    if (httpServer == null)
        return;

    byte[] buffer = Encoding.UTF8.GetBytes("Server Shuting Down");
    httpServer.Response(buffer, HttpContext, request, string.Empty);
    Console.WriteLine("Shutting down application...");
    Environment.Exit(0);
}
