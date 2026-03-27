using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Progress
{
    class ProgressBarEvents
    {
    }
    public class ProgressEvent:EventArgs
    {
        public int currentLine, MaxLine;
        public ProgressEvent(int currentLine,int TotalLines)
        {
            this.currentLine = currentLine;
            this.MaxLine = TotalLines;
        }
    }
    public class Finished:EventArgs
    {
        public int Line_Reduction;
        public Finished(int LineReduction)
        {
            Line_Reduction = LineReduction;
        }
    }
    public class LineMove : EventArgs
    {
        public int Line_Advanced;
        public LineMove(int Line_Advanced)
        {
            this.Line_Advanced = Line_Advanced;
        }
    }
}
