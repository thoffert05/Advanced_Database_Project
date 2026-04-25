"""
This File is allows fastapi to get a detailed momentum day entry data for a single day and allow it to 
be filtered by ship or curise line

Project members: Anthony Hoffert, Able Daniel
"""
import datetime
import pyarrow.fs as fs
import polars as pl
from pyarrow.fs import HadoopFileSystem
from fastapi import HTTPException
from web_hdfs_interface.hdfs_interface import get_dataset

#create hadoop connection
hdfs = HadoopFileSystem(
    host="namenode",
    port=9000,
    user="hadoopuser"
)


"""
this function is used to check dates to verify that they are valid if the date string are not dates then
it throws an exception which is passed back to the browser by fastapi
input: date string
ouput: string converted to a date
throws: an exception if either no date is given or an invalid date string is given
"""
def parse_date(date_str: str) -> datetime.date:
    #if no date is given
    if date_str is None:
        #throw an exception to fastapi
        raise HTTPException(status_code=400, detail="Date is required")
    #try to convert the string to a date and return the date object
    try:
        #convert the string to a date
        return datetime.date.fromisoformat(date_str)
    #if the string cannot be converted to a date
    except ValueError:
        #throw an exception becasue the date could not be converted and what is expected
        raise HTTPException(
            status_code=400,
            detail=f"Invalid date format: '{date_str}'. Expected YYYY-MM-DD"
        )

"""
this function takes a parol data frame and filters out a single ship
input:
       dataframe: holding the table
       ship_name: the name of the ship
output: the filtered dataframe
"""
def filter_ship(df: pl.DataFrame, ship_name: str):
    #filter by ship
    return df.filter(pl.col("ShipName") == ship_name)

"""
this function takes a parol data frame and filters out a single cruise line and all the ships in that line
input:
       dataframe: holding the table
       cruise_line: the name of the cruise line
output: the filtered dataframe
"""
def filter_cruise_line(df: pl.DataFrame, cruise_line: str):
    #filter by cruise line
    return df.filter(pl.col("CruiseLine") == cruise_line)

"""
this function gets raw momentum data in a given day and filters by ship or cruise line if that is 
given

input:
       date: the day to look at
       ship_name: if given it filters the result and only gives lines with this ship
       cruise_line: if given it filters only ships from this cruise line
       this function cannot filter by both ship name and cruise line at the same time
output: the data frame of momentum data with full AIS information with multip entries per day for the 
requested date and it can be filtered by ship name or cruise line if requested
"""
def load_day(date: str,ship_name:str| None = None,cruise_line:str| None = None):
    #get the date as a date if it is not a date then throw an exception
    Date = parse_date(date)
    
    #I only support either a cruise line filter or a ship filter but not both so if both are given
    if ship_name and cruise_line:
       #throw an exception back to the browser
       raise HTTPException(
                           status_code=400,
                           detail="Specify either ship or cruise_line, not both."
                           )

    #stores all the files to be read for the given date range
    files = []

    #while it is within the range
    
    #reset the path to the expected partition folder
    date_path = f"/data/momentum_raw/event_date={Date.isoformat()}"
    # List files inside the partition folder
    for info in hdfs.get_file_info(fs.FileSelector(date_path)):
        #if it is a file
        if info.is_file:
            #add it to the list of files to be read
            files.append(info.path)

    #read the days in the range from the raw momentum database
    polars_data_frame = get_dataset(files)
    #if a cruise line is given
    if cruise_line:
       #filter just that cruise line
       polars_data_frame = filter_cruise_line(polars_data_frame,cruise_line)
    #if a ship is given
    if ship_name:
    #filter just that ship
       polars_data_frame = filter_ship(polars_data_frame,ship_name)

    #compute one row per ship 
    ship_totals = (
                   polars_data_frame
                   .group_by("ShipName")
                   .agg([
                         pl.col("momentum").mean().alias("momentum_avg"),
                         pl.col("momentum").max().alias("momentum_max")
                        ])
                   .with_columns([
                                  pl.lit("ship_total").alias("row_type"),
                                  pl.col("ShipName"),
                                  pl.lit(None).alias("CruiseLine"),
                                  pl.lit(date).cast(pl.Date).alias("date")
                                 ])
                  )

    #create the rows for the ships to add to the dataframe
    ship_rows = ship_totals.with_columns([
                                          pl.lit("ship_total").alias("row_type")
                                         ])
    # Ensure summary rows have all columns from the raw DataFrame
    for col in polars_data_frame.columns:
        if col not in ship_rows.columns:
           ship_rows = ship_rows.with_columns(pl.lit(None).alias(col))
    # Reorder to match raw DataFrame so that they can be added properly
    ship_rows = ship_rows.select(polars_data_frame.columns)
    #add the ship total rows to the dataframe
    polars_data_frame = polars_data_frame.vstack(ship_rows)
    #compute one row per cruiseline
    cruiseline_totals = (
                         polars_data_frame
                         .group_by("CruiseLine")
                         .agg([
                               pl.col("momentum").mean().alias("momentum_avg"),
                               pl.col("momentum").max().alias("momentum_max")
                              ])
                         .with_columns([
                                       pl.lit("cruise_total").alias("row_type"),
                                       pl.lit(None).alias("ShipName"),
                                       pl.col("CruiseLine"),
                                       pl.lit(date).cast(pl.Date).alias("date")
                                      ])
                        )
    #add the cruise line totals to the dataframe
    cruise_rows = cruiseline_totals.with_columns([
                                                  pl.lit("cruiseline_total").alias("row_type")
                                                 ])
    # Ensure summary rows have all columns from the raw DataFrame
    for col in polars_data_frame.columns:
        if col not in cruise_rows.columns:
           cruise_rows = cruise_rows.with_columns(pl.lit(None).alias(col))
    # Reorder to match raw DataFrame so that they can be added properly
    cruise_rows = cruise_rows.select(polars_data_frame.columns)
    #add the cruise total rows for the day tot he table
    polars_data_frame = polars_data_frame.vstack(cruise_rows)
    #comute the maximum momentum and average moementum for the day 
    grand_totals = polars_data_frame.select([
                                             pl.col("momentum").mean().alias("grand_avg_momentum"),
                                             pl.col("momentum").max().alias("grand_max_momentum")
                                            ])
    grand_row = grand_totals.with_columns([
                                           pl.lit("grand_total").alias("row_type")
                                          ])

    # Ensure summary rows have all columns from the raw DataFrame
    for col in polars_data_frame.columns:
        if col not in grand_row.columns:
           grand_row = grand_row.with_columns(pl.lit(None).alias(col))
    # Reorder to match raw DataFrame so that they can be added properly
    grand_row = grand_row.select(polars_data_frame.columns)
    #add the grand total row to the data frame
    polars_data_frame = polars_data_frame.vstack(grand_row)

    #return the polars data frame
    return polars_data_frame
