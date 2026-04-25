"""
This File is the main entrypoint for ingestion of the entire bucket, it ingests all of the CSV files on the
bucket into the apache spark database
with the help calling other classes and files it does the following.  First this script gets a list of all
the files on the bucket and their sizes.  It reads a file called ingested_files.txt on the local drive to
figure out what has already beem ingested and skips those, then it checks hdfs to see if the day in the file
name already exists as a directory and if so it skips that assuming it has already been loaded, a partial 
load will read as already loaded and it will skip those files which can be an issue, after this check is done
it begins ingestion by calling a function in ingest_file.py which first connects to the google bucket and 
connects to the desired file and as it reads the file stream from the bucket it cleans the CSV lines in the file
to make it CSV spark friendly as AIS does not follow standard CSV format causing Spark to crash reading raw
AIS CSVS.  After the sanitized CSV is saved locally it is copied to a HDFS temp directory, after the copy. 
is finished it deletes the temp file on the local drive.  it then calls spark_ingest.py which runs a function 
on Yarn for apache spark to ingest a file as parquet files partititioned by date for easier maitinence, if it 
fails it will try again once, if it succeeds it deletes the temp file off of HDFS temp, while it is running a
multistage progress bar updates to keep the user informed

Project members: Anthony Hoffert, Able Daniel
""" 

import traceback
import warnings
import time
import spark_ingest
import threading
import subprocess
import re
import os
import sys, glob
from logger import logger
from datetime import datetime
from ingest_file import ingest
from multi_progress_bar import multi_progress_bar
from google.cloud import storage
from hdfs import InsecureClient

#bucket where the files are stored
bucket_name='2019-noaa-ais-data'

"""
this function gets all the file names and file sizes from a given bucket

bucket_name: name of the bucket to get the filenames and sizes off of
returns: a list of tuples of the file names and their sizes
"""
def list_bucket_files(bucket_name):
    #client to talk to bhe bucket
    client = storage.Client()
    #connection to the bucket
    bucket = client.bucket(bucket_name)
    #stores the file size tuples
    files = []
    #for each file on the bucket
    for blob in bucket.list_blobs():
        #add the file name and the file size in bytes to the bucket (the file size is used to estimate
        #spark/yarn ingestion time
        files.append((blob.name, blob.size))
    return files

