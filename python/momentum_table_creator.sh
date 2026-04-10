#!/bin/bash
#Based on ais_counts.sh which was MADE WITH COPILOT
/opt/spark/bin/spark-submit \
  --master yarn \
  --deploy-mode client \
  momentum_table_creator.py

