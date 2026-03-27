using ProgressBar;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Progress
{
    public static class Progress_GUI_Handler
    {
        static System.Windows.Forms.Label task_label, over_all_Progress_label, Sub_Progress_label, Overall_time_left, Sub_time_left;
        static ToolTip toolTip;
        static Panel Main_Panel;
        static SplitContainer Overall_Split_Contianer,Time_Remaining_Split_Container,Progress_Split_Container;
        public static ProgressBarHandler Generate_Handler(ref Panel Holding_Panel,ref PictureBox pictureBox,ref ToolTip tip, Color Main_Bar_Color, Color Sub_Bar_Color,bool Show_Task_Name, ProgressBar.ProgressBarHandler.PercentageDisplayMode displayMode, bool Show_Marque=true, bool Show_On_Icon=true)
        {
            Size overall_Size = Holding_Panel.Size;
            ProgressBarHandler output=null;
            float labelsize = 0;
            int calculated_progress_label_size;
            Overall_Split_Contianer = new SplitContainer();
            Time_Remaining_Split_Container = new SplitContainer();
            Progress_Split_Container = new SplitContainer();

            task_label = new System.Windows.Forms.Label();
            Overall_time_left = new System.Windows.Forms.Label();
            Sub_time_left = new System.Windows.Forms.Label();
            over_all_Progress_label = new System.Windows.Forms.Label();
            Sub_Progress_label = new System.Windows.Forms.Label();

            toolTip = tip;

            Main_Panel = Holding_Panel;
            Main_Panel.Controls.Add(Overall_Split_Contianer);
         //   Main_Panel.Location = new Point(13, 217);
            Main_Panel.Name = "panel2";
       //     Main_Panel.Size = new Size(1230, 133);
            Main_Panel.TabIndex = 14;
            // 
            // splitContainer4
            // 
            Overall_Split_Contianer.Dock = DockStyle.Fill;
            Overall_Split_Contianer.Location = new Point(0, 0);
            Overall_Split_Contianer.Name = "ProgressOverallPanel";
            Overall_Split_Contianer.Orientation = Orientation.Horizontal;
            // 
            // splitContainer4.Panel1
            // 
            Overall_Split_Contianer.Panel1.Controls.Add(task_label);
            // 
            // splitContainer4.Panel2
            // 
            Overall_Split_Contianer.Panel2.Controls.Add(Time_Remaining_Split_Container);
            Overall_Split_Contianer.Size = new Size(1230, 133);
            Overall_Split_Contianer.SplitterDistance = 31;
            Overall_Split_Contianer.TabIndex = 0;
            Overall_Split_Contianer.FixedPanel = FixedPanel.Panel1;
            Overall_Split_Contianer.Panel1Collapsed = !Show_Task_Name;
            // 
            // splitContainer5
            // 
            Time_Remaining_Split_Container.Dock = DockStyle.Fill;
            Time_Remaining_Split_Container.Location = new Point(0, 0);
            Time_Remaining_Split_Container.Name = "splitContainer5";
            Time_Remaining_Split_Container.Orientation = Orientation.Horizontal;
            // 
            // splitContainer5.Panel1
            // 
            Time_Remaining_Split_Container.Panel1.Controls.Add(Progress_Split_Container);
            // 
            // splitContainer5.Panel2
            // 
            
            Time_Remaining_Split_Container.Size = new Size(1230, 98);
            if (Sub_Bar_Color != null && Sub_Bar_Color != Color.Transparent)
            {
                Time_Remaining_Split_Container.Panel2MinSize = 70;
                Time_Remaining_Split_Container.Panel2.Controls.Add(Overall_time_left);
                Time_Remaining_Split_Container.Panel2.Controls.Add(Sub_time_left);
                try
                {
                    Time_Remaining_Split_Container.SplitterDistance = 1150;
                }
                catch
                {
                }
            }
            else
            {
                Time_Remaining_Split_Container.Panel2.Controls.Add(Overall_time_left);
                Overall_time_left.Location = new Point(5, 0);
                Time_Remaining_Split_Container.Panel2MinSize = 50;
                Time_Remaining_Split_Container.SplitterDistance = 1180;
            }
            
            Time_Remaining_Split_Container.FixedPanel = FixedPanel.Panel2;
            Time_Remaining_Split_Container.TabIndex = 0;
            // 
            // splitContainer6
            // 
            Progress_Split_Container.Dock = DockStyle.Fill;
            Progress_Split_Container.Location = new Point(0, 0);
            Progress_Split_Container.Name = "Progress_split_Container";
            Progress_Split_Container.Panel1.Controls.Add(pictureBox);
            Holding_Panel.Controls.Remove(pictureBox);
            // 
            // splitContainer6.Panel2
            // 
            Progress_Split_Container.Panel2.Controls.Add(over_all_Progress_label);
            if (Sub_Bar_Color != null && Sub_Bar_Color != Color.Transparent)
            {
                Progress_Split_Container.Panel2.Controls.Add(Sub_Progress_label);
            }
            Progress_Split_Container.Size = new Size(1230, 42);
            
            //hmm need to do some math to figure out the appropiate distance to fit the label size
            labelsize = over_all_Progress_label.CreateGraphics().MeasureString("O: 100%", over_all_Progress_label.Font).Width;
            Progress_Split_Container.Panel2MinSize=(int)Math.Ceiling(labelsize);
            calculated_progress_label_size = (int)1200;//(1230 -(1230 * (labelsize / Progress_Split_Container.Width)));
            Progress_Split_Container.SplitterDistance = calculated_progress_label_size;
            Progress_Split_Container.FixedPanel = FixedPanel.Panel2;
            Progress_Split_Container.TabIndex = 0;
            // 
            // label8
            // 
            over_all_Progress_label.AutoSize = true;
            over_all_Progress_label.Location = new Point(0, 21);
            over_all_Progress_label.Name = "OverallProgressLabel";
            over_all_Progress_label.Size = new Size(57, 25);
            over_all_Progress_label.TabIndex = 1;
            over_all_Progress_label.Text = "O: 0%";
            // 
            // label7
            // 
            Sub_Progress_label.AutoSize = true;
            Sub_Progress_label.Location = new Point(0, -1);
            Sub_Progress_label.Name = "Sub_Progress_label";
            Sub_Progress_label.Size = new Size(47, 25);
            Sub_Progress_label.TabIndex = 0;
            Sub_Progress_label.Text = "S: 0%";
            // 
            // label6
            // 
            Overall_time_left.AutoSize = true;
            Overall_time_left.Location = new Point(5, 22);
            Overall_time_left.Name = "Overall_time_left_label";
            Overall_time_left.Size = new Size(110, 25);
            Overall_time_left.TabIndex = 1;
            Overall_time_left.Text = "Overall Time: ";
            // 
            // label4
            // 
            Sub_time_left.AutoSize = true;
            Sub_time_left.Location = new Point(3, 0);
            Sub_time_left.Name = "Sub_timeleft_label";
            Sub_time_left.Size = new Size(83, 25);
            Sub_time_left.TabIndex = 0;
            Sub_time_left.Text = "Sub time: ";
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.Visible = true;
            Holding_Panel.Size = overall_Size;
            output = new ProgressBarHandler(Main_Bar_Color, Sub_Bar_Color, ref pictureBox, ref toolTip, displayMode, over_all_Progress_label, Overall_time_left, Sub_Progress_label, Sub_time_left, Show_On_Icon, Show_Marque);
            return output;

        }
        public static void Set_Task_Name(string task_name)
        {
            if (task_label == null)
                return;
            if(task_label.InvokeRequired)
            {
                task_label.Invoke(new Action(() =>  Set_Task_Name(task_name)));
                    
            }
            else
            {
                task_label.Text = task_name;
            }
        }
    }
}