"""
this is a helper function used to convert a time span into a human readable time
elapsed: the elapsed time to convert
returns: the elapsed time as a string (hours, minutes, and seconds) only the parts that exist
"""
def convert_elapsed(elapsed):
    #convert to hours
    hours = int(elapsed // 3600)
    #comvet to minutes
    minutes = int((elapsed % 3600) // 60)
    #convert to seconds
    seconds = int(elapsed % 60)
    #compute the text parts for each part of the timespan
    parts = []
    #if there are hours
    if hours > 0:
       #if there is only one hour use hour instead of hours
       if hours==1:
          #use hour and not hours
          parts.append(f"{hours} hour")
       #else there is more than one hour then use hours
       else:
          parts.append(f"{hours} hours")
    #if there are any minutes
    if minutes > 0:
       #if there is only one minute
       if minutes==1:
          #use minute and not minutes
          parts.append(f"{minutes} minute")
       #else there is more than one minute
       else:
          parts.append(f"{minutes} minutes")
    #if there are seconds
    if seconds > 0 or not parts:
       #if there is only one second
       if seconds==1:
          #use second instead of seconds
          parts.append(f"{seconds} second")
       else:
          parts.append(f"{seconds} seconds")
    #if there is more than one part
    if len(parts) > 1:
       #put the parts together seperated by a space and put an and before the last part
       human_time = " ".join(parts[:-1]) + " and " + parts[-1]
    #else there is only one part
    else:
       human_time = parts[0]
    #return the computed elapsed time
    return human_time

"""
this checkes to see if a path exists in HDFS it is used to see if a day is already loaded, we split the days
CSVs into day shards in different folders making it easier to check to see if a day exists or delete a day
without needing a spark query.  this function checks to see if a day is already loaded by seeeing if the 
folder for that day already exists.  If a partial day is loaded this will say yes the day is loaded but
not verify that the day is fully/properly loaded
path: folder path or file path to check in hdfs
returns: True if the folder is in HDFS otherwise false 
"""
def check_hdfs_path(path):
    #run the subprocess to check HDFS to see if the file is in HDFS
    result = subprocess.run(
        #command to test HDFS
        ["hdfs", "dfs", "-test", "-e", path],
        #hide any print statements so that it does not mess with the GUI
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL
    )
    #if the check was successfull aka return code is 0 then the folder exists and return true
    return result.returncode == 0

"""
this checks to see if a day is already loaded by taking the file name extracting the data and computing the
HDFS folder path it would have created and checks to see if that file exists
filename: name of the file
returns: true if the folder for the file is in the system othewise false
"""
def check_if_file_already_loaded(filename):
    #if this is an AIS file then use the AIS HDFS directory
    if filename.startswith("AIS"):
       hdfs_path = "hdfs:///data/ais/"
       #get the date from the file using regular expression
       match = re.match(r"AIS_(\d{4})_(\d{2})_(\d{2})", filename)
       #if the date is not in the AIS file name which is not possible if downloaded from NOAA
       if not match:
          #trhow an except as this is not an AIS file!
          raise ValueError(f"Could not extract date from filename: {filename}")
       #get the year month and day from the regular expression
       year, month, day = match.groups()
       #get the date value from the regular expersion
       date_value = f"{year}-{month}-{day}"
       #generate the path it will be stored on HDFS if it exists
       partition_path = f"{hdfs_path}/date={date_value}"
       #check if the path is already in HDFS
       if check_hdfs_path(partition_path):
          #if it is then return true
          return True
       #else the path is not already in HDFS
       else:
          #if not in HDFS return false
          return False
    #else it is not an AIS file then use the cruise HDFS directory
    else:
       hdfs_path = "hdfs:///data/cruise/"
       #get the path the Cruise CSV will be saved to 
       partition_path = f"{hdfs_path}/{filename}"
       #check if the path is already in HDFS
       if check_hdfs_path(partition_path):
          #if it is then return true
          return True
       #else the path is not already in HDFS
       else:
          #if not in HDFS return false
          return False

"""
this function ingests a single file into hdfs as parquet files in apache spark
it works by calling a function which downlaods the raw AIS CSV from the bucket and sanitizing it as it 
downloads the file from the bucket, NOAA CSV do not follow standard CSV rules and if fed direcectly to
spark, then spark won't read it so my sanitize funcgtion reads the CSV file in python from the byte stream
from the bucket, looks at the schema for the AIS file and converts each column datatype to match the schema
it cleans up all the strings to make sure that they do not contain any hidden characters that Spark wont like
and then uses python to write it as a clean CSV file on to the namenode local drive, then it copies the file
to HDFS, and then another function is called which runs a function in Yarn in Apache spark to ingest the file
into HDFS and write it into parquets files, partitioned by date so that each day is in a seperate folder 
making clean up a lot easier if needed, some rows do not have date and those are stored in a default 
directory in HDFS, they will not be used later by our dashboard

file:          the file name to be ingested
bytes:         the number of bytes in the file
hdfs_client:   an InsecureClient used to interface with HDFS, specifically after the CSV is sanitized this is
               used to uplaod the sanitized CSV into HDFS for ingestion
ais_schema:    this is the schema used to ingest AIS files, the schema is provided to speed up ingestion 
               otherwise spark needs to read the file twice, once to figure out the schema and again to get the
               data based on that schema
cruise_schema: this is the schema used to ingest the cruise ship stats file, the schema is provided to speed
               up ingestion otherwise spark needs to read the file twice once to figure out its shcema and 
               again based on that schema
file_index:    the current file index this is used for overall progress reasons
total_files:   how many total files are there this is used for overall progress reasons
returns:       True if a file was ingested successfully, false otherwise
"""
def ingest_filedata(file,bytes,hdfs_client,ais_schema,cruise_schema,file_index,total_files):
    #get the bucket name and curent file index global variables
    global bucket_name
    #get the start time of this ingestion function for the overall time per file
    start = time.time()
    #print that it is reading the file, the progress bar will overwrite this as soon as it gets going
    print(f"Starting ingestion of {file} at {bytes} bytes")
    #log the start
    logger.log_item(f"Starting ingestion of {file} at {bytes} bytes")
    #this downloads the file and then ingests it and clean up after itself, plus maintane progress
    success=ingest(file,bytes,bucket_name,hdfs_client,ais_schema,cruise_schema,file_index,total_files)
    #if it downloaded and was ingested successfully
    if success:
       #compute the elapsed time it took for the whole file
       elapsed = time.time()-start
       #log how long it took and the results
       logger.log_item(f"successfully ingested file {file} in {convert_elapsed(elapsed)}")
       #add the file name to the saved list of files ingested so that it will be skipped next time
       with open("ingested_files.txt", "a") as f:
           f.write(file + "\n")
       #return success
       return True
    #if the file was not ingested successfully
    else:
      #return failure
      return False

"""
this is the main function that ingests all of the files in the bucket
"""
def main():
    #hide all messages that have an ingore status from google
    warnings.filterwarnings(
    "ignore",
    category=FutureWarning,
    module="google.api_core"
    )
    #create a log file for this run
    log_filename = datetime.now().strftime("/home/hadoopuser/logs/%m-%d-%Y_%H_%M_%S.log")
    #create logger
    logger.set_output_file_path(log_filename)
    #log that it is starting
    logger.log_item("starting")
    #try to opne the file of files already ingested in spark
    try:
        #get a list of all the files already ingested into spark, this is used in the event this script
        # is  stopped and restarted to avoid double ingestion and to alow safe termination
        already_done = set(open("/home/hadoopuser/ingested_files.txt").read().splitlines())
    #if no file is found
    except FileNotFoundError:
        #create an empty ingestion list
        already_done = set()
    #create a spark interface to ingest files into and read the schemas for faster spark ingestion
    ais_schema,cruise_schema=spark_ingest.create_spark()
    #make an HDFS client to upload the clean csv file into HDFS for spark ingestion
    hdfs_client = InsecureClient('http://namenode:9870', user='hadoopuser')
    #get the file metadata for each file in the bucket
    files = list_bucket_files(bucket_name)
    #get the number of files for progress purposes
    total_files=len(files)
    #start up the progress bar to update the progress when needed
    multi_progress_bar.start_progress_monitoring()
    #for each file in the bucket get the name and size
    for i,(file, size) in enumerate(files):
        #if the file has not been already ingested
        if file not in already_done:
           #check to see if already in the database
           if not check_if_file_already_loaded(file):
              #downlaod, sanitize and ingest the file to apache spark hdfs
              success = ingest_filedata(file,size,hdfs_client,ais_schema,cruise_schema,i,total_files)
              #if it succeeded with ingstion into spark
              if success:
                  #log the file was ingrested
                  logger.log_item(f"{file} has been ingested")
              #else it failed to ingest
              else:
                  #end the loop since it failed
                  break
           #else the file is already in the database
           else:
             logger.log_item(f"skipping file {file} it is already in the database")
        #else if the file was already ingested
        else:
           #log the file is being skipped
           logger.log_item(f"skipping file {file} it is alredy ingested")
           #print that it was already ingested and it is skipping it
           print(f"skipping file {file} it is alredy ingested")

    #put the terminal on the last line after the progress bar
    print()
    #stop the progress bar updating thread
    multi_progress_bar.stop_monitoring_progress()

"""
This is the main entry point of this script
it runs main and if it crashes it saves it to a text file becasue a screen has a limited history view so 
this way it can be seen in full later
"""
if __name__ == "__main__":
    #try to run the main program
    try:
        #ingest all the files in the bucket into spark
        main()
    #if an exception happens then save it to a file
    except Exception as e:
        #print an error happened
        print("FATAL ERROR in cleaner:")
        #get the date/time
        timestamp = datetime.now().isoformat()
        #create a log file and append the message into the log file for later review
        with open("/home/hadoopuser/ingestion_errors.log", "a") as log:
             log.write("\n" + "="*80 + "\n")
             log.write(f"Timestamp: {timestamp}\n")
             log.write("Exception:\n")
             traceback.print_exc(file=log)
             log.write("="*80 + "\n")
        #print the exception to the screen and raise a exception to stop the script with an exception
        traceback.print_exc()
        raise


