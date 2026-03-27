#!/bin/bash

# Name of the zip file
ZIPNAME="python.zip"

# Zip everything in the current directory and place it in the parent directory
zip -r "../$ZIPNAME" .
