using Microsoft.FSharp.Core;
using ZeroFormatter.Formatters;
using ZeroFormatter.Internal;

namespace ZeroFormatter.Extensions
{
    internal abstract class FSharpOptionStructBaseFormatter<TTypeResolver, T> : Formatter<TTypeResolver, FSharpOption<T>>
        where TTypeResolver : ITypeResolver, new()
    {
        readonly Formatter<TTypeResolver, T> innerFormatter;

        public FSharpOptionStructBaseFormatter()
        {
            this.innerFormatter = Formatter<TTypeResolver, T>.Default;
        }

        public override int? GetLength()
        {
            var len = innerFormatter.GetLength();
            return (len == null) ? null : len + 1;
        }

        public override int Serialize(ref byte[] bytes, int offset, FSharpOption<T> value)
        {
            var len = GetLength();
            if (len != null)
            {
                BinaryUtil.EnsureCapacity(ref bytes, offset, len.Value);
            }

            var isSome = FSharpOption<T>.get_IsSome(value);
            BinaryUtil.WriteBoolean(ref bytes, offset, isSome);
            if (isSome)
            {
                var startOffset = offset;
                offset += 1;
                offset += innerFormatter.Serialize(ref bytes, offset, value.Value);
                return offset - startOffset;
            }
            else
            {
                return 1;
            }
        }

        public override FSharpOption<T> Deserialize(ref byte[] bytes, int offset, DirtyTracker tracker, out int byteSize)
        {
            byteSize = 1;
            var hasValue = BinaryUtil.ReadBoolean(ref bytes, offset);
            if (!hasValue) return FSharpOption<T>.None;

            offset += 1;

            int size;
            var v = innerFormatter.Deserialize(ref bytes, offset, tracker, out size);
            byteSize += size;

            return FSharpOption<T>.Some(v);
        }
    }

    internal class FSharpOptionStructFormatter<TTypeResolver, T> : FSharpOptionStructBaseFormatter<TTypeResolver, T>
        where T : struct
        where TTypeResolver : ITypeResolver, new()
    { }

    internal class FSharpOptionRecordFormatter<TTypeResolver, T> : FSharpOptionStructBaseFormatter<TTypeResolver, T>
        where TTypeResolver : ITypeResolver, new()
    { }

    internal class FSharpOptionObjectFormatter<TTypeResolver, T> : Formatter<TTypeResolver, FSharpOption<T>>
        where T : class
        where TTypeResolver : ITypeResolver, new()
    {
        readonly Formatter<TTypeResolver, T> innerFormatter;

        public FSharpOptionObjectFormatter()
        {
            this.innerFormatter = Formatter<TTypeResolver, T>.Default;
        }

        public override int? GetLength()
        {
            return innerFormatter.GetLength();
        }

        public override int Serialize(ref byte[] bytes, int offset, FSharpOption<T> value)
        {
            return innerFormatter.Serialize(ref bytes, offset, OptionModule.ToObj(value));
        }

        public override FSharpOption<T> Deserialize(ref byte[] bytes, int offset, DirtyTracker tracker, out int byteSize)
        {
            return OptionModule.OfObj(innerFormatter.Deserialize(ref bytes, offset, tracker, out byteSize));
        }
    }
}
