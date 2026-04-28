"""
This File is responsible for generating a series of tables per day that has all the cruise ship momentums
calculated, has an average total momentum and a maximum momentum for each cruise line and for each ship per day
average momentum, and a day maximum momentum calculated.  The idea is so that the webpage can do quick access
of the average momneum per cruise line, per ship, per day.

Project members: Anthony Hoffert, Able Daniel
""" 

from pyspark.sql import SparkSession
from pyspark.sql.functions import year as spark_year
from pyspark.sql.functions import col, when,to_date,avg, max as spark_max, lit
from datetime import datetime
import time

#get the end time with the special python clock that only moves forward this is used for elapsed time it
#ignores VM time changes and only moves forwards
START = time.monotonic()
print(f"Started at {datetime.now()}")
#interface with spark
spark = SparkSession.builder.appName("Momentum_Calculator").getOrCreate()
print("===============================READING CRUISE TABLE===============================================")
#load cruiseship and riverboat stat data which contains ship sizes
cruise_df = spark.read.parquet("/data/cruise").filter(col("MMSI").isNotNull())
cruise_df = cruise_df.select(
    "MMSI",
    "ShipName",
    "CruiseLine",
    "DWT",
    "GT",
    "PassengerCapacity",
    "CrewCount",
    "YearBuilt"
)
print("===============================READING AIS TABLE==================================================")
#load daily AIS data and remove lines which have no datetime as they are not valid and that is such
#a small percentage it does not really matter, remove ships that either have no speed or the speed is
#anomously high, a cruise ships don't tpyically go beyone 24 knots, some can do up to 32 knots but that is
#rare so a good cap is at 30 knots. this removes spikes in maximumx
ais_df = spark.read.parquet("/data/ais").filter(col("BaseDateTime").isNotNull())\
                                        .filter(col("SOG").isNotNull()) \
                                        .filter((col("SOG") >= 0) & (col("SOG") <= 30))
#remove null MMSI rows
cruise_df = cruise_df.filter(col("MMSI").isNotNull())
print("===============================FILTERING AIS TABLE================================================")
#this filters the AIS table to just store the lines for the cruise ships/river boats we care about reducing
#it to about 106 boats from probable 100,000 boats in the full dataset
ais_df = ais_df.join(cruise_df, on="MMSI", how="inner")
#remove null MMSI rows
ais_df = ais_df.filter(col("MMSI").isNotNull())
print("===============================COMPUTING MOMENTUM FOR EACH SHIP===================================")
#add a momentum column and compute the momentum on each line, by multiplying the speed multiplied by 
#0.514444 to go from knotts to meters per secont multiply it by the mass which is multiplied by 1000 to 
#convert Dead Weight Tonnes to kilograms for the formula to work
ais_df = ais_df.withColumn(
    "momentum",
    col("DWT")*1000 * (col("SOG") * 0.514444)
)
#add the date to each row as metadate so that it can be partitioned later for easy range searches
ais_df = ais_df.withColumn(
    "event_date",
    col("BaseDateTime").cast("date")
)

print("===============================Saving MOMENTUM FOR EACH SHIP======================================")
ais_df.write.mode("overwrite").partitionBy("event_date").parquet("/data/momentum_raw")

print("===============================COMPUTING PER-SHIP DAILY AGGREGATES================================")
#this generates the daily average and daily maximum momentum for each ship to be used for analytics later
#and graphing on the dashboard

#group by date and by MMSI which is unique per ship in a separate dataframe as this is unique
ship_daily = ais_df.groupBy(
    "event_date", "MMSI"
#this will add 2 new columns just for this row one for the ship average momentum and one for the ship max 
#momentum and a row_type column the row will not have a momentum column like the raw ship rows have.
#this is OK since this is just read by the java dashboard 
).agg(
    avg("momentum").alias("ship_avg_momentum"),
    spark_max("momentum").alias("ship_max_momentum")
).withColumn(
    "row_type", lit("ship_daily")
)
#add the date column
ship_daily = ship_daily.withColumn(
    "date",
    when(col("event_date").isNull(), lit("0001-01-01").cast("date")).otherwise(col("event_date"))
)
#add the ship stats to the column
ship_daily = ship_daily.join(
    cruise_df.select(
                     "MMSI", 
                     "ShipName", 
                     "CruiseLine", 
                     "DWT",
                     "GT", 
                     "PassengerCapacity", 
                     "CrewCount",
                     "YearBuilt"
                    ),
    on="MMSI",
    how="left"
).withColumn(
    "row_type", lit("ship_daily")
)


print("===============================COMPUTING PER-CRUISELINE DAILY AGGREGATES==========================")
#this is the daily average and maximum momentum for a cruiseline
#group by date and the cruise line for the cruise line stats in a separate dataframe as this is unique
cruiseline_daily = ais_df.groupBy(
    "event_date", "CruiseLine"
#this will add 2 new columns just for this row one for the cruiseline average momentum and one for the  
#cruiseline maximum momentum and a row_type column the row will not have a momentum column like the raw ship 
#rows have. this is OK since this is just read by the java dashboard
).agg(
    avg("momentum").alias("cruiseline_avg_momentum"),
    spark_max("momentum").alias("cruiseline_max_momentum")
).withColumn(
    "row_type", lit("cruiseline_daily")
)
#add the date to the curise line summary
cruiseline_daily = cruiseline_daily.withColumn(
    "date",
    when(col("event_date").isNull(), lit("0001-01-01").cast("date")).otherwise(col("event_date"))
)
cruiseline_daily = cruiseline_daily.join(
    cruise_df.select(
        "CruiseLine",
        "DWT",
        "GT",
        "PassengerCapacity",
        "CrewCount",
        "YearBuilt"
    ),
    on="CruiseLine",
    how="left"
)
print("===============================COMPUTING GLOBAL DAILY AGGREGATES==================================")
#this comuputes the daily average momentum and daily maximum momentum

