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
using Zongsoft.Reflection;

namespace Zongsoft.Configuration.Models;

public class ModelConfigurationProvider<TModel> : IConfigurationProvider
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
				var target = model;

				if(Accessor.TryGetValue(ref target, source.Mapping.Key, out var key) && key != null)
					this.Data.Add(key.ToString(), model);
			}
		}
	}
	#endregion

	#region 保护属性
	protected IDictionary<string, TModel> Data { get; set; }
	#endregion

	#region 公共方法
	public virtual bool TryGet(string key, out string value)
	{
		if(this.Data.TryGetValue(key, out var model) && Accessor.TryGetValue(ref model, _source.Mapping.Value, out var valueObject))
		{
			value = valueObject?.ToString();
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
			Accessor.TrySetValue(ref model, _source.Mapping.Value, value);
		else
			this.Data[key] = model = Accessor.Create(target =>
			{
				Accessor.TrySetValue(ref target, _source.Mapping.Key, key);
				Accessor.TrySetValue(ref target, _source.Mapping.Value, value);
			});

		var persistents = _source.Persistents;

		if(persistents != null && persistents.Count > 0)
		{
			foreach(var persistent in persistents)
				persistent(model);
		}
	}

	public virtual void Load() { }
	public virtual IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
	{
		var prefix = parentPath == null ? string.Empty : parentPath + ConfigurationPath.KeyDelimiter;

		return this.Data
			.Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			.Select(kv => Segment(kv.Key, prefix.Length))
			.Concat(earlierKeys)
			.OrderBy(k => k, ConfigurationKeyComparer.Instance);
	}

	public IChangeToken GetReloadToken() => _reloadToken;
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
		var index = key.IndexOf(ConfigurationPath.KeyDelimiter, prefixLength);
		return index < 0 ? key[prefixLength..] : key[prefixLength..index];
	}
	#endregion

	#region 嵌套子类
	private static class Accessor
	{
		public static TModel Create(Action<TModel> initiate = null)
		{
			var model = typeof(TModel).IsAbstract ? Model.Build<TModel>() : Activator.CreateInstance<TModel>();
			initiate?.Invoke(model);
			return model;
		}

		public static bool TryGetValue(ref TModel target, string name, out object value)
		{
			if(target is null)
			{
				value = null;
				return false;
			}

			return target switch
			{
				IModel model => model.TryGetValue(name, out value),
				IDataDictionary dictionary => dictionary.TryGetValue(name, out value),
				_ => Reflector.TryGetValue(ref target, name, out value),
			};
		}

		public static bool TrySetValue(ref TModel target, string name, object value)
		{
			if(target is null)
				return false;

			return target switch
			{
				IModel model => model.TrySetValue(name, value),
				IDataDictionary dictionary => dictionary.TrySetValue(name, value),
				_ => Reflector.TrySetValue(ref target, name, value),
			};
		}
	}
	#endregion
}
