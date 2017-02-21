#if NETSTANDARD
using ZeroFormatter.Extensions.Internal.FSharp;
#else
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ZeroFormatter.Formatters;
using ZeroFormatter.Internal;
using ZeroFormatter.Segments;

namespace ZeroFormatter.Extensions
{
    internal static class DynamicFSharpUnionFormatter
    {
        public static object Create<TTypeResolver, T>()
            where TTypeResolver : ITypeResolver, new()
        {
            var t = typeof(T);
            var ti = t.GetTypeInfo();

            if (!FSharpType.IsUnion(t, null))
            {
                throw new InvalidOperationException("Type must be F# Discriminated Union. " + ti.FullName);
            }

            var unionCases = FSharpType.GetUnionCases(t, null);

            var generateTypeInfo = BuildFormatter(typeof(TTypeResolver), t, unionCases);
            var formatter = Activator.CreateInstance(generateTypeInfo.AsType());

            return formatter;
        }

        static TypeInfo BuildFormatter(Type resolverType, Type buildType, Microsoft.FSharp.Reflection.UnionCaseInfo[] unionCases)
        {
            var moduleBuilder = Segments.DynamicAssemblyHolder.Module;

            var ti = buildType.GetTypeInfo();

            var intFormatterTypeInfo = typeof(Formatter<,>).MakeGenericType(resolverType, typeof(int)).GetTypeInfo();

            var typeBuilder = moduleBuilder.DefineType(
                Segments.DynamicAssemblyHolder.ModuleName + "." + resolverType.FullName.Replace(".", "_") + "." + buildType.FullName + "$Formatter",
                TypeAttributes.Public,
                typeof(Formatter<,>).MakeGenericType(resolverType, buildType));

            var formattersInField = new List<Tuple<int, FieldBuilder, List<Tuple<PropertyInfo, FieldBuilder>>>>();
            foreach(var item in unionCases)
            {
                var fs = new List<Tuple<PropertyInfo, FieldBuilder>>();
                foreach(var info in item.GetFields())
                {
                    var field = typeBuilder.DefineField("<>" + item.Name + info.Name + "Formatter", typeof(Formatter<,>).MakeGenericType(resolverType, info.PropertyType), FieldAttributes.Private | FieldAttributes.InitOnly);
                    fs.Add(Tuple.Create(info, field));
                }
                var unionInfo = typeBuilder.DefineField("<>" + item.Name + "UnionCaseInfo", typeof(Microsoft.FSharp.Reflection.UnionCaseInfo), FieldAttributes.Private | FieldAttributes.InitOnly);
                formattersInField.Add(Tuple.Create(item.Tag, unionInfo, fs));
            }

            // .ctor
            {
                var method = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, Type.EmptyTypes);

                var il = method.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(Formatter<,>).MakeGenericType(resolverType, buildType).GetTypeInfo().GetConstructor(Type.EmptyTypes));

                il.DeclareLocal(typeof(Microsoft.FSharp.Reflection.UnionCaseInfo []));

                il.Emit(OpCodes.Ldtoken, buildType);
                il.EmitCall(OpCodes.Call, typeof(Type).GetTypeInfo().GetMethod("GetTypeFromHandle"), null);
                il.Emit(OpCodes.Ldnull); // equal FSharpOpion<T>.None
                il.Emit(
                    OpCodes.Call,
#if NETSTANDARD
                    FSharpType.getUnionCases
#else
                    typeof(Microsoft.FSharp.Reflection.FSharpType).GetTypeInfo().GetMethod("GetUnionCases", new Type[] { typeof(Type), typeof(FSharpOption<BindingFlags>)})
#endif
                );
                il.Emit(OpCodes.Stloc_0);

