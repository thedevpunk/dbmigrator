# Migrations Table

This table is there for tracking the migrations.

```sql
CREATE TABLE Migrations(
    Id INT IDENTITY PRIMARY KEY,
    ScriptName NVARCHAR(255),
    Applied DATETIME DEFAULT GETDATE()
)
```
