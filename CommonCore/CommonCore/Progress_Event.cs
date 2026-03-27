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
    public class Progress_Event:EventArgs
    {
        public float Percentage;
        public Progress_Event(float percent)
        {
            Percentage = percent;
        }
    }
}