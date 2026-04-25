import time
import datetime
import pyarrow.dataset as ds
import pyarrow.fs as fs
import polars as pl

hdfs = fs.HadoopFileSystem("namenode", 9000)

def load_range(start_date: str, end_date: str):
    start = datetime.date.fromisoformat(start_date)
    end = datetime.date.fromisoformat(end_date)

    # Collect all parquet file paths
    files = []
    cur = start
    while cur <= end:
        date_path = f"/data/momentum_raw/event_date={cur.isoformat()}"
        # List files inside the partition
        for info in hdfs.get_file_info(fs.FileSelector(date_path)):
            if info.is_file:
                files.append(info.path)
        cur += datetime.timedelta(days=1)

    # Build dataset from the list of files
    dataset = ds.dataset(files, filesystem=hdfs, format="parquet")

    table = dataset.to_table()
    return pl.from_arrow(table)

def filter_ship(df: pl.DataFrame, ship_name: str):
    return df.filter(pl.col("ShipName") == ship_name)

t0 = time.perf_counter()

df = load_range("2019-12-01", "2019-12-31")
bliss = filter_ship(df, "Norwegian Bliss")

t1 = time.perf_counter()

print(bliss.shape)
print(bliss)
print(f"Elapsed: {t1 - t0:.3f} seconds")
