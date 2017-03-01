using System.IO;
using ZeroFormatter.Formatters;

namespace ZeroFormatter.FSharp
{
    public static class ZeroFormatterSerializer
    {
        public static int MaximumLengthOfDeserialize
        {
            get
            {
                return ZeroFormatter.ZeroFormatterSerializer.MaximumLengthOfDeserialize;
            }
            set
            {
                ZeroFormatter.ZeroFormatterSerializer.MaximumLengthOfDeserialize = value;
            }
        }

        public static void ValidateNewLength(int length)
        {
            ZeroFormatter.ZeroFormatterSerializer.ValidateNewLength(length);
        }

        public static byte[] Serialize<T>(T obj)
        {
            return CustomSerializer<DefaultResolver>.Serialize(obj);
        }

        public static int Serialize<T>(ref byte[] buffer, int offset, T obj)
        {
            return CustomSerializer<DefaultResolver>.Serialize(ref buffer, offset, obj);
        }

        public static void Serialize<T>(Stream stream, T obj)
        {
            CustomSerializer<DefaultResolver>.Serialize(stream, obj);
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            return CustomSerializer<DefaultResolver>.Deserialize<T>(bytes);
        }

        public static T Deserialize<T>(byte[] bytes, int offset)
        {
            return CustomSerializer<DefaultResolver>.Deserialize<T>(bytes, offset);
        }

        public static T Deserialize<T>(Stream stream)
        {
            return CustomSerializer<DefaultResolver>.Deserialize<T>(stream);
        }

        public static T Convert<T>(T obj, bool forceConvert = false)
        {
            return CustomSerializer<DefaultResolver>.Convert<T>(obj, forceConvert);
        }

        public static bool IsFormattedObject<T>(T obj)
        {
            return ZeroFormatter.ZeroFormatterSerializer.IsFormattedObject(obj);
        }
    }

    public static class CustomSerializer<TTypeResolver>
            where TTypeResolver : ITypeResolver, new()
    {
        public static byte[] Serialize<T>(T obj)
        {
            return ZeroFormatter.ZeroFormatterSerializer.CustomSerializer<FSharpResolver<TTypeResolver>>.Serialize(obj);
        }

        public static int Serialize<T>(ref byte[] buffer, int offset, T obj)
        {
            return ZeroFormatter.ZeroFormatterSerializer.CustomSerializer<FSharpResolver<TTypeResolver>>.Serialize(ref buffer, offset, obj);
        }

        public static void Serialize<T>(Stream stream, T obj)
        {
            ZeroFormatter.ZeroFormatterSerializer.CustomSerializer<FSharpResolver<TTypeResolver>>.Serialize(stream, obj);
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            return ZeroFormatter.ZeroFormatterSerializer.CustomSerializer<FSharpResolver<TTypeResolver>>.Deserialize<T>(bytes);
        }

        public static T Deserialize<T>(byte[] bytes, int offset)
        {
            return ZeroFormatter.ZeroFormatterSerializer.CustomSerializer<FSharpResolver<TTypeResolver>>.Deserialize<T>(bytes, offset);
        }

        public static T Deserialize<T>(Stream stream)
        {
            return ZeroFormatter.ZeroFormatterSerializer.CustomSerializer<FSharpResolver<TTypeResolver>>.Deserialize<T>(stream);
        }

        public static T Convert<T>(T obj, bool forceConvert = false)
        {
            return ZeroFormatter.ZeroFormatterSerializer.CustomSerializer<FSharpResolver<TTypeResolver>>.Convert<T>(obj, forceConvert);
        }
    }
}
