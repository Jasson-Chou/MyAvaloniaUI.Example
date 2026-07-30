using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Models
{
    public sealed class DropOldestQueue<T> where T : INumber<T>
    {
        private readonly T[] _buffer;
        private T[]? _cachedFull;   // Count == Capacity 時重複使用
        private int _head;          // 最舊元素位置
        private int _count;
        private readonly object _lock = new();

        public DropOldestQueue(int capacity)
        {
            _buffer = new T[capacity];
        }

        public int Count => _count;
        public int Capacity => _buffer.Length;

        public void Enqueue(T item)
        {
            int tail = (_head + _count) % Capacity;
            _buffer[tail] = item;

            if (_count == Capacity)
                _head = (_head + 1) % Capacity;  // 已滿：覆蓋並丟棄最舊
            else
                _count++;
        }

        public void EnqueueRange(T[] items)
        {
            int startFillIndex = (_head + _count) % Capacity;
            for (int i = 0; i < items.Length; i++)
            {
                _buffer[(startFillIndex + i) % Capacity] = items[i];
            }
            if (_count + items.Length > Capacity)
            {
                _head = (_head + (_count + items.Length - Capacity)) % Capacity;
                _count = Capacity;
            }
            else
            {
                _count += items.Length;
            }
        }

        public void EnqueueRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                Enqueue(item);
        }

        public bool GetMinMax(out T min, out T max)
        { 
            if (_count == 0)
            {
                min = T.Zero;
                max = T.Zero;
                return false;
            }

            min = _buffer[_head];
            max = _buffer[_head];
            for (int i = 1; i < _count; i++)
            {
                int index = (_head + i) % Capacity;
                if (_buffer[index] < min) min = _buffer[index];
                if (_buffer[index] > max) max = _buffer[index];
            }
            return true;
        }

        public T[] ToArray()
        {
            // 未滿：大小不一致，必須配置符合 Count 的陣列
            T[] result = _count == Capacity
                ? (_cachedFull ??= new T[Capacity])
                : new T[_count];

            int firstPart = Math.Min(_count, Capacity - _head);
            Array.Copy(_buffer, _head, result, 0, firstPart);
            if (_count > firstPart)
                Array.Copy(_buffer, 0, result, firstPart, _count - firstPart);

            return result;
        }

        public int CopyTo(T[] destination)
        {
            lock (_lock)
            {
                int firstPart = Math.Min(_count, Capacity - _head);
                Array.Copy(_buffer, _head, destination, 0, firstPart);
                if (_count > firstPart)
                    Array.Copy(_buffer, 0, destination, firstPart, _count - firstPart);
                return _count;
            }
        }
    }
}
