using BIG.NETCORE.Database.SqlServer;
using BIG.NETCORE.Integration;
using System;
using System.Collections.Generic;
//using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIG.INTEGRATION.SERVER
{
    public class Setting
    {
        public string NAME { get; set; }
        public bool DEBUG { get; set; }
        public int THEME { get; set; }
        public bool LOGFILE { get; set; }
        public bool LOGDATABASE { get; set; }
        public string PUBLISH { get; set; }
        public string PORTS { get; set; }
    }
    public class ApiClient
    {
        public int ID { get; set; }
        public string NAME { get; set; }
        public string URI { get; set; }
        public string ENDPOINT { get; set; }
    }
    public class Email
    {
        public int ID { get; set; }
        public string NAME { get; set; }
        public string SMTP { get; set; }
        public int PORT { get; set; }
        public string FROM { get; set; }
        public string PASSWORD { get; set; }
        public string TO { get; set; }
        public string CC { get; set; }
    }
    public class ConnectionString
    {
        public string NAME { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }
    public class Scheduler
    {
        public int ID { get; set; }
        public string NAME { get; set; }
        public bool AUTO { get; set; }
        public string SOURCE { get; set; }
        public TimeSpan SCHEDULE { get; set; }
        public bool Executed { get; set; }
    }

    public static class AllSetting
    {
        public static Setting setting = new Setting();
        public static List<TimestampBuffer> Timestamp = new List<TimestampBuffer>();
        public static List<Intraday> intradays = new List<Intraday>();
        public static Email email = new Email();
        public static List<Scheduler> schedulers = new List<Scheduler>();
        public static List<ApiClient> apiClients = new List<ApiClient>();
        public static List<ConnectionString> connections = new List<ConnectionString>();
        public static Exception LoadFileSetting()
        {
            try
            {
                DataSet ds = Helper.ReadXml();
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    if (ds.Tables[0].Rows[i]["Type"].ToString() == "ConnectionString")
                    {
                        connections.Add(new ConnectionString()
                        {
                            NAME = ds.Tables[0].Rows[i]["Name"].ToString(),
                            Type = ds.Tables[0].Rows[i]["Type"].ToString(),
                            Value = ds.Tables[0].Rows[i]["Value"].ToString()
                        });
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
        public static Exception LoadSetting()
        {
            try
            {                
                Intraday.Load(Helper.BIGConnectionString.Value, Helper.GET_INTRADAY, out intradays);

                using (SqlWrapper sql = new SqlWrapper(Helper.BIGConnectionString.Value))
                {
                    sql.PrepareStoredProcedure("GET_SCHEDULER", 0);
                    SqlDataReader r = sql.ExecuteReader();
                    while (r.Read())
                    {
                        schedulers.Add(new Scheduler()
                        {
                            ID = int.Parse(r["ID"].ToString()),
                            NAME = r["NAME"].ToString(),
                            SOURCE = r["SOURCE"].ToString(),
                            AUTO = Helper.GetBooleanValue(r["AUTO"].ToString()),
                            SCHEDULE = TimeSpan.Parse(r["SCHEDULE"].ToString()),
                            Executed = false
                        });
                    }
                }
                using (SqlWrapper sql = new SqlWrapper(Helper.BIGConnectionString.Value))
                {
                    sql.PrepareStoredProcedure("GET_SETTING", 0);
                    SqlDataReader r = sql.ExecuteReader();
                    while (r.Read())
                    {
                        setting.DEBUG = Helper.GetBooleanValue(r["DEBUG"].ToString());
                        setting.THEME = int.Parse(r["THEME"].ToString());
                        setting.LOGFILE = Helper.GetBooleanValue(r["LOGFILE"].ToString());
                        setting.LOGDATABASE = Helper.GetBooleanValue(r["LOGDATABASE"].ToString());
                        setting.PUBLISH = r["PUBLISH"].ToString();
                        setting.PORTS = r["PORTS"].ToString();
                    }
                }
                using (SqlWrapper sql = new SqlWrapper(Helper.BIGConnectionString.Value))
                {
                    sql.PrepareStoredProcedure("GET_EMAIL", 0);
                    SqlDataReader r = sql.ExecuteReader();
                    while (r.Read())
                    {
                        email.ID = int.Parse(r["ID"].ToString());
                        email.NAME = r["NAME"].ToString();
                        email.SMTP = r["SMTP"].ToString();
                        email.PORT = int.Parse(r["PORT"].ToString());
                        email.FROM = r["FROM"].ToString();
                        email.PASSWORD = r["PASSWORD"].ToString();
                        email.TO = r["TO"].ToString();
                        email.CC = r["CC"].ToString();
                    }
                }
                using (SqlWrapper sql = new SqlWrapper(Helper.BIGConnectionString.Value))
                {
                    sql.PrepareStoredProcedure("GET_APICLIENT", 0);
                    SqlDataReader r = sql.ExecuteReader();
                    while (r.Read())
                    {
                        apiClients.Add(new ApiClient()
                        {
                            NAME = r["NAME"].ToString(),
                            URI = r["URI"].ToString(),
                            ENDPOINT = r["ENDPOINT"].ToString()
                        });
                    }
                }
                TimestampBuffer.Load(Helper.BIGConnectionString.Value, Helper.GET_TIMESTAMPS, out Timestamp);
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
        public static Exception SaveIntradays()
        {
            try
            {
                foreach (Intraday intraday in intradays)
                {
                    Intraday.Save(Helper.BIGConnectionString.Value, Helper.UPDATE_INTRADAY, intraday);
                }
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
        public static Exception SaveSetting()
        {
            try
            {
                using (SqlWrapper sql = new SqlWrapper(Helper.BIGConnectionString.Value))
                {
                    sql.PrepareStoredProcedure("UPDATE_SETTING", 0);
                    sql.AddParameter("@DEBUG", DbType.Boolean, setting.DEBUG);
                    sql.AddParameter("@THEME", DbType.Int32, setting.THEME);
                    sql.AddParameter("@LOGFILE", DbType.Boolean, setting.LOGFILE);
                    sql.AddParameter("@LOGDATABASE", DbType.Boolean, setting.LOGDATABASE);
                    sql.AddParameter("@PORTS", DbType.Boolean, setting.PORTS);
                    sql.ExecuteNonQuery();
                }
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
        public static Exception SaveEmail()
        {
            try
            {
                using (SqlWrapper sql = new SqlWrapper(Helper.BIGConnectionString.Value))
                {
                    sql.PrepareStoredProcedure("UPDATE_EMAIL", 0);
                    sql.AddParameter("@ID", DbType.Int32, email.ID);
                    sql.AddParameter("@NAME", DbType.String, email.NAME);
                    sql.AddParameter("@SMTP", DbType.String, email.SMTP);
                    sql.AddParameter("@PORT", DbType.Int32, email.PORT);
                    sql.AddParameter("@FROM", DbType.String, email.FROM);
                    sql.AddParameter("@PASSWORD", DbType.String, email.PASSWORD);
                    sql.AddParameter("@TO", DbType.String, email.TO);
                    sql.AddParameter("@CC", DbType.String, email.CC);
                    sql.ExecuteNonQuery();
                }
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
        public static Exception SaveScheduler()
        {
            try
            {
                using (SqlWrapper sql = new SqlWrapper(Helper.BIGConnectionString.Value))
                {
                    for (int i = 0; i < schedulers.Count; i++)
                    {
                        sql.PrepareStoredProcedure("UPDATE_SCHEDULER", 0);
                        sql.AddParameter("@ID", DbType.Int32, schedulers[i].ID);
                        sql.AddParameter("@SOURCE", DbType.String, schedulers[i].SOURCE);
                        sql.AddParameter("@NAME", DbType.String, schedulers[i].NAME);
                        sql.AddParameter("@AUTO", DbType.String, schedulers[i].AUTO);
                        sql.AddParameter("@SCHEDULE", DbType.Time, DateTime.Parse(schedulers[i].SCHEDULE.ToString()));
                        sql.ExecuteNonQuery();
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
        public static Exception SaveApiClient()
        {
            try
            {
                using (SqlWrapper sql = new SqlWrapper(Helper.BIGConnectionString.Value))
                {
                    for (int i = 0; i < apiClients.Count; i++)
                    {
                        sql.PrepareStoredProcedure("UPDATE_APICLIENT", 0);
                        sql.AddParameter("@ID", DbType.Int32, apiClients[i].ID);
                        sql.AddParameter("@NAME", DbType.String, apiClients[i].NAME);
                        sql.AddParameter("@URI", DbType.String, apiClients[i].URI);
                        sql.AddParameter("@ENDPOINT", DbType.String, apiClients[i].ENDPOINT);
                        sql.ExecuteNonQuery();
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
        public static Exception SaveAllTimestamp()
        {
            try
            {
                foreach (TimestampBuffer tb in Timestamp)
                {
                    TimestampBuffer.Save(Helper.BIGConnectionString.Value, Helper.UPDATE_TIMESTAMPS, tb);
                }
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
