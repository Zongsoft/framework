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

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services
{
	public static class ServiceModular
	{
		private static readonly ConcurrentDictionary<ModularServiceKey, Type> _cache = new ConcurrentDictionary<ModularServiceKey, Type>();

		public static IEnumerable<ServiceDescriptor> Build(string module, Type type, Type[] contracts)
		{
			if(contracts == null || contracts.Length == 0)
				yield break;

			for(int i = 0; i < contracts.Length; i++)
			{
				yield return Build(module, type, contracts[i]);
			}
		}

		public static ServiceDescriptor Build(string module, Type type, Type contract)
		{
			if(string.IsNullOrEmpty(module))
				throw new ArgumentNullException(nameof(module));

			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(contract == null)
				contract = type;

			return null;
		}

		public static bool TryGet(string module, Type serviceType, out Type resolvedType)
		{
			resolvedType = null;

			if(string.IsNullOrEmpty(module) || serviceType == null)
				return false;

			return true;
		}

		private static Type GenerateContract(Type type)
		{
			throw new NotImplementedException();
		}

		private static Type GenerateImplementation(Type type, Type[] contracts)
		{
			throw new NotImplementedException();
		}

		private static Func<IServiceProvider, object> GetServiceFactory(Type type, Type[] contracts)
		{
			return services =>
			{
				return ActivatorUtilities.CreateInstance(services, type);
			};
		}

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

	internal interface IModularService<out T> where T : class
	{
		T Service { get; }
	}

	internal class ModularService<T> : IModularService<T> where T : class
	{
		private readonly IServiceProvider _provider;

		public ModularService(IServiceProvider provider)
		{
			_provider = provider;
		}

		public T Service => (T)_provider.GetService(typeof(T));
	}
}
