using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;

namespace Progress
{
    public class Console_ProgressBar
    {
        public enum StatsMode { NONE, ELAPSEDTIME, REMAININGTIME, ELAPSEDANDREMAININGTIMESEPERATELINES, ELAPSEDANDREMAININGTIMESAMELINE };
        private static Mutex ConsoleHolder = new Mutex();
        int CurrsorLine;
        int width;
        bool SizeChanged = false;
        bool Running = true;
        bool ClearLine = false;
        private Console_ProgressBar previousBar = null;
        public bool elapsedtimeonly = false;
        public event EventHandler<ProgressEvent> ProgressMade;
        public event EventHandler<Finished> BarFinished;
        public event EventHandler<LineMove> ProgressBarMoved;
        int MinWidth = -1;
        public StatsMode Status;
        public int StatusNumberCharacterCount = 15;
        private string FinishedText = "";
        private ConsoleColor Bar_Color;
        private int totalChunks = 30;
        public DateTime starttime;

        public Console_ProgressBar(string textWhenDone = "", ConsoleColor barColor = ConsoleColor.Green, StatsMode stats = StatsMode.NONE, Console_ProgressBar previousBar = null)
        {
            FinishedText = textWhenDone;
            CurrsorLine = Console.CursorTop;
            width = Console.WindowWidth;
            Thread worker = new Thread(WatchDog);
            worker.Start();
            Bar_Color = barColor;
            starttime = DateTime.Now;
            Status = stats;
            this.previousBar = previousBar;
            switch (Status)
            {
                case StatsMode.NONE:
                    Console.SetCursorPosition(0, CurrsorLine + 1);
                    break;
                case StatsMode.ELAPSEDTIME:
                case StatsMode.REMAININGTIME:
                case StatsMode.ELAPSEDANDREMAININGTIMESAMELINE:
                    Console.SetCursorPosition(0, CurrsorLine + 2);
                    break;
                case StatsMode.ELAPSEDANDREMAININGTIMESEPERATELINES:
                    Console.SetCursorPosition(0, CurrsorLine + 3);
                    break;


            }
            if (this.previousBar != null)
            {
                this.previousBar.BarFinished += PreviousBar_BarFinished;
                this.previousBar.ProgressBarMoved += PreviousBar_ProgressBarMoved;
            }

        }



        public Console_ProgressBar(string textWhenDone, Console_ProgressBar previousBar)
        {
            FinishedText = textWhenDone;
            CurrsorLine = Console.CursorTop;
            width = Console.WindowWidth;
            Thread worker = new Thread(WatchDog);
            worker.Start();
            Bar_Color = ConsoleColor.Green;
            starttime = DateTime.Now;
            Status = StatsMode.NONE;
            this.previousBar = previousBar;

            if (this.previousBar != null)
            {
                this.previousBar.BarFinished += PreviousBar_BarFinished;
                this.previousBar.ProgressBarMoved += PreviousBar_ProgressBarMoved;
            }

        }
        public Console_ProgressBar(string textWhenDone)
        {
            FinishedText = textWhenDone;
            CurrsorLine = Console.CursorTop;
            width = Console.WindowWidth;
            Thread worker = new Thread(WatchDog);
            worker.Start();
            Bar_Color = ConsoleColor.Green;
            starttime = DateTime.Now;
            Status = StatsMode.NONE;


        }
        public Console_ProgressBar(Console_ProgressBar previousBar)
        {
            FinishedText = "";
            CurrsorLine = Console.CursorTop;
            width = Console.WindowWidth;
            Thread worker = new Thread(WatchDog);
            worker.Start();
            Bar_Color = ConsoleColor.Green;
            starttime = DateTime.Now;
            Status = StatsMode.NONE;
            this.previousBar = previousBar;
            if (this.previousBar != null)
            {
                this.previousBar.BarFinished += PreviousBar_BarFinished;
                this.previousBar.ProgressBarMoved += PreviousBar_ProgressBarMoved;
            }

        }
        public void AddPreviousProgressBar(Console_ProgressBar previousBar)
        {
            this.previousBar = previousBar;
            if (this.previousBar != null)
            {
                this.previousBar.BarFinished += PreviousBar_BarFinished;
                this.previousBar.ProgressBarMoved += PreviousBar_ProgressBarMoved;
                MovetoConsoleLine(CurrsorLine += this.previousBar.MovetoConsoleLine(CurrsorLine));


            }

        }

