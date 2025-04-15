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

namespace Zongsoft.Serialization;

public partial class TextSerializationOptions : SerializationOptions, IEquatable<TextSerializationOptions>
{
	#region 单例字段
	public static readonly TextSerializationOptions Default = new(true);
	#endregion

	#region 成员字段
	private bool _indented;
	private bool _typified;
	private SerializationNamingConvention _naming;
	#endregion

	#region 构造函数
	public TextSerializationOptions(Action<object> configure = null) : base(configure) { }
	protected TextSerializationOptions(bool immutable, Action<object> configure = null) : base(immutable, configure) { }
	#endregion

	#region 公共属性
	/// <summary>获取或设置一个值，指示序列化后的文本是否保持缩进风格。</summary>
	public bool Indented
	{
		get => _indented;
		set => _indented = this.Immutable ? throw new InvalidOperationException(IMMUTABLE_EXCEPTION) : value;
	}

	/// <summary>获取或设置一个值，指示序列化的对象是否写入类型信息。</summary>
	public bool Typified
	{
		get => _typified;
		set => _typified = this.Immutable ? throw new InvalidOperationException(IMMUTABLE_EXCEPTION) : value;
	}

	/// <summary>获取或设置一个值，指示序列化成员的命名转换方式。</summary>
	public SerializationNamingConvention NamingConvention
	{
		get => _naming;
		set => _naming = this.Immutable ? throw new InvalidOperationException(IMMUTABLE_EXCEPTION) : value;
	}
	#endregion

	#region 公共方法
	public TextSerializationOptions Immutate() { this.Immutable = true; return this; }
	#endregion

	#region 重写方法
	public bool Equals(TextSerializationOptions other) => other is not null && base.Equals(other) &&
		_indented == other._indented &&
		_typified == other._typified &&
		_naming == other._naming;

	public override bool Equals(object obj) => obj is TextSerializationOptions other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), _indented, _typified, _naming);
	public override string ToString() => $"{base.ToString()} casing:{_naming.ToString().ToLowerInvariant()};{(_typified ? " typified;" : null)}{(_indented ? " indented;" : null)}";
	#endregion
}
