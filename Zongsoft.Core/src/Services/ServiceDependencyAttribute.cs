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

namespace Zongsoft.Services
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
	public class ServiceDependencyAttribute : Attribute
	{
		#region 构造函数
		/// <summary>构造一个服务注入的注解。</summary>
		public ServiceDependencyAttribute() { }

		/// <summary>构造一个服务注入的注解。</summary>
		/// <param name="serviceType">指定要注入的服务类型。</param>
		public ServiceDependencyAttribute(Type serviceType) => this.ServiceType = serviceType;

		/// <summary>构造一个服务注入的注解。</summary>
		/// <param name="serviceName">指定要注入的服务名称，详细说明请参考 <see cref="ServiceName"/> 属性。</param>
		/// <param name="serviceType">指定要注入的服务类型。</param>
		public ServiceDependencyAttribute(string serviceName, Type serviceType = null)
		{
			this.ServiceName = serviceName;
			this.ServiceType = serviceType;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置服务提供程序的名称，默认值为空(<c>null</c>)。</summary>
		/// <remarks>
		///		<list type="bullet">
		///			<item>
		///				<term>当属性值为空(<c>null</c>)或空字符串(<c>""</c>)，即默认值：</term>
		///				<description>
		///					<para>表示服务容器为注入目标所在应用模块的服务提供程序。</para>
		///					<para>注：如果注入目标所在应用模块的服务提供程序未能找到指定的服务，则会再次从全局服务提供程序中获取。</para>
		///				</description>
		///			</item>
		///			<item>
		///				<term>当属性值为“<c>/</c>”或“<c>*</c>”：</term>
		///				<description>表示忽略注入目标所在应用模块，始终为全局(即应用程序)服务提供程序。</description>
		///			</item>
		///			<item>
		///				<term>其他：</term>
		///				<description>
		///					<para>表示名称为属性值的应用模块的服务提供程序。</para>
		///					<para>注：如果指定应用模块的服务提供程序未能找到指定的服务，则会再次从全局服务提供程序中获取。</para>
		///				</description>
		///			</item>
		///		</list>
		/// </remarks>
		public string Provider { get; set; }

		/// <summary>获取或设置注入的服务名称，默认值为空(<c>null</c>)。</summary>
		/// <remarks>
		///		<list type="table">
		///			<item>
		///				<term>当属性值为空(<c>null</c>)，且注入的服务类型为<see cref="IServiceAccessor{T}"/>：</term>
		///				<description>表示从对应的<see cref="IServiceProvider{T}"/>提供程序中获取的服务名为注入目标所在的应用模块名称。</description>
		///			</item>
		///			<item>
		///				<term>当属性值为空字符串(<c>""</c>)，且注入的服务类型为<see cref="IServiceAccessor{T}"/>：</term>
		///				<description>表示从对应的<see cref="IServiceProvider{T}"/>提供程序中获取的服务名为空字符串（注：名称为空字符串的服务通常为所属容器中的默认服务）。</description>
		///			</item>
		///		</list>
		/// </remarks>
		public string ServiceName { get; set; }

		/// <summary>获取或设置注入的服务类型。</summary>
		public Type ServiceType { get; set; }

		/// <summary>获取或设置注入的对象是否不能为空。</summary>
		public bool IsRequired { get; set; }
		#endregion

		#region 内部方法
		internal bool IsApplicationProvider => this.Provider == "/" || this.Provider == "*";
		#endregion
	}
}
