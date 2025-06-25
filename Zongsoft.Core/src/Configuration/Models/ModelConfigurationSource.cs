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
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration.Models;

public class ModelConfigurationSource<TModel> : IConfigurationSource
{
	#region 成员字段
	private readonly HashSet<Action<TModel>> _persistents;
	#endregion

	#region 构造函数
	public ModelConfigurationSource()
	{
		this.Mapping = new ModelConfigurationMapping();
		_persistents = new HashSet<Action<TModel>>();
	}
	#endregion

	#region 公共属性
	public IEnumerable<TModel> Models { get; set; }
	public ModelConfigurationMapping Mapping { get; }
	#endregion

	#region 内部属性
	internal ICollection<Action<TModel>> Persistents => _persistents;
	#endregion

	#region 公共方法
	public IConfigurationProvider Build(IConfigurationBuilder builder) => new ModelConfigurationProvider<TModel>(this);
	public ModelConfigurationSource<TModel> Map(string key, string value = null)
	{
		if(!string.IsNullOrWhiteSpace(key))
			this.Mapping.Key = key.Trim();

		if(!string.IsNullOrWhiteSpace(value))
			this.Mapping.Value = value.Trim();

		return this;
	}

	public ModelConfigurationSource<TModel> OnChange(Action<TModel> persistent)
	{
		if(persistent == null)
			throw new ArgumentNullException(nameof(persistent));

		_persistents.Add(persistent);
		return this;
	}
	#endregion
}

public class ModelConfigurationMapping
{
	public ModelConfigurationMapping()
	{
		this.Key = nameof(this.Key);
		this.Value = nameof(this.Value);
	}

	public ModelConfigurationMapping(string key, string value)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));
		if(string.IsNullOrEmpty(value))
			throw new ArgumentNullException(nameof(value));

		this.Key = key;
		this.Value = value;
	}

	public string Key { get; internal set; }
	public string Value { get; internal set; }
}
