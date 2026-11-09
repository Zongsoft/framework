/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Concurrent;

namespace Zongsoft.Services;

internal static class ModularServiceUtility
{
	#region 常量定义
	private const string ASSEMBLY_NAME = "Zongsoft.Dynamics.Services";
	#endregion

	#region 私有变量
	private static readonly AssemblyBuilder _assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(ASSEMBLY_NAME), AssemblyBuilderAccess.Run);
	private static readonly ModuleBuilder _module = _assembly.DefineDynamicModule(ASSEMBLY_NAME);
	private static readonly ConcurrentDictionary<ModularServiceKey, Type> _cache = new ConcurrentDictionary<ModularServiceKey, Type>();
	#endregion

	#region 公共方法
	public static bool TryGetModularServiceType(string module, Type contractType, out Type modularType)
	{
		return _cache.TryGetValue(new ModularServiceKey(module, contractType), out modularType);
	}

	public static bool TryGetModularServiceType(object target, Type contractType, out Type modularType)
	{
		modularType = null;
		var module = ApplicationModuleAttribute.Find(target.GetType())?.Name;
		return !string.IsNullOrEmpty(module) && _cache.TryGetValue(new ModularServiceKey(module, contractType), out modularType);
	}

	private static Type GetModularServiceType(string module, Type contractType)
	{
		return _cache.GetOrAdd(new ModularServiceKey(module, contractType), key =>
		{
			var definition = _module.DefineType(
				GetModularServiceName(key.Module, key.ServiceType),
				TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
				typeof(ModularServiceBase<>).MakeGenericType(contractType));

			//定义第一个构造函数（一个 Type 类型参数）
			var constructor = definition.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, new[] { typeof(Type) });
			var generator = constructor.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Call, typeof(ModularServiceBase<>).MakeGenericType(contractType).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new[] { typeof(Type) }));
			generator.Emit(OpCodes.Ret);

			//定义第二个构造函数（一个 object 类型参数）
			constructor = definition.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, new[] { typeof(object) });
			generator = constructor.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Call, typeof(ModularServiceBase<>).MakeGenericType(contractType).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new[] { typeof(object) }));
			generator.Emit(OpCodes.Ret);

			return definition.CreateType();
		});

		static string GetModularServiceName(string module, Type type) => $"{module}:{type.FullName}!ModularService";
	}

	public static IModularService GetModularService(string module, Type contractType, object service)
	{
		var modularType = GetModularServiceType(module, contractType);
		return (IModularService)Activator.CreateInstance(modularType, new object[] { service });
	}

	public static IModularService GetModularService(string module, Type contractType, Type serviceType)
	{
		var modularType = GetModularServiceType(module, contractType);
		return (IModularService)Activator.CreateInstance(modularType, new object[] { serviceType });
	}
	#endregion

	#region 嵌套结构
	private readonly struct ModularServiceKey(string module, Type serviceType) : IEquatable<ModularServiceKey>
	{
		public readonly string Module = module;
		public readonly Type ServiceType = serviceType;

		public bool Equals(ModularServiceKey other) => string.Equals(this.Module, other.Module, StringComparison.Ordinal) && this.ServiceType == other.ServiceType;
		public override bool Equals(object obj) => obj is ModularServiceKey other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Module, this.ServiceType);
		public override string ToString() => $"{this.Module}:{this.ServiceType.FullName}";
	}
	#endregion
}
