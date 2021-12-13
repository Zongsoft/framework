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
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration
{
	public class CompositeConfigurationProvider : ICompositeConfigurationProvider, IConfigurationProvider, IDisposable
	{
		#region 私有变量
		private IConfigurationProvider[] _providers;
		private CompositeChangeToken _reloadToken;
		#endregion

		#region 构造函数
		public CompositeConfigurationProvider(IReadOnlyCollection<IConfigurationProvider> providers)
		{
			if(providers == null)
				throw new ArgumentNullException(nameof(providers));

			int index = providers.Count;
			_providers = new IConfigurationProvider[providers.Count];

			//注意：必须反转配置提供程序
			foreach(var provider in providers)
				_providers[--index] = provider;

			_reloadToken = new CompositeChangeToken(providers.Select(p => p.GetReloadToken()).ToArray());
		}
		#endregion

		#region 公共属性
		public IEnumerable<IConfigurationProvider> Providers { get => _providers; }
		#endregion

		#region 公共方法
		public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
		{
			return _providers
				.SelectMany(p => p.GetChildKeys(Enumerable.Empty<string>(), parentPath))
				.Concat(earlierKeys)
				.OrderBy(k => k, ConfigurationKeyComparer.Instance);
		}

		public IChangeToken GetReloadToken()
		{
			return _reloadToken;
		}

		public void Load()
		{
			var providers = _providers;

			for(int i = 0; i < providers.Length; i++)
				providers[i].Load();
		}

		public void Set(string key, string value)
		{
			var providers = _providers;

			for(int i = 0; i < providers.Length; i++)
				providers[i].Set(key, value);
		}

		public bool TryGet(string key, out string value)
		{
			var providers = _providers;

			for(int i = 0; i < providers.Length; i++)
			{
				if(providers[i].TryGet(key, out value))
					return true;
			}

			value = null;
			return false;
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return string.Join(Environment.NewLine, _providers.Select(provider => provider.ToString()));
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			var providers = _providers;

			for(int i = 0; i < providers.Length; i++)
			{
				if(providers[i] is IAsyncDisposable asyncDisposable)
					asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
				else if(providers[i] is IDisposable disposable)
					disposable.Dispose();
			}

			Array.Resize(ref _providers, 0);
		}
		#endregion
	}
}
