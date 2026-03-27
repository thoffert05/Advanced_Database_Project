import sys
import io
from datetime import datetime

"""
this class is similar to one I wrote for work but not as complex it is a basic global logger class a file
name is passed in and logs are passed in and it prepends a time stamp and addes it to a file and saves the
file, this version has no log type it is just a simple log
"""
class logger():
     #stores the log information
     log=""
     #where is the log saved
     output_path=""

     """
     this function sets the output file path, 

     path:    where the log should be saved
     returns: nothing it simply prints where the lgo will be saved
     """
     @staticmethod
     def set_output_file_path(path):
         #print the log  file path
         print(f"log path {path}")
         #i there is already a log file and a log
         if logger.log != "" and logger.output_path!="":
            #log the new file path in the old log as that log is no longer going to be updated
            log_item(f"Changed log path to {path}")
         #set the file path
         logger.output_path = path

     """
     this function saves the log to the preset log path there are no returns
     """
     @staticmethod
     def save_log():
         #open the file
         with open(logger.output_path, "w") as f:
              #write the log to the file
              f.write(logger.log)

     """
     this function adds a line to the log prepended with time stamp  and saves the log, returns nothing

     log_entry: the item to add to the log prepended with a timestamp
     """
     @staticmethod
     def log_item(log_entry):
         #prepend the timestamp in front of the lot entry
         log_entry = datetime.now().strftime("%H:%M:%S")+":" + log_entry
         #append the log entry to the existing log with a new line to the end
         logger.log = logger.log + log_entry+"\r\n"
         #save the log
         logger.save_log()

