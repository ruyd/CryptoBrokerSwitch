using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Piggy
{
    public class ErrorLog
    {
        public int ID { get; set; }
        public string StackTrace { get; set; }
        public string Module { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; }
        public string SourceName { get; set; }
        public DateTime? DateTimeCreated { get; set; } = DateTime.UtcNow;
        public string Data { get; set; }
        public int Filter { get; set; }
    }

    public class AppErrorLog
    {
        public int ID { get; set; }
        public string StackTrace { get; set; }
        public string Module { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; }
        public string SourceName { get; set; }
        public DateTime? DateTimeCreated { get; set; } = DateTime.UtcNow;
        public string Data { get; set; }
        public int Filter { get; set; }
    }

    public static class ErrorFx
    {
        public static int Level { get; set; } = 3;
        public static void Log(string s, string stackTrace = null, string data = null, int level = 3)
        {
            if (level > Level)
            {
                return;
            }

            Task.Run(async () =>
            {
                using (BrokerDatabase context = new BrokerDatabase())
                {
                    ErrorLog l = new ErrorLog();
                    l.Message = s.Clean(500);
                    l.Filter = level;
                    l.StackTrace = stackTrace.Clean(8000);

                    if (data != null)
                    {
                        l.Data = data.Clean(8000);
                    }

                    context.ErrorLogs.Add(l);
                    await context.SaveChangesAsync();
                }
            });
        }
        public static ErrorLog ToLog(this Exception ex, string userName)
        {
            ErrorLog l = new ErrorLog();
            l.Message = ex.InnerException?.Message ?? ex.Message;
            l.StackTrace = ex.StackTrace;
            l.Module = ex.TargetSite?.Name;
            l.SourceName = ex.Source;
            l.UserName = userName;

            return l;
        }

        public static void SaveToDB(this Exception ex, string data = null, [CallerLineNumber] int line = 0, [CallerMemberName] string method = null, [CallerFilePath] string source = null)
        {
            Task.Run(async () =>
            {
                using (BrokerDatabase context = new BrokerDatabase())
                {
                    ErrorLog l = ex.ToLog("Greed");

                    if (data != null)
                    {
                        l.Data = data.Clean(8000);
                    }

                    if (l.Message != null)
                    {
                        l.Message = l.Message.Clean(500);
                    }

                    if (l.StackTrace != null)
                    {
                        l.StackTrace = l.StackTrace.Clean(8000);
                    }

                    if (source?.LastIndexOf("\\") > 0)
                    {
                        source = source.Substring(source.LastIndexOf("\\"), source.Length - source.LastIndexOf("\\"));
                    }

                    l.SourceName = source ?? "Switch";
                    l.Module = $"Ln:{line} | {method}()";

                    context.ErrorLogs.Add(l);

                    await context.SaveChangesAsync();
                }
            });
        }
        public static void SaveToDB(this ErrorLog l)
        {
            Task.Run(async () =>
            {
                using (BrokerDatabase context = new BrokerDatabase())
                {
                    l.SourceName = l.SourceName.Clean(250);
                    l.Module = l.Module.Clean(250);

                    l.Data = l.Data.Clean(8000);
                    l.Message = l.Message.Clean(500);
                    l.StackTrace = l.StackTrace.Clean(8000);

                    context.ErrorLogs.Add(l);
                    await context.SaveChangesAsync();
                }
            });
        }

        public static void SaveToDB(this AppErrorLog l)
        {
            Task.Run(async () =>
            {
                using (BrokerDatabase context = new BrokerDatabase())
                {
                    l.Data = l.Data.Clean(8000);
                    l.Message = l.Message.Clean(500);
                    l.StackTrace = l.StackTrace.Clean(8000);

                    context.AppErrorLogs.Add(l);
                    await context.SaveChangesAsync();
                }
            });
        }
 
        public static void V(string message, int level = 1, string data = null, [CallerLineNumber] int line = 0, [CallerMemberName] string method = null, [CallerFilePath] string source = null)
        {
            if (level > Level)
            {
                return;
            }

            ErrorLog l = new ErrorLog();
            l.Message = message;
            l.Data = data;
            l.Filter = level;

            if (source?.LastIndexOf("\\") > 0)
            {
                source = source.Substring(source.LastIndexOf("\\"), source.Length - source.LastIndexOf("\\"));
            }

            l.SourceName = source ?? "Switch";
            l.Module = $"Ln:{line} | {method}()";
            l.SaveToDB();
        }
    }
}
