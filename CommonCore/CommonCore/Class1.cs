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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Forms.VisualStyles;
using Newtonsoft.Json;
using System.Globalization;


namespace CommonCore
{
    public class CommonCore
    {
        [DllImport("shlwapi.dll")]
        public static extern bool PathIsNetworkPath(string pszPath);




        public static bool Use_Beyond_Compare_To_Avoid_Overwrite = true;
        public static string Temp_Delete_Me_Path = "C:\\Temp";
        public static string[] Local_Drives = null;
        public static Color string_To_Color(string ColorData)
        {
            int A, R, G, B;
            string temp;
            int colorNumber;
            Color output = Color.Transparent;
            if (ColorData.ToLower().Contains("argb") && ColorData.Contains("{") && ColorData.Contains(",") && ColorData.Contains("}"))
            {
                ColorData = ColorData.Substring(ColorData.IndexOf("{") + 1);
                temp = ColorData.Substring(0, ColorData.IndexOf(","));
                A = int.Parse(temp);
                ColorData = ColorData.Substring(ColorData.IndexOf(",") + 1);
                temp = ColorData.Substring(0, ColorData.IndexOf(","));
                R = int.Parse(temp);
                ColorData = ColorData.Substring(ColorData.IndexOf(",") + 1);
                temp = ColorData.Substring(0, ColorData.IndexOf(","));
                G = int.Parse(temp);
                ColorData = ColorData.Substring(ColorData.IndexOf(",") + 1);
                temp = ColorData.Substring(0, ColorData.IndexOf("}"));
                B = int.Parse(temp);
                output = Color.FromArgb(A, R, G, B);
            }
            else
            {
                if (ColorData.ToLower().Contains("argb") && ColorData.Contains("#"))
                {
                    ColorData = ColorData.Trim();
                    ColorData = ColorData.Substring(ColorData.IndexOf("#") + 1);
                    colorNumber = Int32.Parse(ColorData, System.Globalization.NumberStyles.HexNumber);
                    output = Color.FromArgb(colorNumber);
                }

                else
                {
                    output = Color.FromName(ColorData.Trim());
                }
            }
            return output;
        }
        public static int Set_label_width_to_Match_text_Width(ref Label lbl)
        {
            float width = 0;
            using (Graphics g = lbl.CreateGraphics())
            {
                SizeF size = g.MeasureString(lbl.Text, lbl.Font, 495);
                width = size.Width + 10;
                lbl.Height = (int)Math.Ceiling(size.Height);
                lbl.Width = (int)Math.Ceiling(width);
            }
            return (int)Math.Ceiling(width);
        }
        public static int set_CheckBox_Width_To_Match_Text_Width(ref CheckBox check_Box)
        {
            float width = 0;
            using (Graphics g = check_Box.CreateGraphics())
            {
                SizeF size = g.MeasureString(check_Box.Text, check_Box.Font, 495);
                width = size.Width + 20;
                check_Box.Height = (int)Math.Ceiling(size.Height);
                check_Box.Width = (int)Math.Ceiling(width - 0);
            }
            return (int)Math.Ceiling(width);
        }
        public static void Flash_Icon(IntPtr handle)
        {
            FlashWindowHelper.Flash(handle);

        }
        public static bool Matches_Regular_Expression(string text, string expression)
        {
            Regex rgx = new Regex(expression);
            return rgx.Match(text).Success;
        }
        public static void Concactinate_list<T>(ref LinkedList<T> temp_list2, LinkedList<T> temp_list)
        {
            LinkedListNode<T> node = temp_list.First;
            while (node != null)
            {
                temp_list2.AddLast(node.Value);
                node = node.Next;
            }
        }
        public static string Replace_text_based_on_Regular_Expression(string text, string replacement, string expression)
        {
            Regex rgx = new Regex(expression);
            string result = rgx.Replace(text, replacement);
            return result;
        }
        public static string Color_To_String(Color color)
        {
            string output;
            int start;
            output = color.ToString();
            if (!output.ToLower().Contains("["))
            {
                output = "ARGB #" + color.ToArgb().ToString("X");

            }
            else
            {

                output = color.ToString();
                output = output.Substring(output.IndexOf('[') + 1);
                output = output.Replace("]", "");
                output = output.Trim();
                if (output.Contains("A") && output.Contains("R") && output.Contains("G") && output.Contains("B"))
                {
                    start = color.ToArgb();
                    output = "ARGB #" + start.ToString("x");
                }
            }
            return output;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);





