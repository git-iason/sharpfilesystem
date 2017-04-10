using System;
using System.IO;

namespace SharpFileSystem.SharpZipLib
{
    /// <summary>
    /// SeekStream allows seeking on non-seekable streams by buffering read data. 
    /// </summary>
    public class SeekStream : Stream
    {
        private readonly  Stream _baseStream;
        private readonly MemoryStream _innerStream;
        private readonly int _bufferSize = 64 * 1024;
        private readonly byte[] _buffer;

        public delegate void DataWrittenHandler();
        public DataWrittenHandler DataWritten;
        
        public SeekStream(Stream baseStream)
            : this(baseStream, 64 * 1024)
        {
        }

        public SeekStream(Stream baseStream, int bufferSize) : base()
        {
            _baseStream = baseStream;
            _bufferSize = bufferSize;
            _buffer = new byte[_bufferSize];
            _innerStream = new MemoryStream();
        }

        public override bool CanRead
        {
            get { return _baseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _baseStream.CanRead; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Position
        {
            get { return _innerStream.Position; }
            set
            {
                if (value > _baseStream.Position)
                    FastForward(value);
                _innerStream.Position = value;
            }
        }

        public Stream BaseStream
        {
            get { return _baseStream; }
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        private void FastForward(long position = -1)
        {
            while ((position == -1 || position > this.Length) && ReadChunk() > 0)
            {
                // fast-forward
            }
        }

        private int ReadChunk()
        {
            int thisRead, read = 0;
            long pos = _innerStream.Position;
            do
            {
                thisRead = _baseStream.Read(_buffer, 0, _bufferSize - read);
                _innerStream.Write(_buffer, 0, thisRead);
                read += thisRead;
            } while (read < _bufferSize && thisRead > 0);
            _innerStream.Position = pos;
            return read;
        }
        private void FastForwardWrite(long position = -1)
        {
            _innerStream.Position = 0;
            while ((position == -1 || position > this.Length||true) && WriteChunk() > 0)
            {
                // fast-forward
            }
        }
        private int WriteChunk()
        {
            int thisWrite, write = 0;
            long pos = _baseStream.Position;

            do
            {
                thisWrite= _innerStream.Read(_buffer, 0, _bufferSize - write);
                _baseStream.Write(_buffer, 0, thisWrite);
                
                write += thisWrite;
            } while (write < _bufferSize && thisWrite > 0);
            _baseStream.Position = pos;
            return write;
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            FastForward(offset + count);
            return _innerStream.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            FastForward(this.Position + 1);
            return base.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long pos = -1;
            if (origin == SeekOrigin.Begin)
                pos = offset;
            else if (origin == SeekOrigin.Current)
                pos = _innerStream.Position + offset;
            FastForward(pos);
            return _innerStream.Seek(offset, origin);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                _innerStream.Dispose();
                _baseStream.Dispose();
            }
        }

        public override long Length
        {
            get { return _innerStream.Length; }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
            FastForwardWrite(offset + count);
            DataWritten?.Invoke();
        }

        public override void WriteByte(byte value)
        {
            _innerStream.WriteByte(value);
            FastForwardWrite(this.Position + 1);
            DataWritten?.Invoke();
        }
    }
}
