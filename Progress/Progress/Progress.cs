using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Web;

namespace ProgressBar
{
    public class Progress
    {
        internal Color BarColor;
        private DateTime Start = DateTime.MinValue;
        private float ProgressSoFar;
        Label ProgressLabel = null;
        Label TimeRemainingLbl = null;
        string label = "";
        string Graphic_Task_Name = "";
        public bool ShowLabel = true;
        public DateTime end = DateTime.MinValue;
        public int ID = 0;
        

        public delegate void UpdateProgressBarDelegate(float percent);
        private delegate void updateString(Label l, string s);
        private delegate void UpdateBool(Label l, bool v);
        public ProgressBarHandler.PercentageDisplayMode DisplayPercentMode = ProgressBarHandler.PercentageDisplayMode.NONE;
        SortedList<DateTime, float> ProgressHistory = new SortedList<DateTime, float>();
        public ProgressBarHandler handler;
        private LinkedList<DateTime> templist = new LinkedList<DateTime>();
        public Progress(Color barColor, string Name, Label PercentageCompleteLabel, Label TimeRemainingLabel, ProgressBarHandler owner)
        {
            Graphic_Task_Name = Thread.CurrentThread.Name;
            BarColor = barColor;
            ProgressLabel = PercentageCompleteLabel;
            TimeRemainingLbl = TimeRemainingLabel;
            label = Name;
            handler = owner;
            ID = ProgressBarHandler.ID++;
        }
        private void updateLabel(Label lbl, string s)
        {
            lbl.Text = s;
        }
        private void updateLabelVisiblity(Label lbl, bool v)
        {
            lbl.Visible = v;
        }
        public void Done()
        {
            end = DateTime.Now;
        }
        public void updateName(string name)
        {
            label = name;
        }
        internal bool InvokeRequired()
        {
            //there is a glitch in invoke required for C# where sometimes it
            //will say no when it needs one causing it to sometimes be run on
            //the wrong thread, this is a hack that fixes that
            return Thread.CurrentThread.Name != Graphic_Task_Name;
        }
        public string getName()
        {
            return label;
        }
        private void updatelabelText(Label lbl, string s)
        {
            if (InvokeRequired() || lbl.InvokeRequired)
            {
                try
                {
                    updateString S = new updateString(updateLabel);
                    lbl.Invoke(S, new object[] { lbl, s });
                }
                catch
                { }
            }
            else
            {
                try
                {
                    lbl.Text = s;
                }
                catch
                {
                }
            }
        }




        public void SetProgress(float prog)
        {
            if(prog>1)
            {
                prog = 1;
            }
            string Time_Remaining_Text = "";
            if (Start == DateTime.MinValue || prog < ProgressSoFar)
                Start = DateTime.Now;
            ProgressSoFar = prog;
            if (ProgressLabel != null)
                if (ShowLabel)
                {
                    if (label == "Overall Progress")
                    {
                        updatelabelText(ProgressLabel, "O: " + ((int)(prog * 100)) + "%");
                    }
                    else
                    {
                        updatelabelText(ProgressLabel, "S: " + ((int)(prog * 100)) + "%");
                    }
                }
                else
                    updatelabelText(ProgressLabel, ((int)(prog * 100)) + "%");
            if (handler.SecondsToTakeAverageProgressRateOf > 0)
                ProgressHistory.Add(DateTime.Now, prog);
            if (TimeRemainingLbl != null)
            {
                if (label != "")
                    Time_Remaining_Text = label + ": "+ getProgressTimeRemaining();
                else
                {
                    Time_Remaining_Text = getProgressTimeRemaining();
                }
                updatelabelText(TimeRemainingLbl, Time_Remaining_Text);
            }

        }
        public Color GetColor()
        {
            return BarColor;
        }
        public void SetColor(Color color)
        {
            BarColor = color;
        }
        public float GetProgress()
        {
            return ProgressSoFar;
        }
        public void Reset()
        {
            ResetStartTime();
            end = DateTime.MinValue;
        }
        public void ResetStartTime()
        {
            Start = DateTime.Now;
        }
        public string GetElapsedTime()
        {
            TimeSpan elapsed;
            if (end == DateTime.MinValue)
                elapsed = DateTime.Now.Subtract(Start);
            else
                elapsed = end.Subtract(Start);
            string output = "";
            if (elapsed.Days > 0)
                output += elapsed.Days + " ";
            if (elapsed.Hours < 10)
                output += "0";
            output += elapsed.Hours;
            output += ":";
            if (elapsed.Minutes < 10)
                output += "0";
            output += elapsed.Minutes;
            output += ":";
            if (elapsed.Seconds < 10)
                output += "0";
            output += elapsed.Seconds;

            return output;
        }

