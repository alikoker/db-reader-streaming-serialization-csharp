// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Data.Common;
using System.Threading;

namespace MakLib.Data;

/// <summary>
/// Exposes a forward-only <see cref="DbDataReader"/> as a single-pass enumerable.
/// One <see cref="DbRowView"/> instance is reused for every row to avoid per-row
/// dictionary allocation.
/// </summary>
public sealed class DbReaderEnumerable : IEnumerable<DbRowView>, IDisposable
{
    private readonly DbDataReader _reader;
    private readonly DbReaderSchema _schema;
    private readonly bool _leaveOpen;
    private int _started;
    private int _readerClosed;
    private bool _disposed;

    public DbReaderEnumerable(DbDataReader reader, bool leaveOpen = true)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _leaveOpen = leaveOpen;
        _schema = DbReaderSchema.Create(reader);
    }

    public IEnumerator<DbRowView> GetEnumerator()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DbReaderEnumerable));
        }
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("DbReaderEnumerable is single-pass and can only be enumerated once.");
        }

        return new Enumerator(this, _reader, _schema);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CloseReaderOnce();
    }

    private void CloseReaderOnce()
    {
        if (!_leaveOpen && Interlocked.Exchange(ref _readerClosed, 1) == 0)
        {
            _reader.Dispose();
        }
    }

    private sealed class Enumerator : IEnumerator<DbRowView>
    {
        private readonly DbReaderEnumerable _owner;
        private readonly DbDataReader _reader;
        private readonly DbRowView _row;
        private bool _disposed;
        private bool _hasCurrent;

        public Enumerator(DbReaderEnumerable owner, DbDataReader reader, DbReaderSchema schema)
        {
            _owner = owner;
            _reader = reader;
            _row = new DbRowView(reader, schema);
        }

        public DbRowView Current
        {
            get
            {
                if (!_hasCurrent)
                {
                    throw new InvalidOperationException("The enumerator is not positioned on a row.");
                }

                return _row;
            }
        }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Enumerator));
            }
            bool result = _reader.Read();
            _hasCurrent = result;
            _row.SetActive(result);
            return result;
        }

        public void Reset() => throw new NotSupportedException("DbDataReader is forward-only.");

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _hasCurrent = false;
            _row.SetActive(false);
            _owner.CloseReaderOnce();
        }
    }
}
