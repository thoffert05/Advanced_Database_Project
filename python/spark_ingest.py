"""
This File is responsible or ingesting files into spark parquet files partitioned by date, because if a date
is bad it makes it easy to delete the date and reload it each AIS CSV is a single day.  it works by loading
the csv via pregiven schema so ti only has to read the file once instead of twice (default is twice once to
determine schema and a second time to read the data).  it will call the ingest function on Yarn which runs on
the datanode which has more HDD, 

Project members: Anthony Hoffert, Able Daniel
""" 

import sys
import glob
import os
import asyncio
import csv
import shutil
import subprocess
import time
import threading
import json
from hdfs import InsecureClient
from datetime import datetime
from pyspark.sql.functions import to_date
from pyspark.sql import SparkSession
#needed to run spark
from pyspark.sql.types import (
    StructType,
    StructField,
    StringType,
    IntegerType,
    FloatType,
    LongType,
    TimestampType,
    BooleanType
)

"""
this function creates spark and the schemas for the CSVs the schema's dramatically increase load speed
normally spark's read CSV reads the file twice once to figure out the schema and then again to read it
as that schema, by providing the schema before hand it only needs to read it once (against the provided
schema)

returns: ais schema and cruise ship stats schema
"""
def create_spark():
        #generate the spark schema for the AIS CSV file so that spark does not have to read
        #the file twice to generate the proper schema, given an AIS schema has over 7 million rows
        #so a single load takes a while and a double load will take twice as long
        ais_schema = StructType([
                                 StructField("MMSI", LongType(), True),
                                 StructField("BaseDateTime", TimestampType(), True),
                                 StructField("LAT", FloatType(), True),
                                 StructField("LON", FloatType(), True),
                                 StructField("SOG", FloatType(), True),
                                 StructField("COG", FloatType(), True),
                                 StructField("Heading", FloatType(), True),
                                 StructField("VesselName", StringType(), True),
                                 StructField("IMO", StringType(), True),
                                 StructField("CallSign", StringType(), True),
                                 StructField("VesselType", IntegerType(), True),
                                 StructField("Status", IntegerType(), True),
                                 StructField("Length", FloatType(), True),
                                 StructField("Width", FloatType(), True),
                                 StructField("Draft", FloatType(), True),
                                 StructField("Cargo", IntegerType(), True),
                                 StructField("TransceiverClass", StringType(), True)
                               ])
        #create cruise ship schema so that spark does not have to read the file twice though for this file
        #it is not a big deal as it has less than 100 rows
        cruise_schema = StructType([
                                   StructField("ShipName", StringType(),True),
                                   StructField("CruiseLine", StringType(),True),
                                   StructField("MMSI", LongType(),True),
                                   StructField("DWT", IntegerType(),True),
                                   StructField("YearBuilt", IntegerType(),True),
                                   StructField("GT", IntegerType(),True),
                                   StructField("PassengerCapacity", IntegerType(),True),
                                   StructField("CrewCount", IntegerType(),True),
                                   StructField("IMO", LongType(),True)
                                 ])
        #return spark envorinment
        return ais_schema,cruise_schema

