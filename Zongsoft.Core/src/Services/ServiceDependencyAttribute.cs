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
		public ServiceDependencyAttribute()
		{
		}

		public ServiceDependencyAttribute(object parameter, string provider = null)
		{
			this.Parameter = parameter;
			this.Provider = provider;
		}

		public ServiceDependencyAttribute(Type serviceType, string provider = null)
		{
			this.ServiceType = serviceType;
			this.Provider = provider;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置服务的匹配参数。
		/// </summary>
		public object Parameter { get; set; }

		/// <summary>
		/// 获取或设置服务提供程序的名称。
		/// </summary>
		public string Provider { get; set; }

		/// <summary>
		/// 获取或设置依赖的服务类型。
		/// </summary>
		public Type ServiceType { get; set; }

		/// <summary>
		/// 获取或设置注入的对象是否不能为空。
		/// </summary>
		public bool IsRequired { get; set; }
		#endregion
	}
}