        public string getProgressTimeRemaining()
        {
            if (ProgressSoFar < 0.01f)
                return "Calculating time Remaining..";

            TimeSpan timeLeft;
            TimeSpan elapsed = new TimeSpan();
            float secondsPerPercent = 0;
            float progressTemp = ProgressSoFar;
            if (handler.SecondsToTakeAverageProgressRateOf == 0 || DateTime.Now.Subtract(Start).Seconds <= handler.SecondsToTakeAverageProgressRateOf)
            {
                elapsed = DateTime.Now.Subtract(Start);
                //percent per second
                secondsPerPercent = ProgressSoFar / (float)elapsed.TotalSeconds;
            }
            else
            {
                DateTime startOFWindow = DateTime.Now.AddSeconds(handler.SecondsToTakeAverageProgressRateOf * -1);

                foreach (KeyValuePair<DateTime, float> kvp in ProgressHistory)
                {
                    if (kvp.Key > startOFWindow)
                    {
                        progressTemp -= kvp.Value;
                        elapsed = DateTime.Now.Subtract(kvp.Key);
                        //percent per second
                        secondsPerPercent = progressTemp / (float)elapsed.TotalSeconds;
                        break;
                    }
                    else
                        templist.AddLast(kvp.Key);
                }
                LinkedListNode<DateTime> node = templist.First;
                while (node != null)
                {
                    ProgressHistory.Remove(node.Value);
                    node = node.Next;
                }
                templist.Clear();

            }

            //number of percent left until 100% (it will eventually hold seconds left)
            int secondsLeft = (int)(1 - (ProgressSoFar));
            //compute the seconds left until 100%
            secondsLeft = (int)((1 - ProgressSoFar) / secondsPerPercent);

            //convert the seconds left to a timespan
            timeLeft = new TimeSpan(0, 0, 0, secondsLeft, 0);
            //get the number of hours left
            String left = timeLeft.Hours.ToString();
            //make the number of hours grammaticially correct
            if (timeLeft.Hours == 1)
                left += " Hour ";
            else
                left += " Hours ";
            if (timeLeft.Minutes < 10)
                left += "0";
            //get the number of minutes
            left += timeLeft.Minutes;

            //make the number of minutes gramaticially correct
            if (timeLeft.Minutes == 1)
                left += " minute and ";
            else
                left += " minutes and ";
            //get the number of seconds
            if (timeLeft.Seconds < 10)
                left += "0";
            left += timeLeft.Seconds;
            //make the number of seconds gramaticially correct
            if (timeLeft.Seconds == 1)
                left += " second";
            else
                left += " seconds";
            //set the time remaining level to the time remaining
            return left;
        }
        public void HideLabels()
        {

            if (ProgressLabel != null && InvokeRequired())
            {
                UpdateBool b = new UpdateBool(updateLabelVisiblity);
                ProgressLabel.Invoke(b, new object[] { ProgressLabel, false });
            }
            else
            {
                if (ProgressLabel != null)
                    ProgressLabel.Visible = false;
            }
            if (TimeRemainingLbl != null && InvokeRequired())
            {
                UpdateBool b = new UpdateBool(updateLabelVisiblity);
                TimeRemainingLbl.Invoke(b, new object[] { TimeRemainingLbl, false });
            }
            else
            {
                if (TimeRemainingLbl != null)
                    TimeRemainingLbl.Visible = false;
            }

        }
        public void ShowLabels()
        {
            if (ProgressLabel != null && InvokeRequired())
            {
                UpdateBool b = new UpdateBool(updateLabelVisiblity);
                ProgressLabel.Invoke(b, new object[] { ProgressLabel, true });
            }
            else
            {
                if (ProgressLabel != null)
                    ProgressLabel.Visible = true;
            }

            if (TimeRemainingLbl != null && InvokeRequired())
            {
                UpdateBool b = new UpdateBool(updateLabelVisiblity);
                TimeRemainingLbl.Invoke(b, new object[] { TimeRemainingLbl, true });
            }
            else
            {
                if (TimeRemainingLbl != null)
                    TimeRemainingLbl.Visible = true;
            }
        }
        public static Color ContrastColor(Color color)
        {
            int d = 0;

            // Counting the perceptive luminance - human eye favors green color.. 
            double a = 1 - (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;

            if (a < 0.5)
                d = 0; // bright colors - black font
            else
                d = 255; // dark colors - white font

            return Color.FromArgb(d, d, d);
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
        public void DrawPerentage(ref System.Drawing.Graphics ProgressGraphics, int TotalWidth, int TotalHeight, SortedList<float, Progress> allBars = null)
        {
            if (ProgressBarHandler.TextFont == null)
                return;
            try
            {
                bool can_Be_horizontal = false;
                string percentageText = ((int)(ProgressSoFar * 100)).ToString() + "%";
                if (!compute_Font_Size(TotalWidth, TotalHeight, ref ProgressGraphics, false, out can_Be_horizontal))
                    return;
                SizeF textSize = ProgressGraphics.MeasureString(percentageText, ProgressBarHandler.TextFont);
                float TextWidth = textSize.Width;
                float TextHight = textSize.Height;
                float SingleCharacterWidth = ProgressGraphics.MeasureString("T", ProgressBarHandler.TextFont).Width;
                int numberOfCharacters = percentageText.Length;
                if (TotalHeight < TextHight)
                    return;
                float BarWidth = ProgressSoFar * TotalWidth;
                float Start = 0;
                float letter_size;
                float StartY = TotalHeight / 2 - TextHight / 2;
                Color FontColor = ContrastColor(BarColor);
                Color tempColor;

                switch (DisplayPercentMode)
                {
                    case ProgressBarHandler.PercentageDisplayMode.ALL_BARS_MIDDLE_OF_BAR:
                    case ProgressBarHandler.PercentageDisplayMode.HIGHEST_PERCENTAGE_ONLY_MIDDLE_OF_BAR:
                    case ProgressBarHandler.PercentageDisplayMode.OVERALL_PERENTAGE_ONLY_MIDDLE_OF_BAR:
                        if (TextWidth >= BarWidth)
                            return;
                        Start = (BarWidth / 2) - TextWidth / 2;
                        ProgressGraphics.DrawString(percentageText, ProgressBarHandler.TextFont, new SolidBrush(FontColor), new PointF(Start, StartY));
                        break;
                    case ProgressBarHandler.PercentageDisplayMode.HIGHEST_PERCENTAGE_ONLY_MIDDLE_OF_WINDOW:
                    case ProgressBarHandler.PercentageDisplayMode.OVERALL_PERCENTAGE_ONLY_MIDDLE_OF_WINDOW:
                    case ProgressBarHandler.PercentageDisplayMode.OVERALL_AND_SUBPROGRESS_CENTER_OF_WINDOW:
                        Start = (TotalWidth / 2) - TextWidth / 2;
                        for (int i = 0; i < percentageText.Length; i++)
                        {
                            letter_size = ProgressGraphics.MeasureString(percentageText[i].ToString(), ProgressBarHandler.TextFont).Width - 3f;
                            tempColor = GetColorOfClosestPercentage(Start+letter_size/2+3f, TotalWidth, allBars);
                            FontColor = ContrastColor(tempColor);
                            ProgressGraphics.DrawString(percentageText[i].ToString(), ProgressBarHandler.TextFont, new SolidBrush(FontColor), new PointF(Start, StartY));
                            Start += letter_size;
                        }
                        break;

                }
            }
            catch (Exception ex)
            {

            }
        }

        public static bool compute_Font_Size(int totalWidth, int totalHeight, ref System.Drawing.Graphics ProgressGraphics, bool Show_2_Percents, out bool Show_Horizontal)
        {
            SizeF textSize_hor;// ProgressGraphics.MeasureString("100% / 100%", ProgressBarHandler.TextFont);
            SizeF textSize_Ver;// ProgressGraphics.MeasureString("100%/r/n100%", ProgressBarHandler.TextFont);
            SizeF textSize_One;
            SizeF TextSize;
            Font Last_Font = null;
            Font TextFont = null;// new Font(new FontFamily("Times New Roman"), 12, FontStyle.Regular, new GraphicsUnit());
            bool can_be_horizontal = true;
            bool can_be_vertical = true;
            bool last_can_be_horizontal = true;
            bool size_works = true;
            for (int i = 10; i < 26; i++)
            {
                Last_Font = TextFont;
                last_can_be_horizontal = can_be_horizontal;
                TextFont = new Font(new FontFamily("Times New Roman"), i, FontStyle.Regular, new GraphicsUnit());
                textSize_hor = ProgressGraphics.MeasureString("100% / 100%", TextFont);
                textSize_Ver = ProgressGraphics.MeasureString("100%/r/n100%", TextFont);
                textSize_One = ProgressGraphics.MeasureString("100%", TextFont);
                if (Show_2_Percents)
                {
                    if (can_be_horizontal)
                    {
                        if (textSize_hor.Width >= totalWidth || textSize_hor.Height >= totalHeight)
                        {
                            can_be_horizontal = false;
                        }
                    }
                    if (can_be_vertical)
                    {
                        if (textSize_Ver.Width >= totalWidth || textSize_Ver.Height >= totalHeight)
                        {
                            can_be_vertical = false;
                        }
                    }
                    if (!(can_be_horizontal || can_be_vertical))
                    {
                        break;
                    }
                }
                else
                {
                    if (textSize_One.Width >= totalWidth || textSize_One.Height >= totalHeight)
                    {
                        break;
                    }
                }
            }
            ProgressBarHandler.TextFont = Last_Font;
            Show_Horizontal = last_can_be_horizontal;
            if (Show_2_Percents)
            {
                if (can_be_horizontal)
                {
                    TextSize = ProgressGraphics.MeasureString("100% / 100%", TextFont);
                }
                else
                {
                    TextSize = ProgressGraphics.MeasureString("100%/r/n100%", TextFont);
                }
            }
            else
            {
                TextSize = ProgressGraphics.MeasureString("100%", Last_Font);
            }
            if (TextSize.Width >= totalWidth || TextSize.Height >= totalHeight)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Draw(ref System.Drawing.Graphics ProgressGraphics, int TotalWidth, int TotalHeight)
        {
            try
            {
                System.Drawing.SolidBrush myProgressBrush;
                myProgressBrush = new System.Drawing.SolidBrush(BarColor);
                ProgressGraphics.FillRectangle(myProgressBrush, new Rectangle(0, 0, (int)(ProgressSoFar * TotalWidth), TotalHeight));

                myProgressBrush.Dispose();
            }
            catch
            {

            }

        }

        internal void set_bar_Width(int width)
        {

        }
    }
}
