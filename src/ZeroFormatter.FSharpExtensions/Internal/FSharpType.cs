using System;
using System.Reflection;
using Microsoft.FSharp.Reflection;

namespace ZeroFormatter.Extensions.Internal.FSharp
{
#if NETSTANDARD
    internal static class FSharpType
    {
        private static readonly TypeInfo extensions = typeof(FSharpReflectionExtensions).GetTypeInfo();
        private static readonly MethodInfo isRecord = extensions.GetMethod("FSharpType.IsRecord.Static");
        private static readonly MethodInfo isUnion = extensions.GetMethod("FSharpType.IsUnion.Static");
        public static readonly MethodInfo getUnionCases = extensions.GetMethod("FSharpType.GetUnionCases.Static");
        private static readonly MethodInfo getRecordFields = extensions.GetMethod("FSharpType.GetRecordFields.Static");

        public static bool IsRecord(Type type, object fake)
        {
            return (bool)isRecord.Invoke(null, new object[] { type, fake });
        }

        public static bool IsUnion(Type type, object fake)
        {
            return (bool)isUnion.Invoke(null, new object[] { type, fake });
        }

        public static UnionCaseInfo[] GetUnionCases(Type type, object fake)
        {
            return (UnionCaseInfo[])getUnionCases.Invoke(null, new object[] { type, fake });
        }

        public static PropertyInfo[] GetRecordFields(Type type, object fake)
        {
            return (PropertyInfo[])getRecordFields.Invoke(null, new object[] { type, fake });
        }
    }
#endif
}
