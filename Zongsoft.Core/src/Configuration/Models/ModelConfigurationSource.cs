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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
