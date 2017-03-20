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

            var formattersInField = new List<Tuple<int, FieldBuilder, UnionSerializationInfo>>();
            foreach(var item in unionCases)
            {
                var unionInfo = typeBuilder.DefineField("<>" + item.Name + "UnionCaseInfo", typeof(Microsoft.FSharp.Reflection.UnionCaseInfo), FieldAttributes.Private | FieldAttributes.InitOnly);
                formattersInField.Add(Tuple.Create(item.Tag, unionInfo, UnionSerializationInfo.CreateOrNull(buildType, resolverType, typeBuilder, item)));
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

                    foreach(var member in item.Item3.Members)
                    {
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, member.Formatter.FieldType.GetTypeInfo().GetProperty("Default").GetGetMethod());
                        il.Emit(OpCodes.Stfld, member.Formatter);
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

                    foreach(var item in unionCase.Item3.Members)
                    {
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, item.Formatter);
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
                        il.Emit(OpCodes.Callvirt, item.PropertyInfo.GetGetMethod());
                        il.Emit(OpCodes.Callvirt, item.Formatter.FieldType.GetTypeInfo().GetMethod("Serialize"));
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

                var il = method.GetILGenerator();

                il.DeclareLocal(typeof(int)); // size
                il.DeclareLocal(typeof(int)); // Tag
                il.DeclareLocal(buildType);   // T

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

                    il.Emit(OpCodes.Ldarg_S, (byte)4);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stind_I4);
                    foreach (var item in unionCase.Item3.Members)
                    {
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, item.Formatter);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Ldarg_3);
                        il.Emit(OpCodes.Ldloca_S, (byte)0);
                        il.Emit(OpCodes.Callvirt, item.Formatter.FieldType.GetTypeInfo().GetMethod("Deserialize"));
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

                    il.Emit(OpCodes.Call, unionCase.Item3.NewMethod);

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

    internal class UnionSerializationInfo
    {
        public MethodInfo NewMethod { get; set; }
        public EmittableMember[] Members { get; set; }

        UnionSerializationInfo() { }

        public static UnionSerializationInfo CreateOrNull(Type type, Type resolverType, TypeBuilder typeBuilder, Microsoft.FSharp.Reflection.UnionCaseInfo caseInfo)
        {
            var ti = type.GetTypeInfo();

            var members = new List<EmittableMember>();

            foreach (var item in caseInfo.GetFields())
            {
                var field = typeBuilder.DefineField("<>" + item.Name + item.Name + "Formatter", typeof(Formatter<,>).MakeGenericType(resolverType, item.PropertyType), FieldAttributes.Private | FieldAttributes.InitOnly);
                var member = new EmittableMember
                {
                    PropertyInfo = item,
                    Formatter = field
                };
                members.Add(member);
            }

            MethodInfo method;
            var methodParameters = new List<EmittableMember>();

            if (caseInfo.GetFields().Any())
            {
                method = ti.GetMethod("New" + caseInfo.Name, BindingFlags.Static | BindingFlags.Public);
            }
            else
            {
                method = ti.GetProperty(caseInfo.Name, BindingFlags.Public | BindingFlags.Static).GetGetMethod();
            }

            return new UnionSerializationInfo
            {
                NewMethod = method,
                Members = members.ToArray()
            };
        }

        public class EmittableMember
        {
            public Type Type { get { return PropertyInfo.PropertyType; } }
            public PropertyInfo PropertyInfo { get; set; }

            public FieldBuilder Formatter { get; set; }

            public void EmitLoadValue(ILGenerator il)
            {
                il.Emit(OpCodes.Call, PropertyInfo.GetGetMethod());
            }
        }
    }
}