#group by day this has the day statistics in a separate dataframe as this is unique
global_daily = ais_df.groupBy(
    "event_date"
#this will add 2 new columns just for this row one for the day average momentum and one for the day
#maximum momentum and a row_type column the row will not have a momentum column like the raw ship 
#rows have. this is OK since this is just read by the java dashboard
).agg(
    avg("momentum").alias("global_avg_momentum"),
    spark_max("momentum").alias("global_max_momentum")
).withColumn(
    "row_type", lit("global_daily")
)
#add the date to the global summary
global_daily = global_daily.withColumn(
    "date",
    when(col("event_date").isNull(), lit("0001-01-01").cast("date")).otherwise(col("event_date"))
)
print("===============================MERGING ALL DAILY TABLES===========================================")
#this combines all dataframes into one
final_df = (ship_daily
            .unionByName(cruiseline_daily, allowMissingColumns=True)
            .unionByName(global_daily, allowMissingColumns=True)
           )

print("===============================WRITING OUTPUT TABLE===============================================")
#make sure that there is always a date column, because spark will delete null columns
final_df = final_df.withColumn(
    "date",
    when(col("event_date").isNull(), lit("0001-01-01").cast("date"))
    .otherwise(col("event_date"))
)
#this saves the dataframes partitioned by date into a new momentum_daily folder.  they are saved as parquets
#because at this point we have all AIS stored on here and now it is duplicated for each cruise ship/river 
#boat, though this second set is much smaller saving it this way confirms that there will be plenty of space
#on the datanode
final_df.write.mode("overwrite").partitionBy("event_date").parquet("/data/summary_momentum_daily")

#this is the initial table the website will display so precomputing it will help it load a lot faster in
#Polar and fastapi
print("===============================COMPUTING YEARLY SUMMARY TABLE=====================================")
print("===============================COMPUTING SHIP YEARLY SUMMARY TABLE================================")
#Compute the table which stores the ship maximum momentum and ship averages for all ships for the whole year
#extract the year for the ship
ship_yearly = ship_daily.withColumn("year", spark_year(col("event_date")))
#compute the average momentum and maximum momentum per ship or the whole year
ship_yearly = (
                ship_yearly
                .groupBy("year", "MMSI")
                .agg(
                     avg("ship_avg_momentum").alias("ship_year_avg_momentum"),
                     spark_max("ship_max_momentum").alias("ship_year_max_momentum")
                    )
                .join(
            cruise_df.select("MMSI", 
                             "ShipName", 
                             "CruiseLine", 
                             "DWT", 
                             "GT", 
                             "PassengerCapacity", 
                             "CrewCount",
                             "YearBuilt"),
                      on="MMSI",
                      how="left"
                            )
                     .withColumn("row_type", lit("ship_year_total"))
                     )
#put a duplicate column of year into the table to partion it by
ship_yearly = ship_yearly.withColumn("event_year", col("year"))
print("===============================COMPUTING CRUISE LINE YEARLY SUMMARY TABLE=========================")
#Compute the table which stores the cruise line  maximum momentum and cruise line  averages for all ships for 
#the whole year extract the year for the cruise line
cruiseline_yearly = cruiseline_daily.withColumn("year", spark_year(col("event_date")))
#compute the average momentum and maximum momentum per ship or the whole year
cruiseline_yearly = (
                     cruiseline_yearly
                     .groupBy("year", "CruiseLine")
                     .agg(
                          avg("cruiseline_avg_momentum").alias("cruiseline_year_avg_momentum"),
                          spark_max("cruiseline_max_momentum").alias("cruiseline_year_max_momentum")
                         )
                     .join(
                           cruise_df.select(
                                            "CruiseLine",
                                            "DWT",
                                            "GT",
                                            "PassengerCapacity",
                                            "CrewCount",
                                            "YearBuilt"     
                                           ),
                           on="CruiseLine",
                           how="left"
                    )
                    .withColumn("row_type", lit("cruiseline_year_total"))
)
#put a duplicate column of year into the table to partion it by
cruiseline_yearly = cruiseline_yearly.withColumn("event_year", col("year"))
print("===============================COMPUTING GLOBAL YEARLY SUMMARY TABLE==============================")
#stores the maximum momentum and average momentem over all ships for the whole year
global_yearly = global_daily.withColumn("year", spark_year(col("event_date")))
#compute the average momentum and maximum momentum per ship or the whole year
global_yearly = (
                 global_yearly
                 .groupBy("year")
                 .agg(
                      avg("global_avg_momentum").alias("global_year_avg_momentum"),                     
                      spark_max("global_max_momentum").alias("global_year_max_momentum")
                     )
                 .withColumn("row_type", lit("global_year_total"))
                )
#put a duplicate column of year into the table to partion it by
global_yearly = global_yearly.withColumn("event_year", col("year"))
print("===============================MERGING YEARLY SUMMARY TABLE=======================================")
#creat combined table via union
yearly_final = (
    ship_yearly
        .unionByName(cruiseline_yearly, allowMissingColumns=True)
        .unionByName(global_yearly, allowMissingColumns=True)
)
print("===============================WRITING YEARLY SUMMARY TABLE=======================================")
yearly_final.write.mode("overwrite").partitionBy("event_year").parquet("/data/summary_momentum_yearly")
print("===============================FINISHED WRITING TABLES============================================")
#get the end time with the special python clock that only moves forward this is used for elapsed time it
#ignores VM time changes and only moves forwards
END = time.monotonic()
elapsed = END - START
print(f"Finished at {datetime.now()} (elapsed {elapsed:.2f} seconds)")

