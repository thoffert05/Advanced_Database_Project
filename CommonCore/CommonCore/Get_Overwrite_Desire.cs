using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace CommonCore
{
    public partial class Get_Overwrite_Desire : Form
    {
        public Get_Overwrite_Desire(string File_In_Destination, string source_File, bool Copy = true, bool Allow_Recycle_Bin = true)
        {
            InitializeComponent();
            this.File_In_Destination = File_In_Destination;
            this.source_File = source_File;
            //basically hide the use recycle bin option if one file is chosen to replace the other and if not both are selected or not either is selected
            //^ is a bitwise exclusive OR
            splitContainer3.Panel2Collapsed = (!(checkBox1.Checked ^ checkBox2.Checked)&&Allow_Recycle_Bin);
            checkBox1.Text = "File in Destination: ";
            if (Copy)
            {
                button2.Text = "Copy";
                checkBox2.Text = "File Desired To Be Copied: ";
            }
            else
            {
                button2.Text = "Move";
                checkBox2.Text = "File Desired To Be Moved: ";
            }
            checkBox1.Text+= Path.GetFileName(File_In_Destination);
            checkBox2.Text += Path.GetFileName(source_File);
            this.Allow_Recycle_bin = Allow_Recycle_Bin;

        }
        bool Allow_Recycle_bin;
        public bool Use_Recycle_bin = true;
        public bool Keep_Original_File = true;
        public bool Keep_Destination_file = true;
        public string File_In_Destination;
        public string source_File;

        private void Button3_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(!(checkBox1.Checked||checkBox2.Checked))
            {
                MessageBox.Show("Please select at least one File to keep!");
                return;
            }
            Keep_Original_File = checkBox1.Checked;
            Keep_Destination_file= checkBox2.Checked;
            Use_Recycle_bin = checkBox3.Checked&&Allow_Recycle_bin;
            this.DialogResult= DialogResult.OK;
            this.Close();
        }
    }
}
