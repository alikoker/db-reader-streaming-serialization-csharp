// SPDX-License-Identifier: Apache-2.0

using System.Data;
using System.Text.Json;
using System.Xml.Linq;
using MakLib.Data;

internal static class Program
{
    private static int Main()
    {
        try
        {
            JsonRoundTrip();
            XmlRoundTrip();
            EnumerableReusesRowView();
            Console.WriteLine("All tests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static DataTable CreateTable()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("full_name", typeof(string));
        table.Columns.Add("active", typeof(bool));
        table.Columns.Add("amount", typeof(decimal));
        table.Columns.Add("created_at", typeof(DateTime));
        table.Columns.Add("payload", typeof(byte[]));
        table.Columns.Add("optional_note", typeof(string));

        table.Rows.Add(1, "Muhammet Ali Köker", true, 125.50m,
            new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc),
            new byte[] { 1, 2, 3, 4 }, DBNull.Value);
        table.Rows.Add(2, "Streaming row", false, 0.25m,
            new DateTime(2026, 8, 26, 13, 0, 0, DateTimeKind.Utc),
            new byte[] { 10, 20 }, "ok");
        return table;
    }

    private static void JsonRoundTrip()
    {
        using DataTable table = CreateTable();
        using var reader = table.CreateDataReader();
        using var output = new MemoryStream();

        long rows = DbReaderSerializer.WriteJson(reader, output);
        Assert(rows == 2, "JSON row count mismatch.");

        using JsonDocument document = JsonDocument.Parse(output.ToArray());
        JsonElement array = document.RootElement;
        Assert(array.GetArrayLength() == 2, "JSON array length mismatch.");
        Assert(array[0].GetProperty("id").GetInt32() == 1, "JSON integer mismatch.");
        Assert(array[0].GetProperty("full_name").GetString() == "Muhammet Ali Köker", "JSON string mismatch.");
        Assert(array[0].GetProperty("optional_note").ValueKind == JsonValueKind.Null, "JSON null mismatch.");
    }

    private static void XmlRoundTrip()
    {
        using DataTable table = CreateTable();
        using var reader = table.CreateDataReader();
        using var output = new MemoryStream();

        long rows = DbReaderSerializer.WriteXml(reader, output);
        Assert(rows == 2, "XML row count mismatch.");

        output.Position = 0;
        XDocument document = XDocument.Load(output);
        XElement[] rowElements = document.Root!.Elements("row").ToArray();
        Assert(rowElements.Length == 2, "XML row count mismatch.");
        Assert(rowElements[0].Element("id")!.Value == "1", "XML integer mismatch.");
        Assert(rowElements[0].Element("full_name")!.Value == "Muhammet Ali Köker", "XML string mismatch.");
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        Assert(rowElements[0].Element("optional_note")!.Attribute(xsi + "nil")!.Value == "true", "XML null mismatch.");
    }

    private static void EnumerableReusesRowView()
    {
        using DataTable table = CreateTable();
        using var reader = table.CreateDataReader();
        using var rows = new DbReaderEnumerable(reader);
        using IEnumerator<DbRowView> enumerator = rows.GetEnumerator();

        Assert(enumerator.MoveNext(), "First row missing.");
        DbRowView firstReference = enumerator.Current;
        Assert((int)firstReference["id"]! == 1, "First row mismatch.");

        Assert(enumerator.MoveNext(), "Second row missing.");
        Assert(ReferenceEquals(firstReference, enumerator.Current), "Row view should be reused.");
        Assert((int)firstReference["id"]! == 2, "Reused row view did not advance.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
