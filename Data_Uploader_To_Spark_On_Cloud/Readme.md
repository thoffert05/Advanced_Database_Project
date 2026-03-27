# Data Uploader to Spark on Cloud

This is a lightweight Windows utility that uploads extracted AIS CSV files to a Google Cloud Storage bucket. It is part of my Advanced Database project and is used after downloading and extracting NOAA AIS data.

Only this project and the NOAA_Downloader project are meant to be run directly. The other projects in the solution (Logger, Progress, CommonCore) are internal helper libraries required for the uploader and downloader to function.

---

## 🚀 Features

- Uploads all CSV files from a selected local folder
- Skips files that already exist in the bucket
- Uses a clean multi-progress-bar display
- Logs all operations for transparency
- Requires no installation — just run Data_Uploader_To_Spark_On_Cloud.exe

---

## 🖥️ Requirements

- Windows 10 or 11  
- .NET 8 runtime  
- Google Cloud SDK installed  
- Application Default Credentials (ADC) configured  
- A Google Cloud Storage bucket  
- Internet connection

---

## 📦 How to Use

1. Run the executable:
