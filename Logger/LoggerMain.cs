///--------------------------------------------------------------------------------------------------------------------------------------------
/// Log main class
/// 
/// This class stores the logs, saves the logs, clears the logs, calls a form to display the logs, sends notifications when new logs are 
/// received This class also stores the time that the logs are received automatically
/// 
/// 
/// Author: Anthony Hoffert
///--------------------------------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using Settings_Handler;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Logger
{
    /// <summary>
    /// Primary log class
    /// </summary>
    public static class LoggerMain
    {



        #region variables
        /// <summary>
        /// Icon to be displayed on the log viewer window
        /// </summary>
        public static Icon Form_Icon = null;
        //Type dependent Time dependent log list Sorted by type then by time
        //This assumes that no two logs to be entered at the exact same time
        private static SortedList<LogItem.LogType, SortedList<string, SortedList<long, LinkedList<LogItem>>>> All_Logs = new SortedList<LogItem.LogType, SortedList<string, SortedList<long, LinkedList<LogItem>>>>();
        //Sources for the logs;
        public static LinkedList<string> sources = new LinkedList<string>();
        //settings tab for the caller for the log
        internal static Settings_Handler.Tab settings;
        //number of normal logs
        private static int TotalStandardLogs = 0;
        //number of logs that are warnings
        private static int TotalWarnings = 0;
        //number of logs that are errors
        private static int TotalErrors = 0;
        //has the log been saved
        private static bool Saved = true;
        //should the logs be sent to the console at the same time default is no
        public static bool AlsoSendToConsole = false;
        //Should the time be added to the front of the log message default is yes
        public static bool AppendLogTime = true;
        //should the source be added before the time
        public static bool Show_Source = true;
        //Sends notification if a new log is received
        public static event EventHandler<LogRecieved> New_Log_Received;
        //Sends notification if the log is cleared
        public static event EventHandler<LogCleared> Log_Cleared;
        //Sends notification if the log is saved
        public static event EventHandler<LogSaved> Log_Saved;
        //stores a temporary buffer to catch new logs while saving
        private static Queue<LogItem> Temp_Buffer = new Queue<LogItem>();
        //Dialog to show the log, save the log, or clear the log
        private static Log_Viewer Log_Shower = null;

        //how often it should randomly save the logs
        private static TimeSpan auto_log_Interval = TimeSpan.Zero;
        //when was the log last saved
        private static DateTime Last_Save;
        //default directory to save the logs to
        private static string Default_Directory_to_Save_logs_Too = "";
        //current log path to save to
        private static string Log_Path = "";
        //worker to save the logs at regular intervals
        private static Thread Auto_Save_Worker;
        //is the log still running
        private static bool Running = false;
        //has a log been made since the last autosave?
        private static bool Log_Made = false;
        //name of the thread that is handling the GUI used to avoid cross
        //threadded errors since the invokeRequired built into C# is broken
        private static string Gui_Thread_Name = "";
        delegate void UpdateVoid();
        //timespan until next autosave
        public static TimeSpan Time_Until_Next_Autosave { get; private set; }
        public static string Last_Save_Path = @"No logs have been saved yet!";
        #region Auto menu generation variables
        private static MenuStrip Holding_Menu_Strip = null;
        //stores the main log menu on the main menu
        private static ToolStripMenuItem Log_Main_Menu = null;
        //stores the show log menu option
        private static ToolStripMenuItem Show_Log_Option = null;
        //stores the clear log menu option
        private static ToolStripMenuItem Clear_Log_Option = null;
        //stores the save log menu option
        private static ToolStripMenuItem Save_Log_Option = null;
        //Bold Font for when a new log is present
        private static Font Bold_Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold);
        //regular Font for after the log is read or when the log is empty
        private static Font Normal_Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular);
        public static bool AutoShowLogFile = false;
        private static bool Saving;
        #endregion
        #endregion
        #region Event Handler functions
        /// <summary>
        /// Event for when a new log is received
        /// </summary>
        /// <param name="sender">Object that recorded the log</param>
        /// <param name="L">Log itself plus log stats</param>
        public static void NewLogReceived(object sender, LogRecieved L)
        {
            //if the new log handler is suscribed to then send a message to all the suscribers
            New_Log_Received?.Invoke(sender, L);
        }
        /// <summary>
        /// Determines if another program has connected to at least one listener
        /// </summary>
        /// <returns>true if someone somewhere has connected to a listener</returns>
        public static bool At_Least_One_Event_Handler_Is_Linked()
        {
            if (New_Log_Received != null)
                return true;
            if (Log_Cleared != null)
                return true;
            if (Log_Saved != null)
                return true;
            return false;
        }
        /// <summary>
        /// Event for when the log is cleared
        /// </summary>
        /// <param name="sender">Object that cleared the log</param>
        public static void LogCleared(object sender, bool Show_Warning = false)
        {
            //if the clear log handler is suscribed to then send a message to all the suscribers
            Log_Cleared?.Invoke(sender, new Logger.LogCleared(Show_Warning));
        }
        /// <summary>
        /// Event for when the log is saved
        /// </summary>
        /// <param name="sender">Object that saved the log</param>
        public static void LogSaved(string path)
        {
            Last_Save_Path = path;
            //if the save log handler is suscribed to then send a message to all the suscribers
            Log_Saved?.Invoke(null, new Logger.LogSaved(path));
        }
        #endregion
        /// <summary>
        /// This functions sets up a set of menus for the logger for the given menu strip, it also 
        /// sets up for auto bolding of when a new log arrives
        /// </summary>
        /// <param name="Main_Menu">Menu strip on the main form</param>
        public static void Setup_Menu(MenuStrip Main_Menu)
        {
            Holding_Menu_Strip = Main_Menu;
            //check to see if the given menu already has an option for the log and if it does
            //use that
            foreach (ToolStripMenuItem itm in Main_Menu.Items)
            {
                //if it finds a log menu then populate that one if necessary
                if (itm.Text.ToUpper() == "LOG")
                    //set the log menu to the one it found
                    Log_Main_Menu = itm;
            }
            //if the log menu was not found then create a new one
            if (Log_Main_Menu == null)
            {
                //create a new menu for the log
                Log_Main_Menu = new ToolStripMenuItem();
                //set the text to log
                Log_Main_Menu.Text = "Log";
                //add the new log to the main menu
                Main_Menu.Items.Add(Log_Main_Menu);
            }
            //search the log to see if it has view log, clear log, and save log menu options
            foreach (ToolStripMenuItem itm in Log_Main_Menu.DropDownItems)
            {
                //if there is a view log option already in the menu
                if (itm.Name.ToUpper() == "VIEW LOG")
                {
                    //set the view log option to this one
                    Show_Log_Option = itm;
                }
                //if there is a clear log option
                if (itm.Name.ToUpper() == "CLEAR LOG")
                {
                    //set the clear log option to the one it found
                    Clear_Log_Option = itm;
                }
                //if there is a save log option
                if (itm.Name.ToUpper() == "SAVE LOG")
                {
                    //set the save log option to this one
                    Save_Log_Option = itm;
                }
            }
            //if no show log option was found then create one and handle it
            if (Show_Log_Option == null)
            {
                //create a new sho log menu option for the master log menu
                Show_Log_Option = new ToolStripMenuItem();
                //set the text to show log
                Show_Log_Option.Text = "Show Log";
                //set the font for normal assuming that the log is empty on creation
                Show_Log_Option.Font = Normal_Font;
                //set a handler to handle when the show log menu option is clicked
                Show_Log_Option.Click += Show_Log_Option_Click;
                //add this new menu option to the master log menu
                Log_Main_Menu.DropDownItems.Add(Show_Log_Option);
            }
            //if a clear log option was not found then create a new one
            if (Clear_Log_Option == null)
            {
                //create a new menu option to clear the log
                Clear_Log_Option = new ToolStripMenuItem();
                //set the menu text to show clear log
                Clear_Log_Option.Text = "Clear Log";
                //start it out as disabled since the log should already be empty
                Clear_Log_Option.Enabled = false;
                //create a handler to handle when the user clicks to clear the log
                Clear_Log_Option.Click += Clear_Log_Option_Click;
                //add it to the master log menu
                Log_Main_Menu.DropDownItems.Add(Clear_Log_Option);
            }
            //if a save menu option was not found on the master log menu then create a new save log
            //menu option
            if (Save_Log_Option == null)
            {
                //create a new save log menu option
                Save_Log_Option = new ToolStripMenuItem();
                //disable the save log menu option assuming that the log is empty
                Save_Log_Option.Enabled = false;
                //set the menu option text to read save log
                Save_Log_Option.Text = "Save Log";
                //create a handler to handle when the save log menu optin is clicked
                Save_Log_Option.Click += Save_Log_Option_Click;
                //add it to the master log menu
                Log_Main_Menu.DropDownItems.Add(Save_Log_Option);
            }
            //create a handler to handle when a new log is received for bolding log menu options
            //and enabling saving and clearing of the log
            New_Log_Received += LoggerMain_New_Log_Received;
            //create a handler to handler when the log is cleared for disabling menu options
            Log_Cleared += LoggerMain_Log_Cleared;
            Gui_Thread_Name = Thread.CurrentThread.Name;
        }
        /// <summary>
        /// Occurs when the log gets cleared this function handles disabling menu options and making
        /// fonts normal again
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private static void LoggerMain_Log_Cleared(object sender, LogCleared e)
        {
            //disable the clear and save menu options since the logs are now empty
            Disable_Clear_And_Save_Log_Menu_Options();
            //set the font on the menus back to normal since there are no more logs
            Set_Show_Log_Menu_Font_To_Normal();
        }
        /// <summary>
        /// This occurs when a new log is sent to the logger, this function enables menu options and
        /// makes menu options bold to show a new log has been received
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private static void LoggerMain_New_Log_Received(object sender, LogRecieved e)
        {
            //if the log window is not currently open
            if (Log_Shower == null || !Log_Shower.IsDisplayed())
            {
                //set the master log menu text to bold and set the show log menu option to bold
                Set_Show_Log_Menu_Font_To_Bold();
                //enable the save and clear log menu options
                Enable_Clear_And_Save_Log_Menu_Options();
            }
        }
        /// <summary>
        /// this occurs when the user clicks the save log menu option, this gets the log path from
        /// the user and sets the initial path to the same one the auto save is using.  It will save
        /// the logs to the path the user specifies
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private static void Save_Log_Option_Click(object sender, EventArgs e)
        {
            //used to store the final save path
            string temp = "";
            //create a save file dialog to get the path to save the log to
            SaveFileDialog diag = new SaveFileDialog();
            //set the filter to a log type
            diag.Filter = "Log|*.txt";
            //is there an auto save log file name already set?
            if (Log_Path != "")
            {
                //does it have a log file extension?
                if (Log_Path.ToUpper().EndsWith(".TXT") || Log_Path.ToUpper().EndsWith(".LOG"))
                {
                    //if it has a directory structure
                    if (Log_Path.Contains("\\"))
                    {
                        //set the initial directory to the same directory the log file is in
                        diag.InitialDirectory = Log_Path.Substring(0, Log_Path.LastIndexOf("\\"));
                        //get the file name of the log file
                        diag.FileName = Log_Path.Substring(Log_Path.LastIndexOf("\\") + 1);
                    }
                }
            }
            //show the dialog and if the user does not cancel it
            if (diag.ShowDialog() != DialogResult.Cancel)
            {
                //get the file name the user chose
                temp = diag.FileName;
                //save the log to the path specified by the user
                Save(temp);
            }

        }
        /// <summary>
        /// occurs when the clear log menu option is clicked, this function calls the clear log and
        /// disables the save and clear menu options since there is no logs to be saved it also 
        /// changes the font back to normal because there are no new logs
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private static void Clear_Log_Option_Click(object sender, EventArgs e)
        {
            //clear the log
            CLEAR();
            //set the font for the log display and main log to normal since there are no new logs
            Set_Show_Log_Menu_Font_To_Normal();
            //disable the clear and save log options
            Disable_Clear_And_Save_Log_Menu_Options();
        }
        /// <summary>
        /// occurs when the show log option is clicked.  It checks to see if a log window already
        /// exists and if one does it shows it otherwise it creates a new one, it also unbolds the 
        /// log and show log menu options since the user is now seeing the new logs
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private static void Show_Log_Option_Click(object sender, EventArgs e)
        {
            //does a log form already exist and is it not closed?
            if (Log_Shower != null && !Log_Shower.IsDisposed)
            {
                //show the log that already exists
                Log_Shower.Show();
                //unbold the log and show log options since the user is now seeing the logs
                Set_Show_Log_Menu_Font_To_Normal();
            }
            else//no log window already exists or the one that was open has been closed
            {
                //create a new log window
                Log_Shower = new Log_Viewer();
                if (Form_Icon != null)
                    Log_Shower.Icon = Form_Icon;
                //show that log window
                Log_Shower.Show();
                //unbold the log and show log options since the user is now seeing the logs
          //      Set_Show_Log_Menu_Font_To_Normal();
            }
        }
        /// <summary>
        /// sets all of its internal settings from the updated settings in the tab
        /// </summary>
        public static void Settings_Updated()
        {
            TimeSpan interval;
            string output_Directory;
            bool autosave_logs;

            //AutoShowLogFile;
            if (!settings.get_Value("OPEN_LOG_FILE_WHEN_SAVED", out AutoShowLogFile))
            {
                AutoShowLogFile = false;
                Log("unable to read open log file when saved setting, not showing log file when saved as a result!", LogItem.LogType.Warning, "loggerMain");
            }

            if (!settings.get_Value("AUTO_SAVE_LOGS", out autosave_logs))
                Log("unable to autosave log setting, not autosaving log files as a result!", LogItem.LogType.Warning, "loggerMain");

            if (!settings.get_Value("AUTO_SAVE_INTERVAL", out interval))
            {
                Log("unable to read autosave interval from settings, disabling autosave as a result!", LogItem.LogType.Warning, "loggerMain");

                autosave_logs = false;
            }
            if (!settings.get_Value("DEFAULT_LOG_DIRECTORY", out output_Directory))
            {
                Log("unable to read autosave output folder from settings, disabling autosave as a result!", LogItem.LogType.Warning, "loggerMain");
                autosave_logs = false;
            }
            if (autosave_logs)
            {
                Set_Auto_Save_Interval_And_Output_Directory(interval, output_Directory);
            }
            if (!settings.get_Value("SEND_TO_CONSOLE_AS_WELL", out AlsoSendToConsole))
            {
                AlsoSendToConsole = false;
                Log("unable to read also send to console from settings, disabling also send to console as a result!", LogItem.LogType.Warning, "loggerMain");
            }
        }



        /// <summary>
        /// sets the clear log and save log menu options to enabled
        /// </summary>
        private static void Enable_Clear_And_Save_Log_Menu_Options()
        {
            try
            {
                if (Save_Log_Option != null && Save_Log_Option.Owner != null && Save_Log_Option.Owner.IsHandleCreated && Save_Log_Option.Owner.InvokeRequired)
                {
                    UpdateVoid D = new UpdateVoid(Enable_Clear_And_Save_Log_Menu_Options);
                    try
                    {
                        if (Save_Log_Option.GetCurrentParent() != null)
                        {
                            Save_Log_Option.GetCurrentParent().Invoke(D);
                        }
                    }
                    catch
                    { }
                }
                else
                {
                    try
                    {
                        //enable the save log option
                        Save_Log_Option.Enabled = true;
                        //enable the clear log menu option
                        Clear_Log_Option.Enabled = true;
                    }
                    catch
                    {

                    }
                }
            }
            catch
            {

            }

        }
        /// <summary>
        /// sets the clear log and save log menu options to disabled
        /// </summary>
        private static void Disable_Clear_And_Save_Log_Menu_Options()
        {
            try
            {
                if (Save_Log_Option != null && Save_Log_Option.GetCurrentParent() != null && Save_Log_Option.GetCurrentParent().IsHandleCreated)
                {
                    if (Save_Log_Option != null && InvokeRequired())
                    {
                        UpdateVoid D = new UpdateVoid(Disable_Clear_And_Save_Log_Menu_Options);
                        Save_Log_Option.GetCurrentParent().Invoke(D);
                    }
                    else
                    {
                        //disable the save log menu option
                        Save_Log_Option.Enabled = false;
                        //disable the clear log menu option
                        Clear_Log_Option.Enabled = false;
                    }
                }
            }
            catch
            { }
        }
        internal static bool InvokeRequired()
        {
            //there is a glitch in invoke required for C# where sometimes it
            //will say no when it needs one causing it to sometimes be run on
            //the wrong thread, this is a hack that fixes that
            return Thread.CurrentThread.Name != Gui_Thread_Name;
        }
        /// <summary>
        /// sets the log and log viewer font back to normal;
        /// </summary>
        private static void Set_Show_Log_Menu_Font_To_Normal()
        {
            if (InvokeRequired())
            {
                UpdateVoid D = new UpdateVoid(Set_Show_Log_Menu_Font_To_Normal);
                try
                {
                    if (Holding_Menu_Strip != null)
                    {
                        Holding_Menu_Strip.Invoke(D);
                    }

                }
                catch
                {

                }

            }
            else
            {
                try
                {
                    //set the main menu log menu list to normal font
                    Log_Main_Menu.Font = Normal_Font;
                    //set the show log menu option to normal font
                    Show_Log_Option.Font = Normal_Font;
                }
                catch
                {

                }
            }
        }
        /// <summary>
        /// Sets the log and log shower menu options font to bold
        /// </summary>
        private static void Set_Show_Log_Menu_Font_To_Bold()
        {
            //if the log menu exists
            if (Log_Main_Menu != null)
            {
                if(Holding_Menu_Strip!=null)
                {
                    if (Holding_Menu_Strip.InvokeRequired)
                    {
                        UpdateVoid D = new UpdateVoid(Set_Show_Log_Menu_Font_To_Bold);
                        try
                        {
                            Holding_Menu_Strip.Invoke(D);
                        }
                        catch
                        {

                        }
                    }
                }
                else
                {
                    //bold the main menu text
                    Log_Main_Menu.Font = Bold_Font;
                    //if the show log option exists
                    if (Show_Log_Option != null)
                    {
                        //make it bold because a new log exists
                        Show_Log_Option.Font = Bold_Font;
                    }
                    //if the clear log option exists
                    if (Clear_Log_Option != null)
                    {
                        //given making the main menu for log bold, bolds everything, set ths back to 
                        //normal font
                        Clear_Log_Option.Font = Normal_Font;
                    }
                    //if the save log option exists
                    if (Save_Log_Option != null)
                    {
                        //given making the main menu for log bold, bolds everything, set ths back to 
                        //normal font
                        Save_Log_Option.Font = Normal_Font;
                    }
                }
            }

        }
        /// <summary>
        /// Generates Log and adds it to the log list
        /// </summary>
        /// <param name="message">Log Message</param>
        /// <param name="level">Level of the log (Normal Log, Warning Log, Error Log)</param>
        public static void Log(string message, LogItem.LogType level, string source = "Unknown", object Sender = null)
        {
            Log_Made = true;
            //store the log time
            DateTime LogTime = DateTime.Now;
            //if the log is not yet important then check the incoming log for important words


            //Generate a new log item
            LogItem log = new LogItem();
            log.source = source;
            log.message = message;
            log.TimeLogged = LogTime;
            log.type = level;
            if (!sources.Contains(source))
            {
                sources.AddLast(source);
            }
            //add the log to the log list
            AddLog(log, Sender);

        }
        /// <summary>
        /// setup an autosave routine to save the log at regular intervals to save as much data as possible in event
        /// of crash or autosave at close
        /// </summary>
        /// <param name="interval">How often to save the log</param>
        /// <param name="output_Directory">directory to save the logs to</param>
        public static void Set_Auto_Save_Interval_And_Output_Directory(TimeSpan interval, string output_Directory)
        {
            //remove all ending \ so that you have a normal directory path
            while (Default_Directory_to_Save_logs_Too.EndsWith("\\"))
            {
                //remove the last \
                Default_Directory_to_Save_logs_Too = Default_Directory_to_Save_logs_Too.Substring(0, Default_Directory_to_Save_logs_Too.LastIndexOf('\\'));
            }
            //set the interval between autosaves to the set interval
            auto_log_Interval = interval;
            //set the default directory to save the logs to, to the given output directory
            Default_Directory_to_Save_logs_Too = output_Directory;
            //if the directory does not exist
            if (!Directory.Exists(Default_Directory_to_Save_logs_Too))
                //create the output directory for the logs
                Directory.CreateDirectory(Default_Directory_to_Save_logs_Too);
            //create the file path for a file in the output directory using the date and start time
            Log_Path = Default_Directory_to_Save_logs_Too + "\\Log_" + DateTime.Now.ToString("MM-dd-yyyy_hh.mm.ss") + ".txt";
            //if it is not already running
            if (!Running)
            {
                //start it running
                Running = true;
                //if an interval was given that is greater than 0
                if (auto_log_Interval != TimeSpan.Zero)
                {
                    //log that auto save has been activated
                    Log("Started Auto Save log interval of " + auto_log_Interval.ToString(@"hh\:mm\:ss"), LogItem.LogType.Log, "LoggerMain");
                    //create a thread to save the log over the given time period
                    Auto_Save_Worker = new Thread(Monitor_And_AutoSave);
                    //start the thread to save the log between the interval
                    Auto_Save_Worker.Start();
                }
            }
            else//if it is already running
            {
                //log what the interval and path was set/changed to
                Log("Set Auto Save log interval to " + auto_log_Interval.ToString(@"hh\:mm\:ss") + " and set output path to \"" + Log_Path + "\"", LogItem.LogType.Log, "LoggerMain");
            }
        }
        /// <summary>
        /// This function saves the log to a preset log file in a preset interval see 
        /// Set_Auto_Save_Interval_And_Output_Directory
        /// </summary>
        internal static void Monitor_And_AutoSave()
        {
            //if the interval is set to zero then terminate this function before it starts
            if (auto_log_Interval == TimeSpan.Zero)
                //terminate this function before it starts
                return;
            //set the current duration between the start of this function to 0
            TimeSpan current_Duration = TimeSpan.Zero;
            //set the auto log start to now
            Last_Save = DateTime.Now;
            //while it is running
            while (Running)
            {
                //if the auto log interval is set to zero then terminate this function
                if (auto_log_Interval == TimeSpan.Zero)
                    //terminate this function
                    return;
                //calculate the current duration between now and the last time it was saved
                current_Duration = DateTime.Now.Subtract(Last_Save);
                //if the current duration less the last save is greater than the interval then
                //calculate the time until the next save and update the log form if displayed
                //with the time until the next autosave
                if (current_Duration.Ticks < auto_log_Interval.Ticks)
                {
                    //calculate the time until the next save by subtracting the duration from the 
                    //interval amount
                    Time_Until_Next_Autosave = auto_log_Interval.Subtract(current_Duration);
                    //if the log showing form is active
                    if (Log_Shower != null)
                    {
                        //set the time left text to the time left until the next autosave calculated
                        //earlier
                        Log_Shower.Set_Auto_Log_Time_Remaining_Text("Time until next log autosave: " + Time_Until_Next_Autosave.ToString(@"hh\:mm\:ss"));
                    }
                }
                else//if the current duration after the last save is greater than the interval set
                {
                    //if a log has been made since the last autosave and the log is now different
                    if (Log_Made)
                    {
                        //if the log showing form is active
                        if (Log_Shower != null)
                        {
                            //set the time left text to say that it is currently autosaving
                            Log_Shower.Set_Auto_Log_Time_Remaining_Text("Auto Saving..");
                        }
                        //save to the path pre-calculated to update the log the save function also
                        //sets the last save time to now
                        Save(Log_Path);
                        //log that it autosaved the log
                        Log("Autosaved log to \"" + Log_Path + "\"", LogItem.LogType.Log, "LoggerMain");
                        if (AutoShowLogFile)
                            CommonCore.CommonCore.OpenNotePadPP(Log_Path);
                        //clear the new log flag since this log of the save should not be sufficient
                        //to trigger a new autosave
                        Log_Made = false;
                    }
                    else
                    {

                        //if the log showing form is active
                        if (Log_Shower != null)
                        {
                            //set the time remaining text to say that it is waiting for a new log
                            //entry before saving again since if it were to save now the log file
                            //would not change
                            Log_Shower.Set_Auto_Log_Time_Remaining_Text("Awaiting log entry..");
                        }
                    }
                }
                //wait half a second then check again
                Thread.Sleep(500);
            }
            //after it is done running save the log again
            Save(Log_Path);
            //if the log showing form is active
            if (Log_Shower != null)
                //hide the time until autosave interval since it is no longer autosaving if it gets
                //here
                Log_Shower.Hide_Auto_Lot_Time_Remaining_Text();
        }
        /// <summary>
        /// this function terminates the autolog save feature
        /// </summary>
        public static void End_Auto_log()
        {
            //log that the auto save was stopped
            Log("Stopped Auto Save log interval", LogItem.LogType.Log, "LoggerMain");
            //clear the running flag so that the auto save stops
            Running = false;
            //set the autosave interval to zero to make it stop autosaving as well
            auto_log_Interval = TimeSpan.Zero;
        }
        /// <summary>
        /// Add the log item to the log list
        /// also sends notification that a new log was received
        /// </summary>
        /// <param name="evnt">Log item (Message, Level, time of the log)</param>
        /// <param name="sender">Calling object</param>
        private static void AddLog(LogItem evnt, object sender, bool Ignore_saving = false)
        {
            try
            {
                if (Saving && !Ignore_saving)
                {
                    Temp_Buffer.Enqueue(evnt);
                    return;
                }
                long ticks;
                //stores a temp list that has all the logs for all the sources for that log level
                SortedList<string, SortedList<long, LinkedList<LogItem>>> Source_tempList;
                //stores a temp list that has all the logs for that log level
                SortedList<long, LinkedList<LogItem>> tempList;
                //in case more than one entry is at the same time
                LinkedList<LogItem> tempLinkedList;
                //clears the save flag since a new message was just added
                Saved = false;
                //if the log time was not set
                if (evnt.TimeLogged == new DateTime())
                    //set the log time to the current time
                    evnt.TimeLogged = DateTime.Now;
                ticks = evnt.TimeLogged.Ticks;
                //get all the logs that match the same level of this log if any exist
                if (All_Logs.TryGetValue(evnt.type, out Source_tempList))
                {
                    //get all the logs for the source that this log is from
                    if (Source_tempList.TryGetValue(evnt.source, out tempList))
                    {
                        //get all the logs at the same time this log was made
                        if (tempList.TryGetValue(ticks, out tempLinkedList))
                        {
                            //add this log to the list of logs that match this type and this time
                            tempLinkedList.AddLast(evnt);
                        }
                        else//if no logs exist for this time 
                        {
                            //create a new list for this time
                            tempLinkedList = new LinkedList<LogItem>();
                            //add this log to this list for this time
                            tempLinkedList.AddLast(evnt);
                            if (!tempList.ContainsKey(ticks))
                            {
                                //add this log to the list of logs that match this time
                                tempList.Add(ticks, tempLinkedList);
                            }
                            else
                            {
                                System.Console.WriteLine("Impossible!");
                            }
                        }
                    }
                    else//if no logs exist for this source
                    {
                        tempList = new SortedList<long, LinkedList<LogItem>>();
                        tempLinkedList = new LinkedList<LogItem>();
                        //add this log to that list
                        tempLinkedList.AddLast(evnt);
                        if (!tempList.ContainsKey(evnt.TimeLogged.Ticks))
                        {
                            //add this log to the list of logs that match this time
                            tempList.Add(ticks, tempLinkedList);
                        }
                        else
                        {
                            System.Console.WriteLine("Impossible!");
                        }

                        //add this log type list to all the logs Sorted by type
                        Source_tempList.Add(evnt.source, tempList);
                    }


                }
                else//if there are no logs of this type
                {
                    //create new list for the source and then the log sourted by time
                    Source_tempList = new SortedList<string, SortedList<long, LinkedList<LogItem>>>();
                    //create a new list for this log sorted by time
                    tempList = new SortedList<long, LinkedList<LogItem>>();
                    tempLinkedList = new LinkedList<LogItem>();
                    //add this log to that list
                    tempLinkedList.AddLast(evnt);
                    if (!tempList.ContainsKey(ticks))
                    {
                        //add this log to the list of logs that match this time
                        tempList.Add(ticks, tempLinkedList);
                    }
                    else
                    {
                        System.Console.WriteLine("Impossible!");
                    }
                    Source_tempList.Add(evnt.source, tempList);
                    //add this log type list to all the logs Sorted by type
                    All_Logs.Add(evnt.type, Source_tempList);
                }
                //update the appropriate counter based on the log level or log type
                switch (evnt.type)
                {
                    case LogItem.LogType.Error:
                        TotalErrors++;
                        break;
                    case LogItem.LogType.Log:
                        TotalStandardLogs++;
                        break;
                    case LogItem.LogType.Warning:
                        TotalWarnings++;
                        break;
                }
                //if the user wants this also sent to the console then send this log to the console
                if (AlsoSendToConsole)
                {
                    switch (evnt.type)
                    {
                        case LogItem.LogType.Log://if a basic log
                                                 //if the log time should be added before the log
                            if (AppendLogTime)
                                //show the time plus the log message
                                System.Console.WriteLine(evnt.TimeLogged.ToShortTimeString() + ": " + evnt.message);
                            else
                                //just show the log message
                                System.Console.WriteLine(evnt.message);
                            break;
                        case LogItem.LogType.Warning://if a warning log
                                                     //if the time should be added before the log
                            if (AppendLogTime)
                                //show log the time plus the log type plus the log message
                                System.Console.WriteLine(evnt.TimeLogged.ToShortTimeString() + ": Warning " + evnt.message);
                            else
                                //show the log type plus the log message
                                System.Console.WriteLine("Warning " + evnt.message);
                            break;
                        case LogItem.LogType.Error://if it is an error log
                                                   //if the time should be added before the log
                            if (AppendLogTime)
                                //show log the time plus the log type plus the log message
                                System.Console.WriteLine(evnt.TimeLogged.ToShortTimeString() + ": Error " + evnt.message);
                            else
                                //show the log type plus the log message
                                System.Console.WriteLine("Error " + evnt.message);
                            break;
                    }
                }

                //generate the status message of all the logs and include this log message
                LogRecieved.LogStats stats = new LogRecieved.LogStats();
                stats.BasicLogCount = TotalStandardLogs;
                stats.WarningLogCount = TotalWarnings;
                stats.ErrorCount = TotalErrors;
                stats.Log = evnt;
                //notify the listeners that a new log has been created and include the log stats plus the log message
                NewLogReceived(sender, new LogRecieved(stats));
            }
            catch { }
        }
        /// <summary>
        /// Returns the number of basic logs, Warning logs, and error logs
        /// </summary>
        /// <returns>The statistics which is the total number of basic, warning, and error logs</returns>
        public static LogRecieved.LogStats GetLogStats()
        {
            LogRecieved.LogStats output = new LogRecieved.LogStats();
            output.BasicLogCount = TotalStandardLogs;
            output.WarningLogCount = TotalWarnings;
            output.ErrorCount = TotalErrors;
            return output;
        }
        private static void AppendToOutput(KeyValuePair<long, LinkedList<LogItem>> list, ref SortedList<long, LinkedList<LogItem>> output)
        {

            LinkedList<LogItem> templist;
            LinkedListNode<LogItem> node;
            if (output.TryGetValue(list.Key, out templist))
            {
                output.Remove(list.Key);
                if (list.Value != null)
                {
                    node = list.Value.First;
                    while (node != null)
                    {
                        templist.AddLast(node.Value);
                        node = node.Next;
                    }
                    output.Add(list.Key, templist);
                }

            }
            else
            {
                output.Add(list.Key, list.Value);
            }


        }
        /// <summary>
        /// Get the actual log messages filtered by log level or type
        /// </summary>
        /// <param name="IncludeLogs">Should the returned log list include basic logs</param>
        /// <param name="IncludeWarnings">Should the returned log list include warning logs</param>
        /// <param name="IncludeErrors">Should the returned log list include error logs</param>
        /// <returns>A time sorted list of all the logs that are requested</returns>
        public static SortedList<long, LinkedList<LogItem>> GetLogs(string[] sources = null, bool IncludeLogs = true, bool IncludeWarnings = true, bool IncludeErrors = true)
        {
            //output list of logs sorted by time
            SortedList<long, LinkedList<LogItem>> output = new SortedList<long, LinkedList<LogItem>>();
            //temporary source, sorted list then time sorted list of logs for the log level being checked
            SortedList<string, SortedList<long, LinkedList<LogItem>>> Source_Temp_List;
            //temporary time sorted list of logs for the log level being checked.
            SortedList<long, LinkedList<LogItem>> TempList;
            //if basic lobs are to be included
            if (IncludeLogs)
            {
                //get all the basic logs
                if (All_Logs.TryGetValue(LogItem.LogType.Log, out Source_Temp_List))
                {
                    if (sources == null)
                    {
                        foreach (KeyValuePair<string, SortedList<long, LinkedList<LogItem>>> svp in Source_Temp_List)
                        {
                            //for each basic log add it to the output list
                            foreach (KeyValuePair<long, LinkedList<LogItem>> kvp in svp.Value)
                            {
                                AppendToOutput(kvp, ref output);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < sources.Length; i++)
                        {
                            if (Source_Temp_List.TryGetValue(sources[i], out TempList))
                            {
                                //for each basic log add it to the output list
                                foreach (KeyValuePair<long, LinkedList<LogItem>> kvp in TempList)
                                {
                                    AppendToOutput(kvp, ref output);
                                }
                            }
                        }
                    }
                }
            }
            //if warning logs are to be included
            if (IncludeWarnings)
            {
                //get all the warning logs
                if (All_Logs.TryGetValue(LogItem.LogType.Warning, out Source_Temp_List))
                {
                    if (sources == null)
                    {
                        foreach (KeyValuePair<string, SortedList<long, LinkedList<LogItem>>> svp in Source_Temp_List)
                        {
                            //for each basic log add it to the output list
                            foreach (KeyValuePair<long, LinkedList<LogItem>> kvp in svp.Value)
                            {
                                AppendToOutput(kvp, ref output);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < sources.Length; i++)
                        {
                            if (Source_Temp_List.TryGetValue(sources[i], out TempList))
                            {
                                //for each basic log add it to the output list
                                foreach (KeyValuePair<long, LinkedList<LogItem>> kvp in TempList)
                                {
                                    AppendToOutput(kvp, ref output);
                                }
                            }
                        }
                    }
                }
            }
            //if error logs are to be included
            if (IncludeErrors)
            {
                //get all the error logs
                if (All_Logs.TryGetValue(LogItem.LogType.Error, out Source_Temp_List))
                {
                    if (sources == null)
                    {
                        foreach (KeyValuePair<string, SortedList<long, LinkedList<LogItem>>> svp in Source_Temp_List)
                        {
                            //for each basic log add it to the output list
                            foreach (KeyValuePair<long, LinkedList<LogItem>> kvp in svp.Value)
                            {
                                AppendToOutput(kvp, ref output);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < sources.Length; i++)
                        {
                            if (Source_Temp_List.TryGetValue(sources[i], out TempList))
                            {
                                //for each basic log add it to the output list
                                foreach (KeyValuePair<long, LinkedList<LogItem>> kvp in TempList)
                                {
                                    AppendToOutput(kvp, ref output);
                                }
                            }
                        }
                    }
                }
            }
            //return all the selected logs
            return output;
        }
        public static void CloseLogForm()
        {
            //if a form does not already exist
            if (Log_Shower != null && !Log_Shower.IsDisposed)
            {
                Log_Shower.Close();
                Log_Shower.Dispose();
            }
        }



        /// <summary>
        /// Shows a form which shows the logs
        /// The form allows you to filter the logs based on type
        /// The form allows you to clear the logs
        /// the form also allows you to save the logs
        /// </summary>
        public static void ShowLogForm()
        {

            //if a form does not already exist
            if (Log_Shower == null)
            {
                //create a new form
                Log_Shower = new Log_Viewer();
                //show the form to the user
                Log_Shower.Show();
            }
            else //if the form does exist
            {
                //was the form closed?
                if (Log_Shower.WasClosed)
                {
                    //release the resources of the form
                    Log_Shower.Dispose();
                    //create a new form
                    Log_Shower = new Log_Viewer();
                    //show it to the user
                    Log_Shower.Show();
                }
                else
                {

                    //Show the form and bring to the front
                    Log_Shower.Restore_Form();


                }
            }
        }
        /// <summary>
        /// Delete's all data in the log
        /// </summary>
        /// <param name="Sender">Object that requested the log to be deleted</param>
        public static void CLEAR(object Sender = null, bool Show_Warning = true)
        {
            //reset all counters to zero
            TotalErrors = 0;
            TotalWarnings = 0;
            TotalStandardLogs = 0;
            //for each log type clear that log list
            foreach (KeyValuePair<LogItem.LogType, SortedList<string, SortedList<long, LinkedList<LogItem>>>> kvp in All_Logs)
            {
                foreach (KeyValuePair<string, SortedList<long, LinkedList<LogItem>>> svp in kvp.Value)
                {
                    foreach (KeyValuePair<long, LinkedList<LogItem>> dvt in svp.Value)
                    {
                        dvt.Value.Clear();
                    }
                    svp.Value.Clear();
                }
                kvp.Value.Clear();
            }
            //clear all logs
            All_Logs.Clear();
            //notify the listeners that the log was cleared
            LogCleared(Sender, Show_Warning);

        }
        private static bool hasType(LogItem.LogType type, LogItem.LogType[] Types)
        {
            for (int i = 0; i < Types.Length; i++)
            {
                if (Types[i] == type)
                    return true;
            }
            return false;
        }
        public static string GetSelectLogText(LogItem.LogType[] Types)
        {
            SortedList<long, LinkedList<string>> logs = new SortedList<long, LinkedList<string>>();
            //get all the logs from the system
            SortedList<long, LinkedList<LogItem>> AllLogs = GetLogs();
            LinkedListNode<LogItem> node;
            string output = "";
            //for each log in the system
            foreach (KeyValuePair<long, LinkedList<LogItem>> kvp in AllLogs)
            {
                node = kvp.Value.First;
                while (node != null)
                {
                    //if the user wants the time appended to the log message?
                    if (AppendLogTime && hasType(node.Value.type, Types))
                        //append the log time to the log line
                        output += new DateTime(kvp.Key).ToShortTimeString() + ": ";

                    switch (node.Value.type)
                    {
                        case LogItem.LogType.Log://if it is a basic log
                                                 //write the message to the log text then go to the next line
                            if (hasType(LogItem.LogType.Log, Types))
                                output += node.Value.message + "\r\n";
                            break;
                        case LogItem.LogType.Warning://if it is a warning log
                                                     //write the word warning then the log message then go to the next line
                            if (hasType(LogItem.LogType.Warning, Types))
                                output += "Warning " + node.Value.message + "\r\n";
                            break;
                        case LogItem.LogType.Error://if it is an error log
                                                   //write the word error then the log message then go to the next line
                            if (hasType(LogItem.LogType.Error, Types))
                                output += "ERROR " + node.Value.message + "\r\n";
                            break;
                    }
                    node = node.Next;
                }

            }
            return output;
        }
        /// <summary>
        /// Get the log text of all the logs in this logger
        /// </summary>
        /// <returns>Log text of all the logs</returns>
        public static string GetFullLogtext()
        {
            //output text
            string output = "";
            //get all the logs from the system
            SortedList<long, LinkedList<LogItem>> AllLogs = GetLogs();
            LinkedListNode<LogItem> node;
            //for each log in the system
            foreach (KeyValuePair<long, LinkedList<LogItem>> kvp in AllLogs)
            {
                node = kvp.Value.First;
                while (node != null)
                {
                    //if the user wants the time appended to the log message?
                    if (AppendLogTime)
                        //append the log time to the log line
                        output += new DateTime(kvp.Key).ToShortTimeString() + ": ";

                    switch (node.Value.type)
                    {
                        case LogItem.LogType.Log://if it is a basic log
                                                 //write the message to the log text then go to the next line
                            output += node.Value.message + "\r\n";
                            break;
                        case LogItem.LogType.Warning://if it is a warning log
                                                     //write the word warning then the log message then go to the next line
                            output += "Warning " + node.Value.message + "\r\n";
                            break;
                        case LogItem.LogType.Error://if it is an error log
                                                   //write the word error then the log message then go to the next line
                            output += "ERROR " + node.Value.message + "\r\n";
                            break;
                    }
                    node = node.Next;
                }
            }
            //return the full log text
            return output;
        }
        /// <summary>
        /// returns true if the log is empty
        /// </summary>
        /// <returns>true if the log is empty</returns>
        public static bool IsEmpty()
        {
            //if the log count is zeor return true otherwise return false
            return All_Logs.Count == 0;
        }
        /// <summary>
        /// Sets the log viewer Icon to a specific icon
        /// </summary>
        /// <param name="icon">Program Icon to be displayed</param>
        public static void Set_Icon(Icon icon)
        {
            Form_Icon = icon;
        }
        /// <summary>
        /// returns if the log has been saved already
        /// </summary>
        /// <returns>True if the log has been saved and no new logs have been added after it was saved</returns>
        public static bool IsSaved()
        {
            //return the saved flag which is set to true when saved and cleared when a new log is added
            return Saved;
        }
        public static string Save()
        {
            return Save(Log_Path);
        }
        public static string Save(string path, LogItem.LogType[] logTypes, object Sender = null)
        {
            //Error status or blank if the save was successful
            string status = "";
            //stream writer to save the log
            StreamWriter file = null;
            try
            {
                //create a file at the file path
                file = new StreamWriter(path);
                //get the full log text and write it to the file buffer
                file.WriteLine(LoggerMain.GetSelectLogText(logTypes));
                //set the status to blank since the save was successful
                status = "";

            }
            //if an error occurs while saving
            catch (Exception ex)
            {
                //set the status to the error message
                status = ex.Message;
            }
            finally
            {
                //if the file was created
                if (file != null)
                {
                    //write the file buffer to the file
                    file.Flush();
                    //close the file
                    file.Close();
                    //release the file resources to the system
                    file.Dispose();
                    //tell the garbage collector that this file variable is no longer needed
                    file = null;
                }
            }
            //if no errors occurred
            if (status == "")
                //notify all listeners that the log was saved
                LogSaved(path);
            //return the status of the save blank or error message
            return status;
        }
        /// <summary>
        /// saves everything in the log to a text file
        /// returns the error message in the event of failure
        /// returns an empty string if success
        /// </summary>
        /// <param name="Path">File path to save to</param>
        /// <returns>Error message if fails or an empty string if success</returns>
        public static string Save(string Path, object Sender = null)
        {
            if (Saving)
                return "Saving";
            Saving = true;
            //Error status or blank if the save was successful
            string status = "";
            //stream writer to save the log
            StreamWriter file = null;
            try
            {
                //create a file at the file path
                file = new StreamWriter(Path);
                //get the full log text and write it to the file buffer
                file.WriteLine(LoggerMain.GetFullLogtext());
                //set the status to blank since the save was successful
                status = "";
                //set the saved flag
                Saved = true;
                //set the last saved time to now
                Last_Save = DateTime.Now;
            }
            //if an error occurs while saving
            catch (Exception ex)
            {
                //set the status to the error message
                status = ex.Message;
            }
            finally
            {
                //if the file was created
                if (file != null)
                {
                    //write the file buffer to the file
                    file.Flush();
                    //close the file
                    file.Close();
                    //release the file resources to the system
                    file.Dispose();
                    //tell the garbage collector that this file variable is no longer needed
                    file = null;
                }

            }

            Empty_Buffer();
            Saving = false;
            //if no errors occurred
            if (status == "")
                //notify all listeners that the log was saved
                LogSaved(Path);
            //return the status of the save blank or error message
            return status;
        }

        private static void Empty_Buffer()
        {
            LogItem temp_log;
            while (Temp_Buffer.Count > 0)
            {
                temp_log = Temp_Buffer.Dequeue();
                AddLog(temp_log, null, true);
            }
        }

        public static string Get_Output_Log_Path()
        {
            return Log_Path;
        }

        public static Settings_Handler.Tab Get_Settings_Tab()
        {
            //  throw new NotImplementedException();

            LinkedList<Settings_Handler.Setting> Settings_list = new LinkedList<Settings_Handler.Setting>();
            Settings_Handler.Setting DEFAULT_LOG_DIRECTORY = new Settings_Handler.Setting();
            DEFAULT_LOG_DIRECTORY.KeyName = "DEFAULT_LOG_DIRECTORY";
            DEFAULT_LOG_DIRECTORY.Display_Name = "Default directory to save the logs to (also used for autosave)";
            DEFAULT_LOG_DIRECTORY.Tab_Key = "LOGGER";
            DEFAULT_LOG_DIRECTORY.Tab_Text = "Logging";
            DEFAULT_LOG_DIRECTORY.value = @"D:\Item_Handler_logs\[CURRENT_YEAR]\[CURRENT_MONTH_NAME]";
            DEFAULT_LOG_DIRECTORY.datatype = Settings_Handler.Setting.DataType.FOLDER;
            Settings_list.AddLast(DEFAULT_LOG_DIRECTORY);
            Settings_Handler.Setting LAST_LOG_SAVE_PATH = new Settings_Handler.Setting();
            LAST_LOG_SAVE_PATH.KeyName = "LAST_LOG_SAVE_PATH";
            LAST_LOG_SAVE_PATH.Display_Name = "Last Log Save Path";
            LAST_LOG_SAVE_PATH.Tab_Key = "LOGGER";
            LAST_LOG_SAVE_PATH.Tab_Text = "Logging";
            LAST_LOG_SAVE_PATH.value = Last_Save_Path;
            LAST_LOG_SAVE_PATH.datatype = Settings_Handler.Setting.DataType.FILE;
            LAST_LOG_SAVE_PATH.READ_ONLY = true;
            Settings_list.AddLast(LAST_LOG_SAVE_PATH);
            Settings_Handler.Setting AUTO_SAVE_LOGS = new Settings_Handler.Setting();
            AUTO_SAVE_LOGS.KeyName = "AUTO_SAVE_LOGS";
            AUTO_SAVE_LOGS.Display_Name = "Auto Save Logs at regular intervals";
            AUTO_SAVE_LOGS.Tab_Key = "LOGGER";
            AUTO_SAVE_LOGS.Tab_Text = "Logging";
            AUTO_SAVE_LOGS.value = @"True";
            AUTO_SAVE_LOGS.datatype = Settings_Handler.Setting.DataType.BOOLEAN;
            Settings_list.AddLast(AUTO_SAVE_LOGS);
            Settings_Handler.Setting AUTO_SAVE_INTERVAL = new Settings_Handler.Setting();
            AUTO_SAVE_INTERVAL.KeyName = "AUTO_SAVE_INTERVAL";
            AUTO_SAVE_INTERVAL.Display_Name = "Time between log autosaves";
            AUTO_SAVE_INTERVAL.Tab_Key = "LOGGER";
            AUTO_SAVE_INTERVAL.Tab_Text = "Logging";
            AUTO_SAVE_INTERVAL.value = "0:0:5:0.0";//5 minutes and 0 miliseconds
            AUTO_SAVE_INTERVAL.Horizontal = false;
            AUTO_SAVE_INTERVAL.datatype = Settings_Handler.Setting.DataType.TIMESPAN;
            Settings_list.AddLast(AUTO_SAVE_INTERVAL);
            Settings_Handler.Setting OPEN_LOG_FILE_WHEN_SAVED = new Settings_Handler.Setting();
            OPEN_LOG_FILE_WHEN_SAVED.KeyName = "OPEN_LOG_FILE_WHEN_SAVED";
            OPEN_LOG_FILE_WHEN_SAVED.Display_Name = "Open log file when saved";
            OPEN_LOG_FILE_WHEN_SAVED.Tab_Key = "LOGGER";
            OPEN_LOG_FILE_WHEN_SAVED.Tab_Text = "Logging";
            OPEN_LOG_FILE_WHEN_SAVED.value = @"True";
            OPEN_LOG_FILE_WHEN_SAVED.datatype = Settings_Handler.Setting.DataType.BOOLEAN;
            Settings_list.AddLast(OPEN_LOG_FILE_WHEN_SAVED);
            Settings_Handler.Setting SEND_TO_CONSOLE_AS_WELL = new Settings_Handler.Setting();
            SEND_TO_CONSOLE_AS_WELL.KeyName = "SEND_TO_CONSOLE_AS_WELL";
            SEND_TO_CONSOLE_AS_WELL.Display_Name = "Send logs to console as well";
            SEND_TO_CONSOLE_AS_WELL.Tab_Key = "LOGGER";
            SEND_TO_CONSOLE_AS_WELL.Tab_Text = "Logging";
            SEND_TO_CONSOLE_AS_WELL.value = @"False";
            SEND_TO_CONSOLE_AS_WELL.datatype = Settings_Handler.Setting.DataType.BOOLEAN;
            Settings_list.AddLast(SEND_TO_CONSOLE_AS_WELL);


            settings = new Settings_Handler.Tab("LOGGER", "Logging", Settings_list);
            settings.KeyName = "LOGGER";
            settings.Display_Name = "Logging";

            return settings;
        }

        public static bool Load_Previous_Detailed_Log(string path)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(path);
                XmlNode Root = doc.FirstChild;
                XmlNode Log_Type_node = Root.FirstChild;
                XmlNode Source_Node;
                XmlNode Date_Node;
                XmlNode Log_Node;
                LogItem.LogType Temp_Log_Type = LogItem.LogType.Log;
                string Source_Name = "";
                DateTime temp_Date = DateTime.MinValue;
                LogItem log;
                SortedList<string, SortedList<long, LinkedList<LogItem>>> Error_Type_List = null;
                SortedList<long, LinkedList<LogItem>> Date_Log_List = null;
                LinkedList<LogItem> Log_List = null;
                while (Log_Type_node != null)
                {
                    Source_Node = Log_Type_node.FirstChild.FirstChild;

                    for (int i = 0; i < Log_Type_node.Attributes.Count; i++)
                    {
                        switch (Log_Type_node.Attributes[i].Name.ToUpper())
                        {
                            case "BASIC_LOG":
                                Temp_Log_Type = LogItem.LogType.Log;
                                break;
                            case "WARNING_LOG":
                                Temp_Log_Type = LogItem.LogType.Warning;
                                break;
                            case "ERROR_LOG":
                                Temp_Log_Type = LogItem.LogType.Error;
                                break;
                        }
                    }
                    if (!All_Logs.TryGetValue(Temp_Log_Type, out Error_Type_List))
                    {
                        Error_Type_List = new SortedList<string, SortedList<long, LinkedList<LogItem>>>();
                    }
                    while (Source_Node != null)
                    {
                        Date_Node = Source_Node.FirstChild;
                        for (int i = 0; i < Source_Node.Attributes.Count; i++)
                        {
                            switch (Source_Node.Attributes[i].Name.ToUpper())
                            {
                                case "LOG_SOURCE":
                                    Source_Name = Source_Node.Attributes[i].Value;
                                    break;
                            }

                        }
                        if (!Error_Type_List.TryGetValue(Source_Name, out Date_Log_List))
                        {
                            Date_Log_List = new SortedList<long, LinkedList<LogItem>>();
                        }
                        while (Date_Node != null)
                        {
                            Log_Node = Date_Node.FirstChild;
                            for (int i = 0; i < Date_Node.Attributes.Count; i++)
                            {
                                switch (Date_Node.Attributes[i].Name.ToUpper())
                                {
                                    case "DATE":
                                        temp_Date = CommonCore.CommonCore.stringToFullDatetime(Date_Node.Attributes[i].Value);
                                        break;
                                }
                            }
                            if (!Date_Log_List.TryGetValue(temp_Date.Ticks, out Log_List))
                            {
                                Log_List = new LinkedList<LogItem>();
                            }
                            while (Log_Node != null)
                            {
                                Log_List.AddLast(new LogItem(Log_Node));
                                Log_Node = Log_Node.NextSibling;
                            }
                            if (!Date_Log_List.ContainsKey(temp_Date.Ticks))
                            {
                                Date_Log_List.Add(temp_Date.Ticks, Log_List);
                            }

                            Date_Node = Date_Node.NextSibling;
                        }
                        if (!Error_Type_List.ContainsKey(Source_Name))
                            Error_Type_List.Add(Source_Name, Date_Log_List);
                        if (!sources.Contains(Source_Name))
                            sources.AddLast(Source_Name);
                        Source_Node = Source_Node.NextSibling;
                    }
                    if (!All_Logs.ContainsKey(Temp_Log_Type))
                    {
                        All_Logs.Add(Temp_Log_Type, Error_Type_List);
                    }
                    Log_Type_node = Log_Type_node.NextSibling;
                    if (sources.Count > 0)
                        Log_Main_Menu.Font = Bold_Font;
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static void Save_Detailed_Log_Database(string path)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement Root = doc.CreateElement("root");
            XmlElement Log_Type_List;
            foreach (KeyValuePair<LogItem.LogType, SortedList<string, SortedList<long, LinkedList<LogItem>>>> tvp in All_Logs)
            {
                switch (tvp.Key)
                {
                    case LogItem.LogType.Log:
                        Log_Type_List = doc.CreateElement("Basic_Log");
                        Log_Type_List.AppendChild(Get_Logs_As_Element(tvp.Value, ref doc));
                        Root.AppendChild(Log_Type_List);
                        break;
                    case LogItem.LogType.Warning:
                        Log_Type_List = doc.CreateElement("Warning_Log");
                        Log_Type_List.AppendChild(Get_Logs_As_Element(tvp.Value, ref doc));
                        Root.AppendChild(Log_Type_List);
                        break;
                    case LogItem.LogType.Error:
                        Log_Type_List = doc.CreateElement("Error_Log");
                        Log_Type_List.AppendChild(Get_Logs_As_Element(tvp.Value, ref doc));
                        Root.AppendChild(Log_Type_List);
                        break;
                }
            }
            doc.AppendChild(Root);
            doc.Save(path);

        }

        private static XmlElement Get_Logs_As_Element(SortedList<string, SortedList<long, LinkedList<LogItem>>> logs, ref XmlDocument doc)
        {
            XmlElement output = doc.CreateElement("Log_Source_List");
            XmlElement Source_Element;
            XmlElement Day_Element;
            LinkedListNode<LogItem> node;
            DateTime temp_Date;
            foreach (KeyValuePair<string, SortedList<long, LinkedList<LogItem>>> svp in logs)
            {
                Source_Element = doc.CreateElement("source");
                Source_Element.SetAttribute("Log_Source", svp.Key);
                foreach (KeyValuePair<long, LinkedList<LogItem>> kvp in svp.Value)
                {
                    temp_Date = new DateTime(kvp.Key);
                    Day_Element = doc.CreateElement("Day");
                    Day_Element.SetAttribute("Date", temp_Date.ToShortDateString() + " " + temp_Date.ToShortTimeString());
                    node = kvp.Value.First;
                    while (node != null)
                    {
                        Day_Element.AppendChild(node.Value.Get_As_Element(ref doc));
                        node = node.Next;
                    }
                    Source_Element.AppendChild(Day_Element);
                }
                output.AppendChild(Source_Element);
            }
            return output;
        }
    }
}
