"""
This File is work horse ingestion of a single file in the bucket, it ingests a single CSV file on the
bucket into the apache spark database
with the help calling other classes and files it does the following. It first connects to the google bucket and 
connects to the desired file and as it reads the file stream from the bucket it cleans the CSV lines in the file
to make it CSV spark friendly as AIS does not follow standard CSV format causing Spark to crash reading raw
AIS CSVS.  After the sanitized CSV is saved locally it is copied to a HDFS temp directory, after the copy. 
is finished it deletes the temp file on the local drive.  it then calls spark_ingest.py which runs a function 
on Yarn for apache spark to ingest a file as parquet files partititioned by date for easier maitinence, if it 
fails it will try again once, if it succeeds it deletes the temp file off of HDFS temp, while it is running a
multistage progress bar updates to keep the user informed

Project members: Anthony Hoffert, Able Daniel
""" 

import sys
import glob
import os
import re
import io
import csv
import shutil
import subprocess
import spark_ingest
import time
import unicodedata
from google.cloud import storage
from logger import logger
from datetime import datetime
from hdfs import InsecureClient
from predictive_timer import predictive_timer
from multi_progress_bar import multi_progress_bar

from pyspark.sql.types import (
     StructType,
     StructField,
     StringType,
     IntegerType,
     FloatType,
     LongType,
     TimestampType,
)
#used for psuedo timing how long ingestion takes there is no acutal progress feedback to the progress bar is 
#an estimated timer based on bytes per second throughput
total_bytes=0
total_time=0

"""
This is the main entry point for the ingest CSV function from the bucket to HDFS parquets
It first starts a stream from the bucket and cleans the AIS CSV so that it is spark friendly during 
download, then it uploads the cleaned CSV to HDFS and removes it from the local to save space
Then spark ingest python is called which runs a script in Yarn to read the CSV file from HDFS and write
it into HDFS parquets as chunks split up by day so that it makes clean up easier

filename:      the file name on the google bucket to be ingested
bytes:         the number of bytes in the file
bucket_name:   the name of the google bucket which has the file
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
returns:       True if the file was ingested successfully otherwise false
"""
def ingest(filename,bytes,bucket_name,hdfs_client,ais_schema,cruise_schema,file_index,total_files):
         #if it is AIS data otherwise cruiseship data
         if filename.startswith("AIS"):
            #set the schema to AIS for faster loading
            schema = ais_schema
            #set hdfs path for AIS
            hdfs_path = "hdfs:///data/ais/" 
         #else it is not AIS data then it is cruise ship data
         else:
            #set the schema to the cruise ship schema for faster loading but not as important since this is a s>
            schema = cruise_schema
            #set hdfs path for the cruise stats file
            hdfs_path = "hdfs:///data/cruise/" 
         #log that the santiziation fot he file has started
         logger.log_item(f"Started santization of file {filename}")
         #read the file from the bucket and clean it and save it to HDFS temporarily
         success = ingest_csv(bucket_name,filename,bytes,hdfs_client,schema,hdfs_path,file_index,total_files)
         #if it downloaded and sanitized successfully
         if success:
            #log that it has finished ingesting this file
            logger.log_item(f"Finished ingesting {filename}")
            #clear the progress bar and replace it with the text ingested {filename} successfully
            multi_progress_bar.finished_success(filename)
            #save the file name to a text file so that on next run it won't be repeated
            with open("/home/hadoopuser/ingested_files.txt", "a") as f:
                     f.write(filename + "\n")
            #retrun success
            return True
         #else it failed to ingest the file
         else:
            #clear the progress bar and replace it with the text failed to ingest {file_name}
            multi_progress_bar.finished_fail(filename)
            #return failure
            return False

"""
Deletes a folder from HDFS using `hdfs dfs -rm -r`.
this is used in the event that an ingestion failed it is used to delete all the days folder thus removing
its parquet files and to prevent double loading
Returns True if deleted successfully, False otherwise.
"""
def hdfs_delete_folder(foldename):
    result = subprocess.run(
        #command for HDFS to remove the folder given
        ["hdfs", "dfs", "-rm", "-r", foldername],
        #don't print anything to console by redirecting standard out, standared error to null
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        text=True
    )
    #if it finished successfully
    if result.returncode == 0:
        #log that it successfully deleted the folder
        logger.log_item(f"Deleted HDFS folder: {foldername}")
        #return it deleted the folder successfully
        return True
    #if it did not finishe successfully
    else:
        #log that it failed to delete the folder
        logger.log_item(f"Failed to delete {foldername}")
        #log the error
        logger.log_item("HDFS error:", result.stderr.strip())
        #return that it failed to delete the folder
        return False

