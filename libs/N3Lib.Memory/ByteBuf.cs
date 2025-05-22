using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace N3Lib.Buffer;

public partial class ByteBuf : Stream
{
    private const ushort BlockSize = PinnedBlockMemoryPool.BlockSize;

    private int _length;
    private int _position;

    private MemoryBlock _readNode, _writeNode;
    private int _readIndex, _writeIndex;

    private int _refCount;

    /// <summary>
    /// 尾部可写长度
    /// </summary>
    private int WriteableBytes => BlockSize - _writeIndex;

    /// <summary>
    /// 头部可读长度
    /// </summary>
    private int ReadableBytes => Math.Min(_length, BlockSize - _readIndex);

    private byte[] ReadBuffer => _readNode.Bytes;
    private byte[] WriteBuffer => _writeNode.Bytes;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    /// <summary>
    /// 可读数据长度
    /// </summary>
    public override long Length => _length;

    /// <summary>
    /// 读取位置
    /// </summary>
    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public bool Disposed { get; private set; }

    public ByteBuf()
    {
        _readNode = _writeNode = RentBuffer();
    }

    public override void Flush()
    {
    }

    /// <summary>
    /// 只能读取往后移动
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="origin"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin != SeekOrigin.Current)
            throw new NotSupportedException();

        if (offset <= 0 || offset > _length)
            throw new ArgumentOutOfRangeException("offset", offset, "offset must be in the range of 0 - buffer.Length.");

        int offsetTmp = (int)offset;
        while (offsetTmp != 0)
        {
            int len = Math.Min(ReadableBytes, offsetTmp);
            offsetTmp -= len;
            this.ReadAdvance(len);
        }

        return offset;
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (buffer is null)
            throw new ArgumentNullException(nameof(buffer));

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset", offset, "offset must be in the range of 0 - buffer.Length-1.");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count", count, "count must be non-negative.");
        }

        if (count + offset > buffer.Length)
        {
            throw new ArgumentException("count must be greater than buffer.Length - offset.");
        }

        CheckDisposed();

        int readCount = 0;

        // 保证不超过可读的长度
        int limitCount = Math.Min(_length, count);

        while (readCount < limitCount)
        {
            // 剩余多少没读的
            int n = limitCount - readCount;

            // 可读的
            n = Math.Min(ReadableBytes, n);


            Array.Copy(this.ReadBuffer, _readIndex, buffer, readCount + offset, n);
            readCount += n;
            this.ReadAdvance(n);
        }

        return readCount;
    }

    public override int ReadByte()
    {
        CheckDisposed();
        if (_length == 0)
            throw new IndexOutOfRangeException();
        int val = ReadBuffer[_readIndex];
        ReadAdvance(1);
        return val;
    }

    private void ReadAdvance(int count)
    {
        _readIndex += count;
        if (_readIndex == BlockSize)
        {
            _readIndex = 0;
            this.RemoveFirst();
        }

        // 保证逻辑都正确了，才改变长度
        Interlocked.Add(ref _length, -count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (buffer is null)
            throw new ArgumentNullException(nameof(buffer));

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset", offset, "offset must be in the range of 0 - buffer.Length-1.");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count", count, "count must be non-negative.");
        }

        if (count + offset > buffer.Length)
        {
            throw new ArgumentException("count must be greater than buffer.Length - offset.");
        }

        CheckDisposed();

        int writeCount = 0;
        while (writeCount < count)
        {
            // 剩余多少还没写入
            int n = count - writeCount;

            // 限制写入不能超过buffer剩余的长度.
            n = Math.Min(this.WriteableBytes, n);

            Array.Copy(buffer, writeCount + offset, WriteBuffer, this._writeIndex, n);
            writeCount += n;

            this.WriteAdvance(n);
        }
    }

    public override void WriteByte(byte value)
    {
        WriteBuffer[_writeIndex] = value;
        this.WriteAdvance(1);
    }

    private void WriteAdvance(int count)
    {
        _writeIndex += count;
        if (_writeIndex == BlockSize)
        {
            AddLast();
            _writeIndex = 0;
        }

        Interlocked.Add(ref _length, count);
    }

    private void RemoveFirst()
    {
        var tmp = _readNode;
        _readNode = tmp.Next;
        tmp.Dispose();
    }

    private void AddLast()
    {
        _writeNode = _writeNode.Next = RentBuffer();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckDisposed()
    {
        if (!this.Disposed)
            return;
        throw new ObjectDisposedException("The RingBuffer has been disposed.");
    }

    public override void Close()
    {
        CheckDisposed();

        this.Disposed = true;
        base.Close();

        this._refCount = 0;

        _length = 0;
        _position = 0;
        _readIndex = 0;
        _writeIndex = 0;

        // 回收节点
        var node = _readNode;
        _readNode = null;
        _writeNode = null;
        while (node != null)
        {
            var next = node.Next;
            node.Dispose();
            node = next;
        }

        // 回收自己
        Return(this);
    }

    public void Retain(int increment = 1)
    {
        CheckDisposed();
        Interlocked.Add(ref _refCount, increment);
    }

    public void Release(int decrement = 1)
    {
        CheckDisposed();
        if (Interlocked.Add(ref _refCount, -decrement) <= 0)
        {
            Dispose();
        }
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < _length; i++)
        {
            stringBuilder.Append($"{this.ReadBuffer[i]:X2} ");
        }
        return stringBuilder.ToString();
    }
}