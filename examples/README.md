# Examples

`Example.cs` shows three provider-independent entry points:

- write an already executed database command to a JSON array;
- write it to XML;
- enumerate the current `DbDataReader` through the reusable `DbRowView`.

The repository deliberately does not reference a SQL Server, Oracle, MySQL, ODBC, or Ole DB client package. Any ADO.NET provider exposing `DbCommand` / `DbDataReader` can be used by the calling application.