"""
this deletes a file from HDFS it is used for clean up to delete the sanitized CSV once it is in spark in HDFS
filename: the path of the file to be deleted
returns:  true if the file was deleted otherwise false
"""
def hdfs_delete_file(filename):
    result = subprocess.run(
        #command for HDFS to remove the given file
        ["hdfs", "dfs", "-rm", filename],
        #don't print anything to console by redirecting standard out, standared error to null
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        text=True
    )
    #if it deleted the file successfully
    if result.returncode == 0:
        #log that the file was deleted
        logger.log_item(f"Deleted HDFS file: {filename}")
        #return successfull delete
        return True
    #else the file was not successfully deleted
    else:
        #log that it failed to delete the file
        logger.log_item(f"Failed to delete {filename}")
        #log the error
        logger.log_item("HDFS error:", result.stderr.strip())
        #return failed to delete
        return False

"""
this function computes the destination parquet folder for a Given file and returns the filename date if it
is a AIS file name with a date
filename:  name of the file
returns:   path on HDFS for the parquet, date from the AIS file name
"""
def get_hdfs_folder(filename):
    #if it is an AIS file, which has the date in the file name
    if filename.startswith("AIS"):
       hdfs_path = "hdfs:///data/ais/"
       #get the date from the file using regular expression
       match = re.match(r"AIS_(\d{4})_(\d{2})_(\d{2})", filename)
       #if the date could not be foudn in the file name then this is not an AIS file
       if not match:
          #rais an exception as this is not a vaild AIS file name
          raise ValueError(f"Could not extract date from filename: {filename}")
       #get the year, month, day parts of the file name form the regular expression
       year, month, day = match.groups()
       #get the date value as a string
       date_value = f"{year}-{month}-{day}"
       #generate the path it will be stored on HDFS if it exists
       partition_path = f"{hdfs_path}/date={date_value}"
       #return the HDFS parquet folder and the date valuw
       return partition_path,date_value
    #if it is the cruise stats file
    else:
       hdfs_path = "hdfs:///data/cruise/" 
       #generate the path it will be stored on HDFS if it exists
       partition_path = f"{hdfs_path}/{filename}"
       return partition_path,None

"""
this function makes a given string safe for spark to ingest
s:       string to be sanitied
returns: string that is safe for spark to ingest
"""
def spark_safe_string(s: str) -> str:
    #if no string is given then return an empty string
    if s is None:
        return ""
    #normalize unicode to fix multibyte sequences
    s = unicodedata.normalize("NFKC", s)
    #stores characters that have 0 width
    ZERO_WIDTH = [
        '\u200b', '\u200c', '\u200d', '\u200e', '\u200f',
        '\ufeff'
    ]
    #for each zero width character
    for zw in ZERO_WIDTH:
        #if it is in the string remove all occurences of it
        s = s.replace(zw, '')
    #make sure the character is printable and not a tab or new line
    s = ''.join(ch for ch in s if ch.isprintable() or ch in ('\t', '\n'))
    #remove any null bytes
    s = s.replace('\x00', '')
    #strip leading and trailing white space
    s = s.strip()
    #return the cleaned string
    return s

"""
this function converts the given value to the given datatype so that the CSV will match the spark Schema
value:    value read from CSV
datatype: datatype expected
returns:  the value coverted to the appropiate data type and is made spark safe
"""
def convert_data_type(value,datatype):
      #based on datatype
      match(datatype.dataType):
         case LongType():
                #try to convert it to a long type which is an int in python3
                try:
                   #in python3 an int can store a long
                   return int(value)
                #if it cannot be converted
                except:
                   #return a 0 which is a long type
                   return 0
         case IntegerType():
                #try to convert it to an interger
                try:
                   #conert it to an int
                   return int(value)
               #if it cannot be converted
                except:
                   #return 0 which is an integer
                   return 0
         case FloatType():
                #try to convert it to a float type
                try:
                   #convert it to a float
                   return float(value)
                #if it cannot be converted
                except:
                   #return a floating point 0
                   return float(0)
         case TimestampType():
                # Convert ISO-8601 string to Python datetime
                return datetime.fromisoformat(value) 
         case StringType():
                #make the string spark safe
                value = spark_safe_string(value)
                #return the string
                return value
         case _:
                #make the string spark safe
                value = spark_safe_string(value)
                #return the string
                return value

