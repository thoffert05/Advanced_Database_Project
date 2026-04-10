#!/bin/bash
#MADE WITH COPILOT
/opt/spark/bin/spark-submit \
  --master yarn \
  --deploy-mode client \
  ais_counts.py

