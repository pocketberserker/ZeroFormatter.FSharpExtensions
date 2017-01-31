using System;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;
using System.Reflection;
using ZeroFormatter.Formatters;
using ZeroFormatter.Extensions;

namespace ZeroFormatter
{
    public static class FSharp
    {

        public static void Register<TTypeResolver>()
            where TTypeResolver : ITypeResolver, new()
        {
            Formatter.AppendFormatterResolver(t =>
            {

                var resolverType = typeof(TTypeResolver);

                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(FSharpOption<>))
                {
                    var vt = t.GetGenericArguments()[0];
                    var formatter =
                        (vt.IsValueType ? typeof(FSharpOptionStructFormatter<,>) : typeof(FSharpOptionObjectFormatter<,>))
                            .MakeGenericType(resolverType, vt);
                    return Activator.CreateInstance(formatter);
                }

                if (FSharpType.IsRecord(t, FSharpOption<BindingFlags>.Some(BindingFlags.Public)))
                {
                    return typeof(DynamicRecordFormatter).GetMethod("Create")
                        .MakeGenericMethod(new[] { resolverType, t }).Invoke(null, null);
                }

                if (FSharpType.IsUnion(t, FSharpOption<BindingFlags>.Some(BindingFlags.Public)))
                {
                    return typeof(DynamicFSharpUnionFormatter).GetMethod("Create")
                        .MakeGenericMethod(new[] { resolverType, t }).Invoke(null, null);
                }

                return null;
            });
        }
    }
}
