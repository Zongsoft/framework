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
using System.ComponentModel;

namespace Zongsoft.Services
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
	public class ServiceDependencyAttribute : Attribute
	{
		#region 成员字段
		private string _name;
		private string _provider;
		private Type _contract;
		#endregion

		#region 构造函数
		public ServiceDependencyAttribute()
		{
		}

		public ServiceDependencyAttribute(string name, string provider = null)
		{
			this.Name = name;
			this.Provider = provider;
		}

		public ServiceDependencyAttribute(Type contract, string provider = null)
		{
			this.Contract = contract;
			this.Provider = provider;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置服务的名称。
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
			}
		}

		/// <summary>
		/// 获取或设置服务提供程序的名称。
		/// </summary>
		public string Provider
		{
			get
			{
				return _provider;
			}
			set
			{
				_provider = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
			}
		}

		/// <summary>
		/// 获取或设置服务的契约类型。
		/// </summary>
		public Type Contract
		{
			get
			{
				return _contract;
			}
			set
			{
				_contract = value;
			}
		}

		/// <summary>
		/// 获取或设置注入的对象是否不能为空。
		/// </summary>
		public bool IsRequired
		{
			get; set;
		}
		#endregion
	}
}
