// SPDX-License-Identifier: Apache-2.0

using System.Data.Common;

namespace MakLib.Data;

public static class DbReaderExtensions
{
    public static DbReaderEnumerable AsRows(this DbDataReader reader, bool leaveOpen = true) =>
        new(reader, leaveOpen);

    public static long WriteJsonTo(this DbDataReader reader, Stream output, bool indented = false, bool leaveReaderOpen = true) =>
        DbReaderSerializer.WriteJson(reader, output, indented, leaveReaderOpen);

    public static long WriteXmlTo(
        this DbDataReader reader,
        Stream output,
        string rootElement = "rows",
        string rowElement = "row",
        bool indented = false,
        bool leaveReaderOpen = true) =>
        DbReaderSerializer.WriteXml(reader, output, rootElement, rowElement, indented, leaveReaderOpen);
}