"""
this ingests a file from hdfs into spark parquet files which for AIS are partitioned into shards
seperated by day to make it easier to remove corrupted days, this particular function runs in Yarn
to take advantage of parallelization, and to run it on the datanode which has a lot of free space
as opposed to the namenode which has almost no free space

schema:      the schema of the CSV file so that it only needs to read it once
hdfs_source: the place of the CSV currently on HDFS
hdfs_path:   the destination root directory for the day folder to store the parquet data that is written
date_value:  the date the file belongs to so that the number of rows written for this date can be aquired
             from spark for later comparison

returns:     this function returns nothing but saves a temporary file onto HDFS for the namenode to read
             that contains the stats of the ingestion and the rows ingested
"""
def spark_ingest(schema,hdfs_source,hdfs_path,date_value):
    #create a spark environmnt to send data to
    spark = (
             SparkSession.builder
             .appName("IngestionPipeline")
             .config("spark.network.timeout", "600s")
             .config("spark.executor.heartbeatInterval", "30s")
             .config("spark.rpc.askTimeout", "300s")
             .config("spark.rpc.lookupTimeout", "300s")
             .getOrCreate()
             )

    #tell spark to report errors only
    spark.sparkContext.setLogLevel("ERROR")
    #get the read start time for later analysis
    read_start = time.time()
    #read the file from hdfs into a spark dataframe based on the schema provided and the CSV file
    df = spark.read.schema(schema).csv(hdfs_source)
    #check to see if it is an empty frame
    if df.rdd.isEmpty():
       #this is a file with no rows to write and that it is beign skipped
       print("No rows to write — skipping Parquet write.")
       #terminate immedately as the rest of the data is no longer valid, and there is no need to write a
       #blank file to HDFS
       return

    #get the spark start write time
    write_start = time.time()
    #if it is an AIS file with a date
    if "AIS" in hdfs_source:
       #add a date column for sharding it by date
       df = df.withColumn("date", to_date("BaseDateTime"))
       #append the file into hdfs parquet files shareded by date for easier clean up
       df.write.mode("append").partitionBy("date").parquet(hdfs_path)
       #get the number of rows written to spark or later verificaiton
       ingested_row_count = spark.read.parquet(f"{hdfs_path}/date={date_value}").count()
    #else it is not an AIS CSV file with a date then it is the cruise ship stats CSV which does not get 
    #sharded as it is a small file with no date
    else:
       #just write the cruise ship file in parquet format to its directory, it is a small file with only 
       #one parquet so overwrite is the correct choice, AIS files are huge split into multiple parts so 
       #overwrite is not correct for those rather append is correct
       df.write.mode("overwrite").parquet(hdfs_path)
       # Cruise files are not partitioned
       ingested_row_count = df.count()


    #get the time as that is the write finished time
    write_done = time.time()
    #close spark
    spark.stop()

    #the connection of the HDFS nameserver to save the stats to HDFS
    client = InsecureClient("http://namenode:9870", user="hadoopuser")
    #write the stats to hdfs directly in a temproary file
    with client.write("/data/temporary_files/ingest_times_local.txt", overwrite=True) as f:
         f.write(f"READ_START {read_start}\n".encode("utf-8"))
         f.write(f"WRITE_START {write_start}\n".encode("utf-8"))
         f.write(f"WRITE_DONE {write_done}\n".encode("utf-8"))
         f.write(f"ROW_COUNT {ingested_row_count}\n".encode("utf-8"))

"""
this is the main function to run the spark ingestion on Yarn which in this case is typically run on datanode

argv[1]: the schema jason file
argv[2]: the source CSV file on HDFS to be read
argv[3]: the root path to save the CSV parquets to on HDFS in Spark
argv[4]: the date this AIS csv belongs to, if it is a cruise ship then this value does not matter

returns: nothing but a file with stats is saved to HDFS
"""
def main():
    #get the input paramreters from the system arguements
    schema_json_file = sys.argv[1]
    hdfs_source = sys.argv[2]
    hdfs_path = sys.argv[3]
    date_value = sys.argv[4]
    #read the json schema file passed to this function
    with open(schema_json_file, "r") as f:
        schema_json = f.read()
    # Rebuild schema inside YARN
    schema = StructType.fromJson(json.loads(schema_json))
    #ingest the file sith spark inside of Yarn
    spark_ingest(schema, hdfs_source, hdfs_path,date_value)

"""
Main entry point that is run as a subprocess on Yarn

returns: nothing but a file with stats is saved to HDFS
"""
if __name__ == "__main__":
    main()

