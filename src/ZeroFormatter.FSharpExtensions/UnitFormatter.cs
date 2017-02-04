using Microsoft.FSharp.Core;
using System;
using ZeroFormatter.Formatters;

namespace ZeroFormatter.Extensions
{
    internal class UnitFormatter<TTYpeResolver> : Formatter<TTYpeResolver, Unit>
        where TTYpeResolver : ITypeResolver, new()
    {
        readonly Formatter<TTYpeResolver, int> formatter;

        public UnitFormatter()
        {
            this.formatter = Formatter<TTYpeResolver, int>.Default;
        }

        public override int? GetLength()
        {
            return formatter.GetLength();
        }

        public override int Serialize(ref byte[] bytes, int offset, Unit value)
        {
            return formatter.Serialize(ref bytes, offset, -1);
        }

        public override Unit Deserialize(ref byte[] bytes, int offset, DirtyTracker tracker, out int byteSize)
        {
            var value = formatter.Deserialize(ref bytes, offset, tracker, out byteSize);
            // Unit is special and always uses the representation 'null'.
            if (value == -1) return null;
            else throw new Exception($"{value} is not FSharp Unit binary.");
        }
    }
}
