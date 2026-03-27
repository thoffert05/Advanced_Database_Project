///----------------------------------------------------------------------------------
// This helper class was originally written several years ago for a different project.
// It worked reliably, but the original version was not fully documented.
// For this project, only the key logic has been commented (percentage calculations,
// cursor positioning, taskbar integration, etc.). The remaining sections are left
// as-is since they are stable and self-contained.
//
//this class is handles all progress bars and drawing of the progress bars for the
//program.  It also handles the marque and the progress on the icon of the program in
//the task icon. It is responsible for updating all of these things when the progress
//is updated and it also has some helper functions to set colors and other things
//related to the progress bars.
//
//
//
// Author anthony Hoffert
///----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using static System.Windows.Forms.AxHost;
using System.Collections;

namespace ProgressBar
{
    public class ProgressBarHandler
    {
        //should it show the marque
        public bool showMarque = true;
        //total width and height of the picture box that the progress bars are drawn on
        int TotalWidth, TotalHeight;
        //the marque that is drawn on the progress bar if enabled
        Marque marque = new Marque();
        //the graphics object used to draw the progress bars and marque
        System.Drawing.Graphics ProgressGraphics;
        //the picture box that the progress bars are drawn on
        PictureBox pictureBox;
        //the tool tip that the percentage is drawn on
        ToolTip tempTip;
        //the progress bars that are drawn on the picture box only 2 are supported in this class
        private Progress[] progressBars;
        //the overall progress bar that is drawn on the picture box
        Progress OverallProgress;
        //determines if the progress is shown on the icon of the program in the task bar default is yes
        public bool ShowProgressOnIcon = true;
        //id of the progress bar handler used to identify it if there are multiple progress bars
        public static int ID = 0;
        //used to tell the progress bar it can update its drawing for the marque
        private bool running = false;
        //the thread that updates the marque and progress bar drawing
        private Thread worker = null;
        //used to endure that it does not update too often
        DateTime lastUpdate = DateTime.MinValue;
        //name of the GUI thread so that it could decide if an invoke was needed for updating progress labels since invoke did not always work
        private string Graphic_Task_Name = "";
        //the verious ways to draw te progress percentage on the progress bar and window
        public enum PercentageDisplayMode { NONE, OVERALL_PERCENTAGE_ONLY_MIDDLE_OF_WINDOW, HIGHEST_PERCENTAGE_ONLY_MIDDLE_OF_WINDOW, OVERALL_PERENTAGE_ONLY_MIDDLE_OF_BAR, HIGHEST_PERCENTAGE_ONLY_MIDDLE_OF_BAR, ALL_BARS_MIDDLE_OF_BAR, OVERALL_AND_SUBPROGRESS_CENTER_OF_WINDOW };
        //the current way to display the percentage on the progress bar and window
        public PercentageDisplayMode DisplayPercentMode = PercentageDisplayMode.NONE;
        //Looks at a window of time and the rate of percentage completed to calculate the time remaining 0 means no average
        public int SecondsToTakeAverageProgressRateOf = 0;
        //font to draw the percent with
        public static Font TextFont = null;
        //Determines if the user can add or remove progress bars from or to this handler
        bool CanAddORRemoveProgress = false;
        //used for the adding or removing of bars
        SortedList<int, Progress> progressbars = new SortedList<int, Progress>();
        //border pen to draw a border around the progress bar
        private Pen BorderPen = new Pen(Color.Black, 2);
        //last time it updated
        DateTime last_percent_update = DateTime.MinValue;
        //used to update the text on a different thread, for the percent labels
        private delegate void updateString(Label l, string s);
        /// <summary>
        /// this initializes the progress bar handler with the picture box and tool tip and whether to show the marque or not and starts the thread to update the marque and progress bar drawing
        /// </summary>
        /// <param name="pict">picture box the progress bar is drawn on</param>
        /// <param name="tip">tool tip to show the current progress of the hovered bar if one is hovered over on</param>
        /// <param name="ShowMarque">should it show the scrolling marque or not</param>
        private void initializeBase(ref PictureBox pict, ref ToolTip tip, bool ShowMarque)
        {
            showMarque = ShowMarque;


            pictureBox = pict;
            pictureBox.VisibleChanged += Pict_VisibleChanged;
            pictureBox.Resize += Pict_Resize;

            TotalHeight = pictureBox.Height;
            TotalWidth = pictureBox.Width;
            ProgressGraphics = pictureBox.CreateGraphics();
            tempTip = tip;
            tempTip.AutomaticDelay = 1;
            tempTip.AutoPopDelay = 999999999;
            tempTip.IsBalloon = false;
            //need to figure out how to compute the size?
            TextFont = new Font(new FontFamily("Times New Roman"), 12, FontStyle.Regular, new GraphicsUnit());
            StartUpdator();

        }
        /// <summary>
        ///there is a glitch in invoke required for C# where sometimes it
        ///will say no when it needs one causing it to sometimes be run on
        ///the wrong thread, this is a hack that fixes that
        /// </summary>
        /// <returns>yes it is on a different thread</returns>
        internal bool InvokeRequired()
        {
            //there is a glitch in invoke required for C# where sometimes it
            //will say no when it needs one causing it to sometimes be run on
            //the wrong thread, this is a hack that fixes that
            return Thread.CurrentThread.Name != Graphic_Task_Name;
        }
        /// <summary>
        /// this updates the text on a label for percent displays
        /// </summary>
        /// <param name="lbl">the label to display the text on</param>
        /// <param name="s">the text to be displayed</param>
        private void updatelabelText(Label lbl, string s)
        {
            //does it need an invoke
            if (InvokeRequired())
            {
                //if so then invoke this on the GUI thread
                updateString S = new updateString(updatelabelText);
                lbl.Invoke(S, new object[] { lbl, s });
            }
            //else no invoke is requrired it is on the same thread
            else
                //update the text
                lbl.Text = s;
        }

        
        /// <summary>
        /// Should be the default constructor that is always used. If no sub color then set the color to white
        /// </summary>
        /// <param name="Overall_Color">Color of the main progress bar</param>
        /// <param name="SubColor">Color of a secondary progress bar</param>
        /// <param name="pict">refrence to the picture box the progress is drawn on</param>
        /// <param name="tip">refrence to the tip that the progress percent is drawn on</param>
        /// <param name="displayMode">how to display the percents on the progress Bar</param>
        /// <param name="overallPercentageCompleteLabel">label to show the over precentage complete so far Default is none</param>
        /// <param name="overAllTimeRemainingTimeRemainingLabel">label to show the overall time left</param>
        /// <param name="SubItemPercentageCompleteLabel">label to show the secondary progress so far</param>
        /// <param name="SubItemEstimatedTimeRemainingLabel">label to show the secondary time left</param>
        /// <param name="showProgressOnIcon">show the progress over the program icon default is yes</param>
        /// <param name="ShowMarque">show the scrolling marque on the progress bar default is yes</param>
        public ProgressBarHandler(Color Overall_Color, Color SubColor, ref PictureBox pict, ref ToolTip tip, PercentageDisplayMode displayMode = PercentageDisplayMode.NONE, Label overallPercentageCompleteLabel = null, Label overAllTimeRemainingTimeRemainingLabel = null, Label SubItemPercentageCompleteLabel = null, Label SubItemEstimatedTimeRemainingLabel = null, bool showProgressOnIcon = true, bool ShowMarque = true)
        {
            //initialize the progress bar handler stuff
            initializeBase(ref pict, ref tip, ShowMarque);
            //if there is a subcolor meaning 2 progress bars
            if (SubColor != Color.Transparent)
            {
                //create the overall progress bar
                OverallProgress = new Progress(Overall_Color, "Overall Progress", overallPercentageCompleteLabel, overAllTimeRemainingTimeRemainingLabel, this);
                //set the graphic threads name to the current thread since the initialization is running on the GUI thread
                Graphic_Task_Name = Thread.CurrentThread.Name;
                //create a sub progress bar and add it to the list of progress bars
                progressBars = new Progress[] { new Progress(SubColor, "Sub Progress", SubItemPercentageCompleteLabel, SubItemEstimatedTimeRemainingLabel, this) };
            }
            //else if there is no secondary progress bar
            else
            {
                //create the overall progress bar
                OverallProgress = new Progress(Overall_Color, "", overallPercentageCompleteLabel, overAllTimeRemainingTimeRemainingLabel, this);
                //set the graphic threads name to the current thread since the initialization is running on the GUI thread
                Graphic_Task_Name = Thread.CurrentThread.Name;
            }
            //set the display mode
            this.DisplayPercentMode = displayMode;
            //set the display mode for the overall progress bar
            OverallProgress.DisplayPercentMode = DisplayPercentMode;
            //for each progress bar
            if(progressBars!=null&&progressBars.Length>0)
                //set the display mode
                progressBars[0].DisplayPercentMode = DisplayPercentMode;

        }
        /// <summary>
        /// this function updates the text on the tool tip
        /// </summary>
        /// <param name="text">text to display on the tool tip</param>
        /// <param name="alwaysShow">should it always show the tool tip</param>
        private void updateToolTipText(string text, bool alwaysShow)
        {
            //if there has been under half a second since the last update
            if (DateTime.Now.Subtract(last_percent_update).TotalMilliseconds < 500)
                //exit this function
                return;
            //update the last update time to now
            last_percent_update = DateTime.Now;
            try
            {
                //if an invoke is required to get to the GUI thread
                if (pictureBox.InvokeRequired)
                {
                    //invoke this function on the gui thread
                    pictureBox.Invoke(new Action(() => updateToolTipText(text, alwaysShow)));
                }
                else
                {
                    //if always show
                    if (alwaysShow)
                    {
                        //show this tool tip showing the text or percent
                        tempTip.SetToolTip(pictureBox, text);
                        //always show the text
                        tempTip.ShowAlways = alwaysShow;
                    }
                    //if it should not always show the text
                    else
                    {
                        //remove the tool tip
                        tempTip.RemoveAll();
                    }
                }
            }
            catch
            {
                //if an exception occurs just ignore it as it is not important
            }
        }
        /// <summary>
        /// get the overall progress bar
        /// </summary>
        /// <returns>the overall progress bar object</returns>
        public Progress getOverallProgressBar()
        {
            return OverallProgress;
        }
        
