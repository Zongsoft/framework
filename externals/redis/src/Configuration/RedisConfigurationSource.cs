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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Externals.Redis.Configuration
{
	public class RedisConfigurationSource : IConfigurationSource
	{
		#region 常量定义
		private const string DEFAULT_NAMESPACE = "Zongsoft.Configuration";
		#endregion

		#region 成员字段
		private string _namespace = DEFAULT_NAMESPACE;
		#endregion

		#region 构造函数
		public RedisConfigurationSource() { }
		public RedisConfigurationSource(string name) => this.Name = name;
		#endregion

		#region 公共属性
		public string Name { get; set; }
		public string Namespace
		{
			get => _namespace;
			set => _namespace = string.IsNullOrWhiteSpace(value) ? throw new ArgumentNullException() : value.Trim();
		}
		#endregion

		#region 公共方法
		public IConfigurationProvider Build(IConfigurationBuilder builder) => new RedisConfigurationProvider(this);
		#endregion
	}
}