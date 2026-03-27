///----------------------------------------------------------------------------------
//this class is an interface class responsible for updating the
//progress value of the upload process and reporting it back to the
//main form using the progress interface.
//
// Project team members: Anthony Hoffert, Able Daniel
///----------------------------------------------------------------------------------

using ProgressBar;


namespace Data_Uploader_To_Spark_On_Cloud
{
    internal class Progress_Interface: IProgress<double>
    {
        //this is the progress bar handler which is responsible for
        //updating a multi-tier progress bar.  I made the progress
        //bar code at my old job and I am reusing it here to show
        //the progress of the upload process, as it is a nice bar
        //you will noticed later that I recreated a similar bar
        //for ingestion in python
        ProgressBarHandler handler = null;
        //current file index being uploaded, this is used to update
        //the progress bar with the correct file number and total
        //files
        public int file_index = 0;
        //total file count of all the files to be uploaded
        public int Total_Files = 1;

        /// <summary>
        /// this constructor sets the progress bar handler object
        /// which is to be updated to display the progress on the 
        /// main form
        /// </summary>
        /// <param name="handler">progress bar handler object to be updated</param>
        public Progress_Interface(ProgressBarHandler handler)
        {
            //set the progress bar handler object to be updated
            this.handler = handler;
        }

        /// <summary>
        /// this function simply updates the progress bar using the
        /// provided progress
        /// </summary>
        /// <param name="value">current task progress</param>
        public void Report(double value)
        {
            //current progress converted to a float as the default
            //measure of the interface is a double but the progress
            //bar I wrote uses only float
            float progress= (float)value;
            //set the progress accounting for the current file index
            //and total files to be uploaded
            handler.setProgress(progress, file_index, Total_Files);
        }
    }
}
