// SPDX-License-Identifier: Apache-2.0

using System.Data;
using System.Data.Common;
using MakLib.Data;

public static class Example
{
    public static void ExportJson(DbCommand command, Stream output)
    {
        using DbDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
        DbReaderSerializer.WriteJson(reader, output, leaveReaderOpen: true);
    }

    public static void ExportXml(DbCommand command, Stream output)
    {
        using DbDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
        DbReaderSerializer.WriteXml(reader, output, rootElement: "result", rowElement: "record");
    }

    public static void InspectRows(DbDataReader reader)
    {
        using var rows = reader.AsRows(leaveOpen: true);
        foreach (DbRowView row in rows)
        {
            Console.WriteLine(row["id"]);
        }
    }
}
