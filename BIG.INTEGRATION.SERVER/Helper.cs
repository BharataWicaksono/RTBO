using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BIG.NETCORE.Database.SqlServer;
using BIG.NETCORE.Database.PostgresSql;

namespace BIG.INTEGRATION.SERVER
{
    public class Response
    {
        public string Message { get; set; }
    }
    public class ResponseStatus
    {
        public bool MIDDLEDB { get; set; }
        public string MID_CURRENT_PROCESS { get; set; }
        public bool FODB { get; set; }
        public string FO_CURRENT_PROCESS { get; set; }
        public long QUEUE { get; set; }
    }
    public class ResponseNewtwork
    {
        public string Ip { get; set; }
        public string Ports { get; set; }
    }
    public static class Helper
    {
        public static readonly string ADD_LOGMESSAGE = "ADD_LOGMESSAGE";
        public static readonly string BIG_HOUSEKEEPING_LOG = "[HOUSEKEEPING]";
        public static readonly string BIG_HOUSEKEEPING = "[dbo].[HOUSEKEEPING]";

        public static readonly string UPDATE_DBTS = "UPDATE_DBTS";

        public static readonly string GET_TIMESTAMPS = "GET_TIMESTAMPS";
        public static readonly string UPDATE_TIMESTAMPS = "UPDATE_TIMESTAMPS";
        public static readonly string GET_INTRADAY = "GET_INTRADAY";
        public static readonly string UPDATE_INTRADAY = "UPDATE_INTRADAY";

        public static readonly string STARTUP_PATH = AppContext.BaseDirectory; //GetFolderPath(Environment.GetCommandLineArgs()[0]);

        public static ConnectionString BOConnectionString, FOConnectionString, BIGConnectionString;
        public const string BOCS = "BOConnectionString", FOCS = "FOConnectionString", BIGCS = "BIGConnectionString";
        public const string LOG_FILE = "LogFile", LOG_DATABASE = "LogDatabase";
        public const string FILE_SETTING = "Setting.xml";
        public const string RESTART = "RESTART", SOD = "SOD", EOD = "EOD";

        public const string FILE_DATE_FORMAT = "yyyyMMdd", FILE_TIME_FORMAT = "HHmmss", BATCH_FORMAT = "000000";

        public static string Log_FullName, LogError_FullName;
        public static void SetLogFullName(DateTime Date)
        {
            Log_FullName = STARTUP_PATH + @"/Log/" + "BIG_" + Date.ToString("yyyyMMdd") + ".LOG";
        }
        public static void SetLogErrorFullName(DateTime Date)
        {
            LogError_FullName = STARTUP_PATH + @"/Log/" + "ERROR.LOG";
        }
        public static string[] Split(string Text, string SplitText)
        {
            return Text.Split(new string[] { SplitText }, StringSplitOptions.None);
        }
        public static bool GetBooleanValue(string value)
        {
            if (value.Trim().Equals("0") || value.ToLowerInvariant().Trim().Equals("false"))
                return false;
            return true;
        }
        public static T GetEnumByName<T>(string str) where T : struct
        {
            try
            {
                T res = (T)Enum.Parse(typeof(T), str);
                if (!Enum.IsDefined(typeof(T), res)) return default(T);
                return res;
            }
            catch
            {
                return default(T);
            }
        }
        public static string GetEnumName<T>(T Type) where T : struct
        {
            try
            {
                return Enum.GetName(typeof(T), Type);
            }
            catch
            {
                return string.Empty;
            }
        }
        public static string GetQueryString(string Query)
        {
            return Query.StartsWith("&") ? Query.Substring(1) : Query;
        }
        public static DataSet ReadXml()
        {
            DataSet ds = new DataSet();

            if (File.Exists(Path.Combine(STARTUP_PATH, FILE_SETTING)) == false)
                throw new FileNotFoundException();
            ds.ReadXml(Path.Combine(STARTUP_PATH, FILE_SETTING));
            return ds;

        }
        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
        public static bool isPostgresSqlConnect(string connectionString)
        {
            using (PostgresSqlWrapper sql = new PostgresSqlWrapper(connectionString))
            {
                return sql.TestConnection();
            }
        }
        public static string GetErrorPostgresSqlConnect(string connectionString)
        {
            using (PostgresSqlWrapper sql = new PostgresSqlWrapper(connectionString))
            {
                return sql.TestErrorConnection();
            }
        }
        public static bool isSqlServerConnect(string connectionString)
        {
            using (SqlWrapper sql = new SqlWrapper(connectionString))
            {
                return sql.TestConnection();
            }
        }
        public static string GetFullFileName(string Address)
        {
            return Address.Split('/')[Address.Split('/').Length - 1];
        }
        public static string GetFolderPath(string AddressFile)
        {
            string[] split = AddressFile.Split("/");
            string Temp = string.Join("/", split.SkipLast(1));
            return Temp;
        }
        public static string[] GetAllFiles(string Folder, string Filter)
        {
            if (Directory.Exists(Folder) == false) Directory.CreateDirectory(Folder);
            //string[] FilesImport = Directory.GetFiles(Folder, "*_" + Filter + "_" + DateTime.Now.ToString(FILE_DATE_FORMAT) + "_*" + ".csv");
            string[] FilesImport = Directory.GetFiles(Folder, "*_*_" + DateTime.Now.ToString(FILE_DATE_FORMAT) + "_*" + ".csv");
            //FilesImport = FilesImport.Where(w => !w.EndsWith("000000.csv")).ToArray();
            Array.Sort(FilesImport, StringComparer.InvariantCulture);
            return FilesImport;
        }     
    }
}
