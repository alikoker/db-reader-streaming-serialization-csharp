// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Data.Common;

namespace MakLib.Data;

/// <summary>
/// A read-only view over the current row of a <see cref="DbDataReader"/>.
/// The same instance can be reused for successive rows; callers must not retain it
/// after the enumerator advances.
/// </summary>
public sealed class DbRowView : IReadOnlyDictionary<string, object?>
{
    private readonly DbDataReader _reader;
    private readonly DbReaderSchema _schema;
    private bool _active;

    internal DbRowView(DbDataReader reader, DbReaderSchema schema)
    {
        _reader = reader;
        _schema = schema;
    }

    public int Count => _schema.Count;
    public IEnumerable<string> Keys => _schema.Names;
    public IEnumerable<object?> Values => EnumerateValues();

    public object? this[string key]
    {
        get
        {
            EnsureActive();
            if (!_schema.Ordinals.TryGetValue(key, out int ordinal))
            {
                throw new KeyNotFoundException(key);
            }

            return ReadValue(ordinal);
        }
    }

    public bool ContainsKey(string key) => _schema.Ordinals.ContainsKey(key);

    public bool TryGetValue(string key, out object? value)
    {
        EnsureActive();
        if (_schema.Ordinals.TryGetValue(key, out int ordinal))
        {
            value = ReadValue(ordinal);
            return true;
        }

        value = null;
        return false;
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        EnsureActive();
        for (int i = 0; i < _schema.Count; i++)
        {
            yield return new KeyValuePair<string, object?>(_schema.Names[i], ReadValue(i));
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void SetActive(bool active) => _active = active;

    private IEnumerable<object?> EnumerateValues()
    {
        EnsureActive();
        for (int i = 0; i < _schema.Count; i++)
        {
            yield return ReadValue(i);
        }
    }

    private object? ReadValue(int ordinal)
    {
        if (_reader.IsDBNull(ordinal))
        {
            return null;
        }

        return _reader.GetValue(ordinal);
    }

    private void EnsureActive()
    {
        if (!_active)
        {
            throw new InvalidOperationException(
                "The row view is only valid while the enumerator is positioned on a current database row.");
        }
    }
}