                for (var i = 0; i < formattersInField.Count; i++)
                {
                    var item = formattersInField[i];

                    foreach(var field in item.Item3)
                    {
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, field.Item2.FieldType.GetTypeInfo().GetProperty("Default").GetGetMethod());
                        il.Emit(OpCodes.Stfld, field.Item2);
                    }

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);
                    il.Emit(OpCodes.Stfld, item.Item2);
                }

                il.Emit(OpCodes.Ret);
            }

            // public override int? GetLength()
            {
                var method = typeBuilder.DefineMethod("GetLength", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(int?),
                    Type.EmptyTypes);

                var il = method.GetILGenerator();

                il.DeclareLocal(typeof(int?));

                il.Emit(OpCodes.Ldloca_S, (byte)0);
                il.Emit(OpCodes.Initobj, typeof(int?));
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ret);
            }

            // public override int Serialize(ref byte[] bytes, int offset, T value)
            {
                var method = typeBuilder.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(int),
                    new Type[] { typeof(byte[]).MakeByRefType(), typeof(int), buildType });

                var tag = ti.GetProperty("Tag").GetGetMethod();

                var il = method.GetILGenerator();

                il.DeclareLocal(typeof(int)); // startOffset
                il.DeclareLocal(typeof(int)); // writeSize

                var labelA = il.DefineLabel();

                if(ti.IsValueType)
                {
                    il.Emit(OpCodes.Ldarga_S, (byte)3);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_3); // value
                }
                il.Emit(OpCodes.Brtrue_S, labelA);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldc_I4_M1);
                il.Emit(OpCodes.Call, typeof(BinaryUtil).GetTypeInfo().GetMethod("WriteInt32"));
                il.Emit(OpCodes.Ret);

                il.MarkLabel(labelA);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldc_I4_4);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Starg_S, (byte)2);

                // if(value is ...)
                var endLabel = il.DefineLabel();
                var ifElseLabels = new Label[unionCases.Length + 1];
                for (int i = 1; i < unionCases.Length + 1; i++)
                {
                    ifElseLabels[i] = il.DefineLabel();
                }

                for (int i = 0; i < formattersInField.Count; i++)
                {
                    var unionCase = formattersInField[i];

                    if (i != 0) il.MarkLabel(ifElseLabels[i]);
                    if(ti.IsValueType)
                    {
                        il.Emit(OpCodes.Ldarga_S, (byte)3);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg_3);
                    }
                    il.Emit(OpCodes.Call, tag);
                    il.Emit(OpCodes.Ldc_I4, unionCase.Item1);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Brfalse_S, ifElseLabels[i + 1]);

                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, intFormatterTypeInfo.GetProperty("Default").GetGetMethod());
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldc_I4, unionCase.Item1);
                    il.Emit(OpCodes.Callvirt, intFormatterTypeInfo.GetMethod("Serialize"));
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Starg_S, (byte)2);

                    foreach(var item in unionCase.Item3)
                    {
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, item.Item2);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldarg_2);
                        if(ti.IsValueType)
                        {
                            il.Emit(OpCodes.Ldarga_S, (byte)3);
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldarg_3);
                        }
                        il.Emit(OpCodes.Callvirt, item.Item1.GetGetMethod());
                        il.Emit(OpCodes.Callvirt, item.Item2.FieldType.GetTypeInfo().GetMethod("Serialize"));
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Starg_S, (byte)2);
                    }

                    il.Emit(OpCodes.Br, endLabel);
                }
                // else....
                {
                    il.MarkLabel(ifElseLabels.Last());
                    il.Emit(OpCodes.Ldstr, "Unknown case of Discriminated Union: ");
                    if(ti.IsValueType)
                    {
                        il.Emit(OpCodes.Ldarga_S, (byte)3);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg_3);
                    }
                    il.Emit(OpCodes.Callvirt, typeof(Object).GetTypeInfo().GetMethod("GetType"));
                    il.Emit(OpCodes.Callvirt, typeof(Type).GetTypeInfo().GetProperty("FullName").GetGetMethod());
                    il.Emit(OpCodes.Call, typeof(string).GetTypeInfo().GetMethods().First(x => x.GetParameters().Length == 2 && x.GetParameters().All(y => y.ParameterType == typeof(string))));
                    il.Emit(OpCodes.Newobj, typeof(Exception).GetTypeInfo().GetConstructors().First(x => x.GetParameters().Length == 1));
                    il.Emit(OpCodes.Throw);
                }

                // offset - startOffset;
                il.MarkLabel(endLabel);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Stloc_1); // writeSize
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Call, typeof(BinaryUtil).GetTypeInfo().GetMethod("WriteInt32"));
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Ret);
            }

            // public override T Deserialize(ref byte[] bytes, int offset, DirtyTracker tracker, out int byteSize)
            {
                var method = typeBuilder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    buildType,
                    new[] { typeof(byte[]).MakeByRefType(), typeof(int), typeof(DirtyTracker), typeof(int).MakeByRefType() });

                var makeUnion =
#if NETSTANDARD
                    typeof(Microsoft.FSharp.Reflection.FSharpReflectionExtensions).GetTypeInfo()
                        .GetMethod("FSharpValue.MakeUnion.Static");
#else
                    typeof(Microsoft.FSharp.Reflection.FSharpValue).GetTypeInfo().GetMethod(
                        "MakeUnion",
                        new Type[] {
                            typeof(Microsoft.FSharp.Reflection.UnionCaseInfo),
                            typeof(object []),
                            typeof(FSharpOption<BindingFlags>)
                        }
                    );
