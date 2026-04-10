import time
from multi_progress_bar import multi_progress_bar
import asyncio
import threading

"""
this class handles a predictive timer, basically it is given a file size and a throughput and computes how
long it will take to complete based on the throughput, it maintains a progress bar which hits 100% when the
time is up, once at 100% it stops, it used for functions that do not provide progress visibility and is meant
to tell the user it is not crashed and give some kind of indication of when it will be done, it can be 
stopped early if the function finishes early it is giving an estimate for, the external function updates
throughput as it runs so it gets more accurate as time goes by
"""
class predictive_timer:
      """
      this function is the constructor for the timer it starts the timer immediately on launch
      it is a constructor so there is no return

      bytes:              how many bytes are in this file used to estimate completion time
      default_throughput: what is the default rate to use to calculate estimated compledtion tiem
      total_bytes:        how many bytes have been processed so far
      total_time:         how much has been the total time spent on processing all those bytes
      file_index:         what file is it on
      total_files:        how many files are there
      """
      def __init__(self, bytes,default_throughput,total_bytes,total_time,file_index,total_files):
          self.default_throughput = default_throughput
          self.bytes = bytes
          self.total_bytes=total_bytes
          self.total_time=total_time
          self.running=True
          self.stopped=False
          #create the timer thread to compute an estimated time to run and upate the progress bar accordingly
          timer_thread = threading.Thread(
                                          target=self.predictive_timer_progress,
                                          args=(bytes, file_index, total_files),
                                          daemon=True
                                          )
          #start the timer
          timer_thread.start()

      """
      this is a timer used to estimate how long the append will take and produce a progress bar for that time
      so that the user does not think the program is stuck as the append can take several minutes

      bytes:       how many bytes are in this file
      file_index:  what current file is it on
      total_files: how many files are there
      """
      def predictive_timer_progress(self,bytes,file_index,total_files):
          #set th running flag to true
          self.running=True
          #get the start time to compute elapsed time later
          start_time = time.time()
          #compute the estimated duration based on the default throughput
          estimated_duration = float(bytes)/self.default_throughput
          #if it has data based on previous runs
          if self.total_bytes!=0:
             #calculate throughput
             average_throughput = float(self.total_bytes)/self.total_time
             #compute the estimated duration based on file size and calculated throughput
             estimated_duration = float(bytes)/average_throughput
          #compute the estimated completion time based on the estimated duration
          estimated_completion_time = start_time+estimated_duration
          #set the last percent to an impossible percent
          last_percent=-1
          #while spark is ingesting a file note when finished spark will clear this flag stopping the loop
          while self.running:
                #get the current time
                now = time.time()
                #if it expected to be done by now
                if now>=estimated_completion_time:
                   #if it is not already at 100%
                   if 100!=last_percent:
                       #diaplay 100%
                       multi_progress_bar.enqueue_progress(1,
                                                           2,
                                                           file_index,
                                                           total_files)
                       #set the last percent to 100% so that it does not update the progress bar again
                       last_percent=100
                       #sleep the thread so the cpu can do other stuff while we wait
                       time.sleep(0.1)
                       #stop loop
                       self.running=False

                #how much time has elapsed since the beginning
                elapsed = now-start_time
                #compute the progress towards the estimated time
                percent = elapsed/estimated_duration
                #if the precent has changed
                if int(percent*100)!=last_percent:
                   #ask the progress bar to update when it can to the current percent
                   multi_progress_bar.enqueue_progress(percent,
                                                       2,
                                                       file_index,
                                                       total_files)
                #else the progress has not been updated
                else:
                    #sleep for again to let the cpu do something else
                    time.sleep(0.005)
                #update the last percent
                last_percent = int(percent*100)

          #the loop is done by the time it gets here
          #compute the elapsed time
          elapsed = time.time()-start_time
          #update the global values for better throughput calculations next time
          self.total_time = self.total_time+elapsed
          self.total_bytes = self.total_bytes+bytes

      def stop_timer(self):
          self.running=False

