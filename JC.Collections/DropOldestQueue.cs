using System.Numerics;

namespace JC.Collections
{
    public sealed class DropOldestQueue<T> where T : INumber<T>
    {
        private T[] _buffer;
        private T[]? _cachedFull;   // Count == Capacity 時重複使用
        private int _head;          // 最舊元素位置
        private int _count;
        private readonly object _lock = new();

        public DropOldestQueue() : this(1024) { }

        public DropOldestQueue(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0.");
            _buffer = new T[capacity];
        }

        public int Count => _count;
        public int Capacity => _buffer.Length;


        /// <summary>
        /// 重新配置容量，若新容量小於目前元素數量，則會丟棄最舊的元素
        /// </summary>
        /// <param name="newCapacity"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetCapacity(int newCapacity)
        {
            if (newCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(newCapacity), "Capacity must be greater than 0.");
            lock (_lock)
            {
                if (newCapacity == Capacity)
                    return;
                _cachedFull = new T[newCapacity]; // 重新配置容量時，清除快取

                T[] newBuffer = new T[newCapacity];

                int copyCount = Math.Min(_count, newCapacity);

                // first part
                int firstPartStartIndex = (_count + _head - copyCount) % Capacity;
                int firstPartLength = Math.Min(copyCount, Capacity - firstPartStartIndex);
                Array.Copy(_buffer, firstPartStartIndex, newBuffer, 0, firstPartLength);

                // second part
                if (copyCount > firstPartLength)
                {
                    int secondPartLength = copyCount - firstPartLength;
                    Array.Copy(_buffer, 0, newBuffer, firstPartLength, secondPartLength);
                }

                _buffer = newBuffer;
                _count = copyCount;
                _head = 0;
            }
        }

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

        public void Clear()
        {
            _head = 0;
            _count = 0;
        }
    }
}
