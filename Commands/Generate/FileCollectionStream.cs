// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Dox.Commands.Generate;

public class FileCollectionStream : Stream
{
    readonly IEnumerator<string> m_FileEnumerator;
    FileStream m_Stream;

    public FileCollectionStream(IEnumerable<string> files)
    {
        if (files == null)
        {
            m_FileEnumerator = null;
        }
        else
        {
            m_FileEnumerator = files.GetEnumerator();
        }
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (m_FileEnumerator == null)
        {
            return 0;
        }

        if (m_Stream == null && !TryGetNextFileStream(out m_Stream))
        {
            return 0;
        }

        int readed;
        while (true)
        {
            readed = m_Stream.Read(buffer, offset, count);
            if (readed == 0)
            {
                // Dispose current stream before fetching the next one
                m_Stream.Dispose();
                if (!TryGetNextFileStream(out m_Stream))
                {
                    return 0;
                }
            }
            else
            {
                return readed;
            }
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (m_FileEnumerator != null)
            {
                m_FileEnumerator.Dispose();
            }

            if (m_Stream != null)
            {
                m_Stream.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    bool TryGetNextFileStream(out FileStream stream)
    {
        bool next = m_FileEnumerator.MoveNext();
        if (!next)
        {
            stream = null;
            return false;
        }

        stream = new FileStream(m_FileEnumerator.Current, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return true;
    }
}