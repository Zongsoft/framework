﻿/*
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

	public static T Resolve<T>(this IServiceProvider serviceProvider, object parameter = null) =>
		parameter == null ? serviceProvider.GetService<T>() : serviceProvider.MatchService<T>(parameter);

	public static T ResolveRequired<T>(this IServiceProvider serviceProvider, object parameter = null) =>
		parameter == null ? serviceProvider.GetRequiredService<T>() : serviceProvider.MatchService<T>(parameter) ?? throw new InvalidOperationException($"No service for type '{typeof(T)}' has been registered.");

	public static object Resolve(this IServiceProvider serviceProvider, Type serviceType, object parameter = null) =>
		parameter == null ? serviceProvider.GetService(serviceType) : serviceProvider.MatchService(serviceType, parameter);

	public static object ResolveRequired(this IServiceProvider serviceProvider, Type serviceType, object parameter) =>
		parameter == null ? serviceProvider.GetRequiredService(serviceType) : serviceProvider.MatchService(serviceType, parameter) ?? throw new InvalidOperationException($"No service for type '{serviceType}' has been registered.");

	public static IEnumerable<object> ResolveAll(this IServiceProvider serviceProvider, string tag) =>
		throw new NotImplementedException();

	public static IEnumerable<T> ResolveAll<T>(this IServiceProvider serviceProvider, object parameter = null) =>
		parameter == null ? serviceProvider.GetServices<T>() : serviceProvider.MatchServices<T>(parameter);

	public static IEnumerable<object> ResolveAll(this IServiceProvider serviceProvider, Type serviceType, object parameter = null) =>
		parameter == null ? serviceProvider.GetServices(serviceType) : serviceProvider.MatchServices(serviceType, parameter);
	#endregion

	#region 服务匹配
	private static T MatchService<T>(this IServiceProvider serviceProvider, object parameter)
	{
		var services = serviceProvider.GetServices<T>();

		foreach(var service in services)
		{
			if(service is IMatcher<Type> matcher)
			{
				if(matcher.Match(typeof(T), parameter))
					return service;
			}
			else if(typeof(IMatchable).IsAssignableFrom(service.GetType()))
			{
				if(((IMatchable)service).Match(parameter))
					return service;
			}
			else
			{
				if(_matches.TryGetValue(typeof(T), out var match))
				{
					if(match.GenericMethod != null && ((Func<T, object, bool>)match.GenericMethod).Invoke(service, parameter))
						return service;
				}
				else
				{
					//动态编译名称匹配方法
					match = _matches.GetOrAdd(typeof(T), type => Compile(type));

					if(match.GenericMethod != null && ((Func<T, object, bool>)match.GenericMethod).Invoke(service, parameter))
						return service;
				}
			}
		}

		return default;
	}

	private static IEnumerable<T> MatchServices<T>(this IServiceProvider serviceProvider, object parameter)
	{
		var services = serviceProvider.GetServices<T>();

		foreach(var service in services)
		{
			if(service is IMatcher<Type> matcher)
			{
				if(matcher.Match(typeof(T), parameter))
					yield return service;
			}
			else if(typeof(IMatchable).IsAssignableFrom(service.GetType()))
			{
				if(((IMatchable)service).Match(parameter))
					yield return service;
			}
			else
			{
				if(_matches.TryGetValue(typeof(T), out var match))
				{
					if(match.GenericMethod != null && ((Func<T, object, bool>)match.GenericMethod).Invoke(service, parameter))
						yield return service;
				}
				else
				{
					//动态编译名称匹配方法
					match = _matches.GetOrAdd(typeof(T), type => Compile(type));

					if(match.GenericMethod != null && ((Func<T, object, bool>)match.GenericMethod).Invoke(service, parameter))
						yield return service;
				}
			}
		}
	}

	private static object MatchService(this IServiceProvider serviceProvider, Type serviceType, object parameter)
	{
		var services = serviceProvider.GetServices(serviceType);

		foreach(var service in services)
		{
			if(service is IMatcher<Type> matcher)
			{
				if(matcher.Match(serviceType, parameter))
					return service;
			}
			else if(typeof(IMatchable).IsAssignableFrom(service.GetType()))
			{
				if(((IMatchable)service).Match(parameter))
					return service;
			}
			else
			{
				if(_matches.TryGetValue(serviceType, out var match))
				{
					if(match.ClassicMethod != null && ((Func<object, object, bool>)match.ClassicMethod).Invoke(service, parameter))
						return service;
				}
				else
				{
					//动态编译名称匹配方法
					match = _matches.GetOrAdd(serviceType, type => Compile(type));

					if(match.ClassicMethod != null && ((Func<object, object, bool>)match.ClassicMethod).Invoke(service, parameter))
						return service;
				}
			}
		}

		return default;
	}

	private static IEnumerable<object> MatchServices(this IServiceProvider serviceProvider, Type serviceType, object parameter)
	{
		var services = serviceProvider.GetServices(serviceType);

		foreach(var service in services)
		{
			if(service is IMatcher<Type> matcher)
			{
				if(matcher.Match(serviceType, parameter))
					yield return service;
			}
			else if(typeof(IMatchable).IsAssignableFrom(service.GetType()))
			{
				if(((IMatchable)service).Match(parameter))
					yield return service;
			}
			else
			{
				if(_matches.TryGetValue(serviceType, out var match))
				{
					if(match.ClassicMethod != null && ((Func<object, object, bool>)match.ClassicMethod).Invoke(service, parameter))
						yield return service;
				}
				else
				{
					//动态编译名称匹配方法
					match = _matches.GetOrAdd(serviceType, type => Compile(type));

					if(match.ClassicMethod != null && ((Func<object, object, bool>)match.ClassicMethod).Invoke(service, parameter))
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
