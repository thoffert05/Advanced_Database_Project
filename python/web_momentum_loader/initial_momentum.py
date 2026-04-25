"""
This File is allows fastapi to get a summary  momentum day entry data for a date range and ship or curise 
line information

Project members: Anthony Hoffert, Able Daniel
"""

import re
import pyarrow.fs as fs
import polars as pl
from pyarrow.fs import HadoopFileSystem
from fastapi import HTTPException
from web_hdfs_interface.hdfs_interface import get_dataset, get_file_listing


BASE = "/data/summary_momentum_yearly"

"""
this function searches for the greatest year loaded into the database for this project it is 2019, however
if this were in production it will probably be the greatest year which is the current year or the end of last
year

returns: the greatest year in the folder
"""
def get_latest_year():
    years = []
    #get folders from HDFS for the yearly root folder the folder names are partitioned by year
    folders = get_file_listing(BASE)
    #for each folder name in the summary_momentum_yearly directory
    for info in folders:
        #get the file/folder name
        name = info.base_name
        #match it to the expected folder name, note the folder could contain _SUCCESS which we don't want
        m = re.match(r"event_year=(\d{4})$", name)
        #if it is a loaded year
        if m:
            #add it to the set of years
            years.append(int(m.group(1)))
    #if no years were found
    if not years:
        #return an error
        raise RuntimeError("No yearly summary partitions found")
    #return the greatest yeasr found
    return max(years)


"""
this function gets  momentum data in the last year of the database it gets the initial data for the website

output: the data frame wiht the summary momentum data for the whole last year in the database
"""
def load_data():
    #files list to store the files to be read
    files=[]
    #while it is within the range
    last_year = get_latest_year()
    #reset the path to the expected partition folder
    source_folder = f"{BASE}/event_year={last_year}"
    #get files for that year
    source_files = get_file_listing(source_folder)
    # List files inside the partition folder
    for info in source_files:
        #if it is a file
        if info.is_file:
           #add it to the list of files to be read
           files.append(info.path)
    #read the days in the range
    polars_data_frame = get_dataset(files)

    #return the polars data frame
    return polars_data_frame

"""
this function gets momentum data for the proided year of the database it gets the initial data for the website
input: year: the year you want the momentum of
output: the data frame wiht the summary momentum data for the whole last year in the database
"""
def load_year(year):
    #files list to store the files to be read
    files=[]
    #reset the path to the expected partition folder
    source_folder = f"{BASE}/event_year={year}"
    #get files for that year
    source_files = get_file_listing(source_folder)
    # List files inside the partition folder
    for info in source_files:
        #if it is a file
        if info.is_file:
           #add it to the list of files to be read
           files.append(info.path)
    #read the days in the range
    polars_data_frame = get_dataset(files)

    #return the polars data frame
    return polars_data_frame
