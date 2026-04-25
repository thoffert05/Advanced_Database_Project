from pyspark.sql import SparkSession
from pyspark.sql.functions import to_date
#MADE WITH COPILOT
spark = SparkSession.builder.appName("AISCounts").getOrCreate()

df = spark.read.parquet("/data/ais")

result = df.groupBy(to_date("BaseDateTime").alias("date")) \
           .count() \
           .orderBy("date")

result.show(result.count(), truncate=False)


spark.stop()
