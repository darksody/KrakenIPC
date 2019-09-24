using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KrakenIPC
{
    internal static class ProxyHelper
    {
        internal static T GetInstance<T>(Func<string, List<object>, List<object>, Type, object> callbackHook)
        {
            var @interface = typeof(T);

            //build type
            var assemblyName = new AssemblyName($"IpcContract_{@interface.Name}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            var typeBuilder = moduleBuilder.DefineType($"IpcProxy_{@interface.Name}", TypeAttributes.Public | TypeAttributes.Class);
            typeBuilder.AddInterfaceImplementation(@interface);

            var paramValuesListType = typeof(List<object>);
            var paramTypesListType = typeof(List<object>);
            //define our hook func
            var funcType = typeof(Func<string, List<object>, List<object>, Type, object>);
            var fieldBuilder = typeBuilder.DefineField("MethodHook", funcType, FieldAttributes.Public);
            var invokeMethod = fieldBuilder.FieldType.GetMethod("Invoke", new Type[] { typeof(string), paramValuesListType, paramTypesListType, typeof(Type) });

            var methods = @interface.GetMethods();
            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                var returnType = method.ReturnType;

                //define method
                var methodBuilder = typeBuilder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual);
                var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
                methodBuilder.SetParameters(parameterTypes);
                methodBuilder.SetReturnType(returnType);

                //build method logic
                var ilGenerator = methodBuilder.GetILGenerator();

                //build local list variable with the parameter values
                GenerateParameterValuesList(paramValuesListType, parameters, ilGenerator);

                //build parameter types list and load it to local variable 1
                GenerateParameterTypesList(paramTypesListType, parameters, ilGenerator);

                //call the hook func
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
                ilGenerator.Emit(OpCodes.Ldstr, method.Name);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ldloc_1);

                //trying to load the return type
                ilGenerator.Emit(OpCodes.Ldtoken, returnType);
                ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new Type[1] { typeof(RuntimeTypeHandle) }));

                ilGenerator.Emit(OpCodes.Call, invokeMethod);
                if (returnType == typeof(void))
                {
                    ilGenerator.Emit(OpCodes.Pop);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, returnType);
                }
                ilGenerator.Emit(OpCodes.Ret);
            }

            var generatedType = typeBuilder.CreateTypeInfo().AsType();
            dynamic instance = Activator.CreateInstance(generatedType);
            instance.MethodHook = callbackHook;

            return (T)instance;
        }

        private static void GenerateParameterValuesList(Type paramValuesListType, ParameterInfo[] parameters, ILGenerator ilGenerator)
        {
            LocalBuilder list = ilGenerator.DeclareLocal(paramValuesListType);
            ConstructorInfo listConstructor = paramValuesListType.GetConstructor(new Type[] { });
            MethodInfo methodinfo_add = paramValuesListType.GetMethod("Add", new Type[] { typeof(object) });

            ilGenerator.Emit(OpCodes.Newobj, listConstructor);
            ilGenerator.Emit(OpCodes.Stloc_0);

            if (parameters != null && parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    ilGenerator.Emit(OpCodes.Ldloc_0);
                    ilGenerator.Emit(OpCodes.Ldarg_S, i + 1);
                    ilGenerator.Emit(OpCodes.Box, parameters[i].ParameterType);
                    ilGenerator.Emit(OpCodes.Callvirt, methodinfo_add);
                }
            }
        }

        private static void GenerateParameterTypesList(Type paramTypesListType, ParameterInfo[] parameters, ILGenerator ilGenerator)
        {
            LocalBuilder paramTypesListBuilder = ilGenerator.DeclareLocal(paramTypesListType);
            ConstructorInfo paramTypesListConstructor = paramTypesListType.GetConstructor(new Type[] { });
            MethodInfo listParamTypes_methodinfo_add = paramTypesListType.GetMethod("Add", new Type[] { typeof(object) });

            ilGenerator.Emit(OpCodes.Newobj, paramTypesListConstructor);
            ilGenerator.Emit(OpCodes.Stloc_1);

            if (parameters != null && parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    ilGenerator.Emit(OpCodes.Ldloc_1);

                    ilGenerator.Emit(OpCodes.Ldtoken, parameters[i].ParameterType);
                    ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new Type[1] { typeof(RuntimeTypeHandle) }));
                    ilGenerator.Emit(OpCodes.Callvirt, listParamTypes_methodinfo_add);
                }
            }
        }


        internal static object GetDefault(Type t)
        {
            if (t == typeof(void))
            {
                return null;
            }
            Func<object> f = GetDefault<object>;
            return f.Method.GetGenericMethodDefinition().MakeGenericMethod(t).Invoke(null, null);
        }

        private static T GetDefault<T>()
        {
            return default(T);
        }
    }
}
