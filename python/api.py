"""
This File is initializes fastapi and handles web requests and returns json results or errors

Project members: Anthony Hoffert, Able Daniel
"""
import pyarrow.fs as fs
import polars as pl
from datetime import datetime
from fastapi import FastAPI
from fastapi import Response
from fastapi import HTTPException
from pyarrow.fs import HadoopFileSystem
from web_momentum_loader.raw_momentum_handler import load_day
from web_momentum_loader.summary_momentum_handler import load_range
from web_momentum_loader.ship_metadata_handler import get_ship_data
from web_momentum_loader.initial_momentum import load_data, load_year
#initializes fast api for web processing
app = FastAPI()

"""
this function is called when the website sends a summary request meaning they want a daily summary
which can be filtered by ship name or cruise line and also can be filtered on what to include, include the
totals for the day, totals for the ship, and or totals for the crusie line

input:
start:                                         this is the range start date which must be before or the same as
the end date and must be a date string
end:                                           this is the range end date which must be after or the same as 
the start date and must be a date string
ship_name (optional):                          if the user wishes to filter by ship this stores the ship name 
to be filtered

NOTE YOU CANNOT FILTER BY SHIP IF YOU DO NOT SHOW SHIPS, AND THIS FILTER DOES NOT CHANGE THE CRUISE LINE
SUMMARY, IT ALSO DOES NOT CHANGE THE DAY SUMMARY

cruise_line (optional):                        if the user wishes to filter by cruise line this stores the 
cruise line to be filterd
NOTE YOU CANNOT FILTER BY CRUISE LINE IF YOU DO NOT SHOW THE CRUISE LINE, THIS FILTER DOES NOT AFFECT THE DAY
TOTALS
NOTE YOU CANNOT FILTER BY BOTH CRUISE LINE AND SHIP NAME AT THE SAME TIME

show_ship (optional defaulted to true):        this will include the cruise ships summaries in the output
show_cruise_line (optional defaulted to true): this will include cruise line summaries in the output
show_daily (optional defaulted to true):       this will include the daily summaries in the output

ouput:                                         summary as a json
throws:                                        invalid date exceptions, using 2 filter exceptions, trying to
filter on ships or cruse lines that are not shown
"""
@app.get("/summary")
def get_summary(start: str,end: str,ship_name: str | None = None,cruise_line: str | None = None,show_ship: bool = True,show_cruise_line: bool = True, show_day: bool = True):
    #try to get the json range from the information provided
    try:
        #get the data frame for the  summary data for the date range and the filters provided
        df = load_range(start_date=start,end_date=end,ship_name=ship_name,cruise_line=cruise_line,show_ship=show_ship,show_cruise_line=show_cruise_line,show_day=show_day)
        #return the results as a json file
        return df.to_dicts()
    #if an http exception was thrown becasue the inputs were invalid
    except HTTPException:
        #send the exception to fastapi which will send it to the browser
        raise
    #if an error happened in the function
    except Exception as e:
        #send the error to the browwser so that I can debut it
        raise HTTPException(status_code=500, detail=str(e))

"""
this function is called when the website sends a detailed request meaning they want full details for a day 
which can be filtered by ship name or cruise line

input:
ship_name (optional):   if the user wishes to filter by ship this stores the ship name to be filtered

cruise_line (optional): if the user wishes to filter by cruise line this stores the cruise line to be filtered
NOTE YOU CANNOT FILTER BY BOTH CRUISE LINE AND SHIP NAME AT THE SAME TIME

ouput:                  detailed day information as a json
throws:                 invalid date exceptions, using 2 filter exceptions or software exceptions
"""
@app.get("/raw")
def get_raw(date: str,ship_name: str | None = None,cruise_line: str | None = None):
    #try to get the day data
    try:
        #get the data frame for the day data of all the ships
        df = load_day(date=date,ship_name=ship_name,cruise_line=cruise_line)
        #turn the data frame into a json file and send it to the browser
        return df.to_dicts()
    #if there as an HTTP exception raised by the function
    except HTTPException:
        #send it to the browser so that they can debug
        raise
    #if there was an exception in the fucntion itself
    except Exception as e:
        #send it to the browser so that I can debut the code
        raise HTTPException(status_code=500, detail=str(e))

"""
this function is called when the website sends a request for the ship statistics it can be filtered by ship 
name or cruise line but not both

input:
ship_name (optional):   if the user wishes to filter by ship this stores the ship name to be filtered

cruise_line (optional): if the user wishes to filter by cruise line this stores the cruise line to be filtered
NOTE YOU CANNOT FILTER BY BOTH CRUISE LINE AND SHIP NAME AT THE SAME TIME

ouput:                  cruise ship information as a json
throws:                 invalid date exceptions, using 2 filter exceptions or software exceptions
"""
@app.get("/ship")
def get_raw(ship_name: str | None = None,cruise_line: str | None = None):
    #try to get the ship data
    try:
        #get the ship data filtered if requested as a data frame
        df = get_ship_data(ship_name=ship_name,cruise_line=cruise_line)
        #return the data frame as a json string
        return df.to_dicts()
    #if there as an HTTP exception raised by the function
    except HTTPException:
        #send it to the browser so that they can debug
        raise
    #if there was an exception in the fucntion itself
    except Exception as e:
        #send it to the browser so that I can debut the code
        raise HTTPException(status_code=500, detail=str(e))

"""
this runs when fastapi boots up and runs it only once, the point of this function is to perpare the year
list for the initial page when it starts up
"""
@app.on_event("startup")
def init_yearly():
    #create a global variable to store the year data to be access by initialization
    global YEARLY_DATA
    #load the latest year data summary into the global dictionary
    YEARLY_DATA = load_data()

"""
this runs the first time initialization for displaying the website, it provides totals for the whole last
year loaded
output: a dictionary of the whole year summary list for momentum, ship data, curiseline data, global data
"""
@app.get("/initialize")
def get_yearly():
    #return the precomputed Year data as a json dictionary
    return YEARLY_DATA.to_dicts()

"""
this function returns a yearly summary for the entire given  year, it includes ship totals, cruise line 
totals, and global totals which include average momentum and maximum momentum
input: year:  the desired year
output: a dictionary of the whole year summary list for momentum, ship data, curiseline data, global data
"""
@app.get("/year")
def get_yearly(year:str ):
    #load the summary for the given year
    yearly_data = load_data(year)
    #convert the summary to a dictionary
    return yearly_data.to_dicts()


