//Copyright(c) 2016 Yoshifumi Kawai
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;
using System.Collections.Generic;
using System.Linq;
using ZeroFormatter.Formatters;
using ZeroFormatter.Internal;
using ZeroFormatter.Extensions.Segments;
using ZeroFormatter.Extensions.Internal;

namespace ZeroFormatter.Extensions
{
    internal class EmittableMemberInfo
    {
        readonly PropertyInfo info;

        public string Name
        {
            get
            {
                return info.Name;
            }
        }

        public Type MemberType
        {
            get
            {
                return info.PropertyType;
            }
        }

        public EmittableMemberInfo(PropertyInfo info)
        {
            this.info = info;
        }

        public void EmitLoadValue(ILGenerator il)
        {
            il.Emit(OpCodes.Callvirt, info.GetGetMethod());
        }
    }

    internal static class DynamicRecordFormatter
    {
        public static Tuple<int, EmittableMemberInfo>[] GetMembers(Type type)
        {

            //if (type.GetTypeInfo().GetCustomAttributes(typeof(ZeroFormattableAttribute), true).FirstOrDefault() == null)
            //{
            //    throw new InvalidOperationException("Type must be marked with ZeroFormattableAttribute. " + type.Name);
            //}

            var dict = new Dictionary<int, EmittableMemberInfo>();
            foreach (var item in FSharpType.GetRecordFields(type.GetTypeInfo(), FSharpOption<BindingFlags>.Some(BindingFlags.Public)))
            {
                if (item.GetCustomAttributes(typeof(IgnoreFormatAttribute), true).Any()) continue;

                var index = item.GetCustomAttributes(typeof(IndexAttribute), true).Cast<IndexAttribute>().FirstOrDefault();
                if (index == null)
                {
                    throw new InvalidOperationException("Public property must be marked with IndexAttribute or IgnoreFormatAttribute. " + type.Name + "." + item.Name);
                }

                if (dict.ContainsKey(index.Index))
                {
                    throw new InvalidOperationException("IndexAttribute is not allow duplicate number. " + type.Name + "." + item.Name + ", Index:" + index.Index);
                }

                var info = new EmittableMemberInfo(item);
                // VerifyMember(info);
                dict[index.Index] = info;
            }

            return dict.OrderBy(x => x.Key).Select(x => Tuple.Create(x.Key, x.Value)).ToArray();
        }

        public static object Create<TTypeResolver, T>()
            where TTypeResolver : ITypeResolver, new()
        {
            var resolverType = typeof(TTypeResolver);
            var t = typeof(T);
            var ti = t.GetTypeInfo();
            if (!FSharpType.IsRecord(ti, FSharpOption<BindingFlags>.Some(BindingFlags.Public)))
            {
                throw new InvalidOperationException("Type must be F# record. " + t.Name);
            }

            var elementType = t;

            var members = GetMembers(elementType);
            var length = ValidateAndCalculateLength(resolverType, elementType, members);
            var generateTypeInfo = BuildFormatter(DynamicAssemblyHolder.Module, resolverType, elementType, length, members);
            var formatter = Activator.CreateInstance(generateTypeInfo.AsType());

            return formatter;
        }

        static int? ValidateAndCalculateLength(Type resolverType, Type t, IEnumerable<Tuple<int, EmittableMemberInfo>> source)
        {
            var isNullable = false;
            var lengthSum = 0;

            var constructorTypes = new List<Type>();
            var expected = 0;
            foreach (var item in source)
            {
                if (item.Item1 != expected) throw new InvalidOperationException("F# record index must be started with 0 and be sequential. Type: " + t.FullName + " InvalidIndex:" + item.Item1);
                expected++;

                var formatter = typeof(Formatter<,>).MakeGenericType(resolverType, item.Item2.MemberType).GetTypeInfo().GetProperty("Default");
                var len = (formatter.GetGetMethod().Invoke(null, Type.EmptyTypes) as IFormatter).GetLength();
                if (len != null)
                {
                    lengthSum += len.Value;
                }
                else
                {
                    isNullable = true;
                }

                constructorTypes.Add(item.Item2.MemberType);
            }

            if (expected != 0)
            {
                var info = t.GetTypeInfo().GetConstructor(constructorTypes.ToArray());
                if (info == null)
                {
                    throw new InvalidOperationException("F# record needs full parameter constructor of index property types. Type:" + t.FullName);
                }
            }

            return isNullable ? (int?)null : lengthSum;
        }

        static TypeInfo BuildFormatter(ModuleBuilder builder, Type resolverType, Type elementType, int? length, Tuple<int, EmittableMemberInfo>[] memberInfos)
        {
            var typeBuilder = builder.DefineType(
                DynamicAssemblyHolder.ModuleName + "." + resolverType.FullName.Replace(".", "_") + "." + elementType.FullName + "$Formatter",
                TypeAttributes.Public,
                typeof(Formatter<,>).MakeGenericType(resolverType, elementType));

            // field
            var formattersInField = new List<Tuple<int, EmittableMemberInfo, FieldBuilder>>();
            foreach (var item in memberInfos)
            {
                var field = typeBuilder.DefineField("<>" + item.Item2.Name + "Formatter", typeof(Formatter<,>).MakeGenericType(resolverType, item.Item2.MemberType), FieldAttributes.Private | FieldAttributes.InitOnly);
                formattersInField.Add(Tuple.Create(item.Item1, item.Item2, field));
            }
            // .ctor
            {
                var method = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, Type.EmptyTypes);

                var il = method.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(Formatter<,>).MakeGenericType(resolverType, elementType).GetTypeInfo().GetConstructor(Type.EmptyTypes));

