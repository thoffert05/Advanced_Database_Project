"""
This File is allows poral to access HDFS so that it can query the database and return json files to fast for 
vercel to display what was requeisted.  this is responsible for taking hdfs file paths given and returning
them as a table

Project members: Anthony Hoffert, Able Daniel
"""

import pyarrow.dataset as ds
import pyarrow.fs as fs
import polars as pl

#connection to HDFS
hdfs = fs.HadoopFileSystem("namenode", 9000)

"""
This function creates a table from the file paths given in HDFS

input: HDFS parquet files to be read
ouput: the files in a combined table
"""
def get_dataset(files):
    #for defensive codeing if no files are given
    if not files:
        #return an empty table instead of crashing
        return pl.DataFrame() 

    # Build dataset from the list of files
    dataset = ds.dataset(files, filesystem=hdfs, format="parquet")
    #convert the dataset to a table
    table = dataset.to_table()
    #return the dataset as a table
    return pl.from_arrow(table)

"""
This function gets all file names form HDFS in the HDFS root folder provided
THIS FUNCTION WAS WRITTEN BY COPILOT

input: root_folder the hdfs folder you want the file names from
ouput: all files in that folder
"""
def get_file_listing(root_folder):
    selector = fs.FileSelector(root_folder, recursive=False)
    entries = hdfs.get_file_info(selector)
    return entries
