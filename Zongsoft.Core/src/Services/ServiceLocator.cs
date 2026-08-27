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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Globalization;
using System.ComponentModel;

namespace Zongsoft.Services;

/// <summary>提供基于名称和服务类型定位服务的功能。</summary>
/// <remarks>
/// <para>限定名由服务名称和可选的服务容器名称组成，其格式为：<c>name@container</c>。</para>
/// <para>当未指定服务容器时，将依次按注册名称、服务名称以及<see cref="IServiceProvider{T}"/>服务提供程序进行查找。</para>
/// </remarks>
public static class ServiceLocator
{
	/// <summary>从当前应用程序的服务容器中定位指定类型的服务。</summary>
	/// <typeparam name="T">指定要定位的服务类型。</typeparam>
	/// <param name="qualifiedName">指定服务限定名，其格式为：<c>name@container</c>；如果为空则按服务类型直接解析。</param>
	/// <returns>返回定位到的服务；如果指定的服务不存在则返回空(<c>null</c>)。</returns>
	public static T Locate<T>(string qualifiedName) where T : class => Locate<T>(ApplicationContext.Current?.Services, qualifiedName);

	/// <summary>从指定的服务容器中定位指定类型的服务。</summary>
	/// <typeparam name="T">指定要定位的服务类型。</typeparam>
	/// <param name="services">指定要查找的服务容器。</param>
	/// <param name="qualifiedName">指定服务限定名，其格式为：<c>name@container</c>；如果为空则按服务类型直接解析。</param>
	/// <returns>返回定位到的服务；如果指定的服务不存在则返回空(<c>null</c>)。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="services"/>为空。</exception>
	public static T Locate<T>(this IServiceProvider services, string qualifiedName) where T : class
	{
		ArgumentNullException.ThrowIfNull(services);

		if(string.IsNullOrWhiteSpace(qualifiedName))
			return services.Resolve<T>();

		(var name, var container) = Resolve(qualifiedName);

		if(string.IsNullOrEmpty(container))
		{
			var service = services.Resolve(name);

			return service as T ??
			       services.Find<T>(name) ??
			       services.Resolve<IServiceProvider<T>>()?.GetService(name);
		}

		var provider = services.Resolve(container) as IServiceProvider<T> ?? services.Find<IServiceProvider<T>>(container);
		return provider?.GetService(name);

	}

	/// <summary>从当前应用程序的服务容器中定位指定类型的服务。</summary>
	/// <param name="qualifiedName">指定服务限定名，其格式为：<c>name@container</c>；如果为空则按服务类型直接解析。</param>
	/// <param name="serviceType">指定要定位的服务类型。</param>
	/// <returns>返回定位到的服务；如果指定的服务不存在则返回空(<c>null</c>)。</returns>
	public static object Locate(string qualifiedName, Type serviceType) => Locate(ApplicationContext.Current?.Services, qualifiedName, serviceType);

	/// <summary>从指定的服务容器中定位指定类型的服务。</summary>
	/// <param name="services">指定要查找的服务容器。</param>
	/// <param name="qualifiedName">指定服务限定名，其格式为：<c>name@container</c>；如果为空则按服务类型直接解析。</param>
	/// <param name="serviceType">指定要定位的服务类型。</param>
	/// <returns>返回定位到的服务；如果指定的服务不存在则返回空(<c>null</c>)。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="services"/>为空。</exception>
	public static object Locate(this IServiceProvider services, string qualifiedName, Type serviceType)
	{
		ArgumentNullException.ThrowIfNull(services);

		if(string.IsNullOrWhiteSpace(qualifiedName))
			return services.Resolve(serviceType);

		(var name, var container) = Resolve(qualifiedName);
		var providerType = typeof(IServiceProvider<>).MakeGenericType(serviceType);

		if(string.IsNullOrEmpty(container))
		{
			var service = services.Resolve(name);

			if(service != null && serviceType.IsAssignableFrom(service.GetType()))
				return service;

			service = services.Find(serviceType, name);
			if(service != null)
				return service;

			return services.Resolve(providerType) is IServiceProvider<object> namedServiceProvider ? namedServiceProvider.GetService(name) : null;
		}

		var provider = services.Resolve(container);
		if(provider == null || !providerType.IsInstanceOfType(provider))
			provider = services.Find(providerType, container);

		return provider != null && providerType.IsInstanceOfType(provider) && provider is IServiceProvider<object> containerServiceProvider ?
			containerServiceProvider.GetService(name) : null;
	}

	static (string name, string container) Resolve(ReadOnlySpan<char> text)
	{
		var index = text.IndexOf('@');

		return index < 0 ?
			(text.Trim().ToString(), null) :
			(text[..index].Trim().ToString(), text[(index + 1)..].Trim().ToString());
	}

	/// <summary>提供将服务限定名转换为对应服务实例的类型转换器。</summary>
	/// <remarks>
	/// 无参构造的转换器从<see cref="ITypeDescriptorContext.PropertyDescriptor"/>获取服务类型；
	/// 也可以通过<see cref="Converter(Type)"/>显式指定服务类型，以支持无类型描述上下文的转换场景。
	/// </remarks>
	public sealed class Converter : TypeConverter
	{
		#region 成员字段
		private readonly Type _serviceType;
		#endregion

		#region 构造函数
		public Converter(Type serviceType) => _serviceType = serviceType;
		#endregion

		#region 重写方法
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
			sourceType == typeof(string) && IsServiceType(this.GetServiceType(context)) || base.CanConvertFrom(context, sourceType);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			var serviceType = this.GetServiceType(context);

			return value is string text && IsServiceType(serviceType) ?
				Locate(text, serviceType) : base.ConvertFrom(context, culture, value);
		}
		#endregion

		#region 私有方法
		private Type GetServiceType(ITypeDescriptorContext context) => _serviceType ?? context?.PropertyDescriptor?.PropertyType;
		private static bool IsServiceType(Type type) => type != null && !type.ContainsGenericParameters && (type.IsClass || type.IsInterface);
		#endregion
	}
}
