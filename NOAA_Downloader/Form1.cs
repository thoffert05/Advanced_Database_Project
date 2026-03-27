///----------------------------------------------------------------------------------
//this class is the main entrypoint an dmain form of the application.
//It is responsible for handling user input and displaying 
//progress, it launches the download of an entire years worth of AIS data, you specify
//the path to NOAA AIS data for the desired year, it defaults to 2019 which is the year
//chosen for this project as it is the last year I went on a cruise to Alaska on the
//Norwegian Bliss.  this data set takes many days to download if ran continously
//so this program is designed to allow for abrupt stops and it will resume with the last
//file
//
// Project team members: Anthony Hoffert, Able Daniel
///----------------------------------------------------------------------------------
using ProgressBar;
using System.IO.Compression;

namespace NOAA_Downloader
{
    public partial class Form1 : Form
    {
        //stores the multi-progress bar handler
        ProgressBarHandler handler;
        //stores the current file index used for overall progress purposed
        int current_file_index = 0;
        public Form1()
        {
            InitializeComponent();
            //create and setup a progress bar handler for a 2 tier progress bar, in the space of the panel provided
            handler = Progress.Progress_GUI_Handler.Generate_Handler(ref panel1, ref pictureBox1, ref toolTip1, Color.Blue, Color.Cyan, false, ProgressBarHandler.PercentageDisplayMode.OVERALL_AND_SUBPROGRESS_CENTER_OF_WINDOW, true, true);
        }
        /// <summary>
        /// this event occurs when the browse button is clicked for the destination directory. It asks the user to find the directory
        /// after they choose the directory it sets it as the text for the destination
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void button1_Click(object sender, EventArgs e)
        {
            //create a folder browser dialog box
            FolderBrowserDialog diag = new FolderBrowserDialog();
            //allow the user to create a new folder if they choose
            diag.ShowNewFolderButton = true;
            //set the text to display as the title
            diag.Description = "Select the folder to save downloaded NOAA data";
            //if the user does not cancel the box
            if (diag.ShowDialog() == DialogResult.OK)
            {
                //set the destination text for the box
                textBox2.Text = diag.SelectedPath;
            }
        }
        /// <summary>
        /// this occurs when the user clicks the Get needed file list button.  this function downloads
        /// all the files for the year that is specified in the source URL text box.  It first computes
        /// all the days in that year, then it computes the file name for each file and appends it to the
        /// url, then it runs a download for each file and when the download is compelte if successfully
        /// it logs the file name and saves the file name, if the file has already been downlaoded it 
        /// checks the validity of the file and if the zip is valid then it skips the file, but if the 
        /// zip is corrupted it deletes the corrupted zip and redownloads it
        /// <param name="sender">Not Used</param>
        /// <param name="e">Not Used</param>
        private void button2_Click(object sender, EventArgs e)
        {
            //get the year from the given URL
            int year = int.Parse(textBox1.Text.Substring(textBox1.Text.LastIndexOf("/")+1));
            //get all files in the destination directory
            string[] Files_In_Directory = Directory.GetFiles(textBox2.Text);
            //create a list of all valid downloaded files
            SortedList<DateTime,NOAA_Zip_File> Downloaded_Files = new SortedList<DateTime, NOAA_Zip_File>();
            //for each file in the directory
            for (int i = 0; i < Files_In_Directory.Length; i++)
            {
                //if the file is a valid zip file
                if (IsZipValid(Files_In_Directory[i]))
                {
                    //create a NOAA zip file object
                    NOAA_Zip_File temp_file = new NOAA_Zip_File(Files_In_Directory[i]);
                    //add it to the list of downloaded files to be skipped later
                    Downloaded_Files.Add(temp_file.date,temp_file);
                }
                //else if the file is not a valid zip
                else
                {
                    //delete the file
                    File.Delete(Files_In_Directory[i]);
                }
            }
            //create a list of files to be downloaded
            List<NOAA_Zip_File> files_to_Download = new List<NOAA_Zip_File>();
            //store the current day being created and set it as January first of the given year
            DateTime Current_Day = new DateTime(year, 1, 1);
            //get the last day of the given year to know when to stop generating days
            DateTime Last_day = new DateTime(year + 1, 1, 1).AddDays(-1);
            //while it is not the next year
            while(Current_Day<=Last_day)
            {
                //if the date is not already downloaded
                if(!Downloaded_Files.ContainsKey(Current_Day))
                {
                    //create a NOAA zip file object
                    NOAA_Zip_File temp_file = new NOAA_Zip_File(textBox2.Text, Current_Day);
                    //add the file to the list of days to be downloaded
                    files_to_Download.Add(temp_file);
                }
                //go to the next day
                Current_Day = Current_Day.AddDays(1);
            }
            //setup the current file number out of how many files to be downloaded
            label3.Text = "Files (1/" + files_to_Download.Count.ToString() + ")";
            //add all files to be downloaded to the listbox
            listBox1.Items.AddRange(files_to_Download.ToArray());
            //switch to the progress tab
            tabControl1.SelectedIndex = 1;
            //start downlaoding the files no need to wait to finish since this is a GUI thread
            Download_Files(textBox1.Text, files_to_Download);
        }
        /// <summary>
        /// this is a function upates the file it is working on in the listbox to show the user
        /// it uses invokes to always run on the correct thread
        /// </summary>
        /// <param name="index">what index in the listbox it is on and should select</param>
        private void update_listbox_selection(int  index)
        {
            //if it is not on the GUI thread
            if(listBox1.InvokeRequired)
            {
                //execute this function on the GUI thread
                listBox1.Invoke(new Action(() => { update_listbox_selection(index); }));
            }
            //else it is on the GUI thread
            else
            {
                //select this file
                listBox1.SelectedIndex = index;
                //given indexing is 0 based move to the next number
                index++;
                //update the label to show which file out of all files it is curently downloading
                label3.Text = "Files ("+index.ToString()+"/" + listBox1.Items.Count.ToString() + ")";
            }
        }
        /// <summary>
        /// this function downloads all files requested to be downloaded
        /// </summary>
        /// <param name="base_url">the base URL to append the file name to in order to download the file</param>
        /// <param name="files">the files to be downloaded</param>
        /// <returns>nothing is returned as nothing needs to be returned</returns>
        private async Task Download_Files(string base_url,List<NOAA_Zip_File> files)
        {
            //create a progress interface to return the progress and update the main form progress
            //and the status text
            var progress = new Progress<(double progress,string status)>(p =>
            {
                //get the progress and status returned
                (double progress, string status) prog = p;
                //update the progress on the main screen
                handler.setProgress((float)prog.progress, current_file_index, files.Count);
                //update the status text
                textBox3.Text = prog.status;
            });
            //for each file given
            for (int i = 0; i < files.Count; i++)
            {
                //set the current file index for progress purposes
                current_file_index = i;
                //update the listbox
                update_listbox_selection(i);
                //download the file and wait for it to finish
                await files[i].DownloadWithRetryAsync(base_url,progress);
            }

        }
        /// <summary>
        /// this function verifies that a zip file is not corrupted
        /// </summary>
        /// <param name="path">the path to the zip file</param>
        /// <returns>True if the zip file is valid otherwise it returns false</returns>
        public bool IsZipValid(string path)
        {
            try
            {
                //try to open the zip
                using ZipArchive zip = ZipFile.OpenRead(path);
                //if the zip opens without throwing an exception it is valid
                return true;
            }
            catch
            {
                //if it throws and exception while trying to open the zip it is invalid
                return false;
            }
        }
    }
}
