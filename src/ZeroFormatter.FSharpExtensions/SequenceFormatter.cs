using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using ZeroFormatter.Formatters;
using ZeroFormatter.Internal;

namespace ZeroFormatter.Extensions
{
    internal class FSharpListFormatter<TTypeResolver, T> : Formatter<TTypeResolver, FSharpList<T>>
        where TTypeResolver : ITypeResolver, new()
    {
        readonly Formatter<TTypeResolver, T> formatter;
        readonly int? formatterLength;

        public FSharpListFormatter()
        {
            this.formatter = Formatter<TTypeResolver, T>.Default;
            this.formatterLength = formatter.GetLength();
        }

        public override int? GetLength()
        {
            return null;
        }

        public override int Serialize(ref byte[] bytes, int offset, FSharpList<T> value)
        {
            if (value == null)
            {
                BinaryUtil.WriteInt32(ref bytes, offset, -1);
                return 4;
            }

            var length = value.Length;

            if (formatterLength != null)
            {
                // make fixed size.
                BinaryUtil.EnsureCapacity(ref bytes, offset, 4 + formatterLength.Value * length);
            }

            var startOffset = offset;
            offset += BinaryUtil.WriteInt32(ref bytes, offset, length);

            var xs = value;
            while (xs.IsCons)
            {
                offset += formatter.Serialize(ref bytes, offset, xs.Head);
                xs = xs.Tail;
            }

            return offset - startOffset;
        }

        public override FSharpList<T> Deserialize(ref byte[] bytes, int offset, DirtyTracker tracker, out int byteSize)
        {
            // tracker.Dirty(); FSharpList is immutable

            var length = BinaryUtil.ReadInt32(ref bytes, offset);
            if (length == -1)
            {
                byteSize = 4;
                return null;
            }

            var startOffset = offset;
            offset += 4;
            int size;
            ZeroFormatterSerializer.ValidateNewLength(length);
            var result = new T[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = formatter.Deserialize(ref bytes, offset, tracker, out size);
                offset += size;
            }

            byteSize = offset - startOffset;
            return ListModule.OfArray(result);
        }
    }

    internal class FSharpMapFormatter<TTypeResolver, TKey, TValue> : Formatter<TTypeResolver, FSharpMap<TKey, TValue>>
        where TKey : IComparable<TKey>
        where TTypeResolver : ITypeResolver, new()
    {
        readonly Formatter<TTypeResolver, KeyValuePair<TKey, TValue>> kvpFormatter;

        public FSharpMapFormatter()
        {
            this.kvpFormatter = Formatter<TTypeResolver, KeyValuePair<TKey, TValue>>.Default;
        }

        public override bool NoUseDirtyTracker
        {
            get
            {
                return kvpFormatter.NoUseDirtyTracker;
            }
        }

        public override int? GetLength()
        {
            return null;
        }

        public override int Serialize(ref byte[] bytes, int offset, FSharpMap<TKey, TValue> value)
        {
            if (value == null)
            {
                BinaryUtil.WriteInt32(ref bytes, offset, -1);
                return 4;
            }

            var startOffset = offset;
            offset += BinaryUtil.WriteInt32(ref bytes, offset, value.Count);

            foreach (var item in value)
            {
                offset += kvpFormatter.Serialize(ref bytes, offset, item);
            }

            return offset - startOffset;
        }

        public override FSharpMap<TKey, TValue> Deserialize(ref byte[] bytes, int offset, DirtyTracker tracker, out int byteSize)
        {
            // tracker.Dirty(); // immutable

            var length = BinaryUtil.ReadInt32(ref bytes, offset);
            if (length == -1)
            {
                byteSize = 4;
                return null;
            }
            ZeroFormatterSerializer.ValidateNewLength(length);

            var startOffset = offset;
            offset += 4;
            int size;
            var result = new Tuple<TKey, TValue>[length];
            for (int i = 0; i < length; i++)
            {
                var kvp = kvpFormatter.Deserialize(ref bytes, offset, tracker, out size);
                result[i] = Tuple.Create(kvp.Key, kvp.Value);
                offset += size;
            }

            byteSize = offset - startOffset;
            return MapModule.OfArray(result);
        }
    }

    internal class FSharpSetFormatter<TTypeResolver, T> : Formatter<TTypeResolver, FSharpSet<T>>
        where T : IComparable<T>
        where TTypeResolver : ITypeResolver, new()
    {
        readonly Formatter<TTypeResolver, T> formatter;
        readonly int? formatterLength;

        public FSharpSetFormatter()
        {
            this.formatter = Formatter<TTypeResolver, T>.Default;
            this.formatterLength = formatter.GetLength();
        }

        public override int? GetLength()
        {
            return null;
        }

        public override int Serialize(ref byte[] bytes, int offset, FSharpSet<T> value)
        {
            if (value == null)
            {
                BinaryUtil.WriteInt32(ref bytes, offset, -1);
                return 4;
            }

            if (formatterLength != null)
            {
                // make fixed size.
                BinaryUtil.EnsureCapacity(ref bytes, offset, 4 + formatterLength.Value * value.Count);
            }

            var startOffset = offset;
            offset += BinaryUtil.WriteInt32(ref bytes, offset, value.Count);

            foreach (var item in value)
            {
                offset += formatter.Serialize(ref bytes, offset, item);
            }

            return offset - startOffset;
        }

        public override FSharpSet<T> Deserialize(ref byte[] bytes, int offset, DirtyTracker tracker, out int byteSize)
        {
            // tracker.Dirty(); // immutable

            var length = BinaryUtil.ReadInt32(ref bytes, offset);
            if (length == -1)
            {
                byteSize = 4;
                return null;
            }
            ZeroFormatterSerializer.ValidateNewLength(length);

            var startOffset = offset;
            offset += 4;
            int size;
            var result = new T[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = formatter.Deserialize(ref bytes, offset, tracker, out size);
                offset += size;
            }

            byteSize = offset - startOffset;
            return SetModule.OfArray(result);
        }
    }
}