"""
this function converts a row to match the given schema format

row:     row from the CSV file
schema:  expected schema
reutrns: row converted to match the schema format
"""
def convert_row(row,schema):
    #create a list to store the output columns in the proper datatype
    output=[]
    #for each column in the row
    for i , column in enumerate(row):
         #convert this column in the row to its expected datatype
         output.append(convert_data_type(row[i],schema[i]))
    #return the modified row
    return output

"""
this function downlaods a file from a google bucket and while downloading the file it converts it to match
a given spark schema, it downlaods it locally and then copies it to HDFS and then deletes it locally after
the copy, it also displays progress so that the user does not think it is stuck

bucket_name: name of the bucket where the file is stored
filename:    name of the file to be downloded
hdfs_client: client used to interface with HDFS for the upload
hdfs_path:   path to move it to in HDFS
schema:      schema the CSV file must match for spark ingestion
file_index:  current file index for progress reasons
total_files: total number of files for progress reasons
returns:     True if the file was successfully download, sanitized, and moved to HDFS, otherwise false
"""
def sanitize_csv(bucket_name,filename,hdfs_client,hdfs_path,schema,file_index,total_files):

         #set the label to the current task
         multi_progress_bar.task=f"sanitizing file: {filename} from bucket {bucket_name} onto local drive for loading to HDFS"
         #create connection to connect to the storeage
         client = storage.Client()
         #connect to the bucekt
         bucket = client.bucket(bucket_name)
         #get a connection to the file
         blob = bucket.blob(filename)
         #verifies that it got a blob or the file
         if blob is None:
            raise ValueError(f"Could not get size for {repr(bucket_name)}/{repr(filename)}")
         #count the total lines in the file, not as accurate than using csv since records can span
         #multiple rows but this is faster
         with blob.open("r") as f:
              total_lines = sum(1 for _ in f)
         #stores how many rows have valid dates for this file which should match in the parquet written in spark
         expectd_row_count = 0
         #get local storage directory for cleaned file
         local_dir = os.path.expanduser("~/temp_ingestion")
         os.makedirs(local_dir, exist_ok=True)
         #generate path to save the cleaned file to
         local_path = os.path.join(local_dir,filename)
         #stores the rows read as a float for progress reasons
         rows_read = 0.0
         #calculate 1% to update only on percentage changes to avoid flickering
         one_percent = int(total_lines*.01)
         #open the file in memory
         with blob.open("r") as f:
              #read the file and parse it as a csv file
              reader = csv.reader(f)
              #read the header but we will ignore it since it is always the same for AIS
              header = next(reader)
              #create an output file path for the cleaned file
              with open(local_path, "w", newline="", encoding="utf-8") as f:
                    #create the writer to write the CSV file
                    writer = csv.writer(f, quoting=csv.QUOTE_MINIMAL)
                    #write the header as the first row in the file
                    writer.writerow(header)
                    #for each data row in the file
                    for row in reader:
                      #increase the number of rows processed for progress
                      rows_read += 1
                      #try to write the row to the file
                      try:
                          #if the column has a valid date time stamp this is used to make sure the number of 
                          #roes match after it is ingested in spark
                          try:
                              datetime.strptime(ts, "%Y-%m-%d %H:%M:%S")
                              #increment the expected row count
                              expectd_row_count += 1
                          #if the row does not hava a valid datetime stamp then spark will put it in default
                          #so ignore it
                          except:
                              #if it will return null then don't coutn it
                              pass
                          #convert the row to match the schema 
                          converted_row = convert_row(row,schema)
                          #write the row to the sane file
                          writer.writerow(converted_row)
                      #if the row failed to write
                      except Exception as e:
                          #log the failure with its exception
                          logger.log_item(f"failed to convert row number: {rows_read} line: {row} due to exception {e}")

                      #make it sleep to allow the CPU to do stuff so it does not shutdown SSH
                      if rows_read % 5000 == 0:
                          time.sleep(0.001)

                      #if there has been suficient progress update progress
                      if rows_read%one_percent==0:
                          #send the progress to the progress bar
                          multi_progress_bar.enqueue_progress(rows_read / total_lines,
                                                              1,
                                                              file_index,
                                                              total_files)
                      #if there are more rows than lines, this is impossible to happen but a nice check
                      if(rows_read>total_lines):
                           #print the message
                           print(f"more rows than expected hit {rows_read} expected {total_lines}lines for {filename}")
                           #log the error
                           logger.log_item(f"more rows than expectd hit {rows_read} expected {total_lines} lines for {filename}")
                           #return failure
                           return False
         #it is done downlaoding the file so show 100%
         multi_progress_bar.enqueue_progress(1,
                                             1,
                                             file_index,
                                             total_files)
         #change the display to say that it is moving the file to HDFS as thsi may take a while
         multi_progress_bar.task="moving the file to HDFS this may take a few minutes"
         #have the progress bar display the new line but remain at 100%
         multi_progress_bar.enqueue_progress(1,
                                             1,
                                             file_index,
                                             total_files)
         #log that it is copying the file to HDFS
         logger.log_item(f"copying {local_path} to {hdfs_path}")
         #try to copy the file to HDFS
         try:
             #copy to HDFS
             hdfs_client.upload(hdfs_path, local_path, overwrite=True)
             #once copy is complete log that it is deleteing the file
             logger.log_item(f"removing {local_path} from system to conserve space")
             #delete the file from HDFS to conserve space
             os.remove(local_path)
             #return success and the number of expected rows in the file for verification purposes
             return True, expectd_row_count
         #if the copy failed
         except Exception as e:
             #log the error and the reason
             logger.log_item(f"failed to copy {local_path} to {hdfs_path} exception: {str(e)}")
             #return failure
             return False, expected_row_count