        public int MovetoConsoleLine(int ConsoleLine)
        {
            //clear out the current progress bar and its stats
            switch (Status)
            {
                case StatsMode.NONE:
                    Console.SetCursorPosition(0, CurrsorLine);
                    Console.Write(new string(' ', Console.WindowWidth));
                    CurrsorLine = ConsoleLine;
                    return 1;
                case StatsMode.ELAPSEDTIME:
                case StatsMode.REMAININGTIME:
                case StatsMode.ELAPSEDANDREMAININGTIMESAMELINE:
                    Console.SetCursorPosition(0, CurrsorLine);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, CurrsorLine + 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    CurrsorLine = ConsoleLine;
                    return 2;
                case StatsMode.ELAPSEDANDREMAININGTIMESEPERATELINES:
                    Console.SetCursorPosition(0, CurrsorLine);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, CurrsorLine + 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, CurrsorLine + 2);
                    Console.Write(new string(' ', Console.WindowWidth));
                    CurrsorLine = ConsoleLine;
                    return 3;
            }

            return 0;
        }
        private void PreviousBar_ProgressBarMoved(object sender, LineMove e)
        {
            if (e.Line_Advanced == 1)
                MoveToNextLine();
            else
                MoveLine(e.Line_Advanced);
        }
        private void PreviousBar_BarFinished(object sender, Finished e)
        {
            if (sender == this)
                return;
            ConsoleHolder.WaitOne();
            Console.SetCursorPosition(0, CurrsorLine);
            Console.Write(new string(' ', Console.WindowWidth));

            if (!elapsedtimeonly)
            {

                switch (Status)
                {
                    case StatsMode.NONE:
                        break;
                    case StatsMode.ELAPSEDANDREMAININGTIMESAMELINE:
                    case StatsMode.ELAPSEDTIME:
                    case StatsMode.REMAININGTIME:
                        Console.SetCursorPosition(0, CurrsorLine + 1);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, CurrsorLine);


                        break;
                    case StatsMode.ELAPSEDANDREMAININGTIMESEPERATELINES:
                        Console.SetCursorPosition(0, CurrsorLine + 1);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, CurrsorLine + 2);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, CurrsorLine);

