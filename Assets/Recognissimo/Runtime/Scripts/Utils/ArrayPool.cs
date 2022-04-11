using System;
using System.Collections.Concurrent;

namespace Recognissimo.Utils
{
    public class ArrayPool<T>
    {
        public const int MinArraySize = 1024;
        public const int MaxArraySize = 65536;

        private const int NumArraysPerBucket = 10;
        public static readonly ArrayPool<T> Shared = new ArrayPool<T>();

        private static readonly T[] Empty = new T[0];

        private readonly ConcurrentBag<T[]>[] _buckets;

        private ArrayPool()
        {
            var numBuckets = (int) Math.Log(MaxArraySize / MinArraySize, 2) + 1;

            _buckets = new ConcurrentBag<T[]>[numBuckets];
            for (var i = 0; i < numBuckets; i++)
            {
                _buckets[i] = new ConcurrentBag<T[]>();
            }
        }

        public T[] Rent(int minimumSize)
        {
            if (minimumSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumSize));
            }

            if (minimumSize == 0)
            {
                return Empty;
            }

            var size = NextPowOfTwo(minimumSize);
            var index = SizeToIndex(size);

            if (index == -1)
            {
                return new T[size];
            }

            var bag = _buckets[index];

            return bag.TryTake(out var array)
                ? array
                : new T[size];
        }

        public void Return(T[] array)
        {
            if (array == null || array.Length == 0)
            {
                return;
            }

            var index = SizeToIndex(array.Length);
            
            if (index == -1)
            {
                return;
            }

            var bag = _buckets[index];

            if (bag.Count < NumArraysPerBucket)
            {
                bag.Add(array);
            }
        }

        private static int NextPowOfTwo(int x)
        {
            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x++;

            return x;
        }

        private static bool IsPowOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        private static int SizeToIndex(int size)
        {
            if (!IsPowOfTwo(size) || size < MinArraySize || size > MaxArraySize)
            {
                return -1;
            }

            var ratio = (double) size / MinArraySize;
            return (int) Math.Log(ratio, 2);
        }
    }
}