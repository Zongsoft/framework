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

namespace Zongsoft.Data.Common
{
	[System.ComponentModel.DefaultProperty(nameof(Providers))]
	public class DataPopulatorProviderFactory : IDataPopulatorProviderFactory
	{
		#region 单例模式
		public static readonly DataPopulatorProviderFactory Instance = new DataPopulatorProviderFactory();
		#endregion

		#region 成员字段
		private readonly ICollection<IDataPopulatorProvider> _providers;
		#endregion

		#region 构造函数
		private DataPopulatorProviderFactory()
		{
			_providers = new List<IDataPopulatorProvider>(new IDataPopulatorProvider[]
			{
				DictionaryPopulatorProvider.Instance,
				ScalarPopulatorProvider.Instance,
				ModelPopulatorProvider.Instance,
			});
		}
		#endregion

		#region 公共属性
		public ICollection<IDataPopulatorProvider> Providers => _providers;
		#endregion

		#region 公共方法
		public IDataPopulatorProvider GetProvider<T>() => this.GetProvider(typeof(T));
		public IDataPopulatorProvider GetProvider(Type type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			foreach(var provider in _providers)
			{
				if(provider.CanPopulate(type))
					return provider;
			}

			throw new DataException($"No found data populator provider for the '{type.FullName}' type.");
		}
		#endregion

		#region 枚举遍历
		public IEnumerator<IDataPopulatorProvider> GetEnumerator() => _providers.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _providers.GetEnumerator();
		#endregion
	}
}
