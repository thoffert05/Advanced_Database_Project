"""
This File is allows fastapi to get a summary  momentum day entry data for a date range and ship or curise 
line information

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
this function gets  momentum data in a given date range and filters by ship or cruise line if that is 
given

input:
        start_date: the range start date
        end_Date: the range end date
        ship_name: if given it filters the result and only gives lines with this ship
        cruise_line: if given it filters only ships from this cruise line
        show_ship: if it should show all ship summaries after filtering
        show_cruise_line: if it should show all cruise_line summaries of only the filtered curise line
        this won't reflect updated values if the cuise line is filtered to only 1 ship
        show_day: if it should show all day grand totals which won't reflect filtering but will include
        everything in its value
        this function cannot filter by both ship name and cruise line at the same time
        this function also does not allow the filtering by ships if ships are not shown
        this function also does not allow the filtering by cruise lines if cruise lines are not shown
output: the data frame wiht the summary momentum data for the day range requested, ship filterd, 
        cruise line filtered and show only items requested
"""
def load_range(start_date: str, end_date: str,ship_name:str| None = None,cruise_line:str| None = None,show_ship:bool=True,show_cruise_line:bool=True,show_day:bool=True):
    #if the user does not want to see anything then there is no point of running this function so I treat that as an 
    #error
    if not show_ship and not show_cruise_line and not show_day:
       raise HTTPException(
                           status_code=400,
                           detail="At least one of show_ship, show_cruise_line, or show_day must be True."
                           ) 
    
    #get the start date as a date if it is not a date then throw an exception
    start = parse_date(start_date)
    #get the end date as a date if it is not a date then throw an exception
    end = parse_date(end_date)
    #if the start is after the end then throw an exception as this is not a valid range
    if start>end:
       raise HTTPException(
                           status_code=400,
                           detail=f"Start date expected to be before end date: given start: '{start}' given end: '{end}'"
                           )

    #if they are want to filter a ship and are not showing ships I do not allow this
    if ship_name and not show_ship:
       #throw an exception back to the browser
       raise HTTPException(
                           status_code=400,
                           detail="Cannot filter on ship if a ship is not shown"
                           )

    #if they are want to filter a cruise line and are not showing cruise lines  I do not allow this
    if cruise_line and not show_cruise_line:
       #throw an exception back to the browser
       raise HTTPException(
                           status_code=400,
                           detail="Cannot filter on cruise line if a cruise line is not shown"
                           )

    #I only support either a cruise line filter or a ship filter but not both so if both are given
    if ship_name and cruise_line:
       #throw an exception back to the browser
       raise HTTPException(
                           status_code=400,
                           detail="Specify either ship or cruise_line, not both."
                           )

    #stores all the files to be read for the given date range
    files = []
    #set the start of the range
    cur = start
    #while it is within the range
    while cur <= end:
        #reset the path to the expected partition folder
        date_path = f"/data/summary_momentum_daily/event_date={cur.isoformat()}"
        # List files inside the partition folder
        for info in hdfs.get_file_info(fs.FileSelector(date_path)):
            #if it is a file
            if info.is_file:
                #add it to the list of files to be read
                files.append(info.path)
        #go to the next day
        cur += datetime.timedelta(days=1)
    #read the days in the range from the summary momentum database
    polars_data_frame = get_dataset(files)


    #compute the last day read
    last_date = end
    #if a cruise line is given
    if cruise_line:
       #filter just that cruise line
       polars_data_frame = filter_cruise_line(polars_data_frame,cruise_line)
    #if a ship is given
    if ship_name:
       #get the cruise line for the ship
       selected_cruise_line = (
                                polars_data_frame
                                .filter(pl.col("ShipName") == ship_name)
                                .select("CruiseLine")
                                .unique()
                                .item()
                              )
       #create a filter to just that ship
       ship_filter = (
                      (pl.col("row_type") == "ship_daily") &
                      (pl.col("ShipName") == ship_name)
                     )
       #if a cruise line was found for the ship earlier this should always be the case but for derensive 
       #coding we check to see if it was not found
       if selected_cruise_line is not None:
          #there was a cruise line found so create a filter to filter out just the cruise line rows
          cruiseline_filter = (
                                #if it is a cruiseline total row
                               (pl.col("row_type") == "cruiseline_daily") &
                                #and the cruise line matches
                               (pl.col("CruiseLine") == selected_cruise_line)
                              )
       else:
          #else no cruise line was found
          cruiseline_filter = None

       #create a filter just for the global row
       global_filter = (pl.col("row_type") == "global_daily")

       #if a cruise line filter was created earlier
       if cruiseline_filter is not None:
          polars_data_frame = polars_data_frame.filter(
                                                       ship_filter | cruiseline_filter | global_filter
                                                      )
       else:
         #else there is no cruise filter
         polars_data_frame = polars_data_frame.filter(
                                                      ship_filter |  global_filter
                                                     )
    #store the filters for what is allowed to be seen
    viability_filters=[]
    #if the user wants to see summaries for every filtered ship
    if show_ship:
       viability_filters.append("ship_daily")

    #if the user wants to see summaries for every filtered cruise line
    if show_cruise_line:
       viability_filters.append("cruiseline_daily")

    #if the user wishes to see the day totals
    if show_day:
       viability_filters.append("global_daily")

    #if the user does not wish to see everything and what is allowed is less than 3 which is all categories
    if len(viability_filters) < 3:
       #filter to just show what the user wants to see
       polars_data_frame = polars_data_frame.filter(pl.col("row_type").is_in(viability_filters))

    #Make a clean copy BEFORE totals are added
    base_df = polars_data_frame.clone()
    #include only ship daily values to generate the ship total
    ship_base = base_df.filter(pl.col("row_type") == "ship_daily")
    #Compute per-ship totals for the whole range (one row per date per ship)
    ship_totals = (
                   ship_base
                   .group_by([
                              "ShipName",
                              "CruiseLine",
                              "MMSI",
                              "DWT",
                              "GT",
                              "PassengerCapacity",
                              "CrewCount",
                              "YearBuilt"
                           ])
                   .agg([
                         pl.col("ship_avg_momentum").mean().alias("ship_avg_momentum"),
                         pl.col("ship_max_momentum").max().alias("ship_max_momentum")
                       ])
                   .with_columns([
                                  pl.col("MMSI"),
                                  pl.col("ship_avg_momentum"),
                                  pl.col("ship_max_momentum"),
                                  pl.lit("ship_total").alias("row_type"),
                                  pl.lit(last_date).alias("date"),
                                  pl.col("ShipName"),
                                  pl.col("CruiseLine"),
                                  pl.col("DWT"),
                                  pl.col("GT"),
                                  pl.col("PassengerCapacity"),
                                  pl.col("CrewCount"),
                                  pl.lit(None).alias("cruiseline_avg_momentum"),
                                  pl.lit(None).alias("cruiseline_max_momentum"),
                                  pl.lit(None).alias("global_avg_momentum"),
                                  pl.lit(None).alias("global_max_momentum")
                                 ])
                   .select([
                            "MMSI",
                            "ship_avg_momentum",
                            "ship_max_momentum",
                            "row_type",
                            "date",
                            "ShipName",
                            "CruiseLine",
                            "DWT",
                            "GT",
                            "PassengerCapacity",
                            "CrewCount",
                            "YearBuilt",
                            "cruiseline_avg_momentum",
                            "cruiseline_max_momentum",
                            "global_avg_momentum",
                            "global_max_momentum"
                         ])
                  )



    #Reorder columns to match master table
    ship_totals = ship_totals.select(polars_data_frame.columns)

    #filter only cruise line totals
    cruise_line_base = base_df.filter(pl.col("row_type") == "cruiseline_daily")
    #create cruise line totals
    cruiseline_totals = (
                         cruise_line_base
                         .group_by(["CruiseLine"])
                         .agg([
                               pl.col("cruiseline_avg_momentum").mean().alias("cruiseline_avg_momentum"),
                               pl.col("cruiseline_max_momentum").max().alias("cruiseline_max_momentum")
                             ])
                         .with_columns([
                                        pl.lit(None).alias("MMSI"),
                                        pl.lit(None).alias("ship_avg_momentum"),
                                        pl.lit(None).alias("ship_max_momentum"),
                                        pl.lit("cruiseline_total").alias("row_type"),
                                        pl.lit(last_date).alias("date"),
                                        pl.lit(None).alias("ShipName"),
                                        pl.col("CruiseLine"),
                                        pl.lit(None).alias("DWT"),
                                        pl.lit(None).alias("GT"),
                                        pl.lit(None).alias("PassengerCapacity"),
                                        pl.lit(None).alias("CrewCount"),
                                        pl.lit(None).alias("YearBuilt"),
                                        pl.col("cruiseline_avg_momentum"),
                                        pl.col("cruiseline_max_momentum"),
                                        pl.lit(None).alias("global_avg_momentum"),
                                        pl.lit(None).alias("global_max_momentum")
                                     ])
                         .select([
                                  "MMSI",
                                  "ship_avg_momentum",
                                  "ship_max_momentum",
                                  "row_type",
                                  "date",
                                  "ShipName",
                                  "CruiseLine",
                                  "DWT",
                                  "GT",
                                  "PassengerCapacity",
                                  "CrewCount",
                                  "YearBuilt",
                                  "cruiseline_avg_momentum",
                                  "cruiseline_max_momentum",
                                  "global_avg_momentum",
                                  "global_max_momentum"
                               ])
                        )

    #filter only daily global line totals
    global_base = base_df.filter(pl.col("row_type") == "global_daily")
    #Compute grand totals for the entire filtered range
    grand_totals = (
                    global_base
                    .select([
                             pl.col("global_avg_momentum").mean().alias("global_avg_momentum"),
                             pl.col("global_max_momentum").max().alias("global_max_momentum")
                          ])
                    .with_columns([
                                  pl.lit(None).alias("MMSI"),
                                  pl.lit(None).alias("ship_avg_momentum"),
                                  pl.lit(None).alias("ship_max_momentum"),
                                  pl.lit("grand_total").alias("row_type"),
                                  pl.lit(last_date).alias("date"),
                                  pl.lit(None).alias("ShipName"),
                                  pl.lit(None).alias("CruiseLine"),
                                  pl.lit(None).alias("DWT"),
                                  pl.lit(None).alias("GT"),
                                  pl.lit(None).alias("PassengerCapacity"),
                                  pl.lit(None).alias("CrewCount"),
                                  pl.lit(None).alias("YearBuilt"),
                                  pl.lit(None).alias("cruiseline_avg_momentum"),
                                  pl.lit(None).alias("cruiseline_max_momentum"),
                                  pl.col("global_avg_momentum"),
                                  pl.col("global_max_momentum")
                                 ])
                    .select([
                            "MMSI",
                            "ship_avg_momentum",
                            "ship_max_momentum",
                            "row_type",
                            "date",
                            "ShipName",
                            "CruiseLine",
                            "DWT",
                            "GT",
                            "PassengerCapacity",
                            "CrewCount",
                            "YearBuilt",
                            "cruiseline_avg_momentum",
                            "cruiseline_max_momentum",
                            "global_avg_momentum",
                            "global_max_momentum"
                          ])
                   )

    #add all computed totals into the dataaset
    #if the user wants to see summaries for every filtered ship
    if show_ship:
       #add ship totals
       polars_data_frame = polars_data_frame.vstack(ship_totals)
    #if the user wants to see summaries for every filtered cruise line
    if show_cruise_line:
       #add cruise line totals into database
       polars_data_frame = polars_data_frame.vstack(cruiseline_totals)
    #if the user wishes to see the day totals
    if show_day:
       #Add grand_total row to the master table
       polars_data_frame = polars_data_frame.vstack(grand_totals)

    #return the polars data frame
    return polars_data_frame