#endif

                var il = method.GetILGenerator();

                il.DeclareLocal(typeof(int)); // size
                il.DeclareLocal(typeof(int)); // Tag
                il.DeclareLocal(buildType);   // T
                il.DeclareLocal(typeof(object []));

                var labelA = il.DefineLabel();

                il.Emit(OpCodes.Ldarg_S, (byte)4);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, typeof(BinaryUtil).GetTypeInfo().GetMethod("ReadInt32"));
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc_1);
                il.Emit(OpCodes.Stind_I4);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Ldc_I4_M1);

                il.Emit(OpCodes.Bne_Un, labelA);
                il.Emit(OpCodes.Ldarg_S, (byte)4);
                il.Emit(OpCodes.Ldc_I4_4);
                il.Emit(OpCodes.Stind_I4);
                if(ti.IsValueType)
                {
                    il.Emit(OpCodes.Ldloca_S, (byte)2);
                    il.Emit(OpCodes.Initobj, buildType);
                    il.Emit(OpCodes.Ldloc_2);
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }
                il.Emit(OpCodes.Ret);

                il.MarkLabel(labelA);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldc_I4_4);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Starg_S, (byte)2);
                il.Emit(OpCodes.Call, intFormatterTypeInfo.GetProperty("Default").GetGetMethod());
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldloca_S, (byte)0);
                il.Emit(OpCodes.Callvirt, intFormatterTypeInfo.GetMethod("Deserialize"));

                il.Emit(OpCodes.Stloc_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Starg_S, (byte)2);

                // if(value is ...)
                var endLabel = il.DefineLabel();
                var ifElseLabels = new Label[unionCases.Length + 1];
                for (int i = 1; i < unionCases.Length + 1; i++)
                {
                    ifElseLabels[i] = il.DefineLabel();
                }

                for (int i = 0; i < formattersInField.Count; i++)
                {
                    var unionCase = formattersInField[i];

                    if (i != 0) il.MarkLabel(ifElseLabels[i]);

                    il.Emit(OpCodes.Ldloc_1);
                    il.Emit(OpCodes.Ldc_I4, unionCase.Item1);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Brfalse_S, ifElseLabels[i + 1]);

                    il.Emit(OpCodes.Ldc_I4, unionCase.Item3.Count);
                    il.Emit(OpCodes.Newarr, typeof(object));
                    il.Emit(OpCodes.Stloc_3);

                    il.Emit(OpCodes.Ldarg_S, (byte)4);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stind_I4);
                    for(var j = 0; j < unionCase.Item3.Count; j++)
                    {
                        var item = unionCase.Item3[j];

                        il.Emit(OpCodes.Ldloc_3);
                        il.Emit(OpCodes.Ldc_I4, j);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, item.Item2);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Ldarg_3);
                        il.Emit(OpCodes.Ldloca_S, (byte)0);
                        il.Emit(OpCodes.Callvirt, item.Item2.FieldType.GetTypeInfo().GetMethod("Deserialize"));
                        il.Emit(OpCodes.Box, item.Item1.PropertyType);
                        il.Emit(OpCodes.Stelem_Ref);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Starg_S, (byte)2);
                        il.Emit(OpCodes.Ldarg_S, (byte)4);
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldind_I4);
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Stind_I4);
                    }

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, unionCase.Item2);
                    il.Emit(OpCodes.Ldloc_3);
                    il.Emit(OpCodes.Ldnull); // equal FSharpOpion<T>.None
                    il.Emit(OpCodes.Call, makeUnion);
                    il.Emit(OpCodes.Unbox_Any, buildType);

                    il.Emit(OpCodes.Stloc_2);
                    il.Emit(OpCodes.Br, endLabel);
                }
                // else....
                {
                    il.MarkLabel(ifElseLabels.Last());

                    il.Emit(OpCodes.Ldstr, "Unknown Tag type of Discriminated Union, unionKey: {0}");
                    il.Emit(OpCodes.Ldloc_1);
                    il.Emit(OpCodes.Box, typeof(int));
                    il.Emit(OpCodes.Call, typeof(ObjectSegmentHelper).GetTypeInfo().GetMethod("GetException1", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                    il.Emit(OpCodes.Throw);
                }

                il.MarkLabel(endLabel);
                il.Emit(OpCodes.Ldloc_2);
                il.Emit(OpCodes.Ret);
            }

            return typeBuilder.CreateTypeInfo();
        }
    }
}