                foreach (var item in formattersInField)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, item.Item3.FieldType.GetTypeInfo().GetProperty("Default").GetGetMethod());
                    il.Emit(OpCodes.Stfld, item.Item3);
                }
                il.Emit(OpCodes.Ret);
            }
            // public override bool NoUseDirtyTracker
            {
                var method = typeBuilder.DefineMethod("get_NoUseDirtyTracker", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(bool),
                    Type.EmptyTypes);

                var il = method.GetILGenerator();

                if (formattersInField.Count == 0)
                {
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    var label = il.DefineLabel();
                    for (int i = 0; i < formattersInField.Count; i++)
                    {
                        var field = formattersInField[i];
                        if (i != 0) il.Emit(OpCodes.Brfalse, label);

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, field.Item3);
                        il.Emit(OpCodes.Callvirt, field.Item3.FieldType.GetTypeInfo().GetProperty("NoUseDirtyTracker").GetGetMethod());
                    }

                    il.Emit(OpCodes.Ret);
                    il.MarkLabel(label);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ret);
                }

                var props = typeBuilder.DefineProperty("NoUseDirtyTracker", PropertyAttributes.None,
                    typeof(bool),
                    Type.EmptyTypes);
                props.SetGetMethod(method);
            }
            // public override int? GetLength()
            {
                var method = typeBuilder.DefineMethod("GetLength", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(int?),
                    Type.EmptyTypes);

                var il = method.GetILGenerator();
                if (length == null)
                {
                    il.DeclareLocal(typeof(int?));

                    il.Emit(OpCodes.Ldloca_S, (byte)0);
                    il.Emit(OpCodes.Initobj, typeof(int?));
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    il.EmitLdc_I4(length.Value);
                    il.Emit(OpCodes.Newobj, typeof(int?).GetTypeInfo().GetConstructor(new[] { typeof(int) }));
                    il.Emit(OpCodes.Ret);
                }
            }
            // public override int Serialize(ref byte[] bytes, int offset, T value)
            {
                var method = typeBuilder.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(int),
                    new Type[] { typeof(byte[]).MakeByRefType(), typeof(int), elementType });

                var il = method.GetILGenerator();

                il.DeclareLocal(typeof(int)); // startOffset

                if (length != null)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.EmitLdc_I4(length.Value);
                    il.Emit(OpCodes.Call, typeof(BinaryUtil).GetTypeInfo().GetMethod("EnsureCapacity"));
                }

                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stloc_0);

                foreach (var item in formattersInField)
                {
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, item.Item3);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_3);
                    item.Item2.EmitLoadValue(il);
                    il.Emit(OpCodes.Callvirt, item.Item3.FieldType.GetTypeInfo().GetMethod("Serialize"));
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Starg_S, (byte)2);
                }

                // offset - startOffset;
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Ret);
            }
            //// public override T Deserialize(ref byte[] bytes, int offset, DirtyTracker tracker, out int byteSize)
            {
                var method = typeBuilder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    elementType,
                    new[] { typeof(byte[]).MakeByRefType(), typeof(int), typeof(DirtyTracker), typeof(int).MakeByRefType() });

                var il = method.GetILGenerator();

                if (memberInfos.Length != 0)
                {
                    il.DeclareLocal(typeof(int)); // size
                    foreach (var item in formattersInField)
                    {
                        il.DeclareLocal(item.Item2.MemberType); // item1, item2...
                    }
                    il.DeclareLocal(elementType); // result

                    il.Emit(OpCodes.Ldarg_S, (byte)4);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stind_I4);
                    foreach (var item in formattersInField)
                    {
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, item.Item3);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Ldarg_3);
                        il.Emit(OpCodes.Ldloca_S, (byte)0);
                        il.Emit(OpCodes.Callvirt, item.Item3.FieldType.GetTypeInfo().GetMethod("Deserialize"));
                        il.Emit(OpCodes.Stloc, item.Item1 + 1);
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

                    for (int i = 0; i < memberInfos.Length; i++)
                    {
                        il.Emit(OpCodes.Ldloc, i + 1);
                    }

                    var constructor = elementType.GetTypeInfo().GetConstructor(memberInfos.Select(x => x.Item2.MemberType).ToArray());
                    il.Emit(OpCodes.Newobj, constructor);
                }
                else
                {
                    il.DeclareLocal(elementType);
                    il.Emit(OpCodes.Ldloca_S, (byte)0);
                    il.Emit(OpCodes.Initobj, elementType);
                    il.Emit(OpCodes.Ldloc_0);
                }
                il.Emit(OpCodes.Ret);
            }

            return typeBuilder.CreateTypeInfo();
        }
    }
}
