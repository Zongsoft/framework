﻿/*
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
using System.Threading;
using System.Collections.Generic;

namespace Zongsoft.Configuration;

public class Setting : ISetting, IEquatable<Setting>, IEquatable<ISetting>
{
	#region 成员字段
	private string _name;
	private string _value;
	private IDictionary<string, string> _properties;
	#endregion

	#region 构造函数
	/// <summary>构建一个设置项。</summary>
	/// <param name="name">指定的设置项名称。</param>
	/// <param name="value">指定的设置项内容。</param>
	/// <remarks>注意：通过本构造函数的 <paramref name="value"/> 参数设置 <see cref="Value"/> 属性并不会触发 <see cref="OnValueChanged(string)"/> 回调方法。</remarks>
	public Setting(string name, string value = null)
	{
		_name = name == null ? string.Empty : name.Trim();
		_value = value?.Trim();
	}
	#endregion

	#region 公共属性
	public string Name => _name;
	public string Value
	{
		get => _value;
		set
		{
			if(!string.Equals(_value, value))
			{
				_value = value?.Trim();
				this.OnValueChanged(_value);
			}
		}
	}

	public bool HasProperties => _properties?.Count > 0;
	public IDictionary<string, string> Properties
	{
		get
		{
			if(_properties == null)
				Interlocked.CompareExchange(ref _properties, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), null);

			return _properties;
		}
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnValueChanged(string value) { }
	#endregion

	#region 重写方法
	public bool Equals(ISetting setting) => setting != null && string.Equals(_name, setting.Name, StringComparison.OrdinalIgnoreCase);
	public bool Equals(Setting setting) => setting != null && string.Equals(_name, setting._name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is ISetting setting && this.Equals(setting);
	public override int GetHashCode() => HashCode.Combine(_name.ToLowerInvariant());
	public override string ToString() => $"{this.Name}={this.Value}";
	#endregion
}
