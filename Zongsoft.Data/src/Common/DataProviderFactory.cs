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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Collections;

namespace Zongsoft.Data.Common
{
	[System.ComponentModel.DefaultProperty(nameof(Providers))]
	public class DataProviderFactory : IDataProviderFactory
	{
		#region 单例字段
		public static readonly DataProviderFactory Instance = new DataProviderFactory();
		#endregion

		#region 成员字段
		private readonly INamedCollection<IDataProvider> _providers;
		#endregion

		#region 构造函数
		protected DataProviderFactory()
		{
			_providers = new NamedCollection<IDataProvider>(p => p.Name, StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public ICollection<IDataProvider> Providers => _providers;
		#endregion

		#region 公共方法
		public IDataProvider GetProvider(string name)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(_providers.TryGet(name, out var provider))
				return provider;

			lock(_providers)
			{
				if(_providers.TryGet(name, out provider))
					return provider;

				_providers.Add(provider = this.CreateProvider(name));
			}

			return provider;
		}
		#endregion

		#region 虚拟方法
		protected virtual IDataProvider CreateProvider(string name) => new DataProvider(name);
		#endregion

		#region 枚举遍历
		public IEnumerator<IDataProvider> GetEnumerator() => _providers.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _providers.GetEnumerator();
		#endregion
	}
}
