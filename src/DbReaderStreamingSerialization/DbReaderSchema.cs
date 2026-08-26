// SPDX-License-Identifier: Apache-2.0

using System.Data.Common;
using System.Text.Json;
using System.Xml;

namespace MakLib.Data;

internal sealed class DbReaderSchema
{
    private DbReaderSchema(
        string[] names,
        JsonEncodedText[] jsonNames,
        string[] xmlNames,
        Type[] fieldTypes,
        DbValueKind[] valueKinds,
        Dictionary<string, int> ordinals)
    {
        Names = names;
        JsonNames = jsonNames;
        XmlNames = xmlNames;
        FieldTypes = fieldTypes;
        ValueKinds = valueKinds;
        Ordinals = ordinals;
    }

    public string[] Names { get; }
    public JsonEncodedText[] JsonNames { get; }
    public string[] XmlNames { get; }
    public Type[] FieldTypes { get; }
    public DbValueKind[] ValueKinds { get; }
    public Dictionary<string, int> Ordinals { get; }
    public int Count => Names.Length;

    public static DbReaderSchema Create(DbDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        int count = reader.FieldCount;
        var names = new string[count];
        var jsonNames = new JsonEncodedText[count];
        var xmlNames = new string[count];
        var fieldTypes = new Type[count];
        var valueKinds = new DbValueKind[count];
        var ordinals = new Dictionary<string, int>(count, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < count; i++)
        {
            string name = reader.GetName(i);
            if (string.IsNullOrEmpty(name))
            {
                name = "column_" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (!ordinals.TryAdd(name, i))
            {
                throw new InvalidOperationException(
                    "Duplicate database column names are not supported. Use explicit SQL aliases. Duplicate: " + name);
            }

            Type fieldType = reader.GetFieldType(i) ?? typeof(object);
            names[i] = name;
            jsonNames[i] = JsonEncodedText.Encode(name);
            xmlNames[i] = XmlConvert.EncodeLocalName(name);
            fieldTypes[i] = fieldType;
            valueKinds[i] = Classify(fieldType);
        }

        return new DbReaderSchema(names, jsonNames, xmlNames, fieldTypes, valueKinds, ordinals);
    }

    private static DbValueKind Classify(Type type)
    {
        if (type == typeof(string)) return DbValueKind.String;
        if (type == typeof(bool)) return DbValueKind.Boolean;
        if (type == typeof(byte)) return DbValueKind.Byte;
        if (type == typeof(sbyte)) return DbValueKind.SByte;
        if (type == typeof(short)) return DbValueKind.Int16;
        if (type == typeof(ushort)) return DbValueKind.UInt16;
        if (type == typeof(int)) return DbValueKind.Int32;
        if (type == typeof(uint)) return DbValueKind.UInt32;
        if (type == typeof(long)) return DbValueKind.Int64;
        if (type == typeof(ulong)) return DbValueKind.UInt64;
        if (type == typeof(float)) return DbValueKind.Single;
        if (type == typeof(double)) return DbValueKind.Double;
        if (type == typeof(decimal)) return DbValueKind.Decimal;
        if (type == typeof(DateTime)) return DbValueKind.DateTime;
        if (type == typeof(DateTimeOffset)) return DbValueKind.DateTimeOffset;
        if (type == typeof(DateOnly)) return DbValueKind.DateOnly;
        if (type == typeof(TimeOnly)) return DbValueKind.TimeOnly;
        if (type == typeof(TimeSpan)) return DbValueKind.TimeSpan;
        if (type == typeof(Guid)) return DbValueKind.Guid;
        if (type == typeof(char)) return DbValueKind.Char;
        if (type == typeof(byte[])) return DbValueKind.ByteArray;
        return DbValueKind.Object;
    }
}
