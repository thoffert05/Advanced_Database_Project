///--------------------------------------------------------------------------------------------------------------------------------------------
/// Log viewer class
/// 
/// This class stores shows the logs and lets the user filter them, clear them, or save them
/// This class also updates as new logs are received
/// 
/// 
/// Author: Anthony Hoffert
///--------------------------------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;

namespace Logger
{
    public partial class Log_Viewer : Form
    {
        /// <summary>
        /// Constructor this sets up the form
        /// </summary>
        public Log_Viewer()
        {
            //set up the form components
            InitializeComponent();
            Gui_Thread_Name = Thread.CurrentThread.Name;
            //listen to the new log received message from the logger main program
            Logger.LoggerMain.New_Log_Received += LoggerMain_New_Log_Received;
            //listen to the log cleared message from the logger main program
            Logger.LoggerMain.Log_Cleared += LoggerMain_Log_Cleared;
            string[] sources = new string[LoggerMain.sources.Count];
            LoggerMain.sources.CopyTo(sources, 0);
            //gets all the logs from the logger and stores them in a temporary list for display
            TempLogs = LoggerMain.GetLogs(sources,checkBox3.Checked, checkBox2.Checked, checkBox1.Checked);
            
            //populate log viewer window with the sources
            if(sources.Length>0)
            {
                filterToolStripMenuItem.Visible = sources.Length > 1;
                for (int i=0;i<sources.Length;i++)
                {
                    temp_menu_Item = new ToolStripMenuItem();
                    temp_menu_Item.Text = "Don't Show \"" + sources[i] + "\"";
                    temp_menu_Item.Click += Temp_menu_Item_Click;
                    filterToolStripMenuItem.DropDownItems.Add(temp_menu_Item);
                    allowed_Sources.AddLast(sources[i]);
                    all_Sources.AddLast(sources[i]);
                }
            }
            else
            {
                filterToolStripMenuItem.Visible = false;
            }
            //get the log status of how many of each type of log level there are in the logger main program
            LogRecieved.LogStats stats = LoggerMain.GetLogStats();
            //update the check boxes to display the number of each level of log
            UpdateCheckBoxText(checkBox3, "Logs (" + stats.BasicLogCount + ")");
            UpdateCheckBoxText(checkBox2, "Warnings (" + stats.WarningLogCount + ")");
            UpdateCheckBoxText(checkBox1, "Errors (" + stats.ErrorCount + ")");
            //populate the main text display with the log text of all the logs
            PopulateLogText();
        }