                        break;


                }

            }
            Bar_Finished(new Finished(e.Line_Reduction));
            CurrsorLine -= e.Line_Reduction;
            ConsoleHolder.ReleaseMutex();
        }
        protected virtual void Bar_Finished(Finished e)
        {
            BarFinished?.Invoke(this, e);
        }

        protected virtual void Progress_Completed(ProgressEvent e)
        {
            ProgressMade?.Invoke(this, e);
        }
        protected virtual void BarMoved(LineMove e)
        {
            ProgressBarMoved?.Invoke(this, e);
        }
        public void StartTime()
        {
            starttime = DateTime.Now;
        }
        public void Reset()
        {
            Running = true;
            Thread worker = new Thread(WatchDog);
            worker.Start();
            StartTime();

        }
        private string getTimeRemaining(int progress)
        {
            if (progress == 0)
                return "Calculating";
            string output = "";
            TimeSpan elapsed = DateTime.Now.Subtract(starttime);
            double secondsPerPercent = elapsed.TotalSeconds / progress;
            float PercentLeft = 100 - progress;
            int SecondsLeft = (int)(PercentLeft * secondsPerPercent);
            int hours, minutes, seconds;
            hours = (int)(SecondsLeft / 3600);
            SecondsLeft -= hours * 3600;
            minutes = (int)(SecondsLeft / 60);
            SecondsLeft -= minutes * 60;
            output = "Remaining Time: ";
            if (hours < 10)
                output += "0";
            output += hours;
            output += ":";
            if (minutes < 10)
                output += "0";
            output += minutes;
            output += ":";
            if (SecondsLeft < 10)
                output += "0";
            output += ((int)SecondsLeft).ToString();
            return output;
        }
        private string GetElapsedTime()
        {
            TimeSpan elapsed = DateTime.Now.Subtract(starttime);
            string output = "";
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
        public void WatchDog()
        {

            while (Running)
            {
                if (MinWidth != -1)
                {
                    ConsoleHolder.WaitOne();
                    if (Console.WindowWidth < MinWidth)
                        Console.WindowWidth = MinWidth;
                    ConsoleHolder.ReleaseMutex();
                }
                if (Console.WindowWidth != width)
                {
                    ClearLine = true;
                    width = Console.WindowWidth;
                }
                if (elapsedtimeonly)
                    DrawElapsedTime(FinishedText);
                Thread.Sleep(100);
            }
        }
        public void MoveLine(int NewLine)
        {
            ClearCurrentConsoleLine();
            ConsoleHolder.WaitOne();
            CurrsorLine = NewLine;
            ConsoleHolder.ReleaseMutex();
            BarMoved(new LineMove(NewLine));
        }
        public void MoveToNextLine()
        {
            int currentLine = CurrsorLine;
            ClearCurrentConsoleLine();
            ConsoleHolder.WaitOne();
            CurrsorLine++;
            ConsoleHolder.ReleaseMutex();
            BarMoved(new LineMove(1));
            Console.SetCursorPosition(0, currentLine);
        }
        public int drawTextProgressBar(string stepDescription, int progress, int total)
        {
            Progress_Completed(new ProgressEvent(progress, total));

            ConsoleHolder.WaitOne();
            
            int Lines = 1;
            MinWidth = totalChunks + 4 + StatusNumberCharacterCount + stepDescription.Length;
            int halfway = totalChunks / 2 - 1;
            double pctComplete = Convert.ToDouble(progress) / total;
            int percent = (int)(pctComplete * 100);

            int currentLine = Console.CursorTop;

            if (Console.WindowWidth >= MinWidth)
            {

                Console.CursorVisible = false;
                Console.SetCursorPosition(0, CurrsorLine);
                Console.Write("".PadRight(Console.WindowWidth));
                Console.SetCursorPosition(0, CurrsorLine);
                Console.CursorLeft = 0;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("["); //start
                Console.CursorLeft = totalChunks + 1;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("]"); //end
                Console.CursorLeft = 1;
                string percentTxt = pctComplete.ToString("0%");
                int numChunksComplete = Convert.ToInt16(totalChunks * pctComplete);

                int ChuncksCompleteOverHalfWay = numChunksComplete - halfway;
                int ChunksUnderHalfWay = halfway - numChunksComplete;
                ChuncksCompleteOverHalfWay = Math.Max(ChuncksCompleteOverHalfWay, 0);
                int CompleteText = Math.Min(percentTxt.Length, ChuncksCompleteOverHalfWay);

                //draw completed chunks
                Console.BackgroundColor = Bar_Color;
                Console.ForegroundColor = DetermineContrastingTextColor(Bar_Color);

                if (numChunksComplete > halfway + percentTxt.Length)
                {

                    Console.Write("".PadRight(halfway));
                    for (int i = 0; i < CompleteText; i++)
                    {
                        Console.Write(percentTxt[i]);
                    }
                    Console.Write("".PadRight(numChunksComplete - (halfway + percentTxt.Length)));
                    //draw incomplete chunks
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write("".PadRight(30 - numChunksComplete));
                }
                else
                {
                    if (numChunksComplete < halfway)
                    {
                        Console.Write("".PadRight(numChunksComplete));
                        //draw incomplete chunks
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write("".PadRight(ChunksUnderHalfWay));
                        Console.Write(percentTxt);
                        Console.Write("".PadRight(totalChunks - (halfway + percentTxt.Length)));
                    }
                    else
                    {
                        int partsLeft = 0;
                        Console.Write("".PadRight(halfway));
                        for (int i = 0; i < CompleteText; i++)
                        {
                            Console.Write(percentTxt[i]);
                            partsLeft++;
                        }
                        //draw incomplete chunks
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;

                        for (int i = numChunksComplete - halfway; i < percentTxt.Length; i++)
                        {
                            Console.Write(percentTxt[i]);
                            partsLeft++;
                        }
                        Console.Write("".PadRight(totalChunks - (numChunksComplete + partsLeft)));

                    }
                }



                //draw incomplete chunks
                //   Console.BackgroundColor = ConsoleColor.Gray;

                //        Console.Write("".PadRight(totalChunks - numChunksComplete));

                //draw totals
                Console.CursorLeft = totalChunks + 3;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                string output = progress.ToString() + " of " + total.ToString();


                Console.Write(output.PadRight(StatusNumberCharacterCount) + stepDescription); //pad the output so when changing from 3 to 4 digits we avoid text shifting



                ClearLine = false;




                
            }
            else
            {
                System.Console.WriteLine(MinWidth);

                ClearLine = true;
            }
            switch (Status)
            {
                case StatsMode.NONE:
                    Console.SetCursorPosition(0, currentLine);
                    break;
                case StatsMode.ELAPSEDTIME:
                    Console.SetCursorPosition(0, CurrsorLine + 1);
                    Console.WriteLine("Elapsed Time: " + GetElapsedTime());
                    //Console.SetCursorPosition(0, currentLine+1);
                    Lines++;
                    break;
                case StatsMode.REMAININGTIME:
                    Console.SetCursorPosition(0, CurrsorLine + 1);
                    Console.WriteLine(getTimeRemaining(percent));
                    Console.SetCursorPosition(0, currentLine + 1);
                    Lines++;
                    break;
                case StatsMode.ELAPSEDANDREMAININGTIMESEPERATELINES:
                    Console.SetCursorPosition(0, CurrsorLine + 1);
                    Console.WriteLine("Elapsed Time: " + GetElapsedTime());
                    Console.WriteLine(getTimeRemaining(percent));
                    Console.SetCursorPosition(0, currentLine + 2);
                    Lines += 2;
                    break;
                case StatsMode.ELAPSEDANDREMAININGTIMESAMELINE:
                    Console.SetCursorPosition(0, CurrsorLine + 1);
                    Console.Write("Elapsed Time: " + GetElapsedTime() + " ");
                    Console.WriteLine(getTimeRemaining(percent));
                    //Console.SetCursorPosition(0, currentLine+1);
                    Lines++;
                    break;
            }
            ConsoleHolder.ReleaseMutex();
            if (ClearLine)
            {
                ClearCurrentConsoleLine();
                ClearLine = false;
            }
            return Lines;
        }
        public void DrawElapsedTime(string TaskName = "")
        {
            string output = "";
            if (TaskName != "")
                output += TaskName + " " + "Elapsed Time: " + GetElapsedTime();
            else
                output += "Elapsed Time: " + GetElapsedTime();
            int currentLine = Console.CursorTop;
            Console.SetCursorPosition(0, CurrsorLine);
            Console.Write(output);
            Console.SetCursorPosition(0, currentLine);

        }
        public void ClearCurrentConsoleLine()
        {
            ConsoleHolder.WaitOne();
            lock ("Clearing Line")
            {
                // int currentLineCursor = Console.CursorTop;
                Console.SetCursorPosition(0, CurrsorLine);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, CurrsorLine);


            }
            ConsoleHolder.ReleaseMutex();
        }
        public void WriteTextAboveProgressBar(string text)
        {
            ConsoleHolder.WaitOne();
            Console.SetCursorPosition(0, CurrsorLine - 1);
            Console.WriteLine(text);
            ConsoleHolder.ReleaseMutex();
        }
        public ConsoleColor DetermineContrastingTextColor(ConsoleColor backgroundColor)
        {
            switch (backgroundColor)
            {
                case ConsoleColor.Black:
                    return ConsoleColor.White;
                case ConsoleColor.Blue:
                    return ConsoleColor.White;
                case ConsoleColor.Cyan:
                    return ConsoleColor.Black;
                case ConsoleColor.DarkBlue:
                    return ConsoleColor.White;
                case ConsoleColor.DarkCyan:
                    return ConsoleColor.White;
                case ConsoleColor.DarkGray:
                    return ConsoleColor.White;
                case ConsoleColor.DarkGreen:
                    return ConsoleColor.White;
                case ConsoleColor.DarkMagenta:
                    return ConsoleColor.White;
                case ConsoleColor.DarkRed:
                    return ConsoleColor.White;
                case ConsoleColor.DarkYellow:
                    return ConsoleColor.White;
                case ConsoleColor.Gray:
                    return ConsoleColor.Black;
                case ConsoleColor.Green:
                    return ConsoleColor.Black;
                case ConsoleColor.Magenta:
                    return ConsoleColor.White;
                case ConsoleColor.Red:
                    return ConsoleColor.Black;
                case ConsoleColor.White:
                    return ConsoleColor.Black;
                case ConsoleColor.Yellow:
                    return ConsoleColor.Black;
            }
            return ConsoleColor.White;
        }
        public void Finish(string TextWhenFinished = "")
        {
            if (!Running)
                return;
            Running = false;
            int AddLine = 0;
            ConsoleHolder.WaitOne();
            Console.SetCursorPosition(0, CurrsorLine);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, CurrsorLine);
            if (FinishedText.Contains("{ELAPSED TIME}"))
                FinishedText = FinishedText.Replace("{ELAPSED TIME}", GetElapsedTime());
            if (FinishedText != "")
                System.Console.Write(FinishedText);
            else
                AddLine = 1;
            if (elapsedtimeonly)
            {

                if (FinishedText != "")
                    System.Console.WriteLine(FinishedText + "Elapsed Time: " + GetElapsedTime());
                else
                    System.Console.WriteLine("Elapsed Time: " + GetElapsedTime());


                this.Bar_Finished(new Finished(0));
            }
            else
            {

                switch (Status)
                {
                    case StatsMode.NONE:
                        Bar_Finished(new Finished(AddLine));
                        break;
                    case StatsMode.ELAPSEDANDREMAININGTIMESAMELINE:
                    case StatsMode.ELAPSEDTIME:
                    case StatsMode.REMAININGTIME:
                        Console.SetCursorPosition(0, CurrsorLine + 1);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, CurrsorLine);
                        Bar_Finished(new Finished(1 + AddLine));
                        break;
                    case StatsMode.ELAPSEDANDREMAININGTIMESEPERATELINES:
                        Console.SetCursorPosition(0, CurrsorLine + 1);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, CurrsorLine + 2);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, CurrsorLine);
                        Bar_Finished(new Finished(2 + AddLine));
                        break;


                }

            }
            ConsoleHolder.ReleaseMutex();
        }
    }
}
