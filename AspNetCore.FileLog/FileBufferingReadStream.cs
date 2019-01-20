using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// A Stream that wraps another stream and enables rewinding by buffering the content as it is read.
    /// The content is buffered in memory up to a certain size and then spooled to a temp file on disk.
    /// The temp file will be deleted on Dispose.
    /// </summary>
    internal class FileBufferingReadStream : Stream
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="memoryThreshold"></param>
        /// <param name="bufferLimit"></param>
        /// <param name="tempFileDirectoryAccessor"></param>
        public FileBufferingReadStream(
            Stream inner,
            int memoryThreshold,
            long? bufferLimit,
            Func<string> tempFileDirectoryAccessor)
            : this(inner, memoryThreshold, bufferLimit, tempFileDirectoryAccessor, ArrayPool<byte>.Shared)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="memoryThreshold"></param>
        /// <param name="bufferLimit"></param>
        /// <param name="tempFileDirectoryAccessor"></param>
        /// <param name="bytePool"></param>
        public FileBufferingReadStream(
            Stream inner,
            int memoryThreshold,
            long? bufferLimit,
            Func<string> tempFileDirectoryAccessor,
            ArrayPool<byte> bytePool)
        {
            _bytePool = bytePool;
            if (memoryThreshold < _maxRentedBufferSize)
            {
                _rentedBuffer = bytePool.Rent(memoryThreshold);
                _buffer = new MemoryStream(_rentedBuffer);
                _buffer.SetLength(0);
            }
            else
            {
                _buffer = new MemoryStream();
            }

            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _memoryThreshold = memoryThreshold;
            _bufferLimit = bufferLimit;
            _tempFileDirectoryAccessor = tempFileDirectoryAccessor ?? throw new ArgumentNullException(nameof(tempFileDirectoryAccessor));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="memoryThreshold"></param>
        /// <param name="bufferLimit"></param>
        /// <param name="tempFileDirectory"></param>
        public FileBufferingReadStream(
            Stream inner,
            int memoryThreshold,
            long? bufferLimit,
            string tempFileDirectory)
            : this(inner, memoryThreshold, bufferLimit, tempFileDirectory, ArrayPool<byte>.Shared)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="memoryThreshold"></param>
        /// <param name="bufferLimit"></param>
        /// <param name="tempFileDirectory"></param>
        /// <param name="bytePool"></param>
        public FileBufferingReadStream(
            Stream inner,
            int memoryThreshold,
            long? bufferLimit,
            string tempFileDirectory,
            ArrayPool<byte> bytePool)
        {
            _bytePool = bytePool;
            if (memoryThreshold < _maxRentedBufferSize)
            {
                _rentedBuffer = bytePool.Rent(memoryThreshold);
                _buffer = new MemoryStream(_rentedBuffer);
                _buffer.SetLength(0);
            }
            else
            {
                _buffer = new MemoryStream();
            }

            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _memoryThreshold = memoryThreshold;
            _bufferLimit = bufferLimit;
            _tempFileDirectory = tempFileDirectory ?? throw new ArgumentNullException(nameof(tempFileDirectory));
        }

        /// <summary>
        ///
        /// </summary>
        public bool InMemory
        {
            get
            {
                return this._inMemory;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public string TempFileName
        {
            get
            {
                return this._tempFileName;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public override long Length
        {
            get
            {
                return this._buffer.Length;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public override long Position
        {
            get
            {
                return this._buffer.Position;
            }
            set
            {
                this.ThrowIfDisposed();
                this._buffer.Position = value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            this.ThrowIfDisposed();
            bool flag = !this._completelyBuffered && origin == SeekOrigin.End;
            if (flag)
            {
                throw new NotSupportedException("The content has not been fully buffered yet.");
            }
            bool flag2 = !this._completelyBuffered && origin == SeekOrigin.Current && offset + this.Position > this.Length;
            if (flag2)
            {
                throw new NotSupportedException("The content has not been fully buffered yet.");
            }
            bool flag3 = !this._completelyBuffered && origin == SeekOrigin.Begin && offset > this.Length;
            if (flag3)
            {
                throw new NotSupportedException("The content has not been fully buffered yet.");
            }
            return this._buffer.Seek(offset, origin);
        }

        /// <summary>
        ///
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                return this._disposed;
            }
        }
        
        private Stream CreateTempFile()
        {
            bool flag = this._tempFileDirectory == null;
            if (flag)
            {
                Debug.Assert(this._tempFileDirectoryAccessor != null);
                this._tempFileDirectory = this._tempFileDirectoryAccessor();
                Debug.Assert(this._tempFileDirectory != null);
            }
            this._tempFileName = Path.Combine(this._tempFileDirectory, "ASPNETCORE_" + Guid.NewGuid().ToString() + ".tmp");
            return new FileStream(this._tempFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, 16384, FileOptions.Asynchronous | FileOptions.DeleteOnClose | FileOptions.SequentialScan);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        // Token: 0x0600006C RID: 108 RVA: 0x00004838 File Offset: 0x00002A38
        public override int Read(byte[] buffer, int offset, int count)
        {
            this.ThrowIfDisposed();
            bool flag = this._buffer.Position < this._buffer.Length || this._completelyBuffered;
            int result;
            if (flag)
            {
                result = this._buffer.Read(buffer, offset, (int)Math.Min((long)count, this._buffer.Length - this._buffer.Position));
            }
            else
            {
                int read = this._inner.Read(buffer, offset, count);
                bool flag2;
                if (this._bufferLimit != null)
                {
                    long? num = this._bufferLimit - (long)read;
                    long length = this._buffer.Length;
                    flag2 = (num.GetValueOrDefault() < length & num != null);
                }
                else
                {
                    flag2 = false;
                }
                bool flag3 = flag2;
                if (flag3)
                {
                    base.Dispose();
                    throw new IOException("Buffer limit exceeded.");
                }
                bool flag4 = this._inMemory && this._buffer.Length + (long)read > (long)this._memoryThreshold;
                if (flag4)
                {
                    this._inMemory = false;
                    Stream oldBuffer = this._buffer;
                    this._buffer = this.CreateTempFile();
                    bool flag5 = this._rentedBuffer == null;
                    if (flag5)
                    {
                        oldBuffer.Position = 0L;
                        byte[] rentedBuffer = this._bytePool.Rent(Math.Min((int)oldBuffer.Length, 1048576));
                        for (int copyRead = oldBuffer.Read(rentedBuffer, 0, rentedBuffer.Length); copyRead > 0; copyRead = oldBuffer.Read(rentedBuffer, 0, rentedBuffer.Length))
                        {
                            this._buffer.Write(rentedBuffer, 0, copyRead);
                        }
                        this._bytePool.Return(rentedBuffer, false);
                    }
                    else
                    {
                        this._buffer.Write(this._rentedBuffer, 0, (int)oldBuffer.Length);
                        this._bytePool.Return(this._rentedBuffer, false);
                        this._rentedBuffer = null;
                    }
                }
                bool flag6 = read > 0;
                if (flag6)
                {
                    this._buffer.Write(buffer, offset, read);
                }
                else
                {
                    this._completelyBuffered = true;
                }
                result = read;
            }
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            this.ThrowIfDisposed();
            bool flag = this._buffer.Position < this._buffer.Length || this._completelyBuffered;
            int result;
            if (flag)
            {
                int num = await this._buffer.ReadAsync(buffer, offset, (int)Math.Min((long)count, this._buffer.Length - this._buffer.Position), cancellationToken);
                result = num;
            }
            else
            {
                int num2 = await this._inner.ReadAsync(buffer, offset, count, cancellationToken);
                int read = num2;
                bool flag2;
                if (this._bufferLimit != null)
                {
                    long? num3 = this._bufferLimit - (long)read;
                    long length = this._buffer.Length;
                    flag2 = (num3.GetValueOrDefault() < length & num3 != null);
                }
                else
                {
                    flag2 = false;
                }
                if (flag2)
                {
                    base.Dispose();
                    throw new IOException("Buffer limit exceeded.");
                }
                if (this._inMemory && this._buffer.Length + (long)read > (long)this._memoryThreshold)
                {
                    this._inMemory = false;
                    Stream oldBuffer = this._buffer;
                    this._buffer = this.CreateTempFile();
                    if (this._rentedBuffer == null)
                    {
                        oldBuffer.Position = 0L;
                        byte[] rentedBuffer = this._bytePool.Rent(Math.Min((int)oldBuffer.Length, 1048576));
                        for (int copyRead = oldBuffer.Read(rentedBuffer, 0, rentedBuffer.Length); copyRead > 0; copyRead = oldBuffer.Read(rentedBuffer, 0, rentedBuffer.Length))
                        {
                            await this._buffer.WriteAsync(rentedBuffer, 0, copyRead, cancellationToken);
                        }
                        this._bytePool.Return(rentedBuffer, false);
                        rentedBuffer = null;
                    }
                    else
                    {
                        await this._buffer.WriteAsync(this._rentedBuffer, 0, (int)oldBuffer.Length, cancellationToken);
                        this._bytePool.Return(this._rentedBuffer, false);
                        this._rentedBuffer = null;
                    }
                    oldBuffer = null;
                }
                if (read > 0)
                {
                    await this._buffer.WriteAsync(buffer, offset, read, cancellationToken);
                }
                else
                {
                    this._completelyBuffered = true;
                }
                result = read;
            }
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            bool flag = !this._disposed;
            if (flag)
            {
                this._disposed = true;
                bool flag2 = this._rentedBuffer != null;
                if (flag2)
                {
                    this._bytePool.Return(this._rentedBuffer, false);
                }
                if (disposing)
                {
                    this._buffer.Dispose();
                }
            }
        }
        
        private void ThrowIfDisposed()
        {
            bool disposed = this._disposed;
            if (disposed)
            {
                throw new ObjectDisposedException("FileBufferingReadStream");
            }
        }
        
        private const int _maxRentedBufferSize = 1048576;
        
        private readonly Stream _inner;
        
        private readonly ArrayPool<byte> _bytePool;
        
        private readonly int _memoryThreshold;
        
        private readonly long? _bufferLimit;
        
        private string _tempFileDirectory;
        
        private readonly Func<string> _tempFileDirectoryAccessor;
        
        private string _tempFileName;
        
        private Stream _buffer;
        
        private byte[] _rentedBuffer;
        
        private bool _inMemory;
        
        private bool _completelyBuffered;
        
        private bool _disposed;
    }
}
