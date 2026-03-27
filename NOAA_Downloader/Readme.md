# NOAA AIS Data Downloader

This tool downloads daily AIS ZIP files from the NOAA Coast Guard archives.  
It is the first step in my AIS ingestion pipeline for my Advanced Database project.

The downloader retrieves each daily ZIP file, handles retries, and saves the files locally for later extraction and upload.

Only this project is meant to be run directly. The other projects in the solution (Logger, Progress, CommonCore) are internal helper libraries required for the downloader to function.

---

## 🚀 Features

- Downloads all AIS ZIP files for a specified date range
- Automatically retries failed downloads with a cooldown timer
- Skips files that already exist locally
- Logs all operations for transparency
- Uses a clean multi-progress-bar display (via the Progress helper project)

---

## 🖥️ Requirements

- Windows 10 or 11  
- .NET 8 runtime  
- Internet connection  
- Sufficient local disk space (see below)

---

## 📦 Storage Requirements (Actual Measured Values)

Based on real downloaded NOAA AIS data:

- **~99 GB per year (compressed ZIP files)**
- **~370 GB per year (uncompressed CSV files)**

These are not estimates — they are actual measured values from a full AIS dataset.

If downloading multiple years, ensure you have enough space for both:

1. The ZIP files  
2. The extracted CSV files  

Example:  
Downloading **10 years** requires roughly:

- **~990 GB compressed**
- **~3.7 TB uncompressed**

---

## 📁 How to Use
1. Run the executable: \bin\Debug\net8.0-windows\NOAA_Downloader.exe
2. verify the URL to download from has the correct desired year
3. Choose a local folder to store the ZIP files
4. Click **Get Needed File List**
5. The multi-progress-bar UI will show each file’s progress  
- Files already downloaded are skipped automatically  
- Failed downloads trigger a retry countdown
- the downlaod process could take a few days of contious runtime

---

## 🧰 Internal Dependencies

These helper projects are included only so the solution builds:

- **Logger** – logging utilities  
- **Progress** – multi-progress-bar UI  
- **CommonCore** – shared helpers (e.g., time formatting)

They are not standalone applications and intentionally have minimal comments.

---

## 📝 Notes

- The downloader does not extract ZIP files  
- It does not validate AIS content  
- It only downloads files and handles retries  
- Extraction and upload are handled by other tools in this solution




1. Run the executable:
