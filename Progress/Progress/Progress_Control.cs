using ProgressBar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Progress
{
    public partial class Progress_Control : UserControl
    {
        private ProgressBarHandler handler;
        public Color Main_Bar_Color = Color.Blue;
        public Color Sub_Bar_Color = Color.Cyan;
        public ProgressBar.ProgressBarHandler.PercentageDisplayMode display_Mode = ProgressBarHandler.PercentageDisplayMode.NONE;
        public bool Show_on_Icon = true;
        public bool show_Marque = true;
        public Progress_Control()
        {
            InitializeComponent();
            handler = new ProgressBarHandler(Main_Bar_Color, Sub_Bar_Color, ref pictureBox1, ref toolTip1, display_Mode, label5, label3, label2, label4, Show_on_Icon, show_Marque);
        }
        public void Set_Progress(float Progress,int stage,int total_Stages)
        {
            handler.setProgress(Progress, stage, total_Stages);
        }
        public void Set_Overall_Progress(float Progress)
        {
            handler.setOverallProgress(Progress);
        }
        public void Done()
        {
            handler.OverAllDone();
        }
        public void Hide()
        {
            handler.Hide();
            splitContainer1.Visible = false;
        }
        public void Set_Task_Name(string TaskName)
        {
            label1.Text = TaskName;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            handler.SetOverallProgressBarColor(Main_Bar_Color);
            handler.SetSubBarColor(Sub_Bar_Color);
            handler.ShowProgressOnIcon = Show_on_Icon;
            handler.showMarque = show_Marque;
            handler.DisplayPercentMode = display_Mode;
            base.OnPaint(e);
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
