///--------------------------------------------------------------------------------------------------------------------------------------------
/// Log events class
/// 
/// This class is used for event handlers for asyncronous call backs specifically log added and log cleared and log saved
/// 
/// Author: Anthony Hoffert
///--------------------------------------------------------------------------------------------------------------------------------------------
using System;
namespace Logger
{
    /// <summary>
    /// used to notify when a new log is received
    /// </summary>
    public class LogRecieved : EventArgs
    {
        //statistic packet of number of logs of each type and finally the log itself
        public struct LogStats {
            public int BasicLogCount;
            public int WarningLogCount;
            public int ErrorCount;
            public Logger.LogItem Log;
        }
        //stores the statistic packet
        public LogStats LogStatisticsAndLog;
        //constructor for the notification
        public LogRecieved(LogStats stats)
        {
            LogStatisticsAndLog = stats;
        }

    }
    /// <summary>
    /// used to notify when the log is cleared
    /// </summary>
    public class LogCleared:EventArgs
    {
        //not really used but the notification has to store something
        public bool Show_Warning;
        //constructor for the event
        public LogCleared(bool notification)
        {
            Show_Warning = notification;
        }
    }
    /// <summary>
    /// used to notify when the log is saved
    /// </summary>
    public class LogSaved : EventArgs
    {

        public string path;
        //constructor for the event
        public LogSaved(string path)
        {
            this.path = path;
        }
    }
}