        string Gui_Thread_Name = "";
        ToolStripMenuItem temp_menu_Item;
        //stores the all the logs of the currently desired type
        SortedList<long, LinkedList<LogItem>> TempLogs;
        LinkedList<string> allowed_Sources = new LinkedList<string>();
        LinkedList<string> all_Sources = new LinkedList<string>();
        //is this form closed defaults to false since it starts out open or not closed
        public bool WasClosed = false;
        #region delegates
        //update a checkbox with a new display text
        public delegate void UpdateCheckBox(CheckBox check, string text);
        public delegate void UpdateToolStripMenuItem(ToolStripMenuItem item);
        private delegate void UpdateVoid();
        private delegate void UpdateString(string str);
        #endregion
        #region delegate helper functions
        /// <summary>
        /// this updates a checkbox displayed text with a new set of text
        /// </summary>
        /// <param name="check">checkbox to update</param>
        /// <param name="text">New display text</param>
        public void UpdateCheckBoxText(CheckBox check, string text)
        {
            //if this checkbox needs to be invoked since it is on a seperate thread
            if (InvokeRequired()||check.InvokeRequired)
            {
                //create a delegate to handle the update
                UpdateCheckBox C = new UpdateCheckBox(UpdateCheckBoxText);
                try
                {
                    //have that checkbox run this function on its thread
                    check.Invoke(C, new object[] { check, text });
                }
                catch
                {

                }
            }
            else
            {
                //set the checkbox display text to the new text
                check.Text = text;
            }
        }
        /// <summary>
        /// sets the text to the log display
        /// </summary>
        /// <param name="s">New log text</param>
        public void SetLogText(string s)
        {
            //if the log display is on a seperate thread
            if (InvokeRequired()||richTextBox1.InvokeRequired)
            {
                //create a delegate to set the text on the thread the log display belongs to
                UpdateString S = new UpdateString(SetLogText);
                //have the log display run that function to update its text on its own thread
                richTextBox1.Invoke(S, new object[] { s });
            }
            else
            {
                //update the log display text to the text provided
                richTextBox1.Text = s;

            }
            
        }
        /// <summary>
        /// Adds a single line of text to the log display
        /// </summary>
        /// <param name="s">line of text</param>
        public void AppendToLogText(string s)
        {
            //if the log display is on a seperate thread
            if (InvokeRequired()||richTextBox1.InvokeRequired)
            {
                //create a delegate to set the text on the thread the log display belongs to
                UpdateString S = new UpdateString(AppendToLogText);

                try
                {
                    //have the log display run that function to update its text on its own thread
                    richTextBox1.Invoke(S, new object[] { s });
                }
                catch
                {

                }
            }
            else
            {

                try
                {
                    //add the line of text to the log display and then move to the next line
                    richTextBox1.AppendText(s+"\r\n");
                }
                catch
                {

                }
                
               
            }
        }
        public void AddDropDownOptionToDropDownMenu(ToolStripMenuItem item)
        {
            if(InvokeRequired()||richTextBox1.InvokeRequired)
            {
                UpdateToolStripMenuItem D = new UpdateToolStripMenuItem(AddDropDownOptionToDropDownMenu);
                try
                { 
                richTextBox1.Invoke(D, new object[] { item });
                }
                catch
                {
                    
                }
            }
            else
            {
                filterToolStripMenuItem.DropDownItems.Add(item);
            }
        }
        /// <summary>
        /// if autosave is enabled then set the text to the remaining time until the next autosave
        /// this function will update the text and display the auto save time remaining
        /// </summary>
        /// <param name="str">time until next autosave</param>
        public void Set_Auto_Log_Time_Remaining_Text(string str)
        {
            if (InvokeRequired()||textBox2.InvokeRequired)
            {
                try
                {
                    UpdateString S = new Log_Viewer.UpdateString(Set_Auto_Log_Time_Remaining_Text);
                    textBox2.Invoke(S, new object[] { str });
                }
                catch
                {

                }
            }
            else
            {
                try
                {
                    textBox2.Visible = true;
                    textBox2.Text = str;
                }
                catch
                {

                }
            }
        }
        /// <summary>
        /// this will hide the autosave time for when the autosave is disabled
        /// </summary>
        public void Hide_Auto_Lot_Time_Remaining_Text()
        {
            if (InvokeRequired()||textBox2.InvokeRequired)
            {
                UpdateVoid D = new UpdateVoid(Hide_Auto_Lot_Time_Remaining_Text);
                try
                {
                    textBox2.Invoke(D);
                }
                catch
                {

                }
            }
            else
            {
                textBox2.Visible = false;
            }
        }
        #endregion


        private void Temp_menu_Item_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem temp = (ToolStripMenuItem)sender;
            System.Console.WriteLine("Clicked: " + temp.Text);
            bool show = !temp.Text.StartsWith("Don't Show");

