///------------------------------------------------------------------------------------------------------------------
///
///                             Class: Common Core
///                             Author: Anthony Hoffert (thoffert@skbcases.com)
///                             Owner: SKB Cases (https://www.skbcases.com/)
/// 
///  Description:
///  This class stores common functions such as converting a date string to a date time object, Opening a file. 
///  Showing a file in windows explorer, and displaying a time span as an easy to read text string
/// 
/// 
///------------------------------------------------------------------------------------------------------------------
using System;
namespace CommonCore
{
    public class Log_Event:EventArgs
    {
        public enum LogType { ERROR,WARNING,LOG};
        public LogType status;
        public string message;
        public Log_Event(string message, LogType status)
        {
            this.status = status;
            this.message = message;
        }
    }
}