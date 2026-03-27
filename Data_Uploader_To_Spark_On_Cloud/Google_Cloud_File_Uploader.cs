///----------------------------------------------------------------------------------
//this class is responsible for uploading files to a specified Google Cloud Storage 
//bucket. It uses the Google Cloud Storage API to perform the upload and tracks the 
//progress of the upload using a progress interface.
//
// Project team members: Anthony Hoffert, Able Daniel
///----------------------------------------------------------------------------------

using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1;
using Google.Apis.Upload;
using Google.Cloud.Storage.V1;





namespace Data_Uploader_To_Spark_On_Cloud
{
    internal class Google_Cloud_File_Uploader
    {
        //name of the google blucket to upload the files to
        private  string bucketName;
        /// <summary>
        /// This is the constructor which sets the bucket name
        /// </summary>
        /// <param name="bucketName">name of the bucket to upload the files to</param>
        public Google_Cloud_File_Uploader(string bucketName)
        {
            //set the bucket name
            this.bucketName = bucketName;
        }

        /// <summary>
        /// this class uploads the file to the google cloud storage bucket and tracks
        /// the progress and reports it back using the progress interface.
        /// it runs asynchronously to avoid blocking the main thread 
        /// so that the GUI can remain responsive to allow for abort and progress
        /// updates
        /// </summary>
        /// <param name="localPath">path to the file to be uploaded</param>
        /// <param name="objectName">the name of the file</param>
        /// <param name="progress_interface">progress interface to update</param>
        /// <returns>nothing as there is nothing to return</returns>
        public async Task<bool> UploadFileAsync(string localPath, string objectName,IProgress<double> progress_interface)
        {
            //if the main form called for an abort
            if (Form1.abort)
            {
                //imeditly return to stop the upload process
                return false;
            }
            //get logged in credential to upload file to google cloud storage bucket
            GoogleCredential credential = await GoogleCredential.GetApplicationDefaultAsync();
            //create a storage client using the credential
            StorageClient storageClient = StorageClient.Create(credential);
            //create stream to read the file to be uploaded
            FileStream fileStream = new FileStream(
            localPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
            //create a progress stream to track the progress of the file upload 
            Google.Apis.Storage.v1.Data.Object uploadObject = new Google.Apis.Storage.v1.Data.Object
            {
                Bucket = bucketName,
                Name = objectName
            };
            //create a resumable upload session
            ObjectsResource.InsertMediaUpload uploader =
            storageClient.Service.Objects.Insert(
                uploadObject,
                bucketName,
                fileStream,
                "application/octet-stream");

            //about 2 megabyte chunks 
            int chunkSize = ResumableUpload.MinimumChunkSize * 8;
            //set the chunk size for the upload to allow for
            //progress tracking and aborting
            uploader.ChunkSize = chunkSize;
            //store the file size for calculating upload progress
            long fileSize = fileStream.Length;
            //store if it was successfull
            bool success = false;
            //track the progress of the uplaod and report it back
            //to the progress interface
            uploader.ProgressChanged += (IUploadProgress progress) =>
            {
                //if the main form called for an abort,
                //imeditly return to stop the upload process`
                if (Form1.abort)
                {
                    //end the upload immeditely
                    return;
                }
                //if it is uploading
                if (progress.Status == UploadStatus.Uploading)
                {
                    //compute the percentage of the file that has
                    //been uploaded and report it back to the
                    //progress interface
                    double pct = (double)progress.BytesSent / (double)fileSize;
                    //report the progress back to the progress
                    //interface
                    progress_interface.Report(pct);
                }
                //else if the upload is complete, then save the
                //success status to true and print the success to
                //the console
                else if (progress.Status == UploadStatus.Completed)
                {
                    //print success
                    Console.WriteLine("Upload complete!");
                    //set the success status to true
                    success = true;
                }
                //else if it failed then do nothing and print
                //the failure to the console
                else if (progress.Status == UploadStatus.Failed)
                {
                    //prin the failure
                    Console.WriteLine("Upload failed: " + progress.Exception);
                }
            };
            //if the main form called for an abort, imeditly return
            //to stop the upload process
            if (Form1.abort)
            {
                //end the upload immeditely
                return false;
            }
            //wait for the upload to complete
            await uploader.UploadAsync();
            //dispos the file stream to free up resources
            fileStream.Dispose();
            //return the success status of the upload
            return success;
        }
    }
}