"""
this function download&sanitizes the CSV into HDFS, reads it from HDFS and writes it as spark parquets in
a sharded database partitioned by date into HDFS through Yarn and spark

bucket_name: bucket the file lives on
file_name:   the name of the file
bytes:       the size of the file in bytes used to estimate ingestion time
hdfs_client: insecure client used to upload to HDFS
schema:      the schema for this file to be used by spark
hdfs_path:   the root directory to store it in spark
file_index:  the current file index used for progress purposes
total_files: the total number of files used for progress purposes
retries:     how many times should it retry to ingest the file the default is retry just once

returns:     true if the file was ingested successfully, false otherwise
"""
def ingest_csv(bucket_name,file_name,bytes,hdfs_client,schema,hdfs_path,file_index,total_files,retries=1):
    #total time and total bytes used to compute through put by the predicitive timer to update progress
    #during spark ingest when no progress is availible so that the user does not think the program is dead
    global total_time,total_bytes
    #update the stage log
    logger.log_item("sanitizing file")
    #compute the HDFS temporary path to save the file to for spark ingestion
    hdfs_target = f"/data/temporary_files/{file_name}"
    #make the file safe for Apache spark to ingest, download it from the bucket and save it to HDFS and 
    #clean it in the stream
    success,expected_date_rows = sanitize_csv(bucket_name,file_name,hdfs_client,hdfs_target,schema,file_index,total_files)
    #if it failed to download and steralize the file and save it to HDFS
    if not success:
       #log the failure
       logger.log_item(f"failed to download, sanitize, and upload to HDFS the following file: {file_name}")
       #clear the progress bar and change the text to say that it failed
       multi_progress_bar.halt_progress_for_file(False,file_name)
       #return failure
       return False
    #compute expected final folder in spark and the date for the file, the date value is used for to get the 
    #written row coutn in spark for row verification
    expected_final_folder,date_value = get_hdfs_folder(file_name)
    #log that ingestion is starting
    logger.log_item(f"started ingestion of {file_name}")
    #clear the ingested flag
    ingested=False
    #set the label to the current task
    multi_progress_bar.task=f"Ingesting {file_name} into Spark this may take longer than expected"
    #while it can try
    while (retries+1) > 0:
          #decrement the number of retries with each loop
          retries -= 1
          #if it is an AIS file which is massive and takes longer to ingest
          if file_name.startswith("AIS"):
             #default throughput is based on how long the first file took 3 minutes and 12 seconds for a 0.7 gig
             #file create a predictive timer for use during the ingest CSV stage
             progress_timer = predictive_timer(bytes,4213812.0,total_bytes,total_time,file_index,total_files)
          #set ingest start time
          ingest_call_start = time.time()
          #ingest the file from hdfs into apache spark by having apache spark read it and write it into parquets
          #using Yarn for faster processing
          results = spark_ingest.run_spark_ingest(schema, hdfs_target, hdfs_path,date_value)
          #log that the spark ingestion is complete
          logger.log_item(f"spark ingestion for {file_name} complete.")
          #if it is an AIS file which is massive and takes longer to ingest
          if file_name.startswith("AIS"):
             #stop ingest timer 
             progress_timer.stop_timer()
          #compute ingest elaped time used to update throughput to make it more accurate
          ingest_call_elapsed = time.time()-ingest_call_start
          #update time and byte totals for better predictions next time
          total_time = total_time+ingest_call_elapsed
          total_bytes = total_bytes+bytes
          #if no results were given it failed
          if results is None:
             #delete the parquet folder from HDFS to remove an incompelte load
             hdfs_delete_folder(expected_final_folder)
             #update the task name on the progress bar to indicate a retry event
             multi_progress_bar.task=f"Retrying Ingestion of {file_name} into Spark this may take longer than expected"
             #go to the next iteration to try again
             continue
          #if it did not succeed it failed
          if not results["success"]:
             #log the failure
             logger.log_item(f'failed to ingest {file_name} error {results["stderr"]}')
             #if it happens to be an empty then it wasn't supposed to write so it is OK
             if results["empty"]:
                #log that it is a duplicate file
                logger.log_item(f'{file_name} has no valid rows')
                #delete the sanitized file from HDFS to save space
                hdfs_delete_file(hdfs_target)
                #return success
                return True
             #else the failure is not an empty file
             else:
                #set the task to say that it is retrying
                multi_progress_bar.task=f"Retrying Ingestion of {file_name} into Spark this may take longer than expected"
                #log the failure and the reason for failure
                logger.log_item(f'{file_name} failed to ingest stdout: {results["stdout"]} error: {results["stderr"]}')
                #go to the next iteration to try again
                continue
          #this gets the rows written into spark parquet files it is defaulted to -1 incase it is not 
          #availible
          ingested_row_count=-1
          #update the log with the times
          if results["read_start"]:
             readable_read_start  = datetime.fromtimestamp(results["read_start"]).strftime("%Y-%m-%d %H:%M:%S")
             logger.log_item(f"Spark CSV read for {file_name} completed at {readable_read_start}")
          if results["write_start"]:
             readable_write_start = datetime.fromtimestamp(results["write_start"]).strftime("%Y-%m-%d %H:%M:%S")
             logger.log_item(f"Spark CSV read finished and Spark ingestion started at {readable_write_start}")
          if results["write_done"]:
             readable_write_done = datetime.fromtimestamp(results["write_done"]).strftime("%Y-%m-%d %H:%M:%S")
             logger.log_item(f"Spark ingestion completed at {readable_write_done}")
          #read the number of rows ingested if they are procided
          if results["ingested_written_row_count"]:
             ingested_row_count = results["ingested_written_row_count"]
          #if they don't match simply log that they don't match
          if ingested_row_count != expected_final_folder and ingested_row_count != -1:
             logger.log_item(f"failed to fully ingest {file_name} ingested row count {ingested_row_count} expected row count {expected_final_folder}")
             #delete the file off of the HDFS temp directory to save space on HDFS
             hdfs_delete_file(hdfs_target)
             #return success
             return True

          else:
             #delete the file off of the HDFS temp directory to save space on HDFS
             hdfs_delete_file(hdfs_target)
             #return success
             return True
    #if it gets here and has not returned true then it means it keeps failing and is out of retries so return
    #failure
    return False