        /// <summary>
        /// Hide the progress on the icon of the calling program
        /// </summary>
        private void hideProgressOnIconFullProgress()
        {
            //turn off the progress bar on the icon of this program
            var prog = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
            prog.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.NoProgress);
        }
        /// <summary>
        /// Show a progress bar on the program icon in the task bar [default is to show it]
        /// </summary>
        public void showOverProgressOnIcon()
        {
            ShowProgressOnIcon = true;
            if (OverallProgress.GetProgress() == 0)
                return;
            if (OverallProgress.GetProgress() != 1)
            {
                var prog = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
                prog.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.Normal);
                prog.SetProgressValue((int)(OverallProgress.GetProgress() * 100), 100);
            }
        }
        /// <summary>
        /// update the progress shading the icon to match the overall progress bar so that you can tell without even having the program visible
        /// </summary>
        public void updateProgressOnIcon()
        {
            try
            {
                //get the overall progress value as a integer between 0 and 100
                int overallProg = (int)(OverallProgress.GetProgress() * 100);

                //if there is progres and it is not at 100 because if it as at 100 then it will stay like that and never clear
                if (overallProg > 0 && overallProg != 100)
                {
                    //get the icon
                    var prog = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
                    // Put the taskbar into normal progress mode (green bar)
                    prog.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.Normal);
                    //set the progress value on the icon which will be between 0 and 99
                    prog.SetProgressValue(overallProg, 100);
                }
                else
                {
                    //if the progress is not 100%
                    if (overallProg != 100)
                    {
                        //get the icon
                        var prog = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
                        //clear any progress on the icon
                        prog.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.NoProgress);
                    }
                }
            }
            catch(Exception ex)
            {
                //if there is an exception just print it to the console, it is not important enough to stop the program
                System.Console.WriteLine(ex.ToString());
            }
        }
        /// <summary>
        /// Don't show a progress bar on the program icon in the task bar [default is to show it]
        /// </summary>
        public void HideProgressOnIcon()
        {
            //disable showing the progress on the icon
            ShowProgressOnIcon = false;
            //get the icon
            var prog = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
            //clear any progress on the icon
            prog.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.NoProgress);
        }
        /// <summary>
        /// Set the overall progress only for the overall progress bar
        /// </summary>
        /// <param name="Percent">progress so far</param>
        public void setOverallProgress(float Percent)
        {
            //cap progress at 100%
            if (Percent > 1)
            {
                System.Console.WriteLine("Too much progress given!\r\nProgress given: " + Percent);
                Percent = 1;
            }
            //if it should show the progress on the icon and it is not at 100%
            if (ShowProgressOnIcon && Percent != 1)
            {
                try
                {
                    var prog = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
                    prog.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.Normal);
                    prog.SetProgressValue((int)(Percent * 100), 100);
                }
                catch
                {

                }
            }
            //if the progress is at only 1% aka not enough to show anything
            if (Percent == 1 && ShowProgressOnIcon)
                //hid the progress on the icon
                hideProgressOnIconFullProgress();
            //set the overall progress precent value
            OverallProgress.SetProgress(Percent);
            //update for only the main progress bar
            update(true);

            if (!pictureBox.Visible)
                SetPictureBoxVisiblity(true);
        }

        private delegate void updateBool(bool b);
        public Progress[] getProgressBars()
        {
            return progressBars;
        }
        private void SetPictureBoxVisiblity(bool visible)
        {

            if (pictureBox.Visible == visible)
                return;
            if (InvokeRequired())
            {
                if (pictureBox == null)
                    System.Console.WriteLine("Null PictureBox");
                try
                {
                    pictureBox.Load();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.Message);
                }
                updateBool b = new updateBool(SetVisiblity);
                pictureBox.Invoke(b, new object[] { visible });

            }
            else
                SetVisiblity(visible);
        }
        private void SetVisiblity(bool visible)
        {
            if (InvokeRequired())
            {
                updateBool b = new updateBool(SetVisiblity);
                pictureBox.Invoke(b, new object[] { visible });
                return;
            }
            else
            {
                if (pictureBox.InvokeRequired)
                {
                    updateBool b = new updateBool(SetVisiblity);
                    pictureBox.Invoke(b, new object[] { visible });
                    return;
                }
                else
                {


                    pictureBox.Visible = visible;

                }
            }
            if (visible)
            {
                OverallProgress.ShowLabels();
                try
                {
                    if (progressBars != null)
                        for (int i = 0; i < progressBars.Length; i++)
                        {
                            progressBars[i].ShowLabels();
                        }
                }
                catch
                {

                }
            }
            else
            {
                OverallProgress.HideLabels();
                try
                {
                    if (progressbars != null)
                    {
                        foreach(KeyValuePair<int,Progress>kvp in progressbars)
                        {
                            kvp.Value.HideLabels();
                        }

                    }
                    else
                    {
                        if (progressBars != null)
                        {
                            for (int i = 0; i < progressBars.Length; i++)
                            {
                                progressBars[i].HideLabels();
                            }
                        }
                    }
                }
                catch
                {

                }
                hideProgressOnIconFullProgress();
            }


        }

        /// <summary>
        /// Set progress on all bars
        /// </summary>
        /// <param name="OverallProg">Overall progress value</param>
        /// <param name="ProgressValues">Progress values for all progress bars</param>
        public void setProgress(float OverallProg, float[] ProgressValues)
        {
            OverallProgress.SetProgress(OverallProg);
            for (int i = 0; i < progressBars.Length; i++)
            {
                progressBars[i].SetProgress(ProgressValues[i]);
                i++;
            }
            update();
            /* if (!updater.Enabled || !timerRunning)
             {
                 timerRunning = true;
                 updater.Start();
             }*/
            if (!pictureBox.Visible)
                SetPictureBoxVisiblity(true);
        }
        /// <summary>
        /// Sets the progress value on just 2 bars only (overall progress bar and the first sub progress bar only)
        /// </summary>
        /// <param name="OverallProg">over all progress</param>
        /// <param name="SubItem">sub item progress</param>
        public void setProgress(float OverallProg, float SubItem)
        {
            if (OverallProg > 1)
            {
                System.Console.WriteLine("Too much progress given for first progress!\r\nProgress given: " + OverallProg);
                OverallProg = 1;
            }
            if (SubItem > 1)
            {
                System.Console.WriteLine("Too much progress given for second progress!\r\nProgress given: " + SubItem);
                SubItem = 1;
            }
            OverallProgress.SetProgress(OverallProg);
            progressBars[0].SetProgress(SubItem);
            update();
            /*  if (!updater.Enabled || !timerRunning)
              {
                  timerRunning = true;
                  updater.Start();
              }*/
            if (!pictureBox.Visible)
                SetPictureBoxVisiblity(true);
            SetVisiblity(true);

        }

        /// <summary>
        /// Set progress for 2 bars only calculates overall progress and sub item progress
        /// </summary>
        /// <param name="SubItemProgress">sub item progress</param>
        /// <param name="itemIndex">current operation 0 based index</param>
        /// <param name="totalItemCount">total number of items</param>
        public void setProgress(float SubItemProgress, int itemIndex, int totalItemCount)
        {

            if (totalItemCount <= 1)
                OverallProgress.SetProgress(SubItemProgress);
            else
            {
                OverallProgress.SetProgress((SubItemProgress * 100 + itemIndex * 100) / (totalItemCount * 100));
                progressBars[0].SetProgress(SubItemProgress);
            }
            update(totalItemCount <= 1);

            SetVisiblity(true);
        }
        /// <summary>
        /// Hides this progress bar and everything associated with it
        /// </summary>
        public void Hide()
        {
            if (pictureBox.Visible)
                SetPictureBoxVisiblity(false);

        }
        /// <summary>
        /// updates the marque, updates the label for the tool tip, draws the progress bar
        /// </summary>
        public void update(bool only_Main_Progress = false)
        {
            if (DateTime.Now.Subtract(lastUpdate).TotalMilliseconds < 100)
            {
                return;
            }
            lastUpdate = DateTime.Now;
            Draw(only_Main_Progress);
            if (showMarque)
                marque.updateMarque();
            if (ShowProgressOnIcon)
                updateProgressOnIcon();

        }
        LinkedList<Progress> templist = new LinkedList<Progress>();

        public bool ShowMarque { get; internal set; }

        /// <summary>
        /// Draws all the progress bars and the marque if the marque is enabled
        /// </summary>
        public void Draw(bool only_Main_Progress = false)
        {

            try
            {
                ProgressGraphics = pictureBox.CreateGraphics();
                TotalWidth = pictureBox.Width;
                ProgressGraphics.Clear(Color.White);
            }
            catch (Exception ex)
            {
                running = false;
                System.Console.WriteLine("In Progress bar Handler class Draw Function Clear graphics: " + ex.Message);
                return;
            }
            SortedList<float, Progress> PercentSortedProgress = new SortedList<float, Progress>();


            float MarqueStart = marque.getPosition() - marque.getWidth() / 2;
            float MarqueEnd = marque.getPosition() + marque.getWidth() / 2;
            if (progressBars != null)
                for (int i = 0; i < progressBars.Length; i++)
                {
                    if (!PercentSortedProgress.ContainsKey(progressBars[i].GetProgress()))
                    {
                        PercentSortedProgress.Add(progressBars[i].GetProgress(), progressBars[i]);
                    }
                }
            try
            {
                if (!PercentSortedProgress.ContainsKey(OverallProgress.GetProgress()))
                {
                    PercentSortedProgress.Add(OverallProgress.GetProgress(), OverallProgress);
                }
            }
            catch
            { }
            try
            {


                marque.MaxGradientPercent = PercentSortedProgress.Keys.Last();
            }
            catch
            { }
            //PercentSortedProgress.Reverse();
            templist.Clear();
            foreach (KeyValuePair<float, Progress> kvp in PercentSortedProgress)
            {
                templist.AddFirst(kvp.Value);
            }
            LinkedListNode<Progress> node = templist.First;
            Color MarqueStartcolor = Color.Blue, MarqueEndColor = Color.Blue;
            float MarqueStartPercentage = 1, MarqueEndPercentage = 0;
            while (node != null)
            {
                node.Value.Draw(ref ProgressGraphics, TotalWidth, TotalHeight);
                if (showMarque)
                {
                    if (MarqueStart < node.Value.GetProgress())
                    {
                        MarqueStartcolor = node.Value.GetColor();
                        MarqueStartPercentage = node.Value.GetProgress();
                    }
                    if (MarqueEnd < node.Value.GetProgress())
                    {
                        MarqueEndColor = node.Value.GetColor();
                        MarqueEndPercentage = node.Value.GetProgress();
                    }
                }
                node = node.Next;
            }
            if (showMarque)
                marque.DrawMarque(MarqueStartcolor, MarqueEndColor, MarqueStartPercentage, MarqueEndPercentage, TotalWidth, TotalHeight, ref ProgressGraphics);
            try
            {
                switch (DisplayPercentMode)
                {
                    case PercentageDisplayMode.ALL_BARS_MIDDLE_OF_BAR:
                        if (only_Main_Progress)
                        {
                            OverallProgress.DrawPerentage(ref ProgressGraphics, TotalWidth, TotalHeight, PercentSortedProgress);
                        }
                        else
                        {
                            node = templist.First;
                            while (node != null)
                            {
                                node.Value.DrawPerentage(ref ProgressGraphics, TotalWidth, TotalHeight, PercentSortedProgress);
                                node = node.Next;
                            }
                        }
                        break;
                    case PercentageDisplayMode.HIGHEST_PERCENTAGE_ONLY_MIDDLE_OF_BAR:
                        templist.First.Value.DrawPerentage(ref ProgressGraphics, TotalWidth, TotalHeight, PercentSortedProgress);
                        break;
                    case PercentageDisplayMode.HIGHEST_PERCENTAGE_ONLY_MIDDLE_OF_WINDOW:
                        templist.First.Value.DrawPerentage(ref ProgressGraphics, TotalWidth, TotalHeight, PercentSortedProgress);
                        break;
                    case PercentageDisplayMode.OVERALL_PERCENTAGE_ONLY_MIDDLE_OF_WINDOW:
                        OverallProgress.DrawPerentage(ref ProgressGraphics, TotalWidth, TotalHeight, PercentSortedProgress);
                        break;
                    case PercentageDisplayMode.OVERALL_PERENTAGE_ONLY_MIDDLE_OF_BAR:
                        OverallProgress.DrawPerentage(ref ProgressGraphics, TotalWidth, TotalHeight, PercentSortedProgress);
                        break;
                    case PercentageDisplayMode.OVERALL_AND_SUBPROGRESS_CENTER_OF_WINDOW:
                        if (only_Main_Progress)
                        {
                            OverallProgress.DrawPerentage(ref ProgressGraphics, TotalWidth, TotalHeight, PercentSortedProgress);
                        }
                        else
                        {
                            Draw_Both_Progresses_in_Middle(ref ProgressGraphics, TotalWidth, TotalHeight, PercentSortedProgress);
                        }
                        break;
                }
            }
            catch
            { }
            try
            {
                ProgressGraphics.DrawRectangle(BorderPen, new Rectangle(0, 0, TotalWidth, TotalHeight));
            }
            catch
            {

            }
        }

        private void Draw_Both_Progresses_in_Middle(ref Graphics progressGraphics, int totalWidth, int totalHeight, SortedList<float, Progress> percentSortedProgress)
        {
            Color tempColor;
            Color FontColor;
            bool Horizontal = true;
            string Horizontal_text = "";
            string vertical_text = "";
            if (!Progress.compute_Font_Size(TotalWidth, TotalHeight, ref ProgressGraphics, true, out Horizontal))
                return;

            if (progressBars != null && progressBars.Length > 0)
            {
                Horizontal_text = ((int)(progressBars[0].GetProgress() * 100)).ToString() + "% / " + ((int)(OverallProgress.GetProgress() * 100)).ToString() + "%";
                vertical_text = ((int)(progressBars[0].GetProgress() * 100)).ToString() + "%\r\n" + ((int)(OverallProgress.GetProgress() * 100)).ToString() + "%";
            }
            else
            {
                Horizontal_text = ((int)(OverallProgress.GetProgress() * 100)).ToString() + "%";
                vertical_text = ((int)(OverallProgress.GetProgress() * 100)).ToString() + "%";
            }
            SizeF Horizontal_Size = ProgressGraphics.MeasureString(Horizontal_text, TextFont);
            SizeF Vertical_Size = ProgressGraphics.MeasureString(vertical_text, TextFont);
            PointF Draw_Left_Top_Start;
            LinkedList<KeyValuePair<string, PointF>> Line_Pos = new LinkedList<KeyValuePair<string, PointF>>();
            LinkedListNode<KeyValuePair<string, PointF>> node;
            if (Horizontal)
            {
                Draw_Left_Top_Start = new PointF(totalWidth / 2f - Horizontal_Size.Width / 2f, totalHeight / 2f - Horizontal_Size.Height / 2f);
                Draw_Percents(Horizontal_text, Draw_Left_Top_Start, percentSortedProgress);
            }
            else
            {
                Line_Pos = Compute_Line_Pos(ref progressGraphics, vertical_text, totalWidth, totalHeight);
                node = Line_Pos.First;
                while (node != null)
                {
                    Draw_Percents(node.Value.Key, node.Value.Value, percentSortedProgress);
                    node = node.Next;
                }
                // Draw_Left_Top_Start = new PointF(totalWidth / 2f - Vertical_Size.Width / 2f, totalHeight / 2f - Vertical_Size.Height / 2f);

            }
        }

        private LinkedList<KeyValuePair<string, PointF>> Compute_Line_Pos(ref Graphics progressGraphics, string vertical_text, int totalWidth, int totalHeight)
        {
            LinkedList<KeyValuePair<string, PointF>> output = new LinkedList<KeyValuePair<string, PointF>>();
            KeyValuePair<string, PointF> computed_line_pos;
            string[] lines = vertical_text.Split('\n');
            SizeF One_Hundred_Percent_Size = ProgressGraphics.MeasureString("100%", ProgressBarHandler.TextFont);
            float Total_Text_Height = (One_Hundred_Percent_Size.Height + 3f) * lines.Length;
            float Vertical_Start = totalHeight / 2f - Total_Text_Height / 2f;
            float Horizontal_Start;
            for (int i = 0; i < lines.Length; i++)
            {
                Horizontal_Start = ProgressGraphics.MeasureString(lines[i].ToString(), ProgressBarHandler.TextFont).Width + 3f;
                Horizontal_Start = totalWidth / 2f - Horizontal_Start / 2f;
                computed_line_pos = new KeyValuePair<string, PointF>(lines[i], new PointF(Horizontal_Start, Vertical_Start));
                output.AddLast(computed_line_pos);
                Vertical_Start += One_Hundred_Percent_Size.Height + 3f;
            }
            return output;
        }

        private void Draw_Percents(string Horizontal_text, PointF Draw_Left_Top_Start, SortedList<float, Progress> percentSortedProgress)
        {
            Color tempColor;
            Color FontColor;
            float Start = 0;
            float StartY;
            float Letter_Size;
            Start = Draw_Left_Top_Start.X;
            StartY = Draw_Left_Top_Start.Y;
            for (int i = 0; i < Horizontal_text.Length; i++)
            {
                Letter_Size = ProgressGraphics.MeasureString(Horizontal_text[i].ToString(), ProgressBarHandler.TextFont).Width - 3f;
                tempColor = GetColorOfClosestPercentage(Start + Letter_Size / 2, TotalWidth, percentSortedProgress);
                FontColor = Progress.ContrastColor(tempColor);
                ProgressGraphics.DrawString(Horizontal_text[i].ToString(), ProgressBarHandler.TextFont, new SolidBrush(FontColor), new PointF(Start, StartY));
                Start += Letter_Size;
            }
        }

        private Color GetColorOfClosestPercentage(float end, float totalWidth, SortedList<float, Progress> allBars)
        {
            if (allBars == null)
                return Color.White;
            foreach (KeyValuePair<float, Progress> kvp in allBars)
            {
                if (kvp.Key * totalWidth >= end)
                    return kvp.Value.BarColor;
            }
            return Color.White;
        }
        /// <summary>
        /// stops the marque from scrolling until the next progress bar gets updated
        /// </summary>
        public void StopMarqueUntilNextPercentageUpdate()
        {
            running = false;

        }
        private void StartUpdator()
        {
            if (worker == null)
                worker = new Thread(Updater);
            if (!running)
            {
                running = true;
                worker.Start();
            }
        }
        /// <summary>
        /// When the update timer has elapsed it's wait time
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not Used</param>
        private void Updater_Tick(object sender, EventArgs e)
        {
            update();
        }
        private void Updater()
        {
            while (running)
            {
                update();
                Thread.Sleep(200);
            }
        }
        private void Pict_VisibleChanged(object sender, EventArgs e)
        {
            ((PictureBox)sender).UseWaitCursor = ((PictureBox)sender).Visible;
            if (!((PictureBox)sender).Visible)
            {
                StopMarqueUntilNextPercentageUpdate();
            }
        }
        public string getSubProgressElapsedTime()
        {
            if (progressBars.Length == 1)
            {
                return progressBars[0].GetElapsedTime();
            }
            return "No sub-progress Item";
        }
        public string getOverAllProgressElapsedTime()
        {
            return OverallProgress.GetElapsedTime();
        }
        public void OverAllDone()
        {
            OverallProgress.Done();
            running = false;
            if (progressBars != null)
                for (int i = 0; i < progressBars.Length; i++)
                {
                    progressBars[i].Done();
                }
            //turn off the progress bar on the icon of this program
            var prog = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
            prog.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.NoProgress);
        }

        /// <summary>
        /// Occures when the progress bar is resized
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not Used</param>
        private void Pict_Resize(object sender, EventArgs e)
        {
            TotalHeight = ((PictureBox)sender).Height;
            TotalWidth = ((PictureBox)sender).Width;
            ProgressGraphics.Dispose();
            ProgressGraphics = ((PictureBox)sender).CreateGraphics();
        }
        public void HideProgressBars()
        {
            if (pictureBox.Visible)
                SetPictureBoxVisiblity(false);


        }

        public void setSubBarLabel(string label)
        {
            progressBars[0].updateName(label);
        }
    }
}
