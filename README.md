# CPSC 531 Project
My code for the class
Anthony Hoffert
-Common Core: from my old job, not really related to this project but used by my helper programs, this project contains basic helper functions
-Progress: from my old job, not really related to this project but used by my helper programs, this project contains a very nice progress bar that can handle multiple progress bars at the same time
-Logger: from my old job, not really related to this project but used by my helper programs, this project contains a nice GIU logger, for storing, showing and displaying Logs in a nice form
-Data_Uploader_To_Spark_On_Cloud: this program just uploads a folder with CSV files to a google cloud bucket, it was used to upload all the AIS CSV files and the cruise file to google cloud
-MMSI cruise ship mapper: this was a helper program that maps the MMSI numbers to cruise ships for the cruise ship CSV, I still had to do some manual mapping since AIS ship names do not match expected cruise ship names, and AI had to help with some
-NOAA_Downloader: this program downloaded AIS CSV zip files from the NOAA government website, it takes a few days of continous running to download a full year.
-python: this folder contains various python files that were used for ingestion into Spark from the google cloud bucket, ingestion.py is launched and it downloads all the file from google cloud bucket, it cleans the CSV files mid memory stream and writes it to local, then moves it to HDFS then calls a python file called spark_ingest.py to ingest the files from HDFS temp into parquet files into HDFS /data/ais_raw partitioned by day, momentum_table_creator is a python file that runs on yarn and calculates the momentum values and writes them as parquet files in seveal folders raw ais data for cruise ships are written to 
