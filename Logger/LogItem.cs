using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Logger
{
    /// <summary>
    /// Structure of a basic log 
    /// </summary>
    public class LogItem
    {
        //log levels or modes or type
        public enum LogType { Log, Warning, Error };
        //source of the log
        public string source;
        //message of the log
        public string message;
        //level of the log
        public LogType type;
        //time the log was stored
        public DateTime TimeLogged;
        public LogItem()
        {

        }
        public LogItem(XmlNode Root)
        {
            for(int i=0;i<Root.Attributes.Count;i++)
            {
                switch(Root.Attributes[i].Name.ToUpper()) 
                {
                    case "SOURCE":
                        source = Root.Attributes[i].Value;
                        break;
                    case "MESSAGE":
                        message = Root.Attributes[i].Value;
                        break;
                    case "TYPE":
                        switch(Root.Attributes[i].Value.ToUpper())
                        {
                            case "LOG":
                                type = LogType.Log;
                                break;
                            case "WARNING":
                                type = LogType.Warning;
                                break;
                            case "ERROR":
                                type = LogType.Error;
                                break;
                        }
                        break;
                    case "TIME_LOGGED":
                        TimeLogged = CommonCore.CommonCore.stringToFullDatetime(Root.Attributes[i].Value);
                        break;

                
                }
            }
        }

        internal XmlElement Get_As_Element(ref XmlDocument doc)
        {

            XmlElement output = doc.CreateElement("Log");
            output.SetAttribute("Source", source);
            output.SetAttribute("message", message);
            switch (type)
            {
                case LogType.Log:
                    output.SetAttribute("Type", "Log");
                    break;
                case LogType.Warning:
                    output.SetAttribute("Type", "Warning");
                    break;
                case LogType.Error:
                    output.SetAttribute("Type", "Error");
                    break;
            }
            output.SetAttribute("Time_Logged", TimeLogged.ToShortDateString() + " " + TimeLogged.ToShortTimeString());
            return output;
        }
    }
}
