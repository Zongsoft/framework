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
using System.Data;
using System.Text.RegularExpressions;

namespace Zongsoft.Data.Metadata.Profiles
{
	/// <summary>
	/// 表示数据实体单值属性的元数据类。
	/// </summary>
	public class MetadataEntitySimplexProperty : MetadataEntityProperty, IDataEntitySimplexProperty
	{
		#region 静态变量
		private static readonly Regex _regex = new(@"(?<name>\w+)\s*\(\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
		#endregion

		#region 成员字段
		private int _length;
		private string _valueText;
		private bool _isPrimaryKey;
		private Func<object> _defaultThunk;
		#endregion

		#region 构造函数
		public MetadataEntitySimplexProperty(MetadataEntity entity, string name, System.Data.DbType type, bool immutable = false) : base(entity, name, immutable)
		{
			this.Type = type;
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
			get
			{
				if(_defaultThunk != null)
					return _defaultThunk();

				var type = Common.DbTypeUtility.AsType(this.Type);

				if(type.IsValueType && this.Nullable)
				{
					if(string.IsNullOrEmpty(_valueText))
						return null;

					type = typeof(Nullable<>).MakeGenericType(type);
				}

				return Zongsoft.Common.Convert.ConvertValue(_valueText, type);
			}
			set
			{
				_valueText = Zongsoft.Common.Convert.ConvertValue<string>(value);
			}
		}

		/// <summary>获取或设置属性是否允许为空。</summary>
		public bool Nullable { get; set; }

		/// <summary>获取或设置属性是否可以参与排序。</summary>
		public bool Sortable { get; set; }

		/// <summary>获取序号器元数据。</summary>
		public IDataEntityPropertySequence Sequence { get; private set; }
		#endregion

		#region 重写属性
		/// <summary>获取一个值，指示数据实体属性是否为主键。</summary>
		public override bool IsPrimaryKey => _isPrimaryKey;

		/// <summary>获取一个值，指示数据实体属性是否为复合类型。该重写方法始终返回假(<c>False</c>)。</summary>
		public override bool IsComplex => false;

		/// <summary>获取一个值，指示数据实体属性是否为单值类型。该重写方法始终返回真(<c>True</c>)。</summary>
		public override bool IsSimplex => true;
		#endregion

		#region 内部方法
		internal void SetPrimaryKey() => _isPrimaryKey = true;

		internal void SetDefaultValue(string value)
		{
			if(string.IsNullOrWhiteSpace(value))
			{
				_valueText = null;
				return;
			}

			var match = _regex.Match(value);

			if(match.Success)
			{
				_defaultThunk = match.Groups["name"].Value switch
				{
					"now" => GetNow,
					"today" => GetToday,
					"guid" or "uuid" => GetGuid,
					"random" => this.GetRandom,
					_ => throw new MetadataFileException($"Unrecognized {match.Groups["name"].Value} function."),
				};
			}

			_valueText = value;
		}

		internal void SetSequence(string sequence)
		{
			if(string.IsNullOrWhiteSpace(sequence))
				return;

			Sequence = DataEntityPropertySequence.Parse(this, sequence);
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

		#region 私有方法
		private static object GetToday() => DateTime.Today;
		private static object GetNow() => DateTime.Now;
		private static object GetGuid() => Guid.NewGuid();
		private object GetRandom() => this.Type switch
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
			DbType.Binary => Zongsoft.Common.Randomizer.Generate(this.Length > 0 ? this.Length : 8),
			DbType.Guid => Guid.NewGuid(),
			_ => Zongsoft.Common.Randomizer.GenerateString(),
		};
		#endregion
	}
}
