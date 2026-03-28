import sys
import warnings
import shutil
import threading
from queue import Queue

"""
this class is based on a class I wrote for work in C#, I rewrote it in python here, this class handles
having multiple progress bars in one progress bar at the same time and is useful for tracking progress
of multiple stages at once.  It uses a queue to handle updates asyncrounously and uses a task name to
update the name of what it is currently doing so that user can know, it also has cleanup code for to
remove the bar plus task name upon completion and replace it with the actual results

author: Anthony Hoffert
"""
class multi_progress_bar:
    #stores a queue to store update requests either progress updates, or completion requests
    progress_queue = Queue()
    #stores if it is running so that it knows to check for updates
    running = True
    #stores the current task name to display to keep the user updated
    task=""

    """
    this method starts the monitoring of the progress bar for progress updates, it has no inputs and no 
    outputs
    """
    @staticmethod
    def start_progress_monitoring():
        #start the monitor thread to monitor for progress updates
        threading.Thread(target=multi_progress_bar.progress_worker, daemon=True).start()

    """
    this method handles a request to halt this progress bar since the taks is complete

    success:  was this task completed successfully
    filename: what file was this for?
    """
    @staticmethod
    def halt_progress_for_file(success,filename):
        #add the complete task to the queue
        multi_progress_bar.progress_queue.put((success,filename))

    """
    this method inqueues a progress update to be displayed later

    sub_progress: what is the progress of the current task
    fill_stage:   what stage is it on for the current task, CSV sanitize or CSV ingestion
    item_index:   which file number is it on?
    total_items:  how many total files are there?
    """
    @staticmethod
    def enqueue_progress(sub_progress,fill_stage,item_index,total_items):
       #add the progress update task to the queue
       multi_progress_bar.progress_queue.put((sub_progress,fill_stage,item_index,total_items))

    """
    this function monitors the queue and calls the correct function to update the progress bar appropiately
    it polls every tenth of a second, and determines the number of functoins by the size of the tuple on the
    queue it has no inputs and no outputs rather it relies instead on a class level running flag and a class
    level queue, thie function also has no outputs, it relies on try catch mechanisms to stay running in the
    event of faults
    """
    @staticmethod
    def progress_worker():
         #while it is supposed to be monitoring the queue to update the progress bar
         while multi_progress_bar.running:
            #stores a tuple form the queue
            item=None
            #try to wait for a progress update
            try:
                #wait for an update
                item = multi_progress_bar.progress_queue.get(timeout=0.1)
                #is it has 4 parts then this is a normal progress update
                if len(item)==4:
                   #split it up into progress parts
                   sub_progress, fill_stage, item_index, total_items = item
                   #update the progress bar based on data provided
                   multi_progress_bar.Show_Dual_Progress(
                                                         sub_progress,
                                                         fill_stage,
                                                         item_index,
                                                         total_items
                                                        )
                #else if it does not have 4 parts and only has 2 parts then it is a completion update
                elif len(item)==2:
                     #read the success and filename from the item from the queue
                     success, filename = item
                     #if it was successfull
                     if success:
                        #call the finished success flag
                        finished_success(filename)
                     #else if it was not succesfull
                     else:
                        #call the finished failure flag
                        finished_fail(filename)

            #something went wrong, typically a queue wait timeout which is fine
            except Exception as e:
                # Skip this update but keep thread alive:
                continue

    """
    this is the main function that draws the progress bars as smallest on top of largest
    it is great for montitoring multipel stages at the same time, it displays a task at
    the top so the user knows what it is currently doing and at the end displays all 3
    progresses, current task, current file, overall progress using 3 colors for the progress
    Green for current task, Cyan for current file, and Blue for overall, it also has a background
    of Gray, it returns nothing as this function is pure display

    sub_progress: the progress of the current task: GREEN
    fill_stage:   what stage index is it on for the current file: CYAN
    item_index:   what file is it on overall: BLUE
    total_items:  how many files are there: BLUE
    """
    @staticmethod
    def Show_Dual_Progress(sub_progress,fill_stage,item_index,total_items):
        #overall progress color
        BLUE  = "\033[44m \033[0m"
        #sub_progress color
        GREEN = "\033[42m \033[0m"
        #overall current file color
        CYAN = "\033[46m \033[0m"
        #unused progress color
        GRAY = "\033[47m \033[0m"

        # Get terminal size (rows = bottom row number)
        rows, cols = shutil.get_terminal_size()
        #it is reporting a much smaller column with for the terminal than the display size so 100 is correct
        cols=100
        #[]100% / 100% / 100% = 21 characters
        bar_columns = cols - 21
        #ancor to the column
        sys.stdout.write("\033[1G")
        # Clear current line which is the progress bar line
        sys.stdout.write("\033[2K")
        # Move up one row wich is the task label line
        sys.stdout.write("\033[1A")
        #anchor to the first folumn again
        sys.stdout.write("\033[1G")
        # Clear current line which is the task label line
        sys.stdout.write("\033[2K")
        #go to the start of the row with a carriage return
        sys.stdout.write("\r")
        # Print the task label
        sys.stdout.write(multi_progress_bar.task)
        # Move back down to progress bar line
        sys.stdout.write("\033[1B")
        #ancor to the column
        sys.stdout.write("\033[1G")
        # Clear current line which is the progress bar line
        sys.stdout.write("\033[2K")
        #go to the start of the row with a carriage return
        sys.stdout.write("\r")
        # Draw the start of the progress bar
        sys.stdout.write("[")
        #flush the buffer to the screen
        sys.stdout.flush()
        #compute progress values
        current_file_progress = (sub_progress/2)+(0.5*(fill_stage-1))
        overall_progress = current_file_progress*(1/total_items)+(item_index-1)*(1/total_items)
        #make a list of layers to draw the progres bars in order
        layers = [
                 (GREEN, sub_progress),
                 (CYAN, current_file_progress),
                 (BLUE, overall_progress)
                 ]
        #sort the list form smallest to gretest, smallers are drawn first
        layers.sort(key=lambda x: x[1])
        #compute absoulute lengths
        len1 = int(bar_columns * layers[0][1])
        len2 = int(bar_columns * layers[1][1])
        len3 = int(bar_columns * layers[2][1])
        #comute the lenghts of each bar
        bar1 = len1
        bar2 =  len2-len1
        bar3 = len3 - len2
        remainder = bar_columns-len3
        #draw the bar segments
        sys.stdout.write(layers[0][0]*bar1)  
        sys.stdout.write(layers[1][0]*bar2)
        sys.stdout.write(layers[2][0]*bar3)
        sys.stdout.write(GRAY*remainder)
        #flush the buffer to the screen
        sys.stdout.flush()
        #compute the sub item progress as a readable precentage value
        current_progress = int(sub_progress*100)
        #compute the file progress as a readable precentage value
        file_progress = int(current_file_progress*100)
        #compute the overall file progress as a readable percentage value
        overall_prog = int(overall_progress*100)
        #close the progress bar
        sys.stdout.write(']')
        #write the sub progress value
        sys.stdout.write(str(current_progress))
        #add the precent symbol and move to the next percent
        sys.stdout.write('% / ')
        #draw the file progress percent
        sys.stdout.write(str(file_progress))
        #add the percent symbol and move to the next percent
        sys.stdout.write('% / ')
        #draw the overall progress
        sys.stdout.write(str(overall_prog))
        #add the precent symbol
        sys.stdout.write('%')
        #flush it all from the buffer to the screen terminal
        sys.stdout.flush()

    """
    this function clears the progress bar which is the row above, status display, and replaces it with the 
    given text it is basically a clean up function.  it is display only and has no return

    text: text to be displayed in place of task name and progress bar
    """
    @staticmethod
    def Clear_Progress_bar_and_status_and_replace_with_text(text):
        #ancor to the column
        sys.stdout.write("\033[1G")
        # Clear current line
        sys.stdout.write("\033[2K")
        # Move up one row 
        sys.stdout.write("\033[1A")
        #anchor to the first column again
        sys.stdout.write("\033[1G")
        # Clear current line
        sys.stdout.write("\033[2K")
        #go to the start of the row with a carriage return
        sys.stdout.write("\r")
        # Move up one row wich is the progress bar line
        sys.stdout.write("\033[1A")
        #anchor to the first column again
        sys.stdout.write("\033[1G")
        # Clear current line which is the progress bar line
        sys.stdout.write("\033[2K")
        # Move up one row wich is the task label line
        sys.stdout.write("\033[1A")
        #anchor to the first column again
        sys.stdout.write("\033[1G")
        # Clear current line which is the task label line
        sys.stdout.write("\033[2K")
        # Print the task label
        sys.stdout.write(text)
        #go to the next line for the next print statement
        print()

    """
    this function handles a finished success request and clears the progress bar and prints a success message
    with the file name

    file_name:  name of the file that was successfully ingested
    """
    @staticmethod
    def finished_success(file_name):
        #clear the progress bar and replace the label that it was ingested successfully
        multi_progress_bar.Clear_Progress_bar_and_status_and_replace_with_text(f"ingested {file_name} successfully")

    """   
    this function handles a finished failure request and clears the progress bar and prints a success message
    with the file name

    file_name:  name of the file that was not successfully ingested
    """
    @staticmethod
    def finished_fail(file_name):
        #clear the progress bar and replace the label that it faile to be ingested
        multi_progress_bar.Clear_Progress_bar_and_status_and_replace_with_text(f"failed to ingest {file_name}")

    """
    this function stops progress bar monitoring to stop the total ingestion script successfully and free
    resources
    """
    @staticmethod
    def stop_monitoring_progress():
        #clear the running flag which will terminate the running loop as soon as it is done processing
        #what it is working on 
        multi_progress_bar.running = False
