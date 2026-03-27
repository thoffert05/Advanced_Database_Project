///----------------------------------------------------------------------------------
//this class is the NOAA Zip File helper it is responsible for computing the file name,
//downloading the file, and handling throttling if necessary because for lots of
//continous download NOAA will throttle a lot of you go to fast so this program detect
//a potential throttle sitution and holds back long enough for NOAA not to throttle the
//downlaod
//
// Project team members: Anthony Hoffert, Able Daniel
///----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NOAA_Downloader
{
    internal class NOAA_Zip_File
    {
        //path to save the file to
        string localPath = "";
        //computed file name
        string file_name = "";
        //stores the date of the file
        public DateTime date{get;private set;}
        /// <summary>
        /// constructor, which computes the file name, and where to save the file
        /// </summary>
        /// <param name="destination_directory">where to save the file</param>
        /// <param name="date">the date of the file</param>
        public NOAA_Zip_File(string destination_directory,DateTime date)
        {
            //stores the month of the file as MM
            string month = "";
            //if the month is before October
            if (date.Month < 10)
                //add the leading 0 before the month to make it 2 digits
                month = "0" + date.Month;
            //if the month is after October and a 2 digit month
            else
                //just add the month
                month = date.Month.ToString();
            //stores the day as DD
            string day = "";
            //if the day is less than 10
            if (date.Day < 10)
                //add the leading 0 then the day number
                day = "0" + date.Day;
            //else the day is at least 10 thus having 2 digits
            else
                //just set the day
                day = date.Day.ToString();
            //create the file name, all AIS files follow the same pattern AIS_YYYY_MM_DD.zip
            file_name = "AIS_" + date.Year.ToString() + "_" + month + "_" + day + ".zip";
            //get the start of the local path
            localPath = destination_directory;
            //if the local path does not end with \
            if(!localPath.EndsWith("\\"))
                //add the \ at the end of the directory text
                localPath += "\\";
            //add the file name
            localPath += file_name;
            //set the date
            this.date = date;
        }
        /// <summary>
        /// constructor creates the file from the path, it automatically computes the date of the file based on the standard AIS file name
        /// </summary>
        /// <param name="path">local path to the file</param>
        public NOAA_Zip_File(string path)
        {
            //store the local path to the file
            this.localPath = path;
            //get the file name from the path
            this.file_name = path.Substring(path.LastIndexOf("\\") + 1);
            //stores a temp string used for geting the date parts
            string temp = path.Substring(path.LastIndexOf('\\') + 1);
            //remove the AIS_
            temp = temp.Substring(temp.IndexOf('_') + 1);
            //get the year
            string year = temp.Substring(0, temp.IndexOf('_'));
            //remove the year
            temp = temp.Substring(temp.IndexOf('_') + 1);
            //get the month
            string month = temp.Substring(0, temp.IndexOf('_'));
            //remove the month
            temp = temp.Substring(temp.IndexOf('_') + 1);
            //get the day
            string day = temp.Substring(0, temp.IndexOf('.'));
            //create the date
            date = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));
        }
        /// <summary>
        /// this overrides the to string function so that the listbox will display the file name
        /// instead of the class object name
        /// </summary>
        /// <returns>the file name</returns>
        public override string ToString()
        {
            //return the file name
            return file_name;
        }
        /// <summary>
        /// this function downloads the file from NOAA it handles throttling when necessary
        /// it also handles retries when necessary
        /// </summary>
        /// <param name="url">the URL to the file</param>
        /// <param name="progress">the progress interface to return progress to</param>
        /// <param name="maxRetries">maximum number of attempts to retry downloading the file</param>
        /// <returns>Nothing</returns>
        /// <exception cref="Exception">throws an exception if the file could not be downloaded</exception>
        public async Task<bool> DownloadWithRetryAsync(
    string url,
    IProgress<(double progress,string status)> progress,
    int maxRetries = 5)
        {
            //what attempt number it is currently on
            int attempt = 0;
            //while it can download the file
            while (true)
            {
                //try to download the file
                try
                {
                    //wait for the file to download
                    await DownloadWithProgressAsync(url, progress);
                    //if the file downloaded successfully exit
                    return true;
                }
                // network or throttling exeption
                catch (HttpRequestException)
                {
                    //we don't care so ingore it
                }
                // connection dropped mid-stream exception
                catch (IOException)
                {
                    //we don't care so ingore it
                }
                // timeout exception
                catch (TaskCanceledException)
                {
                    //we don't care so ingore it
                }
                //increase the attempt number
                attempt++;
                //if we are attempting more than we are allowed
                if (attempt > maxRetries)
                    //return false
                    return false;
                //if it has tried 3 or more times then NOAA may be doing a hard throttle, NOAA really does not like automated downlaods so a backoff is needed sometimes
                if (attempt >= 3)
                {
                    //wait 5 minutes for a NOAA Hard throttle recovery
                    await CountdownAsync(300,progress);
                }
                //if it has attempted less than 3 times
                else
                {
                    //compute an exponentially loger delay
                    int delay = (int)Math.Pow(2, attempt) * 1000;
                    //wait that number of seconds to avoid a hard throttle
                    await CountdownAsync(delay, progress);
                }

            }
        }
        /// <summary>
        /// this function implements a countdown and returns the time remaining as a status display to keep the user informed
        /// </summary>
        /// <param name="seconds">how long to wait</param>
        /// <param name="status">text interface used for updating the main form with the time remaining</param>
        /// <returns></returns>
        public async Task CountdownAsync(int seconds, IProgress<(double progress, string status)> status)
        {
            //for each second countdown
            for (int i = seconds; i > 0; i--)
            {
                //report how much longer it is waiting
                status?.Report((0,$"Waiting {i}…"));
                //wait 1 second
                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// this function actually downloads the file from the URL
        /// </summary>
        /// <param name="url">where to download the file from</param>
        /// <param name="progress">interface to return progress to the GUI</param>
        /// <returns>Nothing</returns>
        public async Task DownloadWithProgressAsync(string url, 
    IProgress<(double progress, string status)> progress)
        {
            //add the file name to the base url to get the source URL
            url = url + "/" + file_name;
            //create a client to form a connnection to downlaod the file
            using HttpClient client = new HttpClient();
            //connect to the URL to prepare for download
            using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            //verify that it is a proper URL and properly connected
            response.EnsureSuccessStatusCode();
            //get the length of the file
            long totalBytes = response.Content.Headers.ContentLength ?? -1L;
            //did it get and actual file size back from the server
            bool has_total_file_size = totalBytes != -1;
            //get the stream to read the file from the URL
            await using Stream contentStream = await response.Content.ReadAsStreamAsync();
            //get a stream to write the file on the local drive at the save location already given for the file
            await using Stream fileStream = File.Create(localPath);
            //80 kilobyte buffer
            byte[] buffer = new byte[81920];
            //how many bytes have been read so far this is used for progress purposes
            long totalRead = 0;
            //number of bytes just read, 0 if there are no more bytes to read
            int read;
            //while there is data to be read from the file into the buffer
            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                //write the buffer to the file
                await fileStream.WriteAsync(buffer, 0, read);
                //increase the number of bytes read
                totalRead += read;
                //if the total size of the file is known
                if (has_total_file_size)
                {
                    //compute the progress percent
                    double pct = (double)totalRead / totalBytes;
                    //report hte progress and the status text of downloading to the progress interface
                    progress.Report((pct,"Downloading"));
                }
            }
        }
    }
}
