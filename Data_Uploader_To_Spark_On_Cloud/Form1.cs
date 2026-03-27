///----------------------------------------------------------------------------------
//this class is the main entrypoint an dmain form of the application.
//It is responsible for handling user input and displaying 
//progress, it launches the uplaod of all files in a given directory to a specified
//google cloud storage bucket and tracks the progress of the upload process. It also
//allows for aborting the upload process and logs all actions and events to a log file.
//lastly it allows for aborting and it saves progress so that it can be resumed later
//without re-uploading files that have already been uploaded. It uses the Google Cloud
//Storage API to perform the upload and a custom progress bar to display the progress of
//the upload process. given the size of the files and the number of files this process
//can take a long time, infact days so the ability to track progress and abort if needed
//is important, also the ability to resume without re-uploading files is important to save
//time and resources.
//
// Project team members: Anthony Hoffert, Able Daniel
///----------------------------------------------------------------------------------

using Logger;


namespace Data_Uploader_To_Spark_On_Cloud
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// This is the form constructor
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            //create the multi-tier progress bar handler and set it to update the
            //progress bar on the main form
            handler = Progress.Progress_GUI_Handler.Generate_Handler(ref panel1, ref pictureBox1, ref toolTip1, Color.Blue, Color.Cyan, false, ProgressBar.ProgressBarHandler.PercentageDisplayMode.OVERALL_AND_SUBPROGRESS_CENTER_OF_WINDOW);
            //load the list of files that have already been uploaded to avoid re-
            //uploading them
            Load_uploaded_files();
            //setup the progress interface to update the multi-tear progress bar
            progress_interface = new Progress_Interface(handler);
            //populate the log options on the main menu, I wrote the logger class at my
            //previous job and I am reusing it here to log all actions and events to a
            //log file, as it is pretty good at displaying logs
            Logger.LoggerMain.Setup_Menu(menuStrip1);
            //set the log save interval and output directory for the logs, I it will be
            //saved to
            Logger.LoggerMain.Set_Auto_Save_Interval_And_Output_Directory(TimeSpan.FromSeconds(30), "C:\\Temp\\NOAA_DATABASE_UPLOAD_LOGS");
            //set it up to automatically append the current time to the log
            LoggerMain.AppendLogTime = true;
        }
        //this stores the abort flag to stop the upload and it starts cleared
        public static bool abort = false;
        //this stores the multi-tier progress bar handler which is used to update the
        //progress bar on the main form
        ProgressBar.ProgressBarHandler handler;
        //this stores the progress interface which is used to update the progress of the
        //upload process
        Progress_Interface progress_interface;
        //this list stores the files that have already been
        //uploaded to avoid re-uploading
        public List<string> files_uploaded = new List<string>();
        //current file index being uploaded, this is used for
        //progress reasons
        int index = 0;
        //this is the google cloud file uploader object which
        //is used to perform the upload the files to the 
        //google cloud storage bucket
        Google_Cloud_File_Uploader uploader = null;
        /// <summary>
        /// this is the event handler for the button to 
        /// select the directory of files to be uploaded, it 
        /// opens a folder browser dialog and sets the 
        /// selected path to the text box on the main form
        /// for the NOAA AIS CSV files to be uploaded from
        /// </summary>
        /// <param name="sender">Not Used</param>
        /// <param name="e">Not Used</param>
        private void button1_Click(object sender, EventArgs e)
        {
            //create a folder browser to ask for the directory
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            //if the user does not click cancel
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                //get the directory the user chose
                string selectedPath = folderBrowserDialog.SelectedPath;
                //set it to the directory text
                textBox1.Text = selectedPath;

            }
        }
        /// <summary>
        /// this occurs after a file is successfully uploaded, 
        /// it adds the file to the list of uploaded files so
        /// thatit is not re-uploaded if the process is resumed 
        /// later.
        /// </summary>
        /// <param name="path">file path that was just uploaded</param>
        public void file_uploaded(string path)
        {
            //add the path to the files uploaded list
            files_uploaded.Add(path);
            //store the list of files as a string to be saved
            //to a text file, this is used to save the progress
            //of the upload process so that it can be resumed
            //later without re-uploading files that have
            //already been uploaded, which is great for a 
            //process that can take days to complete.
            string files = "";
            //for each file that has been uploaded already
            for (int i = 0; i < files_uploaded.Count; i++)
            {
                //add it to the string to be saved
                files += files_uploaded[i] + "\n";
            }
            //save the string of files uploaded to a text file
            File.WriteAllText("files_uploaded.txt", files);
        }
        /// <summary>
        /// this function loads the list of files that have 
        /// already been uploaded from a text file, this is 
        /// used to resume the upload process from where it
        /// left off without re-uploading files that have 
        /// already been uploaded.
        /// </summary>
        public void Load_uploaded_files()
        {
            //check to see if the file exists
            if (File.Exists("files_uploaded.txt"))
            {
                //load all lines from the file as that is all
                //the files already uploaded
                string[] files = File.ReadAllLines("files_uploaded.txt");
                //for each line that was read
                for (int i = 0; i < files.Length; i++)
                {
                    //if the line is not empty
                    if (files[i].Trim() != "")
                        //add the file to the list of files
                        //already uploaded
                        files_uploaded.Add(files[i]);
                }
            }
        }
        /// <summary>
        /// this is when the user clicks the button to start
        /// the upload process, it initializes the google 
        /// cloud file, uploader, checks what has already been 
        /// uploaded, sets up the progress bar and starts the 
        /// upload
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void button2_Click(object sender, EventArgs e)
        {
            //clear the abort flag in case it was set from a
            //previous upload attempt
            abort = false;
            //get the bucket name from the GUI
            string bucket_name = textBox2.Text;
            //initialize the google cloud file uploader with
            //the bucket name, this handles authentication and
            //the upload process
            uploader = new Google_Cloud_File_Uploader(textBox2.Text);
            //if the user clicked abort then exit this function
            if (abort)
                return;
            //get all the files in the directory that are CSV
            //files that the user specified to be uploaded
            string[] files = Directory.GetFiles(textBox1.Text, "*.csv");
            //put the files on the list box to show the user
            //what files are being upload and to show which
            //file is currently being uploaded
            listBox1.Items.AddRange(files);
            //for each CSV file in the directory
            //this loop is used to find the first file that
            //has not been already uplaodeed to resume the 
            //upload process from where it left off
            for (int i = 0; i < files.Length; i++)
            {
                //if the user clicked abort then exit this
                //function immeditely
                if (abort)
                    //exit this function immediatley
                    return;
                //if the files hasnot already been uploaded
                if (!files_uploaded.Contains(files[i]))
                {
                    //get the index of the first file to be
                    //uploaded
                    index = i;
                    //break out of this loop
                    break;
                }
            }
            //set the total number of files to the count of
            //CSV files in the directory the user wishes to 
            //upload, this is used for progress tracking
            progress_interface.Total_Files = files.Length;
            //set the current file index to the index of
            //the first file to be uploaded
            progress_interface.file_index = index;
            //select the first file to be uplaoded in the 
            //list box to show the user which file is being
            //uploaded
            listBox1.SelectedIndex = index;
            //switch to the upload tab to show the progress
            //of the upload process
            tabControl1.SelectedTab = tabPage2;
            //if the user clicked abort then exit this function
            if (abort)
                //exit this function immediately
                return;
            //create a thread to start uploading the files
            //so that the GUI remains responsive to the user
            Thread worker = new Thread(() => Upload_Files(bucket_name, files));
            //log the start of the upload
            Logger.LoggerMain.Log("Starting upload thread", LogItem.LogType.Log);
            //log the bucket that it is being uploaded to
            Logger.LoggerMain.Log($"Google cloud bucket: {textBox2.Text}", LogItem.LogType.Log);
            //log the directory that it is getting the files
            //from to be upladed
            Logger.LoggerMain.Log($"File Directory to upload: {textBox1.Text}", LogItem.LogType.Log);
            //start the upload process
            worker.Start();
        }
        /// <summary>
        /// this uplaods a list of files to the google cloud
        /// storage bucket
        /// </summary>
        /// <param name="bucket_name">name of the bucket to upload to</param>
        /// <param name="files">file to upload to the bucket</param>
        /// <returns>Nothing</returns>
        public async Task Upload_Files(string bucket_name, string[] files)
        {
            //if the user clicked abort then exit this function
            if (abort)
                //exit this function immediately
                return;
            //for each file in the list of files to be
            //uploaded starting at the first file that has not
            // already been uploaded
            for (int i = index; i < files.Length; i++)
            {
                //verify the file has not been uploaded already
                if (!files_uploaded.Contains(files[i]))
                {
                    //if the user clicked abort then exit this
                    //function
                    if (abort)
                        //exit this function immediately
                        return;
                    //update the selected file on the listbox
                    //to show the user which file is being
                    //uploaded
                    update_listbox_index(i);
                    //update the current task text to show the
                    //user which file is being uploaded
                    update_current_task("Uploading file: " + files[i].Substring(files[i].LastIndexOf('\\') + 1));
                    //log that it is starting to upload the 
                    //file to the bucket and which file it is
                    //uploading
                    LoggerMain.Log($"Started upload of file to bucket: {files[i]}", LogItem.LogType.Log);
                    //wait for the file to be uploaded to the
                    //bucket and get the result of the upload
                    bool success = await uploader.UploadFileAsync(files[i], Path.GetFileName(files[i]), progress_interface);
                    //if the file was uploaded successfully
                    if (success)
                    {
                        //log that the file was successfully
                        //uploaded to the bucket
                        LoggerMain.Log($"Successfully uploaded file to bucket: {files[i]}", LogItem.LogType.Log);
                        //update the list of the files that
                        //have been uploaded to include the
                        //file that was just uploaded to allow
                        //for a safe quit and resume later without
                        //repating files that were already uploaded
                        file_uploaded(files[i]);
                    }
                    //if the user clicked abort then exit this
                    //function immediately
                    if (abort)
                        //exit this function immediately
                        return;
                    //update the progress bar to show the
                    //progress of the upload
                    handler.setProgress(1, i, files.Length);
                    //update the current file index in the
                    //progress interface to keep it accurate
                    progress_interface.file_index = i;
                }
            }
        }
        /// <summary>
        /// this function updates the selected index of the 
        /// list box to show what file it is on
        /// invokes must be used since the upload process is 
        /// running on a different thead than the GUI thread
        /// </summary>
        /// <param name="i">listbox index of the current file</param>
        private void update_listbox_index(int i)
        {
            //if the listbox is running on a different thread
            if (listBox1.InvokeRequired)
            {
                //invoke this function on the GUI thread to update
                //the selected index
                listBox1.Invoke(new Action(() => update_listbox_index(i)));
            }
            //else if it is on the GUI thread
            else
            {
                //update the selected index of the listbox
                listBox1.SelectedIndex = i;
            }
        }
        /// <summary>
        /// this function upates the current task text 
        /// to show the user which file is being uploaded
        /// this must be invoked since the textbox lives on
        /// a different thread
        /// </summary>
        /// <param name="task">new text</param>
        public void update_current_task(string task)
        {
            //if it is on a different thread than the GUI
            //thread
            if (textBox6.InvokeRequired)
            {
                //invoke this function on the GUI thread to
                //update the text of the current task textbox
                textBox6.Invoke(new Action(() => update_current_task(task)));
            }
            //else it is on the GUI thread so it is safe to 
            //update the text of the current task textbox
            //to show the new text
            else
            {
                //set the text of the current task textbox to show
                //the new text
                textBox6.Text = task;
            }
        }
        /// <summary>
        /// this function occurs when the user clicks the 
        /// close menu option so this function handles aborting
        /// the running upload, closing any open log forms
        /// and then closing this function
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //aborts any upload in process
            ABORT();
            //closes any open log forms to avoid leaving open
            //windows after the main form is closed
            LoggerMain.CloseLogForm();
            //closes the main form
            this.Close();
        }
        /// <summary>
        /// this function occurs when the user clicks the 
        /// abort menu option, it sets the abort flag to true
        /// by calling the abort function to signal the upload
        /// process to stop
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void abortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //aborts any upload in process by setting the
            //abort flag to true
            ABORT();
        }
        /// <summary>
        /// this function sets the abort flag to abort any
        /// upload in process, it also logs the abort action
        /// and saves the log
        /// </summary>
        private void ABORT()
        {
            //set the abort flag to true to signal the upload
            //process to stop
            abort = true;
            //log that the upload process is being aborted
            LoggerMain.Log("ABORTING UPLOAD PROCESS", LogItem.LogType.Warning);
            //save the log
            LoggerMain.Save();
        }
    }
}
