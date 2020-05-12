/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Services
{
	internal static class ServiceModular
	{
		#region 常量定义
		private const string ASSEMBLY_NAME = "Zongsoft.Dynamics.Services";
		#endregion

		#region 私有变量
		private static readonly AssemblyBuilder _assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(ASSEMBLY_NAME), AssemblyBuilderAccess.RunAndCollect);
		private static readonly ModuleBuilder _module = _assembly.DefineDynamicModule(ASSEMBLY_NAME);

		private static readonly Dictionary<string, TypeBuilder> _definitions = new Dictionary<string, TypeBuilder>(StringComparer.OrdinalIgnoreCase);
		private static readonly ConcurrentDictionary<ModularServiceKey, Type> _cache = new ConcurrentDictionary<ModularServiceKey, Type>();
		#endregion

		#region 公共方法
		public static bool TryGetContract(string module, Type type, out Type contract)
		{
			return _cache.TryGetValue(new ModularServiceKey(module, type), out contract);
		}

		public static bool TryGetContract(Type type, out Type contract)
		{
			contract = null;
			var module = GetModuleName(type);
			return string.IsNullOrEmpty(module) ? false : _cache.TryGetValue(new ModularServiceKey(module, type), out contract);
		}

		public static Type GenerateContract(string module, Type type)
		{
			static string GetContractName(string name)
			{
				return "IModularService_$" + name.Replace('.', '_');
			}

			return _cache.GetOrAdd(new ModularServiceKey(module, type), key =>
			{
				if(!_definitions.TryGetValue(module, out var definition))
				{
					definition = _module.DefineType(GetContractName(key.Module), TypeAttributes.NotPublic | TypeAttributes.Interface | TypeAttributes.Abstract);
					definition.DefineGenericParameters("T")[0].SetGenericParameterAttributes(GenericParameterAttributes.Covariant | GenericParameterAttributes.ReferenceTypeConstraint);
					_definitions.Add(module, definition);
				}

				return definition.CreateType().MakeGenericType(key.ServiceType);
			});
		}
		#endregion

		#region 内部方法
		internal static string GetModuleName(Type type)
		{
			if(type.Assembly.IsDynamic ||
			   type.Assembly.FullName.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
			   type.Assembly.FullName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase))
				return null;

			var attribute = type.Assembly.GetCustomAttribute<ApplicationModuleAttribute>();

			if(attribute != null)
				return attribute.Name;

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach(var assembly in type.Assembly.GetReferencedAssemblies())
			{
				var found = assemblies.FirstOrDefault(a => a.FullName == assembly.FullName);

				if(found != null)
				{
					attribute = found.GetCustomAttribute<ApplicationModuleAttribute>();

					if(attribute != null)
						return attribute.Name;
				}
			}

			return null;
		}
		#endregion

		private readonly struct ModularServiceKey : IEquatable<ModularServiceKey>
		{
			public readonly string Module;
			public readonly Type ServiceType;

			public ModularServiceKey(string module, Type serviceType)
			{
				this.Module = module.ToLowerInvariant();
				this.ServiceType = serviceType;
			}

			public bool Equals(ModularServiceKey other)
			{
				return string.Equals(this.Module, other.Module, StringComparison.Ordinal) &&
					this.ServiceType == other.ServiceType;
			}

			public override bool Equals(object obj)
			{
				if(obj == null || obj.GetType() != this.GetType())
					return false;

				return this.Equals((ModularServiceKey)obj);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(this.Module, this.ServiceType);
			}

			public override string ToString()
			{
				return this.Module + ":" + this.ServiceType.FullName;
			}
		}
	}
}
