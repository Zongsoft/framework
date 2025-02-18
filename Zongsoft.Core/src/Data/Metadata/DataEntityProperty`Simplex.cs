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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Data;
using System.Text.RegularExpressions;

namespace Zongsoft.Data.Metadata;

public class DataEntitySimplexProperty : DataEntityPropertyBase, IDataEntitySimplexProperty
{
	#region 静态变量
	private static readonly Regex _regex = new(@"(?<name>\w+)\s*\(\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
	#endregion

	#region 成员字段
	private int _length;
	private object _defaultValue;
	private bool? _isPrimaryKey;
	#endregion

	#region 构造函数
	public DataEntitySimplexProperty(IDataEntity entity, string name, DbType type, bool nullable, bool immutable = false) : base(entity, name, immutable)
	{
		this.Type = type;
		this.Nullable = nullable;
	}

	public DataEntitySimplexProperty(IDataEntity entity, string name, DbType type, int length, bool nullable, bool immutable = false) : base(entity, name, immutable)
	{
		this.Type = type;
		this.Length = length;
		this.Nullable = nullable;
	}

	public DataEntitySimplexProperty(IDataEntity entity, string name, DbType type, byte precision, byte scale, bool nullable, bool immutable = false) : base(entity, name, immutable)
	{
		this.Type = type;
		this.Precision = precision;
		this.Scale = scale;
		this.Nullable = nullable;
	}
	#endregion

	#region 公共属性
	/// <summary>获取数据实体属性的字段类型。</summary>
	public DbType Type { get; }

	/// <summary>获取或设置文本或数组属性的最大长度，单位：字节。</summary>
	public int Length
	{
		get
		{
			var length = _length;

			if(length == 0)
			{
				switch(this.Type)
				{
					case DbType.Binary:
						return 100;
					case DbType.AnsiString:
					case DbType.AnsiStringFixedLength:
					case DbType.String:
					case DbType.StringFixedLength:
						return 100;
				}
			}

			return length;
		}
		set
		{
			_length = Math.Max(value, -1);
		}
	}

	/// <summary>获取或设置数值属性的精度。</summary>
	public byte Precision { get; set; }

	/// <summary>获取或设置数值属性的小数点位数。</summary>
	public byte Scale { get; set; }

	/// <summary>获取或设置属性的默认值。</summary>
	public object DefaultValue
	{
		get => _defaultValue is Function function ? function.Execute(this) : _defaultValue;
		set
		{
			if(value is string text)
			{
				var function = this.GetFunction(text);

				if(function != null)
				{
					_defaultValue = function;
					return;
				}
			}

			var type = this.Type.AsType();

			if(type.IsValueType && this.Nullable)
				type = typeof(Nullable<>).MakeGenericType(type);

			_defaultValue = Zongsoft.Common.Convert.ConvertValue(value, type);
		}
	}

	/// <summary>获取或设置属性是否允许为空。</summary>
	public bool Nullable { get; set; }

	/// <summary>获取或设置属性是否可以参与排序。</summary>
	public bool Sortable { get; set; }

	/// <summary>获取或设置序号器元数据。</summary>
	public IDataEntityPropertySequence Sequence { get; set; }

	/// <summary>获取一个值，指示当前属性是否为主键。</summary>
	public bool IsPrimaryKey
	{
		get
		{
			if(_isPrimaryKey.HasValue)
				return _isPrimaryKey.Value;

			if(this.Entity.Key == null || this.Entity.Key.Length == 0)
				return false;

			for(int i = 0; i < this.Entity.Key.Length; i++)
			{
				if(this.Entity.Key[i] == this)
					return (_isPrimaryKey = true).Value;
			}

			return (_isPrimaryKey = false).Value;
		}
	}
	#endregion

	#region 重写属性
	/// <summary>获取一个值，指示数据实体属性是否为复合类型。该重写方法始终返回假(<c>False</c>)。</summary>
	public override bool IsComplex => false;

	/// <summary>获取一个值，指示数据实体属性是否为单值类型。该重写方法始终返回真(<c>True</c>)。</summary>
	public override bool IsSimplex => true;
	#endregion

	#region 虚拟方法
	protected virtual Function GetFunction(string text)
	{
		if(string.IsNullOrWhiteSpace(text))
			return null;

		var match = _regex.Match(text);

		if(match.Success)
		{
			return match.Groups["name"].Value switch
			{
				"now" => Function.Now,
				"today" => Function.Today,
				"guid" or "uuid" => Function.Guid,
				"random" => Function.Random,
				_ => throw new DataException($"Unrecognized {match.Groups["name"].Value} function."),
			};
		}

		return null;
	}
	#endregion

	#region 重写方法
	public override string ToString()
	{
		var nullable = this.Nullable ? "NULL" : "NOT NULL";

		return this.Type switch
		{
			//处理小数类型
			DbType.Currency or
			DbType.Decimal or
			DbType.Double or
			DbType.Single => $"{this.Name} {this.Type}({this.Precision},{this.Scale}) [{nullable}]",

			//处理字符串或数组类型
			DbType.Binary or
			DbType.AnsiString or
			DbType.AnsiStringFixedLength or
			DbType.String or
			DbType.StringFixedLength => $"{this.Name} {this.Type}({_length}) [{nullable}]",

			_ => $"{this.Name} {this.Type} [{nullable}]",
		};
	}
	#endregion

	#region 嵌套子类
	protected class Function(string name, Func<IDataEntitySimplexProperty, object> thunk = null)
	{
		private Func<IDataEntitySimplexProperty, object> _thunk = thunk;
		public string Name { get; } = name;
		public virtual object Execute(IDataEntitySimplexProperty property) => _thunk(property);

		public static readonly Function Now = new(nameof(Now), _ => DateTime.Now);
		public static readonly Function Today = new(nameof(Today), _ => DateTime.Today);
		public static readonly Function Guid = new(nameof(Guid), _ => System.Guid.NewGuid());
		public static readonly Function Random = new(nameof(Random), property => property.Type switch
		{
			DbType.Byte => Zongsoft.Common.Randomizer.Generate(1)[0],
			DbType.SByte => (sbyte)Zongsoft.Common.Randomizer.Generate(1)[0],
			DbType.Int16 => BitConverter.ToInt16(Zongsoft.Common.Randomizer.Generate(2), 0),
			DbType.UInt16 => BitConverter.ToUInt16(Zongsoft.Common.Randomizer.Generate(2), 0),
			DbType.Int32 => Zongsoft.Common.Randomizer.GenerateInt32(),
			DbType.UInt32 => (uint)Zongsoft.Common.Randomizer.GenerateInt32(),
			DbType.Int64 => Zongsoft.Common.Randomizer.GenerateInt64(),
			DbType.UInt64 => (ulong)Zongsoft.Common.Randomizer.GenerateInt64(),
			DbType.Single => BitConverter.ToSingle(Zongsoft.Common.Randomizer.Generate(4), 0),
			DbType.Double => BitConverter.ToDouble(Zongsoft.Common.Randomizer.Generate(8), 0),
			DbType.Decimal or
			DbType.Currency => new Decimal(Zongsoft.Common.Randomizer.GenerateInt64()),
			DbType.Date or
			DbType.Time or
			DbType.DateTime or
			DbType.DateTime2 => new DateTime(Zongsoft.Common.Randomizer.GenerateInt64()),
			DbType.DateTimeOffset => new DateTimeOffset(Zongsoft.Common.Randomizer.GenerateInt64(), TimeSpan.Zero),
			DbType.Binary => Zongsoft.Common.Randomizer.Generate(property.Length > 0 ? property.Length : 8),
			DbType.Guid => System.Guid.NewGuid(),
			_ => Zongsoft.Common.Randomizer.GenerateString(),
		});
	}
	#endregion
}
