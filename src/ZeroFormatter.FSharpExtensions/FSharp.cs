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

                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(FSharpOption<>))
                {
                    var vt = t.GetGenericArguments()[0];
                    var resolverType = typeof(TTypeResolver);
                    var formatter =
                        (vt.IsValueType ? typeof(FSharpOptionStructFormatter<,>) : typeof(FSharpOptionObjectFormatter<,>))
                            .MakeGenericType(resolverType, vt);
                    return Activator.CreateInstance(
                        formatter,
                        typeof(Formatter<,>).MakeGenericType(resolverType, vt).GetTypeInfo().GetProperty("Default").GetGetMethod().Invoke(null, null)
                    );
                }

                if (FSharpType.IsRecord(t, FSharpOption<BindingFlags>.Some(BindingFlags.Public)))
                {
                    return typeof(DynamicRecordFormatter).GetMethod("Create")
                        .MakeGenericMethod(new[] { typeof(DefaultResolver), t }).Invoke(null, null);
                }

                return null;
            });
        }
    }
}
