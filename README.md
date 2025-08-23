# FolderSync — One-Way Folder Synchronization (C#)

A simple C# console app that keeps a **replica** folder fully identical to a **source** folder. After each cycle, the replica’s contents match the source exactly (one-way). Every create/update/delete is logged to **console** and a **log file**.

---

## Features

- One-way sync: `source → replica`
- Periodic sync cycles (interval in seconds)
- Operations:
    - create files (`CREATE`)
    - update/overwrite changed files (`UPDATE`)
    - remove extra files (`DELETE`)
    - create missing directories (`MKDIR`, including empty ones)
    - remove extra directories (`RMDIR`)
- Logs to console **and** file (levels: `INFO`, `SYNC`, `ERROR`, `DEBUG`)
- Change detection: size → mtime (2s tolerance) → MD5

---

## Requirements

- .NET 8 or newer
- Read/write access to the `source` and `replica` paths

---

## Build

```bash
dotnet build
```

## Run
```bash

dotnet run -- \
--source "<PATH_TO_SOURCE>" \
--replica "<PATH_TO_REPLICA>" \
--interval <SECONDS> \
--log "<PATH_TO_LOG_FILE>"
```