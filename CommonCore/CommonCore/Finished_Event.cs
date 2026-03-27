///------------------------------------------------------------------------------------------------------------------
///
///                             Class: Common Core
///                             Author: Anthony Hoffert (thoffert@skbcases.com)
///                            
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
    public class Finished_Event:EventArgs
    {
        public enum Finish_Status { ABORTED,ERROR,SUCCESS};
        public Finish_Status status;
        public Finished_Event(Finish_Status status)
        {
            this.status = status;
        }

    }
}