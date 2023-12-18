# Scripts

The scripts should be in the following format:

```sql
-- Up
BEGIN TRAN
CREATE TABLE ExampleTable (
    ID INT PRIMARY KEY,
    Data NVARCHAR(100)
)
COMMIT

-- Down
BEGIN TRAN
DROP TABLE ExampleTable
COMMIT
```

Every file should have an **Up** and a **Down** migration.
