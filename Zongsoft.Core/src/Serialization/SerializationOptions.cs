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

public class SerializationOptions : IEquatable<SerializationOptions>
{
	#region 常量定义
	internal const string IMMUTABLE_EXCEPTION = $"The serialization options is immutable.";
	#endregion

	#region 成员字段
	private int _maximumDepth;
	private Ignoring _ignoring;
	private bool _includeFields;
	#endregion

	#region 构造函数
	public SerializationOptions(Action<object> configure = null) : this(false, configure) { }
	protected SerializationOptions(bool immutable, Action<object> configure = null)
	{
		_includeFields = true;
		this.Immutable = immutable;
		this.Configure = configure;
	}
	#endregion

	#region 内部属性
	/// <summary>获取当前选项的配置器。</summary>
	internal Action<object> Configure { get; }
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示当前选项是否不可变更。</summary>
	public bool Immutable { get; protected set; }

	/// <summary>获取或设置最大的序列化深度，默认为零(不限制)。</summary>
	public int MaximumDepth
	{
		get => _maximumDepth;
		set => _maximumDepth = this.Immutable ? throw new InvalidOperationException(IMMUTABLE_EXCEPTION) : Math.Max(0, value);
	}

	/// <summary>获取或设置一个值，指示是否忽略空值(<c>null</c>)。</summary>
	public bool IgnoreNull
	{
		get => (_ignoring & Ignoring.Null) == Ignoring.Null;
		set => _ignoring |= this.Immutable ? throw new InvalidOperationException(IMMUTABLE_EXCEPTION) : (value ? Ignoring.Null : Ignoring.None);
	}

	/// <summary>获取或设置一个值，指示是否忽略零。</summary>
	public bool IgnoreZero
	{
		get => (_ignoring & Ignoring.Zero) == Ignoring.Zero;
		set => _ignoring = this.Immutable ? throw new InvalidOperationException(IMMUTABLE_EXCEPTION) : (value ? Ignoring.Zero : Ignoring.None);
	}

	/// <summary>获取或设置一个值，指示是否忽略空集和空字符串。</summary>
	public bool IgnoreEmpty
	{
		get => (_ignoring & Ignoring.Empty) == Ignoring.Empty;
		set => _ignoring |= this.Immutable ? throw new InvalidOperationException(IMMUTABLE_EXCEPTION) : (value ? Ignoring.Empty : Ignoring.None);
	}

	/// <summary>获取或设置一个值，指示是否包含字段。</summary>
	public bool IncludeFields
	{
		get => _includeFields;
		set => _includeFields = this.Immutable ? throw new InvalidOperationException(IMMUTABLE_EXCEPTION) : value;
	}
	#endregion

	#region 公共方法
	public SerializationOptions Ignores(string text)
	{
		_ignoring |= (Ignoring)GetIgnoring(text);
		return this;
	}

	internal static ushort GetIgnoring(string text)
	{
		if(string.IsNullOrEmpty(text))
			return (ushort)Ignoring.None;

		text = text.Trim();

		if(text == "*")
			return (ushort)(Ignoring.Zero | Ignoring.Null | Ignoring.Empty);

		var result = Ignoring.None;

		foreach(var part in Common.StringExtension.Slice(text, ',', '|'))
		{
			if(string.Equals(part, "null", StringComparison.OrdinalIgnoreCase))
				result |= Ignoring.Null;
			else if(string.Equals(part, "zero", StringComparison.OrdinalIgnoreCase))
				result |= Ignoring.Zero;
			else if(string.Equals(part, "empty", StringComparison.OrdinalIgnoreCase))
				result |= Ignoring.Empty;
		}

		return (ushort)result;
	}
	#endregion

	#region 重写方法
	public bool Equals(SerializationOptions other) => other is not null && this.Immutable == other.Immutable &&
		_ignoring == other._ignoring &&
		_maximumDepth == other._maximumDepth &&
		_includeFields == other._includeFields;

	public override bool Equals(object obj) => obj is SerializationOptions other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.Immutable, _maximumDepth, _ignoring, _includeFields);
	public override string ToString()
	{
		var ignoring = _ignoring switch
		{
			Ignoring.None => "none",
			Ignoring.Null | Ignoring.Zero | Ignoring.Empty => "*",
			_ => _ignoring.ToString(),
		};

		return $"[{(this.Immutable ? "Immutable" : "Mutable")}] ignores:{ignoring}; include:{(_includeFields ? "fields,properties" : "properties")};";
	}
	#endregion

	#region 内部方法
	internal TextSerializationOptions ToTextOptions() => new(this.Configure)
	{
		IgnoreNull = this.IgnoreNull,
		IgnoreZero = this.IgnoreZero,
		IgnoreEmpty = this.IgnoreEmpty,
		IncludeFields = this.IncludeFields,
		MaximumDepth = this.MaximumDepth,
		Immutable = this.Immutable,
	};
	#endregion

	#region 枚举定义
	[Flags]
	private enum Ignoring
	{
		None = 0,
		Null = 1,
		Zero = 2,
		Empty = 4,
	}
	#endregion
}
