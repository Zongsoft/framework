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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services
{
	public static class ServiceProviderExtension
	{
		#region 私有变量
		private static readonly ConcurrentDictionary<string, Type> _naming = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 内部方法
		internal static bool Register(string name, Type serviceType)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			if(name.Length > 7 && name.EndsWith("Service"))
				_naming.TryAdd(name.Substring(0, name.Length - 7), serviceType);

			return _naming.TryAdd(name, serviceType);
		}
		#endregion

		#region 公共方法
		public static Data.IDataAccess GetDataAccess(this IServiceProvider services, bool required = false)
		{
			return GetDataAccess(services, null, required);
		}

		public static Data.IDataAccess GetDataAccess(this IServiceProvider services, string name = null, bool required = false)
		{
			var data = services.GetRequiredService<Data.IDataAccessProvider>().GetAccessor(name);

			if(data == null && required)
			{
				if(string.IsNullOrEmpty(name))
					throw new InvalidOperationException($"Unable to acquire the default data accessor.");
				else
					throw new InvalidOperationException($"Unable to acquire the data accessor with the name ‘{name}’.");
			}

			return data;
		}

		public static object Resolve(this IServiceProvider services, string name)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(_naming.TryGetValue(name, out var type))
				return services.GetService(type);

			return null;
		}

		public static object ResolveRequired(this IServiceProvider services, string name)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(_naming.TryGetValue(name, out var type))
				return services.GetRequiredService(type);

			throw new InvalidOperationException($"The service named '{name}' does not exist.");
		}

		public static T Resolve<T>(this IServiceProvider services)
		{
			return services.GetService<T>();
		}

		public static T ResolveRequired<T>(this IServiceProvider services)
		{
			return services.GetRequiredService<T>();
		}

		public static object ResolveRequired(this IServiceProvider services, Type serviceType)
		{
			return services.GetRequiredService(serviceType);
		}

		public static T GetMatchedService<T>(this IServiceProvider services, object parameter)
		{
			return (T)GetMatchedService(services, typeof(T), parameter);
		}

		public static IEnumerable<T> GetMatchedServices<T>(this IServiceProvider services, object parameter)
		{
			var result = GetMatchedServices(services, typeof(T), parameter);

			if(result == null)
				yield break;

			foreach(var item in result)
			{
				yield return (T)item;
			}
		}

		public static object GetMatchedService(this IServiceProvider services, Type type, object parameter)
		{
			if(services == null)
				throw new ArgumentNullException(nameof(services));
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			var instance = services.GetService(type);

			if(instance != null)
			{
				if(instance is Collections.IMatchable matchable && matchable.Match(parameter))
					return instance;

				var matcher = Collections.Matcher.GetMatcher(instance.GetType());

				if(matcher != null && matcher.Match(instance, parameter))
					return instance;
			}

			return null;
		}

		public static IEnumerable GetMatchedServices(this IServiceProvider services, Type type, object parameter)
		{
			if(services == null)
				throw new ArgumentNullException(nameof(services));
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			var items = services.GetServices(type);

			foreach(var item in items)
			{
				if(item is Collections.IMatchable matchable && matchable.Match(parameter))
					yield return item;

				var matcher = Collections.Matcher.GetMatcher(item.GetType());

				if(matcher != null && matcher.Match(item, parameter))
					yield return item;
			}
		}
		#endregion
	}
}
