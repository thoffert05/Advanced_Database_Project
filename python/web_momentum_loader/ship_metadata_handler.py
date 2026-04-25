"""
This File is allows fastapi to get the list of cruise ships filtered if desired by cruise ship name or by 
cruise line

Project members: Anthony Hoffert, Able Daniel
"""
import pyarrow.fs as fs
import polars as pl
from pyarrow.fs import HadoopFileSystem
from fastapi import HTTPException
from web_hdfs_interface.hdfs_interface import get_dataset
from web_hdfs_interface.hdfs_interface import get_file_listing

#create hadoop connection
hdfs = HadoopFileSystem(
    host="namenode",
    port=9000,
    user="hadoopuser"
)

#file path to the cruise dataset
base_directory = "/data/cruise"
#get the files in the cruise directory
files = [
    info.path
    for info in get_file_listing(base_directory)
    if info.is_file and info.base_name.endswith(".parquet")
]
#load the cruise data set load it on import to avoid the need to laod it on every call
cruise_data_set = get_dataset(files)

"""
this takes the loaded data set of cruise ship stats and returns it filtered if necessary to the caller

ship_name (optional):      this is used to filter out a single ship if desired
cruise_line (optional):    this is sued to filter out all the ships of a single cruise line if desired
YOU CANNOT USE BOTH FILTERS AT THE SAMETIME!!!

returns:     the data set containing the cruise ship information which is filtered if desired
"""
def get_ship_data(ship_name:str|None=None,cruise_line:str|None=None):

    #make seperate data set copy to allow filtering without affecting the master
    cruise_set = cruise_data_set
    #I only support either a cruise line filter or a ship filter but not both so if both are given
    if ship_name and cruise_line:
       #throw an exception back to the browser
       raise HTTPException(
                           status_code=400,
                           detail="Specify either ship or cruise_line, not both."
                           )
    #if they want to filter it by ship name
    if ship_name:
       #filter just the ship name
       cruise_set = cruise_set.filter(pl.col("ShipName") == ship_name)
    #if they wish to filter it by cruise line
    if cruise_line:
       #filter jsut the cruise line
       cruise_set = cruise_set.filter(pl.col("CruiseLine") == cruise_line)
    #return the cruise dataset
    return cruise_set
