using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
#if NETSTANDARD
using ZeroFormatter.Extensions.Internal.FSharp;
#else
using Microsoft.FSharp.Reflection;
#endif
using System;
using System.Reflection;
using ZeroFormatter.Extensions;

namespace ZeroFormatter.Formatters
{
    public class FSharpResolver<TTypeResolver> : ITypeResolver
        where TTypeResolver : ITypeResolver, new()
    {
        private readonly TTypeResolver resolver;

        public FSharpResolver()
        {
            this.resolver = new TTypeResolver();
        }

        public bool IsUseBuiltinSerializer
        {
            get
            {
                return resolver.IsUseBuiltinSerializer;
            }
        }

        public void RegisterDynamicUnion(Type unionType, DynamicUnionResolver resolver)
        {
            this.resolver.RegisterDynamicUnion(unionType, resolver);
        }

        public object ResolveFormatter(Type type)
        {
            var resolverType = typeof(FSharpResolver<TTypeResolver>);

            if (type == typeof(Unit))
            {
                return new UnitFormatter<FSharpResolver<TTypeResolver>>();
            }
            
            var ti = type.GetTypeInfo();

            if (ti.IsGenericType && type.GetGenericTypeDefinition() == typeof(FSharpOption<>))
            {
                var vt = ti.GetGenericArguments()[0];
                if (FSharpType.IsRecord(vt, null))
                {
                    var formatter = typeof(FSharpOptionRecordFormatter<,>).MakeGenericType(resolverType, vt);
                    return Activator.CreateInstance(formatter);
                }
                else
                {
                    var formatter =
                        (vt.GetTypeInfo().IsValueType ? typeof(FSharpOptionStructFormatter<,>) : typeof(FSharpOptionObjectFormatter<,>))
                            .MakeGenericType(resolverType, vt);
                    return Activator.CreateInstance(formatter);
                }
            }

            if (ti.IsGenericType && type.GetGenericTypeDefinition() == typeof(FSharpList<>))
            {
                var vt = ti.GetGenericArguments()[0];
                var formatter = typeof(FSharpListFormatter<,>).MakeGenericType(resolverType, vt);
                return Activator.CreateInstance(formatter);
            }

            if (ti.IsGenericType && type.GetGenericTypeDefinition() == typeof(FSharpMap<,>))
            {
                var vt = ti.GetGenericArguments();
                var formatter = typeof(FSharpMapFormatter<,,>).MakeGenericType(resolverType, vt[0], vt[1]);
                return Activator.CreateInstance(formatter);
            }

            if (ti.IsGenericType && type.GetGenericTypeDefinition() == typeof(FSharpSet<>))
            {
                var vt = ti.GetGenericArguments()[0];
                var formatter = typeof(FSharpSetFormatter<,>).MakeGenericType(resolverType, vt);
                return Activator.CreateInstance(formatter);
            }

            if (FSharpType.IsRecord(type, null))
            {
                return typeof(DynamicRecordFormatter).GetTypeInfo().GetMethod("Create")
                    .MakeGenericMethod(new[] { resolverType, type }).Invoke(null, null);
            }

            if (FSharpType.IsUnion(type, null))
            {
                return typeof(DynamicFSharpUnionFormatter).GetTypeInfo().GetMethod("Create")
                    .MakeGenericMethod(new[] { resolverType, type }).Invoke(null, null);
            }

            if (Microsoft.FSharp.Reflection.FSharpType.IsTuple(type) && ti.IsValueType)
            {
                Type tupleFormatterType = null;
                switch (ti.GetGenericArguments().Length)
                {
                    case 1:
                        tupleFormatterType = typeof(ValueTupleFormatter<,>);
                        break;
                    case 2:
                        tupleFormatterType = typeof(ValueTupleFormatter<,,>);
                        break;
                    case 3:
                        tupleFormatterType = typeof(ValueTupleFormatter<,,,>);
                        break;
                    case 4:
                        tupleFormatterType = typeof(ValueTupleFormatter<,,,,>);
                        break;
                    case 5:
                        tupleFormatterType = typeof(ValueTupleFormatter<,,,,,>);
                        break;
                    case 6:
                        tupleFormatterType = typeof(ValueTupleFormatter<,,,,,,>);
                        break;
                    case 7:
                        tupleFormatterType = typeof(ValueTupleFormatter<,,,,,,,>);
                        break;
                    case 8:
                        tupleFormatterType = typeof(ValueTupleFormatter<,,,,,,,,>);
                        break;
                    default:
                        break;
                }

                var formatterType = tupleFormatterType.MakeGenericType(ti.GetGenericArguments().StartsWith(resolverType));
                return Activator.CreateInstance(formatterType);
            }

            return null;
        }
    }
}