            string Source = temp.Text.Substring(temp.Text.IndexOf("\"") + 1);
            Source = Source.Substring(0, Source.LastIndexOf("\""));
            if (!show)
            {
                allowed_Sources.Remove(Source);
                temp.Text = "Show \"" + Source + "\"";
            }
            else
            {
                temp.Text = "Don't Show \"" + Source + "\"";
                allowed_Sources.AddLast(Source);
            }
            PopulateLogText();
        }
        /// <summary>
        /// occurs when the main logger program clears its logs
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void LoggerMain_Log_Cleared(object sender, LogCleared e)
        {
            //clear the temporary logs
            TempLogs.Clear();
            //clear the log display
            SetLogText("");
            //reset all checkboxes to reflect no logs
            UpdateCheckBoxText(checkBox3, "Logs (0)");
            UpdateCheckBoxText(checkBox2, "Warnings (0)");
            UpdateCheckBoxText(checkBox1, "Errors (0)");
            if (e.Show_Warning)
            {
                //tell the user that the logs were cleared
                MessageBox.Show("Logs Cleared!");
            }
        }
        /// <summary>
        /// Occures when the main logger program receives a new log
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void LoggerMain_New_Log_Received(object sender, LogRecieved e)
        {
            try
            {
                if (!all_Sources.Contains(e.LogStatisticsAndLog.Log.source))
                {
                    all_Sources.AddLast(e.LogStatisticsAndLog.Log.source);
                    allowed_Sources.AddLast(e.LogStatisticsAndLog.Log.source);
                    temp_menu_Item = new ToolStripMenuItem();
                    temp_menu_Item.Text = "Don't Show \"" + e.LogStatisticsAndLog.Log.source + "\"";
                    temp_menu_Item.Click += Temp_menu_Item_Click;
                    AddDropDownOptionToDropDownMenu(temp_menu_Item);
                }

                LinkedList<Logger.LogItem> tempLinkedList;
                //based on the received log type
                switch (e.LogStatisticsAndLog.Log.type)
                {


                    case Logger.LogItem.LogType.Log://if it is a basic log
                                                //if the user wants to see basic logs
                        if (checkBox3.Checked)
                        {
                            //add this log to the temporary list of logs of this type
                            if (TempLogs.TryGetValue(e.LogStatisticsAndLog.Log.TimeLogged.Ticks, out tempLinkedList))
                            {
                                tempLinkedList.AddLast(e.LogStatisticsAndLog.Log);
                            }
                            else
                            {
                                tempLinkedList = new LinkedList<Logger.LogItem>();
                                tempLinkedList.AddLast(e.LogStatisticsAndLog.Log);
                                if (!TempLogs.ContainsKey(e.LogStatisticsAndLog.Log.TimeLogged.Ticks))
                                    TempLogs.Add(e.LogStatisticsAndLog.Log.TimeLogged.Ticks, tempLinkedList);
                                else
                                    System.Console.WriteLine("Another Impossible error!");
                            }


                            //if the main logger wants to append the log time
                            if (LoggerMain.AppendLogTime)
                                //add the time the log arrived plus the log message to the main log text
                                AppendToLogText(e.LogStatisticsAndLog.Log.TimeLogged.ToShortTimeString() + ": " + e.LogStatisticsAndLog.Log.source + ": " + e.LogStatisticsAndLog.Log.message);
                            else
                                //add the log message to the main log text
                                AppendToLogText(e.LogStatisticsAndLog.Log.message);
                        }
                        break;
                    case Logger.LogItem.LogType.Warning://if it is a warning log
                                                    //if the user wants to see warning logs
                        if (checkBox2.Checked)
                        {
                            //add this log to the temprorary list of logs of this type
                            if (TempLogs.TryGetValue(e.LogStatisticsAndLog.Log.TimeLogged.Ticks, out tempLinkedList))
                            {
                                tempLinkedList.AddLast(e.LogStatisticsAndLog.Log);
                            }
                            else
                            {
                                tempLinkedList = new LinkedList<Logger.LogItem>();
                                tempLinkedList.AddLast(e.LogStatisticsAndLog.Log);
                                TempLogs.Add(e.LogStatisticsAndLog.Log.TimeLogged.Ticks, tempLinkedList);
                            }
                            //if the main loger wants to append the log time
                            if (LoggerMain.AppendLogTime)
                                //add the time the log arrived plus the word warning plus the log message to the main log text
                                AppendToLogText(e.LogStatisticsAndLog.Log.TimeLogged.ToShortTimeString() + ":Warning " + e.LogStatisticsAndLog.Log.source + ": " + e.LogStatisticsAndLog.Log.message);
                            else
                                //add the word warning plus the log message to the main log text
                                AppendToLogText("Warning " + e.LogStatisticsAndLog.Log.message);
                        }
                        break;
                    case Logger.LogItem.LogType.Error://an error log
                        if (checkBox1.Checked)
                        {
                            //add this log to the temprorary list of logs of this type
                            if (TempLogs.TryGetValue(e.LogStatisticsAndLog.Log.TimeLogged.Ticks, out tempLinkedList))
                            {
                                tempLinkedList.AddLast(e.LogStatisticsAndLog.Log);
                            }
                            else
                            {
                                tempLinkedList = new LinkedList<Logger.LogItem>();
                                tempLinkedList.AddLast(e.LogStatisticsAndLog.Log);
                                TempLogs.Add(e.LogStatisticsAndLog.Log.TimeLogged.Ticks, tempLinkedList);
                            }
                            //if the main loger wants to append the log time
                            if (LoggerMain.AppendLogTime)
                                //add the time the log arrived plus the word error plus the log message to the main log text
                                AppendToLogText(e.LogStatisticsAndLog.Log.TimeLogged.ToShortTimeString() + ": Error " + e.LogStatisticsAndLog.Log.message);
                            else
                                //add the  word error plus the log message to the main log text
                                AppendToLogText("Error " + e.LogStatisticsAndLog.Log.source + ": " + e.LogStatisticsAndLog.Log.message);
                        }
                        break;
                }
                //update the check box counts
                UpdateCheckBoxText(checkBox3, "Logs (" + e.LogStatisticsAndLog.BasicLogCount + ")");
                UpdateCheckBoxText(checkBox2, "Warnings (" + e.LogStatisticsAndLog.WarningLogCount + ")");
                UpdateCheckBoxText(checkBox1, "Errors (" + e.LogStatisticsAndLog.ErrorCount + ")");
            }
            catch (Exception ex)
            {

            }
        }

