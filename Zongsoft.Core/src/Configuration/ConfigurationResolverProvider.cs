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
using System.Collections.Concurrent;

namespace Zongsoft.Configuration
{
	public class ConfigurationResolverProvider : IConfigurationResolverProvider
	{
		#region 单例字段
		public static readonly ConfigurationResolverProvider Default = new ConfigurationResolverProvider();
		#endregion

		#region 私有变量
		private readonly ConcurrentDictionary<Type, IConfigurationResolver> _cache;
		#endregion

		#region 构造函数
		public ConfigurationResolverProvider()
		{
			_cache = new ConcurrentDictionary<Type, IConfigurationResolver>();
		}
		#endregion

		#region 公共方法
		public IConfigurationResolver GetResolver(Type type)
		{
			if(type == null)
				return null;

			return _cache.GetOrAdd(type, type => this.CreateResolver(type));
		}
		#endregion

		#region 公共属性
		public IConfigurationResolverFactory Factory
		{
			get; set;
		}
		#endregion

		#region 虚拟方法
		protected virtual IConfigurationResolver CreateResolver(Type type)
		{
			return (this.Factory ?? ConfigurationResolverFactory.Default).Create(type) ??
				throw new InvalidOperationException();
		}
		#endregion
	}
}
