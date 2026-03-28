@echo off
setlocal enabledelayedexpansion

set SOURCE=C:\AIS_2019
set DEST=C:\Temp\AIS_2019

echo Creating destination folder if it doesn't exist...
if not exist "%DEST%" mkdir "%DEST%"

echo.
echo Extracting CSV files from all ZIPs...
echo.

for %%F in ("%SOURCE%\*.zip") do (
    echo Processing %%~nxF ...
    powershell -command "Expand-Archive -LiteralPath '%%F' -DestinationPath '%DEST%\_temp' -Force"
    
    rem Move only CSV files
    for %%C in ("%DEST%\_temp\*.csv") do (
        move "%%C" "%DEST%" >nul
    )

    rem Clean up temp folder
    rmdir /s /q "%DEST%\_temp"
)

echo.
echo Done. All CSV files extracted to %DEST%.
pause