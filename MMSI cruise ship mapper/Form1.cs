//-------------------------------------------------------------------
//
// this program is designed to read a CSV file containing cruise
// ship information, map the ships based on their names, and update
// the CSV file with the corresponding MMSI numbers. The program also
// provides a graphical user interface (GUI) to display the progress
// of the mapping process.
// Authors: Anthony Hoffert, and Able
//
//-------------------------------------------------------------------
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
using ProgressBar;
using System.Threading;

namespace MMSI_cruise_ship_mapper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //setup progress handler for all files searched
            handler = Progress.Progress_GUI_Handler.Generate_Handler(ref panel1, ref pictureBox1, ref toolTip1, Color.Blue, Color.Cyan, true, ProgressBarHandler.PercentageDisplayMode.OVERALL_AND_SUBPROGRESS_CENTER_OF_WINDOW);
            //setup progress handler for all ships found
            handler2 = Progress.Progress_GUI_Handler.Generate_Handler(ref panel2, ref pictureBox2, ref toolTip1, Color.Green, Color.LightGreen, false, ProgressBarHandler.PercentageDisplayMode.OVERALL_AND_SUBPROGRESS_CENTER_OF_WINDOW);
        }
        ProgressBarHandler handler;
        ProgressBarHandler handler2;
        string Cruise_Ship_CSV_path = "";
        float total_ships = 0;
        float ships_found_so_far = 0;
        /// <summary>
        /// occurs when the browse for ship directory button is clicked
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not Used</param>
        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (diag.ShowDialog() == DialogResult.OK)
            {
                Cruise_Ship_CSV_path = diag.FileName;
                textBox2.Text = Cruise_Ship_CSV_path;
            }
        }
        /// <summary>
        /// occurs when the browse button for the AIS directory is clicked
        /// this method lets the user choose what folder has all the AIS CSVs
        /// </summary>
        /// <param name="sender">Not Used</param>
        /// <param name="e">Not Used</param>
        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog diag = new FolderBrowserDialog();
            if (diag.ShowDialog() == DialogResult.OK)
            {
                string output_folder = diag.SelectedPath;
                textBox1.Text = output_folder;

            }
        }
        /// <summary>
        /// this method maps cruise ships to AIS entries where the 
        /// ship name matches it automatically gets the MMSI number
        /// of the cruise ship and updates the CSV file with the new 
        /// information. It also keeps track of progress so that if 
        /// the program is closed it can be resumed where it left off.
        /// </summary>
        /// <param name="csv_path">Path to the cruise ship CSV</param>
        /// <param name="AIS_folder">Path to the AIS directory folder</param>
        public void Map_Cruise_Ships(string csv_path, string AIS_folder)
        {
            int skip_to_line = 0;
            int last_file_index = 0;
            //load the last progress if it exists so that we can
            //resume where we left off if the program was closed
            (skip_to_line, last_file_index) = Get_Last_Progress();
            //read the cruise ship CSV and get the names of all the
            //ships and if their MMSI numbers were already found
            string ship_csv_data = System.IO.File.ReadAllText(csv_path);
            //store the names of the ships in a list for easy access
            //later
            List<string> ship_names = new List<string>();
            //get all the lines of the CSV so that we can update them
            //later with the new MMSI numbers
            string[] lines = ship_csv_data.Split('\n');
            //for each line
            for(int i=1;i<lines.Length; i++)
            {
                //get the columns in the line
                string[] columns = lines[i].Split(',');
                //if there are columns in the line
                if (columns.Length > 0)
                {
                    //if there is a blank column which would be MMSI
                    //number
                    if (lines[i].Contains(",,"))
                    {
                        //add the name to the list of ships to be
                        //found 
                        ship_names.Add(columns[0].Trim().ToUpper());
                    }
                    //else the ship already has an MMSI number
                    else
                    {
                        //increment the number of ships found so far
                        //since this ship already has an MMSI number
                        ships_found_so_far++;
                    }
                    //increment the total number of ships for progress
                    //tracking purposes
                    total_ships++;
                }
            }
            //get all files in the AIS directory
            string[] files = System.IO.Directory.GetFiles(AIS_folder, "*.csv");
            //did it find a ship when the loop finished and it needs to save
            //the CSV file with the new information
            bool need_to_save = false;
            //for each file in the AIS directory
            for (int i=0; i<files.Length; i++)
            {
                //if the file has already been processed in a
                //previous run of the program then skip it
                if (i<last_file_index)
                {
                    continue;
                }
                //if it is now past where it resumed from then reset
                //the line index to 0 so that it starts
                if (i!=last_file_index)
                {
                    //go back to the start of the file
                    skip_to_line = 0;
                }
                //update the progress of the AIS file searching
                handler.setSubBarLabel(files[i].Substring(files[i].LastIndexOf('\\')+1));
                //map the try tp update the ship MMSI numbers based on
                //the lines in the AIS file
                if(Update_CSV_And_Map(ref ship_names, ref lines, files[i], csv_path, skip_to_line, i,files.Length))
                {
                    //if a match was found then set the flag we need
                    //to save
                    need_to_save = true;
                    //tell the user that all cruise ships are accounted for
                    MessageBox.Show("All cruise ships have been mapped successfully!");
                    //stop the loop
                    break;
                }
            }
            //if there were new ships found in this file
            if(need_to_save)
            {
                //join all the lines together and save it back to the CSV file
                System.IO.File.WriteAllText(csv_path, string.Join("\n", lines));
                //tell the user that the file has been saved
                MessageBox.Show("Cruise Ship Status Updated!");
            }
        }
        /// <summary>
        /// when a ship is found this method is called to add the ship
        /// to the list box to show it to the user
        /// </summary>
        /// <param name="ship_name">Name of the ship to add</param>
        public void Add_Ship_Found_To_List_Box(string ship_name)
        {
            //if this is not called from the GUI thread
            if (listBox1.InvokeRequired)
            {
                //invoke this method on the GUI thread to avoid cross
                //thread operation exceptions
                listBox1.Invoke(new Action(() => Add_Ship_Found_To_List_Box(ship_name)));
                //exit this function
                return;
            }
            //else it is called from the GUI thread so we can safely
            //update the list box
            else
            {
                //add the ship to the list of ships found
                listBox1.Items.Add(ship_name);
                //update the label to show how many ships have been
                //found so far
                label3.Text = "Ships Found: " + listBox1.Items.Count.ToString();
            }
        }
        /// <summary>
        /// this method reads through the 
        /// AIS file line by line and tries 
        /// to find MMSI numbers for the 
        /// cruise ships based on their 
        /// names. If it finds a match
        /// then it puts the MMSI number in 
        /// the correct place in the CSV 
        /// file and returns true once all
        /// ships in the CSV file have been 
        /// mapped to an MMSI number. 
        /// </summary>
        /// <param name="ship_names">All ship names missing an MMSI number</param>
        /// <param name="lines">All lines from the CSV file</param>
        /// <param name="path">Path to the AIS file</param>
        /// <param name="csv_path">Path to the CSV file</param>
        /// <param name="skip_to_line">Line number to start processing from</param>
        /// <param name="file_index">Index of the current file being processed</param>
        /// <param name="total_file_count">Total number of AIS files for progress purposes</param>
        /// <returns>True once all ships have been mapped</returns>
        public bool Update_CSV_And_Map(ref List<string> ship_names,ref string[] lines,string path,string csv_path,int skip_to_line,int file_index,int total_file_count)
        {
            //stores the number of lines
            //in the AIS file for progress
            //tracking purposes, it is a 
            //long because some AIS files
            //can be very large and have
            //millions of lines
            long count = 0;
            //open the AIS file and count
            //the number of lines so that
            //we can compute file progress
            using (var reader = new StreamReader(path))
            {
                //while there is a line to
                //read
                while (reader.ReadLine() != null)
                    //increase the line
                    //count
                    count++;
            }
            //compute the number of lines
            //in 1 percent of the file to
            //reduce the number of times
            //we update the progress of
            //the file read
            int one_percent_index = (int)(count / 100);
            //stores the names of mapped
            //ships so that they are 
            //skipped in future iterations
            List<string> mapped_ships = new List<string>();
            //update the progress of the ships
            //found so far before
            //processing this file
            handler2.setProgress(((ships_found_so_far + mapped_ships.Count) / total_ships), mapped_ships.Count / (float)ship_names.Count);
            //open the AIS file and read
            //through
            using (var reader = new StreamReader(path))
            {
                string line;
                string Upper_line;
                float index = 0;
                //flag so it only updates
                //the progress at the start
                //and then at 1% intervals
                //to reduce the number of
                //times
                bool progress_not_set = true;
                //stream the file line by
                //line because it is too
                //big to read into memory
                //all at once
                while ((line = reader.ReadLine()) != null)
                {
                    //catch up to where we
                    //left off 
                    if (index++ <= skip_to_line)
                    {
                        continue;
                    }
                    //if this is the first
                    //iteration after 
                    //resuming then update
                    //the progress
                    if (progress_not_set)
                    {
                        //update the progress
                        handler.setProgress(index / count, file_index, total_file_count);
                        //clear the flag
                        progress_not_set = false;
                    }
                    //convert the line to
                    //upper case to 
                    //make it case
                    //insensitive 
                    Upper_line = line.ToUpper();
                    string ship_name = "";
                    //for each ship name
                    for (int s = 0; s < ship_names.Count; s++)
                    {
                        //if the ship has
                        //already been
                        //mapped then skip
                        //it
                        if (mapped_ships.Contains(ship_names[s].ToUpper()))
                        {
                            continue;
                        }
                        //get the ship name
                        //as uppercase
                        ship_name = ship_names[s].ToUpper();
                        //if the ship is in
                        //the line and it is
                        //not a special case
                        if (Upper_line.Contains(ship_name)&&!line.Contains("INNOVATION")&&line.Contains(",366"))
                        {
                            //map the ship
                            //get its MMSI
                            //number and 
                            //update the CSV
                            //line to match
                            if (Map_MMSI(line.Substring(0, line.IndexOf(',')), ship_names[s], ref lines))
                            {
                                //if it has
                                //not in the
                                //mapped list
                                if (!mapped_ships.Contains(ship_names[s]))
                                {
                                    //update the file
                                    System.IO.File.WriteAllText(csv_path, string.Join("\n", lines)); 
                                    //add the ship to the list of shippes mapped
                                    mapped_ships.Add(ship_name);
                                    //update the display with the new ship found
                                    Add_Ship_Found_To_List_Box(ship_names[s]);
                                    //update the ships found progress
                                    handler2.setProgress(((ships_found_so_far + mapped_ships.Count) / total_ships), mapped_ships.Count / (float)ship_names.Count);
                                    //exit the ship search loop since an AIS line will only contain one ship
                                    break;
                                }
                                //if all ships are found
                                if (mapped_ships.Count == ship_names.Count)
                                {
                                    //exit the loop saying it is done
                                    return true;
                                }
                            }

                        }
                    }
                    //if the progress has
                    //been updated by 1%
                    if (index % one_percent_index == 0)
                    {
                        //update the progress
                        handler.setProgress(index / count, file_index, total_file_count);
                        //save the progress 
                        //to be able to 
                        //resume here later
                        Save_Progress(index, file_index);
                    }
                }
                //if it gets here the AIS
                //file is done and not all 
                //ships have been found so
                //return false
                return false;
            }

        }
        /// <summary>
        /// this method saves where it is 
        /// at which file it is on and 
        /// which line number so that it 
        /// can resume from where it left
        /// off later
        /// </summary>
        /// <param name="index">file index</param>
        /// <param name="file_index">line number</param>
        public void Save_Progress(float index,int file_index)
        {
            //file name
            string current_progress = "Status.txt";
            //save the 2 numbers
            string progress=index+"\r\n"+file_index;
            //save the text file
            System.IO.File.WriteAllText(current_progress, progress);
        }
        /// <summary>
        /// this method loads the progress
        /// file and returns the file index
        /// and line index in that file
        /// </summary>
        /// <returns>file index and line index</returns>
        public (int,int) Get_Last_Progress()
        {
            //file name to read
            string current_progress = "Status.txt";
            //if the file exists
            if (System.IO.File.Exists(current_progress))
            {
                //read the lines in the file
                string[] data = System.IO.File.ReadAllText(current_progress).Split('\n');
                //if there are 2 lines in
                //the file
                if(data.Length>=2)
                {
                    //read the file index
                    int index = int.Parse(data[0].Trim());
                    //read the line index
                    int file_index = int.Parse(data[1].Trim());
                    //return both indices
                    return (index, file_index);
                }
            }
            //if no file return 0 for both
            //indices to start from the start
            return (0, 0);
        }
        /// <summary>
        /// this method takes a MMSI
        /// number and maps it to a ship
        /// name
        /// </summary>
        /// <param name="mmsi">MMSI number for the ship</param>
        /// <param name="ship_name">ship it belongs to</param>
        /// <param name="lines">lines of the file</param>
        /// <returns>true if ship was found</returns>
        public bool Map_MMSI(string mmsi,string ship_name,ref string[] lines)
        {
            //for each file in the CSV file
            for(int i=1; i<lines.Length; i++)
            {
                //if the line has no MMSI
                //number
                if (lines[i].Contains(",,"))
                {
                    //if the line has the
                    //ship name
                    if (lines[i].ToUpper().Contains(ship_name))
                    {
                        //put the MMSI 
                        //number in the blank
                        //column
                        lines[i] = lines[i].Replace(",,", "," + mmsi + ",");
                        //return true
                        //that it was mapped
                        return true;
                    }
                }
            }
            //if here then it was not mapped
            return false;
        }
        /// <summary>
        /// this happens when  the process
        /// button is clicked it starts the
        /// mapping process
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void button3_Click(object sender, EventArgs e)
        {
            //create a thread to find the
            //MMSI numbers so that the GUI
            //can stay respondive
            Thread worker = new Thread(() => Map_Cruise_Ships(textBox2.Text, textBox1.Text));
            //launch the search thread
            worker.Start();
        }

        /// <summary>
        /// this happens when the current
        /// file progress panel is resized
        /// it is meant to redraw the progress
        /// bar. this is done because the
        /// progres bar is not continously
        /// updated
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void panel1_SizeChanged(object sender, EventArgs e)
        {
            //try to do update the bar
            try
            {
                //if the file progress bar
                //handler exists
               if (handler != null)
                    //draw the progress bar
                    handler.Draw();
            }
            //if it fails we don't care
            catch
            {
            }
        }
        /// <summary>
        /// this method happens when the
        /// panel of the ship progress
        /// is resized this redraws it
        /// so that it can be seen again
        /// this is done because the 
        /// progress bar is not continously
        /// updated
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        private void panel2_SizeChanged(object sender, EventArgs e)
        {
            //try to update the bar
            try
            {
                //if the bar exists
                if (handler2 != null)
                    //draw it
                    handler2.Draw();
            }
            //if it fails we don't care
            catch
            {
            }
        }
    }
}
