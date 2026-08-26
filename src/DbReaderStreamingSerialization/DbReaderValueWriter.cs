// SPDX-License-Identifier: Apache-2.0

using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using System.Xml;

namespace MakLib.Data;

internal static class DbReaderValueWriter
{
    public static void WriteJson(Utf8JsonWriter writer, DbDataReader reader, int ordinal, DbValueKind kind)
    {
        if (reader.IsDBNull(ordinal))
        {
            writer.WriteNullValue();
            return;
        }

        switch (kind)
        {
            case DbValueKind.String: writer.WriteStringValue(reader.GetString(ordinal)); return;
            case DbValueKind.Boolean: writer.WriteBooleanValue(reader.GetBoolean(ordinal)); return;
            case DbValueKind.Byte: writer.WriteNumberValue(reader.GetByte(ordinal)); return;
            case DbValueKind.SByte: writer.WriteNumberValue(reader.GetFieldValue<sbyte>(ordinal)); return;
            case DbValueKind.Int16: writer.WriteNumberValue(reader.GetInt16(ordinal)); return;
            case DbValueKind.UInt16: writer.WriteNumberValue(reader.GetFieldValue<ushort>(ordinal)); return;
            case DbValueKind.Int32: writer.WriteNumberValue(reader.GetInt32(ordinal)); return;
            case DbValueKind.UInt32: writer.WriteNumberValue(reader.GetFieldValue<uint>(ordinal)); return;
            case DbValueKind.Int64: writer.WriteNumberValue(reader.GetInt64(ordinal)); return;
            case DbValueKind.UInt64: writer.WriteNumberValue(reader.GetFieldValue<ulong>(ordinal)); return;
            case DbValueKind.Single: writer.WriteNumberValue(reader.GetFloat(ordinal)); return;
            case DbValueKind.Double: writer.WriteNumberValue(reader.GetDouble(ordinal)); return;
            case DbValueKind.Decimal: writer.WriteNumberValue(reader.GetDecimal(ordinal)); return;
            case DbValueKind.DateTime: writer.WriteStringValue(reader.GetDateTime(ordinal)); return;
            case DbValueKind.DateTimeOffset: writer.WriteStringValue(reader.GetFieldValue<DateTimeOffset>(ordinal)); return;
            case DbValueKind.DateOnly: writer.WriteStringValue(reader.GetFieldValue<DateOnly>(ordinal).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); return;
            case DbValueKind.TimeOnly: writer.WriteStringValue(reader.GetFieldValue<TimeOnly>(ordinal).ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture)); return;
            case DbValueKind.TimeSpan: writer.WriteStringValue(reader.GetFieldValue<TimeSpan>(ordinal).ToString("c", CultureInfo.InvariantCulture)); return;
            case DbValueKind.Guid: writer.WriteStringValue(reader.GetGuid(ordinal)); return;
            case DbValueKind.Char: writer.WriteStringValue(reader.GetFieldValue<char>(ordinal).ToString()); return;
            case DbValueKind.ByteArray: writer.WriteBase64StringValue(reader.GetFieldValue<byte[]>(ordinal)); return;
            default:
                object value = reader.GetValue(ordinal);
                writer.WriteStringValue(FormatFallback(value));
                return;
        }
    }

    public static void WriteXml(XmlWriter writer, DbDataReader reader, int ordinal, DbValueKind kind)
    {
        if (reader.IsDBNull(ordinal))
        {
            writer.WriteAttributeString("xsi", "nil", DbReaderSerializer.XmlSchemaInstanceNamespace, "true");
            return;
        }

        switch (kind)
        {
            case DbValueKind.String: writer.WriteString(reader.GetString(ordinal)); return;
            case DbValueKind.Boolean: writer.WriteString(reader.GetBoolean(ordinal) ? "true" : "false"); return;
            case DbValueKind.Byte: writer.WriteString(reader.GetByte(ordinal).ToString(CultureInfo.InvariantCulture)); return;
            case DbValueKind.SByte: writer.WriteString(reader.GetFieldValue<sbyte>(ordinal).ToString(CultureInfo.InvariantCulture)); return;
            case DbValueKind.Int16: writer.WriteString(reader.GetInt16(ordinal).ToString(CultureInfo.InvariantCulture)); return;
            case DbValueKind.UInt16: writer.WriteString(reader.GetFieldValue<ushort>(ordinal).ToString(CultureInfo.InvariantCulture)); return;
            case DbValueKind.Int32: writer.WriteString(reader.GetInt32(ordinal).ToString(CultureInfo.InvariantCulture)); return;
            case DbValueKind.UInt32: writer.WriteString(reader.GetFieldValue<uint>(ordinal).ToString(CultureInfo.InvariantCulture)); return;
            case DbValueKind.Int64: writer.WriteString(reader.GetInt64(ordinal).ToString(CultureInfo.InvariantCulture)); return;
            case DbValueKind.UInt64: writer.WriteString(reader.GetFieldValue<ulong>(ordinal).ToString(CultureInfo.InvariantCulture)); return;
            case DbValueKind.Single: writer.WriteString(reader.GetFloat(ordinal).ToString("R", CultureInfo.InvariantCulture)); return;
            case DbValueKind.Double: writer.WriteString(reader.GetDouble(ordinal).ToString("R", CultureInfo.InvariantCulture)); return;
            case DbValueKind.Decimal: writer.WriteString(reader.GetDecimal(ordinal).ToString(CultureInfo.InvariantCulture)); return;
            case DbValueKind.DateTime: writer.WriteString(XmlConvert.ToString(reader.GetDateTime(ordinal), XmlDateTimeSerializationMode.RoundtripKind)); return;
            case DbValueKind.DateTimeOffset: writer.WriteString(reader.GetFieldValue<DateTimeOffset>(ordinal).ToString("O", CultureInfo.InvariantCulture)); return;
            case DbValueKind.DateOnly: writer.WriteString(reader.GetFieldValue<DateOnly>(ordinal).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); return;
            case DbValueKind.TimeOnly: writer.WriteString(reader.GetFieldValue<TimeOnly>(ordinal).ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture)); return;
            case DbValueKind.TimeSpan: writer.WriteString(XmlConvert.ToString(reader.GetFieldValue<TimeSpan>(ordinal))); return;
            case DbValueKind.Guid: writer.WriteString(reader.GetGuid(ordinal).ToString("D")); return;
            case DbValueKind.Char: writer.WriteString(reader.GetFieldValue<char>(ordinal).ToString()); return;
            case DbValueKind.ByteArray:
                byte[] bytes = reader.GetFieldValue<byte[]>(ordinal);
                writer.WriteBase64(bytes, 0, bytes.Length);
                return;
            default:
                writer.WriteString(FormatFallback(reader.GetValue(ordinal)));
                return;
        }
    }

    private static string FormatFallback(object value)
    {
        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return value.ToString() ?? string.Empty;
    }
}
