using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Progress
{
    class ProgressMiddleMan
    {

        ProgressMiddleMan UpperLevelBar =null;
        ProgressBar.Progress bar;
        int level = 0;

        float percentageOfThisBarInParent;
        int SubBarsCompleted = 0;
        ProgressMiddleMan(ProgressBar.Progress thisBar, ProgressMiddleMan upperLevel,int thisLevel,float percentageThisTaskMakesUptheWhole)
        {
            bar = thisBar;
            percentageOfThisBarInParent = percentageThisTaskMakesUptheWhole;
            UpperLevelBar = upperLevel;
            level = thisLevel;

        }
        public void ThisBarCompleted()
        {
            SubBarsCompleted++;
            bar.SetProgress(0);
        }
        public void SetProgress(float Progress)
        {
            bar.SetProgress(Progress);
            //compute ProgressForParentBar
            if (UpperLevelBar != null)
            {
                float ParentProgress = Progress * percentageOfThisBarInParent + SubBarsCompleted * percentageOfThisBarInParent;
                UpperLevelBar.SetProgress(ParentProgress);
            }
        }
        public float GetProgress()
        {
            return bar.GetProgress(); 
        }

    }
}
