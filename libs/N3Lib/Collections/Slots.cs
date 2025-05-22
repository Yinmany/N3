using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace N3Lib
{


    /// <summary>
    /// 基于插槽的集合
    /// </summary>
    public class Slots<T> : IEnumerable<T>
    {
        struct KeyValuePair
        {
            public uint Handle;
            public T? Value;
        }

        public readonly int MaxSize;

        private KeyValuePair?[] _arr;
        private uint _handleIndex = 1;

        public int Count { get; private set; }

        public Slots(int initSize = 4, int maxSize = ushort.MaxValue)
        {
            this.MaxSize = maxSize;
            _arr = new KeyValuePair?[initSize];
        }

        /// <summary>
        /// 指定位置设置/获取值(位置不存在将抛出异常)
        /// </summary>
        /// <param name="handle"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        public T? this[uint handle]
        {
            get
            {
                uint hash = Handle2Hash(handle);
                KeyValuePair? kv = _arr[hash];
                if (kv == null)
                {
                    throw new KeyNotFoundException($"找不到Handle: {handle}, hash={hash}");
                }

                return kv.Value.Value;
            }
            set
            {
                uint hash = Handle2Hash(handle);
                KeyValuePair? kv = _arr[hash];
                if (kv == null)
                {
                    throw new KeyNotFoundException($"找不到Handle: {handle}, hash={hash}");
                }

                _arr[hash] = new KeyValuePair
                {
                    Handle = handle,
                    Value = value
                };
            }
        }

        public bool TryGet(uint handle, out T? t)
        {
            KeyValuePair? kv = _arr[Handle2Hash(handle)];
            if (kv == null)
            {
                t = default;
                return false;
            }

            t = kv.Value.Value;
            return true;
        }

        /// <summary>
        /// 添加值到指定位置(会自动扩容到最大值)
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="value"></param>
        /// <exception cref="Exception">已大最大容量</exception>
        public void Add(uint handle, T value)
        {
            uint hash = Handle2Hash(handle);
            do
            {
                if (!_arr[hash].HasValue)
                {
                    _arr[hash] = new KeyValuePair()
                    {
                        Handle = handle,
                        Value = value
                    };
                    ++Count;
                    break;
                }

                // 调整大小
                if (!Resize())
                    throw new Exception($"已大最大容量: {handle}, hash={hash}");

                hash = Handle2Hash(handle); // 重新计算一下
            } while (true);
        }

        /// <summary>
        /// 调整大小
        /// </summary>
        private bool Resize()
        {
            if (_arr.Length > MaxSize) return false;

            // 全部找完都没有空位，就扩容, 并根据handle放入新的位置。
            KeyValuePair?[] newArr = new KeyValuePair?[_arr.Length * 2];
            foreach (var kv in _arr)
            {
                if (!kv.HasValue) continue;
                uint rehash = kv.Value.Handle & ((uint)newArr.Length - 1);
                newArr[rehash] = kv;
            }

            _arr = newArr;
            return true;
        }

        public bool TryAdd(T? value, out uint handle)
        {
            while (true)
            {
                handle = _handleIndex;
                for (uint i = 0, len = (uint)_arr.Length; i < len; ++i, ++handle)
                {
                    if (handle > MaxSize)
                        handle = 1;

                    uint hash = handle & (len - 1);
                    KeyValuePair? kv = _arr[hash];
                    if (kv == null)
                    {
                        _handleIndex = handle + 1;
                        _arr[hash] = new KeyValuePair
                        {
                            Handle = handle,
                            Value = value
                        };
                        ++Count;
                        return true;
                    }
                }

                if (!Resize())
                    return false;
            }
        }

        public bool Remove(uint handle)
        {
            return Remove(handle, out _);
        }

        public bool Remove(uint handle, out T? value)
        {
            uint hash = Handle2Hash(handle);
            KeyValuePair? kv = _arr[hash];
            if (kv.HasValue)
            {
                value = kv.Value.Value;
                _arr[hash] = null;
                --Count;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Handle2Hash(uint handle) => handle & ((uint)_arr.Length - 1);

        public void Clear()
        {
            for (int i = 0; i < _arr.Length; i++)
                _arr[i] = default;
            Count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (KeyValuePair? value in _arr)
            {
                if (value != null && value.Value.Value != null)
                    yield return value.Value.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}