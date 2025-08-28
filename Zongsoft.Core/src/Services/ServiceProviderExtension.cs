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
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services;

public static class ServiceProviderExtension
{
	#region 私有变量
	private static readonly ConcurrentDictionary<Type, ServiceMatcher> _matches = new();
	private static readonly ConcurrentDictionary<string, Type> _naming = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 注册方法
	internal static bool Register(string name, Type serviceType)
	{
		if(string.IsNullOrEmpty(name))
			return false;

		if(name.Length > 7 && name.EndsWith("Service"))
			_naming.TryAdd(name[..^7], serviceType);

		return _naming.TryAdd(name, serviceType);
	}
	#endregion

	#region 解析方法

	#region 名称解析
	public static object Resolve(this IServiceProvider serviceProvider, string name)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(_naming.TryGetValue(name, out var type))
			return serviceProvider.GetService(type);

		return null;
	}

	public static object ResolveRequired(this IServiceProvider serviceProvider, string name)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(_naming.TryGetValue(name, out var type))
			return serviceProvider.GetRequiredService(type);

		throw new InvalidOperationException($"The service named '{name}' does not exist.");
	}
	#endregion

	#region 类型解析
	public static TService Resolve<TService>(this IServiceProvider serviceProvider) => serviceProvider.GetService<TService>();
	public static TService ResolveRequired<TService>(this IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<TService>();
	public static object Resolve(this IServiceProvider serviceProvider, Type serviceType) => serviceProvider.GetService(serviceType);
	public static object ResolveRequired(this IServiceProvider serviceProvider, Type serviceType) => serviceProvider.GetRequiredService(serviceType);
	public static IEnumerable<TService> ResolveAll<TService>(this IServiceProvider serviceProvider) => serviceProvider.GetServices<TService>();
	public static IEnumerable<object> ResolveAll(this IServiceProvider serviceProvider, Type serviceType) => serviceProvider.GetServices(serviceType);
	#endregion

	#region 服务查找
	public static TService Find<TService>(this IServiceProvider serviceProvider, object argument) => serviceProvider.MatchService<TService>(argument, false);
	public static TService FindRequired<TService>(this IServiceProvider serviceProvider, object argument) => serviceProvider.MatchService<TService>(argument, true);
	public static object Find(this IServiceProvider serviceProvider, Type serviceType, object argument) => serviceProvider.MatchService(serviceType, argument, false);
	public static object FindRequired(this IServiceProvider serviceProvider, Type serviceType, object argument) => serviceProvider.MatchService(serviceType, argument, true);
	public static IEnumerable<TService> FindAll<TService>(this IServiceProvider serviceProvider, object argument) => serviceProvider.MatchServices<TService>(argument);
	public static IEnumerable<object> FindAll(this IServiceProvider serviceProvider, Type serviceType, object argument) => serviceProvider.MatchServices(serviceType, argument);
	#endregion

	#region 标签解析
	public static IEnumerable<Type> GetTags(this IServiceProvider serviceProvider, string tag)
	{
		if(string.IsNullOrEmpty(tag))
			return [];

		var tagged = ServiceAssistant.GetTagged(serviceProvider, tag);
		if(tagged == null)
			return [];

		return tagged.Services.Keys;
	}

	public static IEnumerable<object> Resolves(this IServiceProvider serviceProvider, string tag)
	{
		var tagged = ServiceAssistant.GetTagged(serviceProvider, tag);
		if(tagged == null)
			yield break;

		foreach(var service in tagged.Services)
			yield return serviceProvider.GetService(service.Key);
	}

	public static IEnumerable<TService> Resolves<TService>(this IServiceProvider serviceProvider, string tag)
	{
		var tagged = ServiceAssistant.GetTagged(serviceProvider, tag);
		if(tagged == null)
			yield break;

		foreach(var service in tagged.Services)
		{
			if(service.Key == typeof(TService) || service.Value.Contains(typeof(TService)))
				yield return serviceProvider.GetService<TService>();
		}
	}

	public static IEnumerable<object> Resolves(this IServiceProvider serviceProvider, Type serviceType, string tag)
	{
		var tagged = ServiceAssistant.GetTagged(serviceProvider, tag);
		if(tagged == null)
			yield break;

		foreach(var service in tagged.Services)
		{
			if(service.Key == serviceType || service.Value.Contains(serviceType))
				yield return serviceProvider.GetService(service.Key);
		}
	}
	#endregion

	#endregion

	#region 服务匹配
	private static TService MatchService<TService>(this IServiceProvider serviceProvider, object argument, bool required)
	{
		var services = serviceProvider.GetServices<TService>();

		foreach(var service in services)
		{
			if(service is IMatcher<Type> matcher)
			{
				if(matcher.Match(typeof(TService), argument))
					return service;
			}
			else if(typeof(IMatchable).IsAssignableFrom(service.GetType()))
			{
				if(((IMatchable)service).Match(argument))
					return service;
			}
			else
			{
				if(_matches.TryGetValue(typeof(TService), out var match))
				{
					if(match.GenericMethod != null && ((Func<TService, object, bool>)match.GenericMethod).Invoke(service, argument))
						return service;
				}
				else
				{
					//动态编译名称匹配方法
					match = _matches.GetOrAdd(typeof(TService), Compile);

					if(match.GenericMethod != null && ((Func<TService, object, bool>)match.GenericMethod).Invoke(service, argument))
						return service;
				}
			}
		}

		return required ? throw new InvalidOperationException($"No service for type '{typeof(TService)}' has been registered.") : default;
	}

	private static IEnumerable<TService> MatchServices<TService>(this IServiceProvider serviceProvider, object argument)
	{
		var services = serviceProvider.GetServices<TService>();

		foreach(var service in services)
		{
			if(service is IMatcher<Type> matcher)
			{
				if(matcher.Match(typeof(TService), argument))
					yield return service;
			}
			else if(typeof(IMatchable).IsAssignableFrom(service.GetType()))
			{
				if(((IMatchable)service).Match(argument))
					yield return service;
			}
			else
			{
				if(_matches.TryGetValue(typeof(TService), out var match))
				{
					if(match.GenericMethod != null && ((Func<TService, object, bool>)match.GenericMethod).Invoke(service, argument))
						yield return service;
				}
				else
				{
					//动态编译名称匹配方法
					match = _matches.GetOrAdd(typeof(TService), Compile);

					if(match.GenericMethod != null && ((Func<TService, object, bool>)match.GenericMethod).Invoke(service, argument))
						yield return service;
				}
			}
		}
	}

	private static object MatchService(this IServiceProvider serviceProvider, Type serviceType, object argument, bool required)
	{
		var services = serviceProvider.GetServices(serviceType);

		foreach(var service in services)
		{
			if(service is IMatcher<Type> matcher)
			{
				if(matcher.Match(serviceType, argument))
					return service;
			}
			else if(typeof(IMatchable).IsAssignableFrom(service.GetType()))
			{
				if(((IMatchable)service).Match(argument))
					return service;
			}
			else
			{
				if(_matches.TryGetValue(serviceType, out var match))
				{
					if(match.ClassicMethod != null && ((Func<object, object, bool>)match.ClassicMethod).Invoke(service, argument))
						return service;
				}
				else
				{
					//动态编译名称匹配方法
					match = _matches.GetOrAdd(serviceType, Compile);

					if(match.ClassicMethod != null && ((Func<object, object, bool>)match.ClassicMethod).Invoke(service, argument))
						return service;
				}
			}
		}

		return required ? throw new InvalidOperationException($"No service for type '{serviceType}' has been registered.") : default;
	}

	private static IEnumerable<object> MatchServices(this IServiceProvider serviceProvider, Type serviceType, object argument)
	{
		var services = serviceProvider.GetServices(serviceType);

		foreach(var service in services)
		{
			if(service is IMatcher<Type> matcher)
			{
				if(matcher.Match(serviceType, argument))
					yield return service;
			}
			else if(typeof(IMatchable).IsAssignableFrom(service.GetType()))
			{
				if(((IMatchable)service).Match(argument))
					yield return service;
			}
			else
			{
				if(_matches.TryGetValue(serviceType, out var match))
				{
					if(match.ClassicMethod != null && ((Func<object, object, bool>)match.ClassicMethod).Invoke(service, argument))
						yield return service;
				}
				else
				{
					//动态编译名称匹配方法
					match = _matches.GetOrAdd(serviceType, Compile);

					if(match.ClassicMethod != null && ((Func<object, object, bool>)match.ClassicMethod).Invoke(service, argument))
						yield return service;
				}
			}
		}
	}
	#endregion

	#region 动态编译
	private static readonly MethodInfo StringEqualsMethod = typeof(string).GetMethod(nameof(string.Equals), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string), typeof(StringComparison) }, null);
	private static readonly MethodInfo ObjectToStringMethod = typeof(object).GetMethod(nameof(object.ToString), BindingFlags.Public | BindingFlags.Instance);

	private static ServiceMatcher Compile(Type serviceType)
	{
		return new ServiceMatcher()
		{
			GenericMethod = CompileGeneric(serviceType),
			ClassicMethod = CompileClassic(serviceType),
		};
	}

	/*
	 * 为指定的服务类型生成名称匹配方法，如果指定的服务类型没有定义“Name”属性则返回空。
	 * 生成的动态方法如下所示：
	 * 
	 * bool Match(XXXXX service, object parameter)
	 * {
	 *     return parameter != null && string.Equals(service.Name, parameter.ToString(), StringComparison.OrdinalIgnoreCase);
	 * }
	 */
	private static Delegate CompileGeneric(Type serviceType)
	{
		var property = serviceType.GetProperty("Name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		if(property == null || property.PropertyType != typeof(string) || !property.CanRead)
			return null;

		var serviceParameter = Expression.Parameter(serviceType, "service");
		var parameterParameter = Expression.Parameter(typeof(object), "parameter");
		var parameterNullCheck = Expression.NotEqual(parameterParameter, Expression.Constant(null));
		var parameterToString = Expression.Call(parameterParameter, ObjectToStringMethod);

		var equalsCall = Expression.Call(StringEqualsMethod,
			Expression.Property(serviceParameter, property),
			parameterToString,
			Expression.Constant(StringComparison.OrdinalIgnoreCase));

		var functionType = typeof(Func<,,>).MakeGenericType(serviceType, typeof(object), typeof(bool));

		return Expression.Lambda(
			functionType,
			Expression.Block(typeof(bool), Expression.AndAlso(parameterNullCheck, equalsCall)),
			serviceParameter,
			parameterParameter).Compile();
	}

	/*
	 * 为指定的服务类型生成名称匹配方法，如果指定的服务类型没有定义“Name”属性则返回空。
	 * 生成的动态方法如下所示：
	 * 
	 * bool Match(object service, object parameter)
	 * {
	 *     return parameter != null && string.Equals(((XXXXX)service).Name, parameter.ToString(), StringComparison.OrdinalIgnoreCase);
	 * }
	 */
	private static Delegate CompileClassic(Type serviceType)
	{
		var property = serviceType.GetProperty("Name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		if(property == null || property.PropertyType != typeof(string) || !property.CanRead)
			return null;

		var serviceParameter = Expression.Parameter(typeof(object), "service");
		var parameterParameter = Expression.Parameter(typeof(object), "parameter");
		var parameterNullCheck = Expression.NotEqual(parameterParameter, Expression.Constant(null));
		var parameterToString = Expression.Call(parameterParameter, ObjectToStringMethod);

		var equalsCall = Expression.Call(StringEqualsMethod,
			Expression.Property(Expression.Convert(serviceParameter, serviceType), property),
			parameterToString,
			Expression.Constant(StringComparison.OrdinalIgnoreCase));

		var functionType = typeof(Func<object, object, bool>);

		return Expression.Lambda(
			functionType,
			Expression.Block(typeof(bool), Expression.AndAlso(parameterNullCheck, equalsCall)),
			serviceParameter,
			parameterParameter).Compile();
	}

	private struct ServiceMatcher(Delegate genericMethod, Delegate classicMethod)
	{
		public Delegate GenericMethod = genericMethod;
		public Delegate ClassicMethod = classicMethod;
	}
	#endregion
}