"""
this function is called on the namenode python script it performs ingestion of a CSV file with Spark on Yarn

schema:      the schema for this CSV file to speed up ingestion by having it only read the file once
hdfs_source: the HDFS source path to the CSV file
hdfs_path:   the HDFS destination root path for the spark parquet file shards
date_value:  the date for this file if it is an HDFS for to check for partial files

returns:     a structure of results which contains success,start read time, start write time, finished
write time, standard out, standard errors, and rows ingested
"""
def run_spark_ingest(schema, hdfs_source, hdfs_path,date_value):

     #convert the schema to a string since all parameters passed to a subprocess must be a string
     schema_json = json.dumps(schema.jsonValue())
     #save the json to a file since spark/yarn can't handle a json string being passed as an arguement
     schema_file = "schema.json"
     with open(schema_file, "w") as f:
        f.write(schema_json)
     if date_value is None:
        date_value = "no date"

     #create the command to run the ingestion in spark on Yarn to take advantage of parallel processing
     cmd = [
          "/opt/spark/bin/spark-submit",
          #run it with yarn, wich gives me the ability to run it on the datanode and isolation and the 
          #ability to limit memory usage to prevent proces hogs
          "--master", "yarn",
          #execute it on the datanodes
          "--deploy-mode", "client",
          #only run once do not retry
          "--conf", "spark.yarn.maxAppAttempts=1",
          #memory should be specified otherwise spark will defauilt to incompatable amounts
          "--executor-memory", "2g",
          "--driver-memory", "1g",
          "--executor-cores", "1",
          #my setup is too small to take advantage of true parallism so ths value must be set to 1,
          #if I had more datanodes I could set this higher and take advantage of parallelism
          "--conf", "spark.executor.instances=1",
          #this passes the schema file to the node doing the work in this case datanode
          "--files", schema_file,
          #this calls the file to run on the datanode in this case this file
          "spark_ingest.py",
          #these are the parameters to pass for this ingestion
          schema_file,
          hdfs_source,
          hdfs_path,
          date_value
          ]
       
     #this launches ingest into spark using Apache on Yarn to take advantage of resource manager
     #also it redirects all screen output to the results and waits for ingestion completion11
     result = subprocess.run(cmd, capture_output=True, text=True)
     #determine if it succeeded or not
     success = (result.returncode == 0)
     #if the process failed
     if not success:
        #if there were no rows to write and that is why it failed
        if "No rows to write — skipping Parquet write." in result.stdout:
           print(f"File already in HDFS")
           #package the results together, nothing was run so no data is really needed just set the flag
           #that says no rows were run
           results = {
                     "success": success,
                     "read_start": 0,
                     "write_start": 0,
                     "write_done": 0,
                     "stdout": result.stdout,
                     "stderr": result.stderr,
                     "empty":True,
                     "ingested_written_row_count":0
                     }
           #return the results package
           return results
        #else it failed but it was not an empty file
        else:
          #package the results together
          results = {
                    "success": success,
                    "read_start": 0,
                    "write_start": 0,
                    "write_done": 0,
                    "stdout": result.stdout,
                    "stderr": result.stderr,
                    "empty":False,
                    "ingested_written_row_count":0
                    }
          #return the packaged results
          return results
     #else it succeeded
     else:
          #create some place holders to store the timestamps for when events happened
          read_start = None
          #write starts as soon as read is finished
          write_start = None
          #time stamp for when the whole thing is done
          write_done = None
          #the path the stats were saved to on HDFS
          ts_path = "hdfs:///data/temporary_files/ingest_times_local.txt"
          #get the file contents
          output = subprocess.run(
          ["hdfs", "dfs", "-cat", ts_path],
          capture_output=True, text=True
          ).stdout
          #delete the stats from HDFS as it is no longr needed
          subprocess.run(["hdfs", "dfs", "-rm", ts_path])
          #parse the file contents to get the times including how many rows were ingested
          for line in output.splitlines():
              parts = line.split()
              if len(parts) == 2:
                 tag, ts = parts
                 ts = float(ts)

                 if tag == "READ_START":
                    read_start = ts
                 elif tag == "WRITE_START":
                    write_start = ts
                 elif tag == "WRITE_DONE":
                    write_done = ts
                 elif tag == "ROW_COUNT":
                    ingested_row_count = int(ts)

          #package the results together
          results = {
                    "success": success,
                    "read_start": read_start,
                    "write_start": write_start,
                    "write_done": write_done,
                    "stdout": result.stdout,
                    "stderr": result.stderr,
                    "empty":False,
                    "ingested_written_row_count":ingested_row_count
                    }
          #return the packaged results
          return results