        internal bool IsDisplayed()
        {
            return !WasClosed;
        }

        /// <summary>
        /// populate the log text based on the logs in the temporary log which is filtered by log type
        /// This is a separate object to keep all the logs in time order since logs of one type could arrive before logs of a different type
        /// </summary>
        private void PopulateLogText()
        {
            
            //display text
            string text = "";
            LinkedListNode<Logger.LogItem> node;
            //foreach log in the temprary filtered log list
            foreach (KeyValuePair<long,LinkedList<Logger.LogItem>>kvp in TempLogs)
            {
                node = kvp.Value.First;
                while (node != null)
                {
                    if (!(allowed_Sources.Count > 0 && allowed_Sources.Contains(node.Value.source)))
                    {
                        node = node.Next;
                        continue;
                    }
                    switch (node.Value.type)
                    {
                        case Logger.LogItem.LogType.Log://for basic logs
                                                    //if the main form wants the time added before the log
                            if (LoggerMain.Show_Source)
                            {
                                text += node.Value.source + ": ";
                                if (LoggerMain.AppendLogTime)
                                    //display the log time followed by the log message
                                    text += node.Value.TimeLogged.ToShortTimeString() + ": " + node.Value.message + "\r\n";
                                else
                                    //display the log message
                                    text += node.Value.message + "\r\n";
                            }
                            else
                            {
                                if (LoggerMain.AppendLogTime)
                                    //display the log time followed by the log message
                                    text += node.Value.TimeLogged.ToShortTimeString() + ": " + node.Value.message + "\r\n";
                                else
                                    //display the log message
                                    text += node.Value.message + "\r\n";
                            }
                            break;
                        case Logger.LogItem.LogType.Warning://for warning logs
                                                        //if the main form wants the time added before the log
                            if (LoggerMain.Show_Source)
                            {
                                text += node.Value.source + ": ";
                                if (LoggerMain.AppendLogTime)
                                    //display the log time followed by the log message
                                    text += node.Value.TimeLogged.ToShortTimeString() + ": Warning: " + node.Value.message + "\r\n";
                                else
                                    //display the log message
                                    text += "Warning: "+node.Value.message + "\r\n";
                            }
                            else
                            {
                                if (LoggerMain.AppendLogTime)
                                    //display the log time followed by the log message
                                    text += node.Value.TimeLogged.ToShortTimeString() + ": Warning: " + node.Value.message + "\r\n";
                                else
                                    //display the log message
                                    text +="Warning: "+ node.Value.message + "\r\n";
                            }
                            break;
                        case Logger.LogItem.LogType.Error://for error logs
                                                      //if the main form wants the time added before the log
                            if (LoggerMain.Show_Source)
                            {
                                text += node.Value.source + ": ";
                                if (LoggerMain.AppendLogTime)
                                    //display the log time followed by the log message
                                    text += node.Value.TimeLogged.ToShortTimeString() + ": Error: " + node.Value.message + "!\r\n";
                                else
                                    //display the log message
                                    text += "Error: " + node.Value.message + "!\r\n";
                            }
                            else
                            {
                                if (LoggerMain.AppendLogTime)
                                    //display the log time followed by the log message
                                    text += node.Value.TimeLogged.ToShortTimeString() + ": Error: " + node.Value.message + "!\r\n";
                                else
                                    //display the log message
                                    text += "Error: " + node.Value.message + "!\r\n";
                            }
                            break;
                    }
                    node = node.Next;
                }
            }
            //set the generated total log text to the log display
            SetLogText(text);
        }
        internal bool InvokeRequired()
        {
            //there is a glitch in invoke required for C# where sometimes it
            //will say no when it needs one causing it to sometimes be run on
            //the wrong thread, this is a hack that fixes that
            return Thread.CurrentThread.Name != Gui_Thread_Name;
        }
        /// <summary>
        /// occurs if the show basic log checkbox is changed
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            //clear the temp logs since they are no longer filtered correctly
            TempLogs.Clear();
            string[] sources = new string[allowed_Sources.Count];
            allowed_Sources.CopyTo(sources, 0);
            //generatre a new temp log set based on the updated filters
            TempLogs = LoggerMain.GetLogs(sources,checkBox3.Checked, checkBox2.Checked, checkBox1.Checked);
            //update the log display text with the new filter
            PopulateLogText();
        }
        /// <summary>
        /// Occurs if the warning checkbox is changed
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            //clear the temp logs since they are no longer filtered correctly
            TempLogs.Clear();
            string[] sources = new string[allowed_Sources.Count];
            allowed_Sources.CopyTo(sources, 0);
            //generatre a new temp log set based on the updated filters
            TempLogs = LoggerMain.GetLogs(sources, checkBox3.Checked, checkBox2.Checked, checkBox1.Checked);
            //update the log display text with the new filter
            PopulateLogText();
        }
        /// <summary>
        /// occurs if the error checkbox is changed
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            //clear the temp logs since they are no longer filtered correctly
            TempLogs.Clear();
            string[] sources = new string[allowed_Sources.Count];
            allowed_Sources.CopyTo(sources, 0);
            //generatre a new temp log set based on the updated filters
            TempLogs = LoggerMain.GetLogs(sources, checkBox3.Checked, checkBox2.Checked, checkBox1.Checked);
            //update the log display text with the new filter
            PopulateLogText();
        }
        /// <summary>
        /// Occurs when the user clicks the clear log menu option
        /// this function clears the log
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if the log has not been saved
            if(!LoggerMain.IsSaved())
            {
                //sets the need saving flag to true
                bool NeedsSaving = true;
                do
                {
                    //ask the user if they want to save the log and if they say yes
                    if (MessageBox.Show("Do you wish to save the log first?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        //try to save the log
                        saveToolStripMenuItem_Click(this, null);
                        //if the log is saved then it no longer needs saving
                        NeedsSaving = !LoggerMain.IsSaved();
                    }
                    else
                        //if the user said no not to save it first then it does not need saving
                        NeedsSaving = false;
                }
                while (NeedsSaving);//keep this up until the user says no or the log is saved
            }
            //clear the log
            LoggerMain.CLEAR(this);
        }
        /// <summary>
        /// occurs if the user clicks the save menu option
        /// this function ask the user for a destination then saves the log
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //create a save file dialog to ask the user where to save the log to
            SaveFileDialog diag = new SaveFileDialog();
            //set the filter to text documents
            diag.Filter = "Log Text|*.txt";
            //askt he user if they want to save the log
            if (diag.ShowDialog() != DialogResult.Cancel)
            {
                //save the log to the file path
                string message = LoggerMain.Save(diag.FileName);
                //if the log was not saved correctly
                if (message != "")
                {
                    //tell the user and display the error message
                    MessageBox.Show("Failed to save the log!\r\n" + message);
                }
            }
        }
        /// <summary>
        /// occurs when the user clicks the close option in the menu
        /// this function closed the log display form
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //unregister from the new log event so that the event does not fill up with orphened listeners
            Logger.LoggerMain.New_Log_Received -= LoggerMain_New_Log_Received;
            //unregister from the clear log event so that the event does not fill up with orphened listeners
            Logger.LoggerMain.Log_Cleared -= LoggerMain_Log_Cleared;
            //close this form
            this.Close();

        }
        public void Restore_Form()
        {
            if(richTextBox1.InvokeRequired)
            {
                UpdateVoid D = new UpdateVoid(Restore_Form);
                richTextBox1.Invoke(D);
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                //bring back to the front
                this.BringToFront();
            }
            
        }
        private void Log_Viewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            //sets the flag that this form was closed
            WasClosed = true;

        }

        private void Log_Viewer_Shown(object sender, EventArgs e)
        {
            WasClosed = false;
        }

        private void filterToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
