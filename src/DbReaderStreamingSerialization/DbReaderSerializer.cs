// SPDX-License-Identifier: Apache-2.0

using System.Data.Common;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace MakLib.Data;

/// <summary>
/// Writes a forward-only <see cref="DbDataReader"/> directly to JSON or XML without
/// first materializing rows into a DataTable, DTO list, or dictionary-per-row graph.
/// </summary>
public static class DbReaderSerializer
{
    internal const string XmlSchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    public static long WriteJson(
        DbDataReader reader,
        Stream output,
        bool indented = false,
        bool leaveReaderOpen = true)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(output);
        if (!output.CanWrite) throw new ArgumentException("The output stream must be writable.", nameof(output));

        long rowCount = 0;

        try
        {
            DbReaderSchema schema = DbReaderSchema.Create(reader);
            using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = indented });
            writer.WriteStartArray();

            while (reader.Read())
            {
                writer.WriteStartObject();
                for (int i = 0; i < schema.Count; i++)
                {
                    writer.WritePropertyName(schema.JsonNames[i]);
                    DbReaderValueWriter.WriteJson(writer, reader, i, schema.ValueKinds[i]);
                }
                writer.WriteEndObject();
                rowCount++;
            }

            writer.WriteEndArray();
            writer.Flush();
            return rowCount;
        }
        finally
        {
            if (!leaveReaderOpen)
            {
                reader.Dispose();
            }
        }
    }

    public static long WriteXml(
        DbDataReader reader,
        Stream output,
        string rootElement = "rows",
        string rowElement = "row",
        bool indented = false,
        bool leaveReaderOpen = true)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(output);
        if (!output.CanWrite) throw new ArgumentException("The output stream must be writable.", nameof(output));

        string rootName = XmlConvert.EncodeLocalName(string.IsNullOrWhiteSpace(rootElement) ? "rows" : rootElement);
        string rowName = XmlConvert.EncodeLocalName(string.IsNullOrWhiteSpace(rowElement) ? "row" : rowElement);
        long rowCount = 0;

        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = indented,
            CloseOutput = false,
            OmitXmlDeclaration = false
        };

        try
        {
            DbReaderSchema schema = DbReaderSchema.Create(reader);
            using XmlWriter writer = XmlWriter.Create(output, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement(rootName);
            writer.WriteAttributeString("xmlns", "xsi", null, XmlSchemaInstanceNamespace);

            while (reader.Read())
            {
                writer.WriteStartElement(rowName);
                for (int i = 0; i < schema.Count; i++)
                {
                    writer.WriteStartElement(schema.XmlNames[i]);
                    DbReaderValueWriter.WriteXml(writer, reader, i, schema.ValueKinds[i]);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                rowCount++;
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            return rowCount;
        }
        finally
        {
            if (!leaveReaderOpen)
            {
                reader.Dispose();
            }
        }
    }
}
