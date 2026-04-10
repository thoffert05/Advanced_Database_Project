"""
This File is responsible for generating a series of tables per day that has all the cruise ship momentums
calculated, has an average total momentum and a maximum momentum for each cruise line and for each ship per day
average momentum, and a day maximum momentum calculated.  The idea is so that the webpage can do quick access
of the average momneum per cruise line, per ship, per day.

Project members: Anthony Hoffert, Able Daniel
""" 

from pyspark.sql import SparkSession
from pyspark.sql.functions import col
from pyspark.sql.functions import to_date
from pyspark.sql.functions import avg, max as spark_max, lit
from datetime import datetime
import time

#get the end time with the special python clock that only moves forward this is used for elapsed time it
#ignores VM time changes and only moves forwards
START = time.monotonic()
print(f"Started at {datetime.now()}")
#interface with spark
spark = SparkSession.builder.appName("Momentum_Calculator").getOrCreate()
print("====================================READING AIS TABLE====================================")
#load daily AIS data
ais_df = spark.read.parquet("/data/ais")
print("====================================READING CRUISE TABLE====================================")
#load cruiseship and riverboat stat data which contains ship sizes
cruise_df = spark.read.parquet("/data/cruise")
print("====================================FILTERING AIS TABLE====================================")
#this filters the AIS table to just store the lines for the cruise ships/river boats we care about reducing
#it to about 106 boats from probable 100,000 boats in the full dataset
ais_df = ais_df.join(cruise_df, on="IMO", how="inner")
print("====================================COMPUTING MOMENTUM FOR EACH SHIP====================================")
#add a momentum column and compute the momentum on each line, by multiplying the speed multiplied by 
#0.514444 to go from knotts to meters per secont multiply it by the mass which is multiplied by 1000 to 
#convert Dead Weight Tonnes to kilograms for the formula to work
ais_df = ais_df.withColumn(
    "momentum",
    col("DWT")*1000 * (col("SOG") * 0.514444)
)
#add the date to each row as metadate so that it can be partitioned later for easy range searches
ais_df = ais_df.withColumn(
    "date",
    to_date(col("BaseDateTime"), "yyyy-MM-dd'T'HH:mm:ss")
)


print("====================================COMPUTING PER-SHIP DAILY AGGREGATES====================================")
#this generates the daily average and daily maximum momentum for each ship to be used for analytics later
#and graphing on the dashboard

#group by date and by MMSI which is unique per ship in a separate dataframe as this is unique
ship_daily = ais_df.groupBy(
    "date", "IMO"
#this will add 2 new columns just for this row one for the ship average momentum and one for the ship max 
#momentum and a row_type column the row will not have a momentum column like the raw ship rows have.
#this is OK since this is just read by the java dashboard 
).agg(
    avg("momentum").alias("ship_avg_momentum"),
    spark_max("momentum").alias("ship_max_momentum")
).withColumn(
    "row_type", lit("ship_daily")
)
#add the ship stats to the column
ship_daily = ship_daily.join(
    cruise_df.select("IMO", "ShipName", "CruiseLine", "DWT","GT", "PassengerCapacity", "CrewCount"),
    on="IMO",
    how="left"
).withColumn(
    "row_type", lit("ship_daily")
)


print("====================================COMPUTING PER-CRUISELINE DAILY AGGREGATES====================================")
#this is the daily average and maximum momentum for a cruiseline
#group by date and the cruise line for the cruise line stats in a separate dataframe as this is unique
cruiseline_daily = ais_df.groupBy(
    "date", "CruiseLine"
#this will add 2 new columns just for this row one for the cruiseline average momentum and one for the  
#cruiseline maximum momentum and a row_type column the row will not have a momentum column like the raw ship 
#rows have. this is OK since this is just read by the java dashboard
).agg(
    avg("momentum").alias("cruiseline_avg_momentum"),
    spark_max("momentum").alias("cruiseline_max_momentum")
).withColumn(
    "row_type", lit("cruiseline_daily")
)
print("====================================COMPUTING GLOBAL DAILY AGGREGATES====================================")
#this comuputes the daily average momentum and daily maximum momentum
#group by day this has the day statistics in a separate dataframe as this is unique
global_daily = ais_df.groupBy(
    "date"
#this will add 2 new columns just for this row one for the day average momentum and one for the day
#maximum momentum and a row_type column the row will not have a momentum column like the raw ship 
#rows have. this is OK since this is just read by the java dashboard
).agg(
    avg("momentum").alias("global_avg_momentum"),
    spark_max("momentum").alias("global_max_momentum")
).withColumn(
    "row_type", lit("global_daily")
)
print("====================================UNIONING ALL DAILY TABLES====================================")
#this combines all dataframes into one
final_df = ship_daily.unionByName(cruiseline_daily, allowMissingColumns=True).unionByName(global_daily, allowMissingColumns=True)

print("====================================WRITING OUTPUT TABLE====================================")
#this saves the dataframes partitioned by date into a new momentum_daily folder.  they are saved as parquets
#because at this point we have all AIS stored on here and now it is duplicated for each cruise ship/river 
#boat, though this second set is much smaller saving it this way confirms that there will be plenty of space
#on the datanode
final_df.write.mode("overwrite").partitionBy("date").parquet("/data/momentum_daily")
#get the end time with the special python clock that only moves forward this is used for elapsed time it
#ignores VM time changes and only moves forwards
END = time.monotonic()
elapsed = END - START
print(f"Finished at {datetime.now()} (elapsed {elapsed:.2f} seconds)")