        public static event EventHandler<Log_Event> log_Occured;
        public static event EventHandler<Progress_Event> Progress_Made;
        public static event EventHandler<Finished_Event> completed;
        public enum File_Comparison_Results { BINARY_SAME, RULES_SAME, DIFFERENT, CANT_COMPARE };
        private static LinkedList<string> Files_Opened_In_NotepadPP = new LinkedList<string>();
        public static string Beyond_Compare_Com_Path;
        /// <summary>
        /// get the title of the window that currently has the users attention
        /// </summary>
        /// <returns>title of the window that currently has the users attention</returns>
        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }
        public static void Safe_Delete_To_Recycle_Bin(string filepath)
        {
            FileSystem.DeleteFile(filepath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);

        }
        public static void Send_Deleted_Network_File_To_Recycle_Bin(string filepath)
        {
            Deleted_File_Database.Send_File_To_Recycle_Bin(filepath);
        }
        /// <summary>
        /// converts a month to a quater
        /// </summary>
        /// <param name="month">month of the year</param>
        /// <returns>returns it's quarter of the year or 0 if a non month was given</returns>
        public static int Get_Quarter(int month)
        {
            switch (month)
            {
                case 1:
                case 2:
                case 3:
                    return 1;
                case 4:
                case 5:
                case 6:
                    return 2;
                case 7:
                case 8:
                case 9:
                    return 3;
                case 10:
                case 11:
                case 12:
                    return 4;
            }
            return 0;


        }
        public static bool is_network_PAth(string path)
        {
            return PathIsNetworkPath(path);
        }
        /// <summary>
        /// deletes a file and sends it to the recycle bin
        /// </summary>
        /// <param name="filename">file to be deleted</param>
        public static bool DeleteFileToRecycleBin(string filename)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filename))
                {
                    Console.WriteLine("Invalid filename.");
                    return false;
                }

                if (!System.IO.File.Exists(filename))
                {
                    Console.WriteLine("File does not exist.");
                    return false;
                }

                // Delete the file and send it to the recycle bin
                FileSystem.DeleteFile(
                    filename,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);

                Console.WriteLine($"File '{filename}' successfully moved to the recycle bin.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public static string[] Safe_Copy_Files_To_Directory(string[] files, string destination_Folder, bool suppress_warnings, bool overwrite = false, bool allow_renaming = true, bool use_recyclebin = true)
        {
            string[] output = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                output[i] = Safe_Copy_To_Directory(files[i], destination_Folder, overwrite, allow_renaming, use_recyclebin, suppress_warnings);
                if (output[i] == "SAME_LOCATION" || output[i]== "Canceled")
                {
                    output[i] = files[i];
                }
            }
            return output;
        }

        public static void Read_Deleted_Files(string path = "Deleted_File_Directory.xml")
        {
            Deleted_File_Database.Read_List(path);
        }
        public static bool Safe_Delete(string path, bool suppress_warnings, bool use_recyclebin = true)
        {
            bool local = false;
            if (!suppress_warnings)
                MessageBox.Show("Make sure the \"" + path + "\" is closed");
            if (PathIsNetworkPath(path))
            {
                //copy to temp path then delete the file
                Deleted_File_Database.Delete_File(path, use_recyclebin, suppress_warnings);

            }
            if (Local_Drives != null)
            {
                for (int i = 0; i < Local_Drives.Length; i++)
                {
                    if (path.Contains(Local_Drives[i]))
                    {
                        local = true;
                        break;
                    }
                }
            }
            else
            {
                local = true;
            }
            if (!local)
            {
                if (File.Exists(path))
                    Safe_Delete_To_Recycle_Bin(Safe_move_file_to_directory(path, "C:\\Temp", true, false, false, true));

            }
            bool deleted = false;
            while (!deleted)
            {
                try
                {
                    if (use_recyclebin)
                    {
                        try
                        {
                            if (local)
                                DeleteFileToRecycleBin(path);
                            else
                            {
                                if (File.Exists(path))
                                    File.Delete(path);
                            }
                        }
                        catch
                        {
                            DeleteFileToRecycleBin(Safe_Copy(path, "c:\\Temp", true, false, false, true));
                            File.Delete(path);
                        }
                    }
                    else
                    {
                        File.Delete(path);
                    }
                    deleted = true;
                    return true;
                }
                catch (Exception ex)
                {
                    if (!suppress_warnings)
                    {
                        if (MessageBox.Show("Failed to delete file: \"" + path + "\"\r\nDo you wish to try again?\r\n" + ex.Message, "", MessageBoxButtons.YesNo) == DialogResult.No)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                }

            }
            return true;
        }

        public static void openWebPage(string Webpage, string Browser = @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe")
        {
            Process proc = new Process();
            if (Browser.Contains(" "))
            {
                proc.StartInfo.FileName = "\"" + Browser + "\"";
            }
            else
            {
                proc.StartInfo.FileName = Browser;
            }
            proc.StartInfo.Arguments = Webpage;
            proc.Start();
        }

        public static string convert_To_Month_Name(int Month)
        {
            switch (Month)
            {
                case 1:
                    return "January";
                case 2:
                    return "February";
                case 3:
                    return "March";
                case 4:
                    return "April";
                case 5:
                    return "May";
                case 6:
                    return "June";
                case 7:
                    return "July";
                case 8:
                    return "August";
                case 9:
                    return "September";
                case 10:
                    return "October";
                case 11:
                    return "November";
                case 12:
                    return "December";
            }
            return "Unknown";
        }

        public static string Get_New_Destination_Path(string current_file_path, string destination_folder)
        {
            string file_name = current_file_path.Substring(current_file_path.LastIndexOf('\\') + 1);
            if (!destination_folder.EndsWith("\\"))
            {
                destination_folder += "\\";
            }
            return destination_folder + file_name;
        }
        public static string Safe_Copy_To_Folder(string origin_file_Path, string destination_folder, bool overwrite, bool allow_renaming, bool recycle, bool suppress_warnings)
        {
            string File_Name = origin_file_Path;
            if (File_Name.Contains('\\'))
                File_Name = File_Name.Substring(File_Name.LastIndexOf('\\'));
            else
                File_Name = '\\' + File_Name;
            string destination_File_Path = destination_folder + File_Name;
            if (!Directory.Exists(destination_folder))
                Directory.CreateDirectory(destination_folder);
            return Safe_Copy(origin_file_Path, destination_File_Path, overwrite, allow_renaming, recycle, suppress_warnings);
        }
        public static string Safe_Copy(string origin_file_Path, string destination_file_path, bool overwrite, bool allow_renaming, bool recycle, bool suppress_warnings)
        {
            if (origin_file_Path == destination_file_path)
                return "SAME_LOCATION";
            bool Copied = false;
            string Destination_Directory;
            if (destination_file_path.Contains('\\'))
            {
                Destination_Directory = destination_file_path.Substring(0, destination_file_path.LastIndexOf('\\'));
            }
            else
            {
                log_Occured?.Invoke(null, new Log_Event("Invalid log directory given \"" + destination_file_path + "\"", Log_Event.LogType.ERROR));
                return "Canceled";
            }
            if (!Directory.Exists(Destination_Directory))
                Directory.CreateDirectory(Destination_Directory);
            string orig_Dest_Path = destination_file_path;
            destination_file_path = Check_And_Get_Destination_Path(origin_file_Path, destination_file_path, overwrite, recycle, allow_renaming, suppress_warnings, true);
            if (destination_file_path == "Canceled")
            {
                log_Occured?.Invoke(null, new Log_Event("Copy of \"" + origin_file_Path + "\" canceled by user!", Log_Event.LogType.LOG));
                return "Canceled";
            }
            if (destination_file_path == "MATCH")
            {
                //  log_Occured?.Invoke(null, new Log_Event("File \"" + origin_file_Path + "\" alread exists at \""+destination_file_path+"\"", Log_Event.LogType.LOG));
                return orig_Dest_Path;
            }
            if (destination_file_path == "OCRD")
            {
                return orig_Dest_Path;
            }
            Copied = false;
            while (!Copied)
            {
                try
                {
                    File.Copy(origin_file_Path, destination_file_path);
                    Copied = true;
                    return destination_file_path;
                }
                catch (Exception ex)
                {
                    if (!suppress_warnings)
                    {
                        if (MessageBox.Show("Unable to copy the file \"" + origin_file_Path + "\" to \"" + destination_file_path + "\"\r\n" + ex.Message + "\r\nDo you wish to try again?", "", MessageBoxButtons.YesNo) == DialogResult.No)
                        {
                            log_Occured?.Invoke(null, new Log_Event("Unable to copy the file \"" + origin_file_Path + "\" to \"" + destination_file_path + "\" Error: " + ex.Message, Log_Event.LogType.ERROR));
                            Copied = true;
                        }
                    }
                    else
                    {

                        Copied = true;
                    }
                }
            }
            return "Canceled";
        }

        public static string INT_TO_MONTH_STR(int month)
        {
            switch (month)
            {
                case 1:
                    return "January";
                case 2:
                    return "February";
                case 3:
                    return "March";
                case 4:
                    return "April";
                case 5:
                    return "May";
                case 6:
                    return "June";
                case 7:
                    return "July";
                case 8:
                    return "August";
                case 9:
                    return "September";
                case 10:
                    return "October";
                case 11:
                    return "November";
                case 12:
                    return "December";
                default:
                    return "Unknown";
            }
        }

        private static string Check_And_Get_Destination_Path(string Source_file_path, string destination_file_path, bool overwrite, bool recycle, bool allow_renaming, bool suppress_warnings, bool copy)
        {
            bool source_OCRd = false;
            bool Destination_OCRd = false;
            string directory = destination_file_path.Substring(0, destination_file_path.LastIndexOf("\\"));
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                return destination_file_path;

            }
            if (File.Exists(destination_file_path))
            {
                if (Use_Beyond_Compare_To_Avoid_Overwrite)
                {
                    if (Beyond_Compare_Com_Path != "")
                    {
                        switch (Beyond_Compare(Source_file_path, destination_file_path, Beyond_Compare_Com_Path))
                        {
                            case File_Comparison_Results.BINARY_SAME:
                                log_Occured?.Invoke(null, new Log_Event("File: \"" + Source_file_path + "\" is already at \"" + destination_file_path + "\" deleting the source file instead", Log_Event.LogType.WARNING));

                                return "MATCH";

                        }
                    }
                    else
                    {
                        log_Occured?.Invoke(null, new Log_Event("Unable to use beyond compare since the beyond compare path was not set!", Log_Event.LogType.ERROR));
                    }
                }
                //hmm need to figure out if one has been OCRd and one has not?
                source_OCRd = Has_Been_OCRd(Source_file_path);
                Destination_OCRd = Has_Been_OCRd(destination_file_path);
                if (Destination_OCRd)
                {
                    if (!source_OCRd)
                    {
                        if (!copy)
                        {
                            if (recycle)
                            {

                                DeleteFileToRecycleBin(Source_file_path);
                                log_Occured?.Invoke(null, new Log_Event("\"" + destination_file_path + "\" is OCRd version of \"" + Source_file_path + "\" which not OCRd, deleted \"" + Source_file_path + "\"", Log_Event.LogType.WARNING));
                                return "OCRD";
                            }
                            else
                            {
                                File.Delete(Source_file_path);
                                log_Occured?.Invoke(null, new Log_Event("\"" + destination_file_path + "\" is OCRd version of \"" + Source_file_path + "\" which not OCRd, deleted \"" + Source_file_path + "\"", Log_Event.LogType.WARNING));
                                return "OCRD";
                            }
                        }
                    }
                    else//both are OCRD and different
                    {
                        destination_file_path = Handle_Overwrite(Source_file_path, destination_file_path, overwrite, recycle, allow_renaming, suppress_warnings, copy);
                    }
                }
                else//destination is not OCRd
                {
                    if (source_OCRd)
                    {
                        if (recycle)
                        {
                            DeleteFileToRecycleBin(destination_file_path);
                            log_Occured?.Invoke(null, new Log_Event("\"" + Source_file_path + "\" is OCRd version of \"" + destination_file_path + "\" which not OCRd, overwrote \"" + destination_file_path + "\"", Log_Event.LogType.WARNING));
                            return destination_file_path;
                        }
                        else
                        {
                            File.Delete(destination_file_path);
                            log_Occured?.Invoke(null, new Log_Event("\"" + Source_file_path + "\" is OCRd version of \"" + destination_file_path + "\" which not OCRd, overwrote \"" + destination_file_path + "\"", Log_Event.LogType.WARNING));
                            return destination_file_path;
                        }
                    }
                    else//neither are OCRD 
                    {
                        destination_file_path = Handle_Overwrite(Source_file_path, destination_file_path, overwrite, recycle, allow_renaming, suppress_warnings, copy);
                    }
                }

            }
            return destination_file_path;
        }
        private static string Handle_Overwrite(string Source_file_path, string destination_file_path, bool overwrite, bool recycle, bool allow_renaming, bool suppress_warnings, bool copy)
        {
            int rename_count = 0;
            int Max_Tries = 100;
            string destination_path_before_Extension;
            string extension;
            string temp_dest;
            if (destination_file_path == "Canceled")
                return "Canceled";
            if (overwrite)
            {
                if (recycle)
                {
                    log_Occured?.Invoke(null, new Log_Event("Overwrote: \"" + destination_file_path + "\"", Log_Event.LogType.WARNING));
                    DeleteFileToRecycleBin(destination_file_path);
                }
                else
                {
                    log_Occured?.Invoke(null, new Log_Event("Overwrote: \"" + destination_file_path + "\"", Log_Event.LogType.WARNING));
                    File.Delete(destination_file_path);
                }
                return destination_file_path;
            }
            else
            {
                if (MessageBox.Show("The file: \"" + destination_file_path + "\"\r\nAlready exists in the destination location do you wish to overwrite it?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (recycle)
                    {
                        log_Occured?.Invoke(null, new Log_Event("Overwrote: \"" + destination_file_path + "\"", Log_Event.LogType.WARNING));
                        DeleteFileToRecycleBin(destination_file_path);
                    }
                    else
                    {
                        log_Occured?.Invoke(null, new Log_Event("Overwrote: \"" + destination_file_path + "\"", Log_Event.LogType.WARNING));
                        File.Delete(destination_file_path);
                    }
                    return destination_file_path;
                }
                else
                {
                    if (allow_renaming && !suppress_warnings)
                    {
                        string file_Name = Source_file_path.Substring(Source_file_path.LastIndexOf('\\') + 1);
                        extension = file_Name.Substring(file_Name.LastIndexOf('.') + 1);
                        SaveFileDialog diag = new SaveFileDialog();
                        diag.FileName = file_Name + "_" + DateTime.Now.ToShortDateString().Replace('/', '-') + "." + extension;
                        diag.InitialDirectory = destination_file_path.Substring(0, destination_file_path.LastIndexOf('\\'));
                        diag.Filter = "pdf|*." + extension;
                        diag.OverwritePrompt = true;
                        if (diag.ShowDialog() != DialogResult.Cancel)
                        {
                            return diag.FileName;

                        }
                        log_Occured?.Invoke(null, new Log_Event("File: \"" + Source_file_path + "\" is already at \"" + destination_file_path + "\" overwrite canceled by user", Log_Event.LogType.WARNING));
                        return "Canceled";
                    }
                    else
                    {
                        if (!suppress_warnings)
                        {
                            Get_Overwrite_Desire diag = new Get_Overwrite_Desire(destination_file_path, Source_file_path, copy, recycle);
                            if (diag.ShowDialog() != DialogResult.Cancel)
                            {
                                if (diag.Keep_Destination_file)
                                {
                                    if (diag.Keep_Original_File)
                                    {
                                        destination_path_before_Extension = destination_file_path.Substring(0, destination_file_path.LastIndexOf('.'));
                                        extension = Path.GetExtension(destination_file_path);
                                        while (rename_count <= Max_Tries)
                                        {
                                            temp_dest = destination_file_path + "_" + rename_count.ToString();
                                            rename_count++;
                                            if (!File.Exists(temp_dest))
                                            {
                                                return temp_dest;
                                            }
                                        }
                                        return "Canceled";
                                    }
                                    else
                                    {
                                        if (diag.Use_Recycle_bin)
                                        {
                                            Safe_Delete_To_Recycle_Bin(Source_file_path);
                                        }
                                        else
                                        {
                                            Safe_Delete(Source_file_path, suppress_warnings, false);
                                        }
                                        return "Canceled";
                                    }
                                }
                                else
                                {
                                    if (diag.Use_Recycle_bin)
                                    {
                                        Safe_Delete_To_Recycle_Bin(destination_file_path);
                                    }
                                    else
                                    {
                                        Safe_Delete(destination_file_path, suppress_warnings, false);
                                    }
                                    return destination_file_path;
                                }
                            };
                        }
                        log_Occured?.Invoke(null, new Log_Event("File: \"" + Source_file_path + "\" is already at \"" + destination_file_path + "\" overwrite not allowed", Log_Event.LogType.WARNING));
                        return "Canceled";
                    }
                }
            }
        }
        public static DateTime String_to_DateTime_AnyFormat(string datetime)
        {
            string[] formats = {
                "M/d/yyyy",
                "M/dd/yyyy",
                "MM/dd/yyyy",
                "yyyyMMdd",
                "dddd MM/dd/yyyy",
    "MM/dd/yyyy HH:mm:ss",
    "MM/dd/yyyy hh:mm:ss tt",
    "yyyy-MM-ddTHH:mm:ss",
    "dd-MMM-yyyy HH:mm:ss",
    "MMMM dd, yyyy HH:mm:ss"

};
            if (DateTime.TryParseExact(datetime, formats,
    System.Globalization.CultureInfo.InvariantCulture,
    System.Globalization.DateTimeStyles.None, out DateTime parsedDateTime))
            {
                return parsedDateTime;
            }
            else
            {
                return DateTime.MinValue;
            }
        }
        public static DateTime stringToFullDatetime(string datetime, string format = "mm/dd/yyyy HH:MM:SS")
        {

            datetime = datetime.ToUpper().Replace("SS", "00").Replace("MM:","00:").Replace("HH:","00:").Replace("MM/","09/").Replace("DD/","10/").Replace("YYYY","1987");
            //convert ther format to lower case so that it is not case sensitive
            format = format.ToLower();
            //stores the month portion of the string, day portion of the string and the year portion of the string
            string month = "", day = "", year = "";
            //stores the month day and year as numbers
            int m = 0, d = 0, y = 0;
            int H = 0, M = 0, S = 0;
            //stores the starting position of the month, day, and year sections
            int mpos = -1, dpos = -1, ypos = -1;
            //stores the dividing character
            string dividingCharacter = "";
            string date = "";
            string time = "";
            bool PM = false;
            //stores the characters of the date format as an array
            char[] DateFormatCharacters = null;
            //stores the characters of the time format as an array
            char[] TimeFormatCharacters = null;
            if (format.Contains(' ') && datetime.Contains(' '))
            {
                date = datetime.Substring(0, datetime.IndexOf(' '));
                time = datetime.Substring(datetime.IndexOf(' ') + 1);
                if (time.ToUpper().Contains("AM"))
                {
                    PM = false;
                    time = time.ToUpper().Replace("AM", "");
                }
                else
                {
                    if (time.ToUpper().Contains("PM"))
                    {
                        PM = true;
                        time = time.ToUpper().Replace("PM", "");
                    }
                }

                DateFormatCharacters = format.Substring(0, format.IndexOf(' ')).ToArray();
                TimeFormatCharacters = format.Substring(format.IndexOf(' ') + 1).ToArray();

            }
            else
            {
                format.ToArray();
            }

            //stores the characters of the date as an array
            char[] dateCharacters = date.ToArray();
            if (date.StartsWith("Monday,") || date.StartsWith("Tuesday,") || date.StartsWith("Wednesday,") || date.StartsWith("Thursday,") || date.StartsWith("Friday,") || date.StartsWith("Saturday,") || date.StartsWith("Sunday,"))
            {
                date = date.Substring(date.IndexOf(",") + 1).Trim();
                month = date.Substring(0, date.IndexOf(' '));
                switch (month.ToUpper())
                {
                    case "JANUARY":
                        m = 1;
                        break;
                    case "FEBRUARY":
                        m = 2;
                        break;
                    case "MARCH":
                        m = 3;
                        break;
                    case "APRIL":
                        m = 4;
                        break;
                    case "MAY":
                        m = 5;
                        break;
                    case "JUNE":
                        m = 6;
                        break;
                    case "JULY":
                        m = 7;
                        break;
                    case "AUGUST":
                        m = 8;
                        break;
                    case "SEPTEMBER":
                        m = 9;
                        break;
                    case "OCTOBER":
                        m = 10;
                        break;
                    case "NOVEMBER":
                        m = 11;
                        break;
                    case "DECEMBER":
                        m = 12;
                        break;
                }
                day = date.Substring(date.IndexOf(' ')).Trim();
                day = day.Substring(0, day.IndexOf(','));
                d = int.Parse(day);
                date = date.Substring(date.IndexOf(',') + 1).Trim();
                y = int.Parse(date);
                return new DateTime(y, m, d);
            }
            //parse through the format
            for (int i = 0; i < DateFormatCharacters.Length; i++)
            {
                //look at the format character and see if it is a month,day,year, or a dividing character
                switch (DateFormatCharacters[i])
                {
                    case 'm'://it is a month
                        //store the position of the first month character
                        if (mpos == -1)
                            mpos = i;
                        break;
                    case 'd'://it is a day
                        //store the position of the first day character
                        if (dpos == -1)
                            dpos = i;
                        break;
                    case 'y'://it is a year
                        //store the position of the first year character
                        if (ypos == -1)
                            ypos = i;
                        break;
                    default://it is a dividing character sequence
                        //add it to the dividing character
                        dividingCharacter += DateFormatCharacters[i];
                        break;
                }
            }
            //if the dividing character lenght is even then that means it is the same between month and day as it is
            //between day and year and it is a single character
            if (dividingCharacter.Length % 2 == 0)
            {
                //if it is a two / then it the divider is / however one between month and day and another between
                //day and year so set the character to one /
                if (dividingCharacter == "//")
                    dividingCharacter = "/";
                //if it is a two - then it the divider is - however one between month and day and another between
                //day and year so set the character to one -
                if (dividingCharacter == "--")
                    dividingCharacter = "-";

            }
            //if the dividing character exists and it is a single character
            if (dividingCharacter != "" && dividingCharacter.Length == 1)
            {
                //split the date string into parts using that dividing character
                string[] parts = date.Split(dividingCharacter[0]);
                //compute which part is which using the starting positions of the month, day and year
                //if there is a year
                if (ypos != -1)
                {
                    //if there is a day
                    if (dpos != -1)
                    {
                        //if there is a month
                        if (mpos != -1)
                        {
                            //if the month is less than the day and less than the year then it is the first part
                            if (mpos < dpos && mpos < ypos)
                            {
                                mpos = 0;
                                //if the day part is before the year then set the day as the second part and the year
                                //as the third part
                                if (dpos < ypos)
                                {
                                    dpos = 1;
                                    ypos = 2;
                                }
                                else//if the day is after the year then set the year to the second part and the
                                    //day to the third part
                                {
                                    ypos = 1;
                                    dpos = 2;
                                }

                            }
                            else
                            {
                                if (dpos < mpos && dpos < ypos)
                                {
                                    dpos = 0;
                                    if (mpos < ypos)
                                    {
                                        mpos = 1;
                                        ypos = 2;
                                    }
                                    else
                                    {
                                        ypos = 1;
                                        mpos = 2;
                                    }
                                }
                                else
                                {
                                    ypos = 0;
                                    if (mpos < dpos)
                                    {
                                        mpos = 1;
                                        dpos = 2;
                                    }
                                    else
                                    {
                                        dpos = 1;
                                        mpos = 2;
                                    }
                                }
                            }
                        }
                        else //if there is no month
                        {
                            //if there is a day
                            if (dpos != -1)
                            {
                                //if the day is before the year position then set the day as the first part and the 
                                //year as the second part
                                if (dpos < ypos)
                                {
                                    dpos = 0;
                                    ypos = 1;
                                }
                                else//if the day is after the year position then set the year as the first part and 
                                {//the day as the second part
                                    ypos = 0;
                                    dpos = 1;
                                }
                            }
                            else//if there is no month and no day then set the year to the first and only part
                                ypos = 0;
                        }
                    }
                    else//if there is no day
                    {
                        if (mpos != -1)//if there is a month
                        {
                            //if the month is before the year then set the month as the first part and the year as
                            //the second part
                            if (mpos < ypos)
                            {
                                mpos = 0;
                                ypos = 1;
                            }
                            else//if the year is before the month then set the year as the first part and the month
                            {//as the second part
                                ypos = 0;
                                mpos = 1;
                            }
                        }
                        else//if there is no month and no day
                        {
                            if (ypos != -1)
                            {
                                //set the year as the first and only part
                                ypos = 0;
                            }
                            else//if there is no year day or month then throw an exception because the format given
                            {//was bad
                                throw new Exception("No month, day, or year found in format!");
                            }
                        }
                    }
                }
                else//if there is no year in the format
                {
                    if (mpos != -1)//if there is a month
                    {
                        if (dpos != -1)//if there is a day
                        {
                            if (mpos < dpos)//if the month is before the day then set the month as the first part
                            {//and the day as the second part
                                mpos = 0;
                                dpos = 1;
                            }
                            else//if the month is after the day then store the day as the first part and the month
                            {//as the second part
                                dpos = 0;
                                mpos = 1;
                            }
                        }
                        else//if there is no day, and no year given
                        {
                            if (mpos != -1)//if there is a month then set it as the first and only part
                            {
                                mpos = 0;
                            }
                            else//if there is no month, day, or year given then throw an exception
                            {
                                throw new Exception("No month, day, or year found in format!");
                            }
                        }
                    }
                    else //if there is no month, and no year given
                    {
                        if (dpos != -1)//if there is a day given then set it as the first and only part
                        {
                            dpos = 0;
                        }
                        else//if no month,day, or year was given then throw an exception
                        {
                            throw new Exception("No month, day, or year found in format!");
                        }
                    }


                }
                #region get the month day and year
                if (mpos != -1)//if there is a month then get the month part of the date
                {
                    switch (parts[mpos].ToUpper())
                    {
                        case "JAN":
                            m = 1;
                            break;
                        case "FEB":
                            m = 2;
                            break;
                        case "MAR":
                            m = 3;
                            break;
                        case "APR":
                            m = 4;
                            break;
                        case "MAY":
                            m = 5;
                            break;
                        case "JUN":
                            m = 6;
                            break;
                        case "JUL":
                            m = 7;
                            break;
                        case "AUG":
                            m = 8;
                            break;
                        case "SEP":
                            m = 9;
                            break;
                        case "OCT":
                            m = 10;
                            break;
                        case "NOV":
                            m = 11;
                            break;
                        case "DEC":
                            m = 12;
                            break;
                        default:
                            m = int.Parse(parts[mpos]);
                            break;
                    }

                }
                else//if there is no month then set it to January
                    m = 1;
                if (dpos != -1)//if there is a day then get the day part of the date
                {
                    d = int.Parse(parts[dpos]);
                }
                else//if there is not day part then set it to the first
                    d = 1;
                if (ypos != -1)//if there is a year part then get the year
                {
                    y = int.Parse(parts[ypos]);
                }
                else//if there is no year then set it to the current year
                    y = DateTime.Now.Year;
                #endregion

            }
            else//the dividing character is more than one character long or non-existant
            {
                //if there is no dividing character
                if (dividingCharacter == "")
                {
                    //read the month part, day part, and year part from the date in the same order as the format 
                    //string
                    for (int i = 0; i < DateFormatCharacters.Length; i++)
                    {
                        switch (DateFormatCharacters[i])
                        {
                            case 'm':
                                month += dateCharacters[i];
                                break;
                            case 'd':
                                day += dateCharacters[i];
                                break;
                            case 'y':
                                year += dateCharacters[i];
                                break;

                        }
                    }
                }
                else//if there is more than one dividing character in the format string then throw an error
                {
                    throw new Exception("multiple dividing characters found");
                }
                //if there is no month then set the month to January
                if (month == "")
                    m = 1;
                else//if there is a month then parse the month string
                    m = int.Parse(month);
                //if there is no day then set the day to first
                if (day == "")
                    d = 1;
                else//if there is a day then parse the day
                    d = int.Parse(day);
                //if there is no year then set the year to this year
                if (year == "")
                    y = DateTime.Now.Year;
                else//if there is a year then parse the year
                    y = int.Parse(year);
            }

            //if the year is less than 100 ie a two character abbreviation like /19 for /2019 
            //then add 2000 to it to make it the actual year, note there is a Y2K style bug here.
            if (y < 100)
            {
                y += 2000;
            }
            if (TimeFormatCharacters != null)
            {
                int hpos = -1, minpos = -1, spos = -1;
                string div = "";
                for (int i = 0; i < TimeFormatCharacters.Length; i++)
                {
                    switch (TimeFormatCharacters[i].ToString().ToUpper())
                    {
                        case "H":
                            if (hpos == -1)
                                hpos = i;
                            break;
                        case "M":
                            if (minpos == -1)
                                minpos = i;
                            break;
                        case "S":
                            if (spos == -1)
                            {
                                spos = i;
                            }
                            break;
                        default:
                            div += TimeFormatCharacters[i];
                            break;
                    }

                }
                if (div.Length == 2)
                {
                    if (div[0] == div[1])
                    {
                        div = div[0].ToString();
                    }
                }
                if (hpos < minpos && hpos < spos)
                {
                    hpos = 0;
                    if (minpos < spos)
                    {
                        minpos = 1;
                        spos = 2;
                    }
                    else
                    {
                        spos = 1;
                        minpos = 2;
                    }
                }
                else
                {
                    if (minpos < hpos && minpos < spos)
                    {
                        minpos = 0;
                        if (hpos < spos)
                        {
                            hpos = 1;
                            spos = 2;
                        }
                        else
                        {
                            hpos = 2;
                            spos = 1;
                        }
                    }
                    else
                    {
                        if (spos < hpos && spos < minpos)
                        {
                            spos = 0;
                            if (minpos < hpos)
                            {
                                minpos = 1;
                                hpos = 2;
                            }
                            else
                            {
                                minpos = 2;
                                hpos = 1;
                            }
                        }
                    }

                }
                string[] parts = time.Split(div[0]);
                if (parts.Length < 3)
                {
                    time += div[0] + "0";
                    parts = time.Split(div[0]);
                }

                if (parts.Length < 3)
                {
                    time += div[0] + "0";
                    parts = time.Split(div[0]);
                }

                H = int.Parse(parts[hpos]);
                M = int.Parse(parts[minpos]);
                S = int.Parse(parts[spos]);
                if (!PM)
                {
                    if (H == 12)
                        H = 0;
                }
                if (PM)
                {
                    if (H != 12)
                        H += 12;
                }


                return new DateTime(y, m, d, H, M, S);

            }
            //create the date time object from the date calculated
            return new DateTime(y, m, d);
        } //*/

        public static void OpenNotePadPP(string File_Path)
        {
            if (Files_Opened_In_NotepadPP.Contains(File_Path))
                return;
            if (File_Path.Contains(" "))
                File_Path = "\"" + File_Path + "\"";
            Process temp = new Process();
            temp.StartInfo.FileName = "\"C:\\Program Files\\Notepad++\\notepad++.exe\"";
            temp.StartInfo.Arguments = File_Path;
            temp.StartInfo.CreateNoWindow = true;
            Files_Opened_In_NotepadPP.AddLast(File_Path);
            temp.Start();
        }

        /// <summary>
        /// This class converts a date into a date time object
        /// </summary>
        /// <param name="date">date string</param>
        /// <param name="format">format of the date default is MM/DD/YYYY</param>
        /// <returns>date as a DateTime Object</returns>
        public static DateTime stringToDatetime(string date, string format = "mm/dd/yyyy")
        {

       //     Write_To_Debug_Window(date);
            //convert ther format to lower case so that it is not case sensitive
            format = format.ToLower();
            //stores the month portion of the string, day portion of the string and the year portion of the string
            string month = "", day = "", year = "";
            //stores the month day and year as numbers
            int m = 0, d = 0, y = 0;
            //stores the starting position of the month, day, and year sections
            int mpos = -1, dpos = -1, ypos = -1;
            //stores the dividing character
            string dividingCharacter = "";
            //stores the characters of the date as an array
            char[] dateCharacters = date.ToArray();
            //stores the characters of the date format as an array
            char[] formatCharacters = format.ToArray();
            month = date.ToUpper();
            if (month.StartsWith("JAN") || month.StartsWith("ENE") || month.StartsWith("FEB") || month.StartsWith("MAR") || month.StartsWith("APR") || month.StartsWith("ABR") || month.StartsWith("MAY") || month.StartsWith("JUN") || month.StartsWith("JUL") || month.StartsWith("AGO") || month.StartsWith("AUG") || month.StartsWith("SEP") || month.StartsWith("OCT") || month.StartsWith("NOV") || month.StartsWith("DIC") || month.StartsWith("DEC"))
            {
                if (month.Contains(" "))
                {
                    if (month.Contains(","))
                    {
                        month = month.Substring(0, month.IndexOf(" "));
                        switch (month)
                        {
                            case "JANUARY":
                                m = 1;
                                break;
                            case "FEBRUARY":
                                m = 2;
                                break;
                            case "MARCH":
                                m = 3;
                                break;
                            case "APRIL":
                                m = 4;
                                break;
                            case "MAY":
                                m = 5;
                                break;
                            case "JUNE":
                                m = 6;
                                break;
                            case "JULY":
                                m = 7;
                                break;
                            case "AUGUST":
                                m = 8;
                                break;
                            case "SEPTEMBER":
                                m = 9;
                                break;
                            case "OCTOBER":
                                m = 10;
                                break;
                            case "NOVEMBER":
                                m = 11;
                                break;
                            case "DECEMBER":
                                m = 12;
                                break;
                        }
                        day = date.Substring(date.IndexOf(' ')).Trim();
                        day = day.Substring(0, day.IndexOf(','));
                        d = int.Parse(day);
                        date = date.Substring(date.IndexOf(',') + 1).Trim();
                        y = int.Parse(date);
                        return new DateTime(y, m, d);
                    }
                }
            }
            else
            {
                if (date.StartsWith("Monday,") || date.StartsWith("Tuesday,") || date.StartsWith("Wednesday,") || date.StartsWith("Thursday,") || date.StartsWith("Friday,") || date.StartsWith("Saturday,") || date.StartsWith("Sunday,"))
                {
                    date = date.Substring(date.IndexOf(",") + 1).Trim();
                    month = date.Substring(0, date.IndexOf(' '));
                    switch (month.ToUpper())
                    {
                        case "JANUARY":
                            m = 1;
                            break;
                        case "FEBRUARY":
                            m = 2;
                            break;
                        case "MARCH":
                            m = 3;
                            break;
                        case "APRIL":
                            m = 4;
                            break;
                        case "MAY":
                            m = 5;
                            break;
                        case "JUNE":
                            m = 6;
                            break;
                        case "JULY":
                            m = 7;
                            break;
                        case "AUGUST":
                            m = 8;
                            break;
                        case "SEPTEMBER":
                            m = 9;
                            break;
                        case "OCTOBER":
                            m = 10;
                            break;
                        case "NOVEMBER":
                            m = 11;
                            break;
                        case "DECEMBER":
                            m = 12;
                            break;
                    }
                    day = date.Substring(date.IndexOf(' ')).Trim();
                    day = day.Substring(0, day.IndexOf(','));
                    d = int.Parse(day);
                    date = date.Substring(date.IndexOf(',') + 1).Trim();
                    y = int.Parse(date);
                    return new DateTime(y, m, d);


                }
            }
            //parse through the format
            for (int i = 0; i < formatCharacters.Length; i++)
            {
                //look at the format character and see if it is a month,day,year, or a dividing character
                switch (formatCharacters[i])
                {
                    case 'm'://it is a month
                        //store the position of the first month character
                        if (mpos == -1)
                            mpos = i;
                        break;
                    case 'd'://it is a day
                        //store the position of the first day character
                        if (dpos == -1)
                            dpos = i;
                        break;
                    case 'y'://it is a year
                        //store the position of the first year character
                        if (ypos == -1)
                            ypos = i;
                        break;
                    default://it is a dividing character sequence
                        //add it to the dividing character
                        dividingCharacter += formatCharacters[i];
                        break;
                }
            }
            //if the dividing character lenght is even then that means it is the same between month and day as it is
            //between day and year and it is a single character
            if (dividingCharacter.Length % 2 == 0)
            {
                //if it is a two / then it the divider is / however one between month and day and another between
                //day and year so set the character to one /
                if (dividingCharacter == "//")
                    dividingCharacter = "/";
                //if it is a two - then it the divider is - however one between month and day and another between
                //day and year so set the character to one -
                if (dividingCharacter == "--")
                    dividingCharacter = "-";

            }
            //if the dividing character exists and it is a single character
            if (dividingCharacter != "" && dividingCharacter.Length == 1)
            {
                //split the date string into parts using that dividing character
                string[] parts = date.Split(dividingCharacter[0]);
                //compute which part is which using the starting positions of the month, day and year
                //if there is a year
                if (ypos != -1)
                {
                    //if there is a day
                    if (dpos != -1)
                    {
                        //if there is a month
                        if (mpos != -1)
                        {
                            //if the month is less than the day and less than the year then it is the first part
                            if (mpos < dpos && mpos < ypos)
                            {
                                mpos = 0;
                                //if the day part is before the year then set the day as the second part and the year
                                //as the third part
                                if (dpos < ypos)
                                {
                                    dpos = 1;
                                    ypos = 2;
                                }
                                else//if the day is after the year then set the year to the second part and the
                                    //day to the third part
                                {
                                    ypos = 1;
                                    dpos = 2;
                                }

                            }
                            else
                            {
                                if (dpos < mpos && dpos < ypos)
                                {
                                    dpos = 0;
                                    if (mpos < ypos)
                                    {
                                        mpos = 1;
                                        ypos = 2;
                                    }
                                    else
                                    {
                                        ypos = 1;
                                        mpos = 2;
                                    }
                                }
                                else
                                {
                                    ypos = 0;
                                    if (mpos < dpos)
                                    {
                                        mpos = 1;
                                        dpos = 2;
                                    }
                                    else
                                    {
                                        dpos = 1;
                                        mpos = 2;
                                    }
                                }
                            }
                        }
                        else //if there is no month
                        {
                            //if there is a day
                            if (dpos != -1)
                            {
                                //if the day is before the year position then set the day as the first part and the 
                                //year as the second part
                                if (dpos < ypos)
                                {
                                    dpos = 0;
                                    ypos = 1;
                                }
                                else//if the day is after the year position then set the year as the first part and 
                                {//the day as the second part
                                    ypos = 0;
                                    dpos = 1;
                                }
                            }
                            else//if there is no month and no day then set the year to the first and only part
                                ypos = 0;
                        }
                    }
                    else//if there is no day
                    {
                        if (mpos != -1)//if there is a month
                        {
                            //if the month is before the year then set the month as the first part and the year as
                            //the second part
                            if (mpos < ypos)
                            {
                                mpos = 0;
                                ypos = 1;
                            }
                            else//if the year is before the month then set the year as the first part and the month
                            {//as the second part
                                ypos = 0;
                                mpos = 1;
                            }
                        }
                        else//if there is no month and no day
                        {
                            if (ypos != -1)
                            {
                                //set the year as the first and only part
                                ypos = 0;
                            }
                            else//if there is no year day or month then throw an exception because the format given
                            {//was bad
                                throw new Exception("No month, day, or year found in format!");
                            }
                        }
                    }
                }
                else//if there is no year in the format
                {
                    if (mpos != -1)//if there is a month
                    {
                        if (dpos != -1)//if there is a day
                        {
                            if (mpos < dpos)//if the month is before the day then set the month as the first part
                            {//and the day as the second part
                                mpos = 0;
                                dpos = 1;
                            }
                            else//if the month is after the day then store the day as the first part and the month
                            {//as the second part
                                dpos = 0;
                                mpos = 1;
                            }
                        }
                        else//if there is no day, and no year given
                        {
                            if (mpos != -1)//if there is a month then set it as the first and only part
                            {
                                mpos = 0;
                            }
                            else//if there is no month, day, or year given then throw an exception
                            {
                                throw new Exception("No month, day, or year found in format!");
                            }
                        }
                    }
                    else //if there is no month, and no year given
                    {
                        if (dpos != -1)//if there is a day given then set it as the first and only part
                        {
                            dpos = 0;
                        }
                        else//if no month,day, or year was given then throw an exception
                        {
                            throw new Exception("No month, day, or year found in format!");
                        }
                    }


                }
                #region get the month day and year
                if (mpos != -1)//if there is a month then get the month part of the date
                {
                    switch (parts[mpos].ToUpper())
                    {
                        case "ENE":
                        case "JAN":
                            m = 1;
                            break;
                        case "FEB":
                            m = 2;
                            break;
                        case "MAR":
                            m = 3;
                            break;
                        case "ABR":
                        case "APR":
                            m = 4;
                            break;
                        case "MAY":
                            m = 5;
                            break;
                        case "JUN":
                            m = 6;
                            break;
                        case "JUL":
                            m = 7;
                            break;
                        case "AGO":
                        case "AUG":
                            m = 8;
                            break;
                        case "SEP":
                            m = 9;
                            break;
                        case "OCT":
                            m = 10;
                            break;
                        case "NOV":
                            m = 11;
                            break;
                        case "DIC":
                        case "DEC":
                            m = 12;
                            break;
                        default:
                            try
                            {
                                m = int.Parse(parts[mpos]);
                            }
                            catch
                            {
                                CommonCore.Write_To_Debug_Window(date);

                            }
                            break;
                    }

                }
                else//if there is no month then set it to January
                    m = 1;
                if (dpos != -1)//if there is a day then get the day part of the date
                {
                    d = int.Parse(parts[dpos]);
                }
                else//if there is not day part then set it to the first
                    d = 1;
                if (ypos != -1)//if there is a year part then get the year
                {
                    y = int.Parse(parts[ypos]);
                }
                else//if there is no year then set it to the current year
                    y = DateTime.Now.Year;
                #endregion

            }
            else//the dividing character is more than one character long or non-existant
            {
                //if there is no dividing character
                if (dividingCharacter == "")
                {
                    month = "";
                    day = "";
                    year = "";
                    //read the month part, day part, and year part from the date in the same order as the format 
                    //string
                    for (int i = 0; i < formatCharacters.Length; i++)
                    {
                        switch (formatCharacters[i])
                        {
                            case 'm':
                                month += dateCharacters[i];
                                break;
                            case 'd':
                                day += dateCharacters[i];
                                break;
                            case 'y':
                                year += dateCharacters[i];
                                break;

                        }
                    }
                }
                else//if there is more than one dividing character in the format string then throw an error
                {
                    throw new Exception("multiple dividing characters found");
                }
                //if there is no month then set the month to January
                if (month == "")
                    m = 1;
                else//if there is a month then parse the month string
                    m = int.Parse(month);
                //if there is no day then set the day to first
                if (day == "")
                    d = 1;
                else//if there is a day then parse the day
                    d = int.Parse(day);
                //if there is no year then set the year to this year
                if (year == "")
                    y = DateTime.Now.Year;
                else//if there is a year then parse the year
                    y = int.Parse(year);
            }

            //if the year is less than 100 ie a two character abbreviation like /19 for /2019 
            //then add 2000 to it to make it the actual year, note there is a Y2K style bug here.
            if (y < 100)
            {
                y += 2000;
            }
            //create the date time object from the date calculated
            return new DateTime(y, m, d);
        }
        /// <summary>
        /// converts a date time object into a MMDDYYYY string that is what DEJA expects
        /// </summary>
        /// <param name="date">DateTime date</param>
        /// <returns>MMDDYYYY string</returns>
        public static string CONVERT_DATE_TO_DEJA_ENTERABLE_DATE(DateTime date)
        {
            string output = "";
            if (date.Month < 10)
                output += "0";
            output += date.Month;
            if (date.Day < 10)
                output += "0";
            output += date.Day;
            output += date.Year;
            return output;
        }
        /// <summary>
        /// returns the date time as a date time string MM/DD/YYYY HH:MM:SS
        /// example February 2nd 2022 at 1:35:16 PM is returned as 02/02/2022 13:35:16
        /// </summary>
        /// <param name="time">date time</param>
        /// <returns>date time string</returns>
        public static string DateTime_to_Date_Time_String(DateTime time)
        {
            string output = "";
            if (time.Month < 10)
                output += "0";
            output += time.Month;
            output += "/";
            if (time.Day < 10)
                output += "0";
            output += time.Day;
            output += "/";
            output += time.Year;
            output += " ";

            if (time.Hour < 10)
                output += "0";
            output += time.Hour;
            output += ":";
            if (time.Minute < 10)
                output += "0";
            output += time.Minute;
            output += ":";
            if (time.Second < 10)
                output += "0";
            output += time.Second;
            return output;
        }
        public static DateTime String_To_Time(string timestring)
        {
            DateTime output;
            int hours = 0, minutes = 0, seconds = 0, miliseconds = 0;
            bool PM = false;
            if (timestring.ToUpper().Contains("PM") || timestring.ToUpper().Contains("AM"))
            {
                if (timestring.ToUpper().Contains("PM"))
                    PM = true;
                timestring = timestring.Replace("AM", "").Replace("PM", "").Trim();
            }
            hours = int.Parse(timestring.Substring(0, timestring.IndexOf(":")));
            timestring = timestring.Substring(timestring.IndexOf(":") + 1);
            if (timestring.Contains(":"))
            {
                minutes = int.Parse(timestring.Substring(0, timestring.IndexOf(":")));
                timestring = timestring.Substring(timestring.IndexOf(":") + 1);
                if (timestring.Contains(":"))
                {
                    seconds = int.Parse(timestring.Substring(0, timestring.IndexOf(":")));
                    timestring = timestring.Substring(timestring.IndexOf(":") + 1);
                    miliseconds = int.Parse(timestring);
                }
                else
                {
                    seconds = int.Parse(timestring);
                }
            }
            else
            {
                minutes = int.Parse(timestring);
            }
            if (hours == 12)
                hours = 0;
            if (PM)
                hours += 12;

            output = new DateTime(1987, 9, 10, hours, minutes, seconds, miliseconds);
            return output;
        }
        public static string DateTime_To_Time_String(DateTime time, bool AM_PM_Format = false, bool Include_Miliseconds = false)
        {
            string output = "";
            if (AM_PM_Format)
            {
                if (Include_Miliseconds)
                {
                    output = time.ToString("hh:mm:ss:fff tt");
                }
                else
                {
                    output = time.ToString("hh:mm:ss tt");
                }
            }
            else
            {
                if (Include_Miliseconds)
                {
                    output = time.ToString("HH:mm:ss:fff");
                }
                else
                {
                    output = time.ToString("HH:mm:ss");
                }
            }

            return output;
        }
        /// <summary>
        /// Opens windows explorer and highlights the given file
        /// It does not open the file
        /// </summary>
        /// <param name="FilePath">File to be shown</param>
        public static void ShowFileInExplorer(string FilePath)
        {
            //run the explorer process and pass it the command arguement /select,"file path"
            Process diagnosticProcess = new Process();
            diagnosticProcess.StartInfo.FileName = "Explorer.exe";
            diagnosticProcess.StartInfo.Arguments = "/select,\"" + FilePath + "\"";
            diagnosticProcess.Start();
            diagnosticProcess.WaitForExit();
        }
        /// <summary>
        /// opens the file given if it exists
        /// </summary>
        /// <param name="path">file to open</param>
        /// <returns>true if it exists and false if it does not</returns>
        public static bool OpenFile(string path)
        {

            //check to see if the file exists
            if (File.Exists(path))
            {
                /*  if(path.Contains(" "))
                  {
                      path = "\"" + path + "\"";
                  }*/
                //launch a process of just the file
                try
                {
                    Process Proc = new Process();
                    Proc.StartInfo.FileName = path;
                    Proc.Start();
                }
                catch
                {
                    if (path.Contains(" "))
                        path = "\"" + path + "\"";
                    Process Proc = new Process();
                    Proc.StartInfo.FileName = "CMD";
                    Proc.StartInfo.RedirectStandardInput = true;
                    Proc.Start();
                    StreamWriter stdin = Proc.StandardInput;
                    stdin.WriteLine(path);
                    stdin.Flush();
                    stdin.WriteLine("exit");
                    stdin.Flush();

                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// opens the folder given if it exists
        /// </summary>
        /// <param name="directory">folder to open in explorer</param>
        /// <returns>true if the directory exists and false otherwise</returns>
        public static bool OpenFolder(string directory)
        {
            if (Directory.Exists(directory))
            {
                if (directory.Contains(' '))
                    directory = "\"" + directory + "\"";

                //launch a process of just the file
                Process Proc = new Process();
                Proc.StartInfo.FileName = "Explorer";
                Proc.StartInfo.Arguments = directory;
                Proc.Start();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Converts a date time to a string in the form of mm/dd/yyyy
        /// </summary>
        /// <param name="date">Date to be written</param>
        /// <returns>date in the form of mm/dd/yyyy where m is a digit of the month, d is a digit of the day, and y is a digit of the year </returns>
        public static string DateTimeToString(DateTime date)
        {
            string output = "";
            if (date.Month < 10)
                output = "0";
            output += date.Month + "/";
            if (date.Day < 10)
                output += "0";
            output += date.Day + "/";
            output += date.Year;
            return output;

        }
        /// <summary>
        /// Prints a pdf
        /// </summary>
        /// <param name="path"> path to the PDF</param>
        /// <param name="printer_Name">Name of the printer to print to</param>
        public static void Print_File(string path, string printer_Name = "Brother MFC-L5900DW BR-Script3")
        {
            Process proc = new Process();
            proc.StartInfo.FileName = path;
            proc.StartInfo.Verb = "printto";
            proc.StartInfo.Arguments = "\"" + printer_Name + "\"";
            proc.Start();
            proc.WaitForInputIdle();
            proc.Kill();
        }
        public static TimeSpan Flattened_TimeSpan_String_To_TimeSpan(string span)
        {
            if (span.ToUpper() == "D:HH:MM:SS")
                return TimeSpan.Zero;
            try
            {
                int D = 0, H = 0, M = 0, S = 0;
                string[] parts = span.Split(':');
                if (parts.Length == 4)
                {
                    D = int.Parse(parts[0]);
                    H = int.Parse(parts[1]);
                    M = int.Parse(parts[2]);
                    S = int.Parse(parts[3]);
                }
                else
                {
                    if (parts.Length == 3)
                    {
                        D = 0;
                        H = int.Parse(parts[0]);
                        M = int.Parse(parts[1]);
                        S = int.Parse(parts[2]);
                    }
                }
                TimeSpan Temp = new TimeSpan(D, H, M, S, 0);
                return Temp;
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }
        public static string TimeSpanToFlattenedTimeSpanString(TimeSpan span)
        {
            string output = "";
            if (span == TimeSpan.Zero)
                return "DD:HH:MM:SS";
            output += span.Days + ":";
            if (span.Hours < 10)
                output += "0";
            output += span.Hours;
            output += ":";
            if (span.Minutes < 10)
                output += "0";
            output += span.Minutes;
            output += ":";
            if (span.Seconds < 10)
                output += "0";
            output += span.Seconds;
            return output;
        }
        /// <summary>
        /// Converts a time span object to and easy to read string
        /// </summary>
        /// <param name="span">time span object</param>
        /// <returns>easy to read string</returns>
        public static string TimeSpanToString(TimeSpan span)
        {
            //stores the output text
            string output = "";
            //if there are multiple days
            if (span.Days != 0)
            {
                //add the number of days to the string
                output += span.Days.ToString();
                //if there is only one day then don't end it with an s
                if (span.Days == 1)
                    output += " Day";
                else
                    output += " Days";

            }
            //if there is more than one hour
            if (span.Hours != 0)
            {

                if (span.Days > 0)//if there are already days in the string
                {
                    //if there are no minutes and no seconds left then end it with and hours
                    if (span.Minutes == 0 && span.Seconds == 0 && span.Milliseconds == 0)
                        output += " and";
                    //add a space before the hours
                    output += " ";
                }
                //add the hours to the string
                output += span.Hours;
                //if there is only one hour then use hour instead of hours
                if (span.Hours == 1)
                {
                    output += " Hour";
                }
                else
                {
                    output += " Hours";
                }
            }
            //if there are minutes in the time span
            if (span.Minutes != 0)
            {
                if (span.Milliseconds == 0 && span.Seconds == 0)
                {
                    output += " and ";
                }
                else
                {
                    //if there are hours or days already in the string then add a seperating space
                    if (span.Days != 0 || span.Hours != 0)
                    {
                        output += " ";
                    }
                }
                //add the minutes to the text
                output += span.Minutes;
                //if there is only one minute use minute and not minutes
                if (span.Minutes == 1)
                {
                    output += " Minute";
                }
                else
                {
                    output += " Minutes";
                }

            }
            //if there are seconds remaining
            if (span.Seconds != 0)
            {
                //if there are minutes or hours or days already in the string then add a seperating space
                if (span.Days != 0 || span.Hours != 0 || span.Minutes != 0)
                {
                    output += " ";
                }
                //if there are days, hours, or minutes already in the string then put an and before the seconds
                if (span.Minutes > 0 || span.Hours > 0 || span.Days > 0)
                {
                    if (span.Milliseconds == 0)
                        output += " and ";
                }
                //add the seconds to the string
                output += span.Seconds;
                //if there is one second left use second instead of seconds
                if (span.Seconds == 1)
                {
                    output += " second";
                }
                else
                {
                    output += " seconds";
                }

            }
            if (span.Milliseconds != 0)
            {
                if (span.Seconds > 0 || span.Minutes > 0 || span.Hours > 0 || span.Days > 0)
                {
                    output += " and ";
                }
                output += span.Milliseconds.ToString();
                if (span.Milliseconds == 1)
                {
                    output += " millisecond";
                }
                else
                {
                    output += " milliseconds";
                }
            }
            if (output == "")
                output = "no time";
            //return the formattted string
            return output;

        }

        public static TimeSpan ReadableStringToTimeSpan(string readable_Time_Span)
        {
            readable_Time_Span = readable_Time_Span.ToLower().Replace("and", "");
            int days = 0, hours = 0, minutes = 0, seconds = 0, milliseconds = 0;
            string d, h, m, s;
            if (readable_Time_Span.Contains("day"))
            {
                d = readable_Time_Span.Substring(0, readable_Time_Span.IndexOf("day"));
                days = int.Parse(d);
                readable_Time_Span = readable_Time_Span.Substring(readable_Time_Span.IndexOf("day") + 3);
                readable_Time_Span = readable_Time_Span.Substring(readable_Time_Span.IndexOf(' ')).Trim();
            }
            if (readable_Time_Span.Contains("hour"))
            {
                h = readable_Time_Span.Substring(0, readable_Time_Span.IndexOf("hour"));
                hours = int.Parse(h);
                readable_Time_Span = readable_Time_Span.Substring(readable_Time_Span.IndexOf("hour") + 4);
                readable_Time_Span = readable_Time_Span.Substring(readable_Time_Span.IndexOf(' ')).Trim();
            }
            if (readable_Time_Span.Contains("minute"))
            {
                m = readable_Time_Span.Substring(0, readable_Time_Span.IndexOf("minute"));
                minutes = int.Parse(m);
                readable_Time_Span = readable_Time_Span.Substring(readable_Time_Span.IndexOf("minute") + 6);
                if (readable_Time_Span.Contains(' '))
                    readable_Time_Span = readable_Time_Span.Substring(readable_Time_Span.IndexOf(' ')).Trim();
            }
            if (readable_Time_Span.Contains("second"))
            {
                s = readable_Time_Span.Substring(0, readable_Time_Span.IndexOf("second"));
                seconds = int.Parse(s);

            }

            if (readable_Time_Span.Contains("millisecond"))
            {
                s = readable_Time_Span.Substring(0, readable_Time_Span.IndexOf("millisecond"));
                milliseconds = int.Parse(s);

            }
            if (readable_Time_Span.Contains(":"))
            {
                string[] parts = readable_Time_Span.Split(':');
                switch (parts.Length)
                {
                    case 4:
                        days = int.Parse(parts[0]);
                        hours = int.Parse(parts[1]);
                        minutes = int.Parse(parts[2]);
                        seconds = int.Parse(parts[3]);
                        break;
                    case 3:
                        days = 0;
                        hours = int.Parse(parts[0]);
                        minutes = int.Parse(parts[1]);
                        seconds = int.Parse(parts[2]);
                        break;
                    case 2:
                        days = 0;
                        hours = 0;
                        minutes = int.Parse(parts[0]);
                        seconds = int.Parse(parts[1]);
                        break;
                    case 1:
                        days = 0;
                        hours = 0;
                        minutes = 0;
                        seconds = int.Parse(parts[0]);
                        break;
                }
            }
            return new TimeSpan(days, hours, minutes, seconds, milliseconds);

        }
        /// <summary>
        /// Finds the positive difference of whole months between the dates (Day is ignored)
        /// </summary>
        /// <param name="date1">first date</param>
        /// <param name="date2">second date</param>
        /// <returns>difference in whole months</returns>
        public static int Months_Between_Dates(DateTime date1, DateTime date2)
        {
            int Month_Difference = 0;
            DateTime swap_Date;
            if (date2 > date1)
            {
                swap_Date = date2;
                date2 = date1;
                date1 = swap_Date;
            }
            if (date1.Year - date2.Year > 2)
            {
                Month_Difference = 12 * ((date1.Year - date2.Year) - 1);
            }

            if (date2.Month > date1.Month)
            {
                Month_Difference += (12 - date2.Month + date1.Month);
            }
            else
            {
                Month_Difference += date1.Month - date2.Month;
            }
            return Month_Difference;
        }
        /// <summary>
        /// Gets the file that was created on the latest date from a directory or its subdirectories.
        /// The user can tell it just to include the directory given and for a specific file pattern
        /// </summary>
        /// <param name="directory">directory to get the latest file from</param>
        /// <param name="pattern">file extension type, default is all files</param>
        /// <param name="option">include sub directories or not, default is to include sub directories</param>
        /// <returns>the most recently modified file and its path</returns>
        public static string Get_Newest_File_Path(string directory, string pattern = "*.txt", System.IO.SearchOption option = System.IO.SearchOption.AllDirectories)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            if (Directory.GetFiles(directory).Length > 0)
            {
                var file = (from f in dirInfo.GetFiles(pattern, option) orderby f.LastWriteTime descending select f).First();
                string path = file.FullName;
                return path;
            }
            return "Empty Directory";
        }
        /// <summary>
        /// Gets the file that was created on the earliest date from a directory or its subdirectories.
        /// The user can tell it just to include the directory given and for a specific file pattern
        /// </summary>
        /// <param name="directory">directory to get the earlier file from</param>
        /// <param name="pattern">file extension type, default is all files</param>
        /// <param name="option">include sub directories or not, default is to include sub directories</param>
        /// <returns>the oldest modified file and its path</returns>
        public static string Get_Oldest_File_Path(string directory, string pattern = "*.*", System.IO.SearchOption option = System.IO.SearchOption.AllDirectories)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            var file = (from f in dirInfo.GetFiles(pattern, option) orderby f.LastWriteTime descending select f).Last();
            string path = file.FullName;
            return path;
        }

        public static string GetMD5HashFromFile(string fileName)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(fileName))
                    {

                        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message + " FILE: " + fileName;
            }
        }
        public static Byte[] FileToByteArray(string fileName)
        {
            byte[] buff = null;
            FileStream fs = new FileStream(fileName,
                                           FileMode.Open,
                                           FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            long numBytes = new FileInfo(fileName).Length;
            buff = br.ReadBytes((int)numBytes);
            return buff;
        }

        public static File_Comparison_Results Beyond_Compare(string File_Path1, string File_Path2, string Beyond_Compare_Com_Path)
        {
            if (Beyond_Compare_Com_Path == null || Beyond_Compare_Com_Path == "")
                return File_Comparison_Results.CANT_COMPARE;
            string bcomp = Beyond_Compare_Com_Path;
            if (bcomp.Contains(" "))
                bcomp = "\"" + bcomp + "\"";
            Process proc = new Process();
            //proc.StartInfo.WorkingDirectory = "C:\\Program Files (x86)\\Beyond Compare 4";

            proc.StartInfo.FileName = bcomp;
            proc.StartInfo.Arguments = "/Silent /qc ";
            if (File_Path1.Contains(" "))
                File_Path1 = "\"" + File_Path1 + "\"";
            if (File_Path2.Contains(" "))
                File_Path2 = "\"" + File_Path2 + "\"";
            proc.StartInfo.Arguments += File_Path1 + " " + File_Path2;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            proc.WaitForExit();
            int code = proc.ExitCode;
            /*    proc.OutputDataReceived += Proc_OutputDataReceived;

                StreamReader STD_Error = proc.StandardError;

                string line = "";
                line = STD_Error.ReadLine();
                if(line==null)
                    line = STD_Error.ReadLine();*/
            if (code == 2)
                return File_Comparison_Results.RULES_SAME;
            if (code <= 1)
                return File_Comparison_Results.BINARY_SAME;

            return File_Comparison_Results.DIFFERENT;
            /*   if (line == "0" || line == "1")
                   return File_Comparison_Results.BINARY_SAME;
               if (line == "2")
                   return File_Comparison_Results.RULES_SAME;
               return File_Comparison_Results.DIFFERENT;  */

        }

        public static DateTime Get_File_Creation_Date(string path)
        {
            if (!File.Exists(path))
            {
                return DateTime.MinValue;
            }
            try
            {
                DateTime creation = File.GetCreationTime(path);
                return creation;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
        public static DateTime Get_File_Modified_Date(string path)
        {
            if (!File.Exists(path))
            {
                return DateTime.MinValue;
            }
            try
            {
                DateTime creation = File.GetLastWriteTime(path);
                return creation;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
        public static bool Is_Work_Computer(string Expected_Work_Network_Path)
        {
            return Directory.Exists(Expected_Work_Network_Path);
        }
        public static string reverseCharacterCase(string text)
        {
            char[] caseSwappedChars = new char[text.Length];
            for (int i = 0; i < caseSwappedChars.Length; i++)
            {
                char c = text[i];
                if (char.IsLetter(c))
                {
                    caseSwappedChars[i] =
                        char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c);
                }
                else
                {
                    caseSwappedChars[i] = c;
                }
            }
            return new string(caseSwappedChars);
        }
        public static bool Is_Header(string line)
        {
            if (line.StartsWith(((char)12).ToString()))
                return true;
            return false;
        }
        public static void Skip_Header(string[] lines, ref int i)
        {
            for (; i < lines.Length; i++)
            {
                if (lines[i].Contains("---------------------------------------------------------------------"))
                {
                    i++;
                    return;
                }
            }
        }
        public static void Skip_Header(string[] lines, ref int i, ref string Report_Name)
        {
            if (Report_Name == null)
                Report_Name = "";
            for (; i < lines.Length; i++)
            {
                if (lines[i].Contains("Page"))
                {
                    //i++;
                    if (Report_Name == "")
                    {
                        Report_Name = lines[i];
                        Report_Name = Report_Name.Substring(11).Trim();
                        Report_Name = Report_Name.Substring(0, Report_Name.IndexOf("Page")).Trim();
                        if (Report_Name == "SKB Corporation")
                        {
                            i++;
                            Report_Name = lines[i];
                            Report_Name = Report_Name.Substring(Report_Name.IndexOf(' ')).Trim();
                            Report_Name = Report_Name.Substring(Report_Name.IndexOf(' ')).Trim();
                        }
                        if(Report_Name== "Detail Aged Trial Balance")
                        {
                            if (lines[i+2].Contains("Sorted By Vendor ID"))
                            {
                                Report_Name = "AP Detail Aged Trial Balance";
                            }
                            else
                            {
                                Report_Name = "AR Detail Aged Trial Balance";
                            }
                        }
                        //Report_Name = Report_Name.Substring(0, Report_Name.IndexOf("Page")).Trim();
                    }

                }

                if (lines[i].Contains("---------------------------------------------------------------------"))
                {
                    i++;
                    return;
                }
            }
        }
        /// <summary>
        /// converts a Timespan to a string HH:MM:SS
        /// </summary>
        /// <param name="T">timespan to convert</param>
        /// <returns>timespan as string HH:MM:SS</returns>
        public static string TimeSpan_To_String(TimeSpan T)
        {
            string output = "";
            if (T.Hours <= 10)
                output += "0";
            output += T.Hours;
            output += ":";
            if (T.Minutes < 10)
            {
                output += "0";
            }
            output += T.Minutes;
            output += ":";
            if (T.Seconds < 10)
            {
                output += "0";
            }
            output += T.Seconds;
            return output;
        }
        /// <summary>
        /// Timespan to input string
        /// </summary>
        /// <param name="T">Timespan</param>
        /// <returns>time span as format -D:H:M:S:miliseconds if negative or format D:H:M:S:miliseconds if positive</returns>
        public static string TimeSpan_To_Input_String(TimeSpan T)
        {
            //format 
            string output = "";
            if (T.Days < 0 || T.Hours < 0 || T.Minutes < 0 || T.Seconds < 0 || T.Milliseconds < 0)
                output += "-";
            output += Math.Abs(T.Days) + ":" + Math.Abs(T.Hours) + ":" + Math.Abs(T.Minutes) + ":" + Math.Abs(T.Seconds) + ":" + Math.Abs(T.Milliseconds);
            return output;
        }
        /// <summary>
        /// removes any non digit characters from the string with the exception of decimal places
        /// for example $3,542.08 becomes 3543.08 or $3,542.08.12 becomes 354208.12
        /// </summary>
        /// <param name="numberString">number string</param>
        /// <returns>string with numbers and decimal place only</returns>
        public static string Get_Numbers_Only(string numberString)
        {
            string output = "";
            int dummy;
            for (int i = 0; i < numberString.Length; i++)
            {
                if (numberString[i] == '.')
                {
                    if (!output.Contains('.'))
                        output += '.';
                }
                else
                {
                    if (int.TryParse(numberString[i].ToString(), out dummy))
                    {
                        output += dummy;
                    }
                }
            }
            return output;
        }
        public static TimeSpan String_To_TimeSpan(string timespan_String)
        {
            if (!timespan_String.Contains(':'))
                return TimeSpan.Zero;
            //  if (timespan_String.Split(':').Length != 3)
            //     return TimeSpan.Zero;
            try
            {
                TimeSpan output;
                string secondstr, partseconds;
                string[] parts = timespan_String.Split(':');
                int days = 0, hours, minutes, seconds, miliseconds;
                if (parts.Length == 3)
                {
                    hours = int.Parse(parts[0]);
                    minutes = int.Parse(parts[1]);
                    if (parts[2].Contains(','))
                    {
                        secondstr = parts[2].Substring(0, parts[2].IndexOf(','));
                        partseconds = parts[2].Substring(parts[2].IndexOf(',') + 1);
                        seconds = int.Parse(secondstr);
                        miliseconds = int.Parse(partseconds);
                    }
                    else
                    {
                        if (parts[2].Contains('.'))
                        {
                            secondstr = parts[2].Substring(0, parts[2].IndexOf('.'));
                            partseconds = parts[2].Substring(parts[2].IndexOf('.') + 1);
                            seconds = int.Parse(secondstr);
                            miliseconds = int.Parse(partseconds);
                        }
                        else
                        {
                            seconds = int.Parse(parts[2]);
                            miliseconds = 0;
                        }

                    }
                    output = new TimeSpan(0, hours, minutes, seconds, miliseconds);
                }
                else
                {
                    if (parts.Length == 4)
                    {
                        days = int.Parse(parts[0]);
                        hours = int.Parse(parts[1]);
                        minutes = int.Parse(parts[2]);
                        if (parts[3].Contains(','))
                        {
                            secondstr = parts[3].Substring(0, parts[3].IndexOf(','));
                            partseconds = parts[3].Substring(parts[3].IndexOf(',') + 1);
                            seconds = int.Parse(secondstr);
                            miliseconds = int.Parse(partseconds.Substring(0, 3));
                        }
                        else
                        {
                            if (parts[3].Contains('.'))
                            {
                                secondstr = parts[3].Substring(0, parts[3].IndexOf('.'));
                                partseconds = parts[3].Substring(parts[3].IndexOf('.') + 1);
                                seconds = int.Parse(secondstr);
                                miliseconds = int.Parse(partseconds.Substring(0, 3));

                            }
                            else
                            {
                                seconds = int.Parse(parts[3]);
                                miliseconds = 0;
                            }

                        }
                        //seconds = int.Parse(parts[3]);
                        output = new TimeSpan(days, hours, minutes, seconds, miliseconds);
                    }
                    else
                    {
                        output = TimeSpan.Zero;
                    }
                }
                return output;
            }
            catch
            {
                return TimeSpan.Zero;
            }

        }
        /// <summary>
        /// input string to timespan object
        /// </summary>
        /// <param name="str">format -D:H:M:S:miliseconds if negative or format D:H:M:S:miliseconds or D H:M:S or H:M:S if positive</param>
        /// <returns>TimeSpan object representing that string</returns>
        public static TimeSpan Input_String_To_TimeSpan(string str, out string error)
        {
            try
            {
                bool negative = false;
                if (str.StartsWith("-"))
                    negative = true;
                str = str.Replace("-", "");
                string[] parts;
                string dy = "0";
                string hr = "0";
                string mn = "0";
                string sec = "0";
                string mil = "0";
                if (str.Contains(" "))
                {
                    if (str.Contains('.'))
                    {
                        mil = str.Substring(str.IndexOf('.') + 1);
                        str = str.Substring(0, str.IndexOf('.'));
                        dy = str.Substring(0, str.IndexOf(' '));
                        str = str.Substring(str.IndexOf(' ')).Trim();

                    }
                    else
                    {
                        parts = str.Split(':');

                        hr = parts[0];
                        mn = parts[1];
                        sec = parts[2];


                    }
                }
                else
                {
                    if (str.Contains('.'))
                    {
                        mil = str.Substring(str.IndexOf('.') + 1);
                        str = str.Substring(0, str.IndexOf('.'));
                        parts = str.Split(':');
                        if (parts.Length == 4)
                        {
                            dy = parts[0];
                            hr = parts[1];
                            mn = parts[2];
                            sec = parts[3];
                        }
                        else
                        {
                            hr = parts[0];
                            mn = parts[1];
                            sec = parts[2];
                        }
                    }
                    else
                    {
                        parts = str.Split(':');
                        if (parts.Length == 4)
                        {
                            dy = parts[0];
                            hr = parts[1];
                            mn = parts[2];
                            sec = parts[3];
                        }
                        else
                        {
                            hr = parts[0];
                            mn = parts[1];
                            sec = parts[2];
                        }
                    }
                }

                int days = int.Parse(dy);
                if (negative && days > 0)
                    days *= -1;

                int Hours = int.Parse(hr);

                if (negative && days == 0 && Hours > 0)
                    Hours *= -1;
                int Minutes = int.Parse(mn);

                if (negative && days == 0 && Hours == 0 && Minutes > 0)
                    Minutes *= -1;
                int Seconds = int.Parse(sec);

                if (negative && days == 0 && Hours == 0 && Minutes == 0 && Seconds > 0)
                    Seconds *= -1;
                int miliseconds = int.Parse(mil);
                if (negative && days == 0 && Hours == 0 && Seconds == 0 && miliseconds > 0)
                    miliseconds *= -1;
                error = "";
                return new TimeSpan(days, Hours, Minutes, Seconds, miliseconds);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return new TimeSpan();

            }
        }
        /// <summary>
        /// this safely moves files, if it can't it will ask the user if it should try again without crashing
        /// </summary>
        /// <param name="Source_File_Path">the file to be moved</param>
        /// <param name="base_destination_File_Path">the file path it should be moved to</param>
        /// <param name="overwrite">should it just overwrite the file if it is arleady there</param>
        /// <param name="use_recycle_bin">if it has to delete should it use the recycle bin</param>
        /// <returns>true if move was successful or false if it was not</returns>
        public static bool Safe_Move_File(string Source_File_Path, ref string base_destination_File_Path, bool overwrite, bool use_recycle_bin, bool suppress_warnings, bool allow_renaming = true)
        {
            if (Source_File_Path == base_destination_File_Path)
            {
                base_destination_File_Path = "MATCH";
                return true;
            }

            string filename = Source_File_Path.Substring(Source_File_Path.LastIndexOf('\\'));
            string destination = base_destination_File_Path;
            bool moved = false;
            base_destination_File_Path = Check_And_Get_Destination_Path(Source_File_Path, destination, overwrite, use_recycle_bin, allow_renaming, suppress_warnings, false);
            if (base_destination_File_Path == "Canceled")
                return false;
            if (base_destination_File_Path == "MATCH")
            {
                try
                {
                    File.Delete(Source_File_Path);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            if (Source_File_Path != destination)
            {
                if (!File.Exists(Source_File_Path))
                {
                    System.Console.WriteLine("Break");
                }
                while (!moved)
                {
                    try
                    {
                        File.Move(Source_File_Path, destination);
                        moved = true;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        if (MessageBox.Show("Failed to move the file: \"" + Source_File_Path + "\" to \"" + destination + "\"\r\nDo you wish to try again?\r\n" + ex.Message, "", MessageBoxButtons.YesNo) == DialogResult.No)
                        {
                            moved = true;
                            return false;
                        }
                    }
                }
            }
            else
            {

                return true;
            }
            return false;
        }
        public static string Safe_Rename_File(string source_file_path, string destination_File_Path, bool overwrite, bool allow_renaming, bool use_recycle_bin, bool suppress_warnings)
        {
            string source_directory = "";
            string destination_directory = "";
            string source_file_name = "";
            string destination_file_name = "";
            string source_extension = "";
            string destination_extension = "";
            bool renamed = false;
            if (source_file_path.Contains('\\'))
            {
                source_directory = source_file_path.Substring(0, source_file_path.LastIndexOf('\\'));
                source_file_name = source_file_path.Substring(source_file_path.LastIndexOf('\\') + 1);
            }
            else
            {
                source_directory = "";
                source_file_name = source_file_path;
            }
            if (source_file_name.Contains("."))
            {
                source_extension = source_file_name.Substring(source_file_name.LastIndexOf(".") + 1);
                source_file_name = source_file_name.Substring(0, source_file_name.LastIndexOf("."));
            }

            if (destination_File_Path.Contains('\\'))
            {
                destination_directory = destination_File_Path.Substring(0, destination_File_Path.LastIndexOf('\\'));
                destination_file_name = destination_File_Path.Substring(destination_File_Path.LastIndexOf('\\') + 1);
            }
            else
            {
                destination_directory = "";
                destination_file_name = destination_File_Path;
            }
            if (destination_file_name.Contains("."))
            {
                destination_extension = destination_file_name.Substring(destination_file_name.LastIndexOf(".") + 1);
                destination_file_name = destination_file_name.Substring(0, destination_file_name.LastIndexOf("."));
            }

            if (!Directory.Exists(destination_directory))
            {
                Directory.CreateDirectory(destination_directory);
            }
            while (!renamed)
            {
                try
                {
                    if (File.Exists(destination_File_Path))
                    {
                        if (overwrite)
                        {
                            File.Move(source_file_path, destination_File_Path);
                            return "Success: " + destination_File_Path;
                        }
                        else
                        {
                            if (suppress_warnings)
                            {
                                Safe_Delete(source_file_path, suppress_warnings, use_recycle_bin);
                                return "Canceled File already exists!";
                            }
                            else
                            {
                                if (allow_renaming)
                                {
                                    switch (MessageBox.Show("The file \"" + destination_File_Path + "\" Already exists do you wish to choose a different path?", "", MessageBoxButtons.YesNoCancel))
                                    {
                                        case DialogResult.Cancel:
                                            return "Canceled by user!";
                                        case DialogResult.No:
                                            Safe_Delete(source_file_path, suppress_warnings, use_recycle_bin);
                                            return "Canceled File already exists!";
                                        case DialogResult.Yes:
                                            SaveFileDialog diag = new SaveFileDialog();
                                            diag.Filter = "*." + destination_extension;
                                            diag.InitialDirectory = destination_directory;
                                            diag.FileName = destination_file_name;
                                            if (diag.ShowDialog() != DialogResult.Cancel)
                                            {
                                                destination_File_Path = diag.FileName;
                                                break;
                                            }
                                            else
                                            {
                                                return "Canceled by user!";
                                            }

                                    }
                                }

                                else
                                {
                                    Safe_Delete(source_file_path, suppress_warnings, use_recycle_bin);
                                    return "Canceled File already exists!";
                                }
                            }
                        }
                    }
                    else
                    {
                        File.Move(source_file_path, destination_File_Path);
                        return "Success: " + destination_File_Path;
                    }

                }
                catch (Exception ex)
                {
                    if (suppress_warnings)
                    {
                        return "Failed To Rename: " + ex.Message;
                    }
                    else
                    {
                        MessageBox.Show("Failed to rename file \"" + source_file_path + "\" to \"" + destination_File_Path + "\"!\r\n" + ex.Message);
                        return "Failed To Rename: " + ex.Message;
                    }
                }
            }
            return "IMPOSSIBLE MESSAGE!!!";

        }
        /// <summary>
        /// this safely moves files, if it can't it will ask the user if it should try again without crashing
        /// </summary>
        /// <param name="Source_File_Path">the file to be moved</param>
        /// <param name="base_destination_folder">where it should be moved to</param>
        /// <param name="overwrite">should it just overwrite the file if it is arleady there</param>
        /// <param name="use_recycle_bin">if it has to delete should it use the recycle bin</param>
        /// <returns>true if move was successfull or false if it was not</returns>
        public static string Safe_Move(string Source_File_Path, string base_destination_folder, bool overwrite, bool allow_renaiming, bool use_recycle_bin, bool suppress_warnings)
        {
            string filename = "";
            if (Source_File_Path.Contains("\\"))
                filename = Source_File_Path.Substring(Source_File_Path.LastIndexOf('\\') + 1);
            else
                filename = Source_File_Path;
            if (!base_destination_folder.EndsWith("\\"))
                base_destination_folder += "\\";
            string destination = base_destination_folder + filename;
            bool moved = false;
            destination = Check_And_Get_Destination_Path(Source_File_Path, destination, overwrite, use_recycle_bin, allow_renaiming, suppress_warnings, false);
            if (destination == "Canceled")
                return "Canceled";
            if (destination == "MATCH")
            {
                Safe_Delete(Source_File_Path, use_recycle_bin, suppress_warnings);

                return destination;
            }
            while (!moved)
            {
                try
                {
                    File.Move(Source_File_Path, destination);
                    moved = true;
                    return destination;
                }
                catch (Exception ex)
                {
                    if (MessageBox.Show("Failed to move the file \"" + Source_File_Path + "\" to \"" + destination + "\"\r\nDo you wish to try again?\r\n" + ex.Message, "", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        moved = true;
                        return "Canceled";
                    }
                }
            }
            return "Failed";
        }
        /// <summary>
        /// Copies a file to a directory
        /// </summary>
        /// <param name="file_path">Source file to be copied</param>
        /// <param name="destination_directory">destination directory</param>
        /// <param name="overwrite">if the file is already in the directory overwrite it with the source file</param>
        /// <param name="use_Recyle_bin">if it should be overwritten then delete the already existing file to the recycle bin</param>
        public static string Safe_Copy_To_Directory(string file_path, string destination_directory, bool overwrite, bool allow_renaiming, bool use_Recyle_bin, bool suppress_warnings)
        {
            string destination_path = "";
            string file_name = "";
            if (file_path.Contains("\\"))
                file_name = file_path.Substring(file_path.LastIndexOf("\\") + 1);
            else
                file_name = file_path;
            if (!destination_directory.EndsWith("\\"))
                destination_directory += "\\";
            destination_path = destination_directory + file_name;
            return Safe_Copy(file_path, destination_path, overwrite, allow_renaiming, use_Recyle_bin, suppress_warnings);
        }
        /// <summary>
        /// moves a group of files to a directory using a seperate thread must attach to completed handler
        /// to know when done. Progress made handler shows the current progress of the move.
        /// log occured handler will send out a log of each file and where it was moved to.
        /// </summary>
        /// <param name="files">Files to be moved</param>
        /// <param name="destination_directory">directory to move them to</param>
        /// <param name="overwrite">if a file with the same name is already in that directory should it 
        /// overwrite that file?</param>
        /// <param name="use_Recycle_bin">if it should overwrite that file should it put the origional file
        /// in the recycle bin.  if it should not overwrite the file this parameter does nothing</param>
        public static void Safe_move_files_to_directory_Threaded(string[] files, string destination_directory, bool overwrite, bool allow_rename, bool use_Recycle_bin, bool suppress_warnings)
        {


            Thread worker = new Thread(() => Safe_move_files_to_directory(files, destination_directory, overwrite, allow_rename, use_Recycle_bin, suppress_warnings));
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();
        }
        /// <summary>
        /// moves a group of files to a directory
        /// The completed handler will also notifiy when this function is done
        /// to know when done. Progress made handler shows the current progress of the move.
        /// log occured handler will send out a log of each file and where it was moved to.
        /// </summary>
        /// <param name="files">Files to be moved</param>
        /// <param name="destination_directory">directory to move them to</param>
        /// <param name="overwrite">if a file with the same name is already in that directory should it 
        /// overwrite that file?</param>
        /// <param name="use_Recycle_bin">if it should overwrite that file should it put the origional file
        /// in the recycle bin.  if it should not overwrite the file this parameter does nothing</param>
        public static void Safe_move_files_to_directory(string[] files, string destination_directory, bool overwrite, bool allow_rename, bool use_Recycle_bin, bool suppress_warnings)
        {
            float total = files.Length;
            string dest_path;
            for (int i = 0; i < files.Length; i++)
            {
                dest_path = Safe_Move(files[i], destination_directory, overwrite, allow_rename, use_Recycle_bin, suppress_warnings);
                if (dest_path != "Canceled" && dest_path != "MATCH")
                    log_Occured?.Invoke(null, new Log_Event("Moved File " + files[i] + " to " + dest_path, Log_Event.LogType.LOG));

                Progress_Made?.Invoke(null, new Progress_Event(i / total));
            }
            completed?.Invoke(null, new Finished_Event(Finished_Event.Finish_Status.SUCCESS));
        }
        public static bool Has_Been_OCRd(string path)
        {
            bool output = false;
            StreamReader file = null;
            string temp;
            try
            {
                file = new StreamReader(path);
                temp = file.ReadToEnd();
                if (temp.Contains("<pdf:Producer>Adobe Acrobat Pro (32-bit) 23 Paper Capture Plug-in</pdf:Producer>"))
                    output = true;
            }
            catch (Exception ex)
            {
                log_Occured?.Invoke(null, new Log_Event("Failed OCR check for file \"" + path + "\" Error: " + ex.Message, Log_Event.LogType.ERROR));

            }
            finally
            {
                if (file != null)
                {
                    file.Close();
                    file.Dispose();
                    file = null;
                }
            }
            return output;
        }
        public static long Get_File_Size(string path)
        {
            try
            {
                return new System.IO.FileInfo(path).Length;
            }
            catch
            {
                return -1;
            }
        }

        public static bool Can_Files_Be_Restored(string[] files)
        {
            return Deleted_File_Database.Can_Files_Be_Resotred(files);
        }

        public static string Restore_File(string file, bool suppress_warnings)
        {

            return Deleted_File_Database.Restore_File(file, suppress_warnings);
        }

        public static bool Connected_to_network(string Network_Path)
        {
            try
            {
                if (Directory.Exists(Network_Path))
                    return true;
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static string Safe_move_file_to_directory(string Source_Path, string Destination_Directory, bool overwrite, bool recycle, bool allow_renaming, bool suppress_warnings)
        {
            string destination = Destination_Directory + Source_Path.Substring(Source_Path.LastIndexOf('\\'));
            string orig_destination = destination;
            destination = Check_And_Get_Destination_Path(Source_Path, destination, overwrite, recycle, allow_renaming, suppress_warnings, false);
            if (destination == "MATCH")
            {
                File.Delete(Source_Path);
                return orig_destination;
            }
            else
            {
                if (Safe_Move_File(Source_Path, ref destination, overwrite, recycle, allow_renaming, suppress_warnings))
                    return destination;
            }
            return "Canceled";


        }

        public static bool Rename_File(string local_File_Path, ref string updated_File_Name, bool suppress_warnings, bool overwrite = false, bool useRecycleBin = true, bool AllowRenaming = true)
        {
            string base_path = local_File_Path.Substring(0, local_File_Path.LastIndexOf('\\'));
            string updated_path = base_path + "\\" + updated_File_Name;
            bool success = CommonCore.Safe_Move_File(local_File_Path, ref updated_path, overwrite, useRecycleBin, AllowRenaming, suppress_warnings);
            if (success)
            {
                updated_File_Name = updated_path;
            }
            return success;
        }

        public static string Get_Path(string generic_path, SortedList<string, string> replacements)
        {
            generic_path = generic_path.Replace("[YEAR]", DateTime.Now.Year.ToString());
            string Month = "", Last_Month = "", Next_Month = "";
            switch (DateTime.Now.Month)
            {
                case 1:
                    Month = "January";
                    Last_Month = "December";
                    Next_Month = "February";
                    break;
                case 2:
                    Month = "February";
                    Last_Month = "January";
                    Next_Month = "March";
                    break;
                case 3:
                    Month = "March";
                    Last_Month = "February";
                    Next_Month = "April";
                    break;
                case 4:
                    Month = "April";
                    Last_Month = "March";
                    Next_Month = "May";
                    break;
                case 5:
                    Month = "May";
                    Last_Month = "April";
                    Next_Month = "June";
                    break;
                case 6:
                    Month = "June";
                    Last_Month = "May";
                    Next_Month = "July";
                    break;
                case 7:
                    Month = "July";
                    Last_Month = "August";
                    Next_Month = "June";
                    break;
                case 8:
                    Month = "August";
                    Last_Month = "July";
                    Next_Month = "September";
                    break;
                case 9:
                    Month = "September";
                    Last_Month = "August";
                    Next_Month = "October";
                    break;
                case 10:
                    Month = "October";
                    Last_Month = "September";
                    Next_Month = "November";
                    break;
                case 11:
                    Month = "November";
                    Last_Month = "October";
                    Next_Month = "December";
                    break;
                case 12:
                    Month = "December";
                    Last_Month = "November";
                    Next_Month = "January";
                    break;
            }
            generic_path = generic_path.Replace("[NEXT MONTH]", Next_Month).Replace("[MONTH]", Month).Replace("[LAST MONTH]", Last_Month);
            return generic_path;
        }
        public static void Write_To_Debug_Window(object item)
        {
            Write_To_Debug_Window(item.ToString());
        }
        public static void Write_To_Debug_Window(string message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(message);
            }
            catch
            {
                System.Console.WriteLine(message);
            }
        }

        public static string Make_XML_Friendly(string key)
        {
            return key.Replace(" ", "_").Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;").Replace("'", "&apos;").Replace("\"", "&quot;");
        }

        public static bool Is_LeapYear(int year)
        {
            //divisible by 4 and not divisible by 100 unless also divisible by 400
            return (year % 4 == 0) && ((year % 100 != 0) || (year % 400 == 0));
        }

        public static void replace_text_Case_insensitive(string key, string replacement_Text, ref string output)
        {
            int start_Index;
            key = key.ToUpper();
            string before, after;
            start_Index = output.ToUpper().IndexOf(key);
            if (start_Index != -1)
            {
                before = output.Substring(0, start_Index);
                after = output.Substring(start_Index + key.Length);
                output = before + replacement_Text + after;
            }
        }

        public static void Open_Explorer(string Directory)
        {
            Process diagnosticProcess = new Process();
            diagnosticProcess.StartInfo.FileName = "Explorer.exe";
            diagnosticProcess.StartInfo.Arguments = "\"" + Directory + "\"";
            diagnosticProcess.Start();
            diagnosticProcess.WaitForExit();
        }

        public static void Rename_Directory(string path, ref string updated_Folder_Name, bool suppress_warnings = false)
        {
            if (Directory.Exists(updated_Folder_Name))
            {
                FolderBrowserDialog diag = new FolderBrowserDialog();
                diag.SelectedPath = updated_Folder_Name;
                diag.ShowNewFolderButton = true;
                if (diag.ShowDialog() == DialogResult.OK)
                {
                    updated_Folder_Name = diag.SelectedPath;
                }
                else
                {
                    return;
                }
            }
            //if the directory still exists then merge the files into the other directory
            if (Directory.Exists(updated_Folder_Name))
            {
                Move_And_Merge_With_Folder(path, updated_Folder_Name, suppress_warnings);
            }
            else
            {
                Directory.Move(path, updated_Folder_Name);
            }
        }

        private static void Move_And_Merge_With_Folder(string path, string updated_Folder_Name, bool suppress_Warnings)
        {
            //need to move recursively through each sub folder
            string[] Folders = Directory.GetDirectories(path);
            string[] Files = Directory.GetFiles(path);
            string folder_Name;
            string File_name;
            foreach (string folder in Folders)
            {
                folder_Name = Path.GetFileName(folder);
                Move_And_Merge_With_Folder(folder, updated_Folder_Name + "\\" + folder_Name, suppress_Warnings);
            }
            if (!Directory.Exists(updated_Folder_Name))
            {
                Directory.Move(path, updated_Folder_Name);
            }
            else
            {
                Safe_move_files_to_directory(Files, updated_Folder_Name, false, true, true, suppress_Warnings);
                Files = Directory.GetFiles(path);
                if (Files.Length != 0)
                {
                    System.Console.WriteLine("Warning non-empty directory!" + path);
                }
                else
                {
                    Directory.Delete(path);
                }
            }

        }

        public static DateTime Get_Last_Date()
        {
            int year = DateTime.Now.Year;
            int Month = DateTime.Now.Month;
            if (Month == 12)
            {
                Month = 1;
                year++;
            }
            else
            {
                Month++;
            }
            DateTime temp = new DateTime(year, Month, 1);
            return temp.AddDays(-1);
        }

        public static bool Get_Date_Text(string line, out string date_Text)
        {
            // Regular expression pattern for mm/dd/yyyy format
            string pattern = @"\b(\d{2}/\d{2}/\d{4})\b";

            // Find the match
            Match match = Regex.Match(line, pattern);

            // Check if a match was found
            if (match.Success)
            {
                // Extract the date from the match
                date_Text = match.Value;
                Console.WriteLine("Extracted Date: " + date_Text);
                return true;
            }
            else
            {
                date_Text = "No Date";
                Console.WriteLine("No date found in the string.");
                return false;
            }
        }
        public static TValue Get_Dictionary_Value_at_Index<TKey, TValue>(Dictionary<TKey, TValue> dictionary, int index)
        {
            // Ensure the index is valid
            if (index < 0 || index >= dictionary.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

            int i = 0;

            // Iterate through the dictionary using a foreach loop
            foreach (var kvp in dictionary)
            {
                if (i == index)
                {
                    return kvp.Value; // Return the value at the specified index
                }
                i++;
            }

            // Return default value if index is invalid (shouldn't reach here due to the check above)
            return default(TValue);
        }
        // Generic function to insert an item at the front of a Dictionary
        public static Dictionary<TKey, TValue> InsertAtFront<TKey, TValue>(
            Dictionary<TKey, TValue> originalDict, TKey key, TValue value)
        {
            // Create a new dictionary with the new item at the front
            Dictionary<TKey, TValue> newDict = new Dictionary<TKey, TValue>();

            // Add the new item at the front
            newDict[key] = value;

            // Add existing items after the new one
            foreach (var kvp in originalDict)
            {
                newDict[kvp.Key] = kvp.Value;
            }

            return newDict;
        }
        /// <summary>
        /// Creates a deep copy of a dictionary using JSON serialization.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary's keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary's values.</typeparam>
        /// <param name="originalDictionary">The original dictionary to deep copy.</param>
        /// <returns>A new dictionary that is a deep copy of the original.</returns>
        /// <remarks>
        /// - This method uses JSON serialization to serialize the entire dictionary into a JSON string
        ///   and then deserialize it back into a new dictionary.
        /// - All objects within the dictionary, including nested objects, must be serializable.
        /// - The method assumes that the dictionary's keys are not modified (e.g., immutable types like strings).
        /// </remarks>
        public static Dictionary<TKey, TValue> DeepCopyDictionary<TKey, TValue>(
            Dictionary<TKey, TValue> originalDictionary)
        {
            // Ensure the input dictionary is not null to avoid a null reference exception.
            if (originalDictionary == null)
            {
                throw new ArgumentNullException(nameof(originalDictionary), "The dictionary cannot be null.");
            }

            // Serialize the original dictionary into a JSON string.
            string serializedDictionary = JsonConvert.SerializeObject(originalDictionary);

            // Deserialize the JSON string back into a new dictionary.
            // This creates a deep copy by reconstructing the dictionary and its contents.
            Dictionary<TKey, TValue> deepCopiedDictionary =
                JsonConvert.DeserializeObject<Dictionary<TKey, TValue>>(serializedDictionary);

            // Return the newly created deep copy.
            return deepCopiedDictionary;
        }
        public static string Get_Generic_Path(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Input string cannot be null or empty.", nameof(path));
            }
            // Regex for a 4-digit year
            string yearPattern = @"\b\d{4}\b";
            // Replace year
            string result = Regex.Replace(path, yearPattern, "[YEAR]");

            // Get all month names in current culture
            string[] monthNames = DateTimeFormatInfo.CurrentInfo.MonthNames;
            foreach (var monthName in monthNames)
            {
                if (!string.IsNullOrEmpty(monthName)) // Ignore empty entries
                {
                    path = Regex.Replace(result, $@"\b{Regex.Escape(monthName)}\b", "[MONTH_NAME]", RegexOptions.IgnoreCase);
                }
            }

            return path;
        }
        public static string Get_Path(string text, int year, int month)
        {
            text = text.Replace("[MONTH]", convert_To_Month_Name(month));
            text = text.Replace("[MONTH_NAME]", convert_To_Month_Name(month));
            text = text.Replace("[MONTH_NUMBER]", month.ToString());
            text = text.Replace("[YEAR]", year.ToString());
            return text;
        }

        public static DateTime TimeOnly_To_DateTime(int Hour, int Minute, int Second)
        {
            return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Hour, Minute, Second);
        }
    }

}

