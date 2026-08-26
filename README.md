# DbReader Streaming Serialization for C#

A provider-independent, forward-only C# helper for exposing `DbDataReader` rows as a reusable enumerable view and writing database results directly to JSON or XML without materializing a `DataTable`, DTO list, or dictionary per row.

## Design

The repository preserves the useful idea in Muhammet Ali Köker's historical database-enumerable source while tightening its contract for publication:

- `DbDataReader` replaces provider-specific reader types;
- `DbRowView` is read-only;
- the enumerable is explicitly single-pass;
- one row-view instance is reused across successive records;
- schema metadata is cached once;
- JSON property names are pre-encoded as `JsonEncodedText`;
- XML element names are normalized once;
- common CLR database types use typed getters;
- JSON and XML can be written directly from the reader with no intermediate result graph.

## Fast path

```text
DbDataReader
    -> one-time schema cache
    -> Read()
    -> typed field getter
    -> Utf8JsonWriter / XmlWriter
    -> Stream
```

This design targets lower allocation and bounded auxiliary memory. No benchmark multiplier is claimed without a reproducible provider/query/data benchmark.

## JSON

```csharp
using DbDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
long rows = DbReaderSerializer.WriteJson(reader, outputStream);
```

## XML

```csharp
using DbDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
long rows = DbReaderSerializer.WriteXml(reader, outputStream);
```

Database `NULL` values become JSON `null` and XML `xsi:nil="true"`.

## Enumerable row view

```csharp
using var rows = reader.AsRows();
foreach (DbRowView row in rows)
{
    Console.WriteLine(row["id"]);
}
```

`DbRowView` is deliberately ephemeral. The same instance is reused after each `MoveNext()`. Do not retain it after advancing the reader. Take an explicit snapshot if durable row objects are required.

## Provider independence

The core references only ADO.NET base classes. SQL Server, Oracle, MySQL, PostgreSQL, ODBC, Ole DB, or another provider can be used by the calling application as long as it exposes `DbCommand` / `DbDataReader`.

The library deliberately does not accept connection strings or raw SQL. Parameterization, authorization, connection lifecycle, transaction policy, and credential security belong to the calling application.

## Type handling

Native JSON/XML handling is included for common CLR types including numeric primitives, booleans, strings, dates/times, GUIDs, and byte arrays. Unknown provider-specific CLR values use an invariant string fallback.

Very large BLOB/CLOB columns are outside the optimized scalar-field contract. Provider-level `GetStream` / `GetTextReader` pipelines are preferable for multi-megabyte or multi-gigabyte LOB export.

## Relationship to earlier work

The architecture follows the same principle as the author's earlier enumerable CSV implementation: precompute stable schema information once and keep the repeated row path short.

- Turkish: https://alikoker.com.tr/enumerable-csv-isleme-algoritmasi
- English: https://alikoker.com.tr/en/enumerable-csv-processing-algorithm

## Related articles

- English canonical article: https://alikoker.com.tr/en/streaming-json-xml-serialization-from-dbdatareader
- Turkish article: https://alikoker.com.tr/dbdatareader-akiskan-json-xml-serilestirme

The repository archive intentionally does not embed a `docs` copy of the articles. The website remains the canonical article location.

## Validation

The repository includes dependency-free tests built on `DataTableReader`, covering:

- JSON scalar typing and `NULL`;
- XML values and `xsi:nil`;
- reusable row-view behavior.

No external database is required for these deterministic tests.

## License

Apache License 2.0.

## Author

Muhammet Ali Köker  
https://alikoker.com.tr/
