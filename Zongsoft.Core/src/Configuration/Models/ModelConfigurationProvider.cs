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
using System.Threading;
using System.Collections.Generic;

using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;

using Zongsoft.Data;

namespace Zongsoft.Configuration.Models
{
	public class ModelConfigurationProvider<TModel> : IConfigurationProvider where TModel : IModel
	{
		#region 成员字段
		private readonly ModelConfigurationSource<TModel> _source;
		private ConfigurationReloadToken _reloadToken;
		#endregion

		#region 构造函数
		public ModelConfigurationProvider(ModelConfigurationSource<TModel> source)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
			_reloadToken = new ConfigurationReloadToken();

			this.Data = new Dictionary<string, TModel>(StringComparer.OrdinalIgnoreCase);

			if(source.Models != null)
			{
				foreach(var model in source.Models)
				{
					if(model.TryGetValue(source.Mapping.Key, out var key) && key != null)
						this.Data.Add(key.ToString(), model);
				}
			}
		}
		#endregion

		#region 保护属性
		protected IDictionary<string, TModel> Data
		{
			get; set;
		}
		#endregion

		#region 公共方法
		public virtual bool TryGet(string key, out string value)
		{
			value = null;

			if(this.Data.TryGetValue(key, out var model) && model.TryGetValue(_source.Mapping.Value, out var valueObject))
			{
				if(valueObject != null)
					value = valueObject.ToString();

				return true;
			}

			value = null;
			return false;
		}

		public virtual void Set(string key, string value)
		{
			if(string.IsNullOrEmpty(value))
				value = null;

			if(this.Data.TryGetValue(key, out var model))
				model.TrySetValue(_source.Mapping.Value, value);
			else
				this.Data[key] = model = Model.Build<TModel>(m =>
				{
					m.TrySetValue(_source.Mapping.Key, key);
					m.TrySetValue(_source.Mapping.Value, value);
				});

			var persistents = _source.Persistents;

			if(persistents != null && persistents.Count > 0)
			{
				foreach(var persistent in persistents)
					persistent(model);
			}
		}

		public virtual void Load()
		{
		}

		public virtual IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
		{
			var prefix = parentPath == null ? string.Empty : parentPath + ConfigurationPath.KeyDelimiter;

			return this.Data
				.Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				.Select(kv => Segment(kv.Key, prefix.Length))
				.Concat(earlierKeys)
				.OrderBy(k => k, ConfigurationKeyComparer.Instance);
		}

		public IChangeToken GetReloadToken()
		{
			return _reloadToken;
		}
		#endregion

		#region 保护方法
		protected void OnReload()
		{
			var previousToken = Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken());
			previousToken.OnReload();
		}
		#endregion

		#region 私有方法
		private static string Segment(string key, int prefixLength)
		{
			var indexOf = key.IndexOf(ConfigurationPath.KeyDelimiter, prefixLength);
			return indexOf < 0 ? key.Substring(prefixLength) : key.Substring(prefixLength, indexOf - prefixLength);
		}
		#endregion
	}
}
