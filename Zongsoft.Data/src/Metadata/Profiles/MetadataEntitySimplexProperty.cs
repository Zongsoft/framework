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
		private static readonly Regex _regex = new Regex(@"(?<name>\w+)\s*\(\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
		#endregion

		#region 成员字段
		private bool _isPrimaryKey;
		private int _length;
		private byte _precision;
		private byte _scale;
		private string _valueText;
		private IDataEntityPropertySequence _sequence;
		private Func<object> _defaultThunk;
		#endregion

		#region 构造函数
		public MetadataEntitySimplexProperty(MetadataEntity entity, string name, System.Data.DbType type, bool immutable = false) : base(entity, name, immutable)
		{
			this.Type = type;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据实体属性的字段类型。
		/// </summary>
		public System.Data.DbType Type
		{
			get;
		}

		/// <summary>
		/// 获取或设置文本或数组属性的最大长度，单位：字节。
		/// </summary>
		public int Length
		{
			get
			{
				var length = _length;

				if(length == 0)
				{
					switch(this.Type)
					{
						case System.Data.DbType.Binary:
							return 100;
						case System.Data.DbType.AnsiString:
						case System.Data.DbType.AnsiStringFixedLength:
						case System.Data.DbType.String:
						case System.Data.DbType.StringFixedLength:
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

		/// <summary>
		/// 获取或设置数值属性的精度。
		/// </summary>
		public byte Precision
		{
			get
			{
				return _precision;
			}
			set
			{
				_precision = value;
			}
		}

		/// <summary>
		/// 获取或设置数值属性的小数点位数。
		/// </summary>
		public byte Scale
		{
			get
			{
				return _scale;
			}
			set
			{
				_scale = value;
			}
		}

		/// <summary>
		/// 获取或设置属性的默认值。
		/// </summary>
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

		/// <summary>
		/// 获取或设置属性是否允许为空。
		/// </summary>
		public bool Nullable
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置属性是否可以参与排序。
		/// </summary>
		public bool Sortable
		{
			get; set;
		}

		/// <summary>
		/// 获取序号器元数据。
		/// </summary>
		public IDataEntityPropertySequence Sequence
		{
			get
			{
				return _sequence;
			}
		}
		#endregion

		#region 重写属性
		/// <summary>
		/// 获取一个值，指示数据实体属性是否为主键。
		/// </summary>
		public override bool IsPrimaryKey
		{
			get
			{
				return _isPrimaryKey;
			}
		}

		/// <summary>
		/// 获取一个值，指示数据实体属性是否为复合类型。该重写方法始终返回假(False)。
		/// </summary>
		public override bool IsComplex
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// 获取一个值，指示数据实体属性是否为单值类型。该重写方法始终返回真(True)。
		/// </summary>
		public override bool IsSimplex
		{
			get
			{
				return true;
			}
		}
		#endregion

		#region 内部方法
		internal void SetPrimaryKey()
		{
			_isPrimaryKey = true;
		}

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
				switch(match.Groups["name"].Value)
				{
					case "now":
						_defaultThunk = GetNow;
						break;
					case "today":
						_defaultThunk = GetToday;
						break;
					case "guid":
					case "uuid":
						_defaultThunk = GetGuid;
						break;
					case "random":
						_defaultThunk = GetRandom;
						break;
					default:
						throw new MetadataFileException($"Unrecognized {match.Groups["name"].Value} function.");
				}
			}

			_valueText = value;
		}

		internal void SetSequence(string sequence)
		{
			if(string.IsNullOrWhiteSpace(sequence))
				return;

			_sequence = DataEntityPropertySequence.Parse(this, sequence);
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			var nullable = this.Nullable ? "NULL" : "NOT NULL";

			switch(this.Type)
			{
				//处理小数类型
				case System.Data.DbType.Currency:
				case System.Data.DbType.Decimal:
				case System.Data.DbType.Double:
				case System.Data.DbType.Single:
					return $"{this.Name} {this.Type.ToString()}({_precision},{_scale}) [{nullable}]";

				//处理字符串或数组类型
				case System.Data.DbType.Binary:
				case System.Data.DbType.AnsiString:
				case System.Data.DbType.AnsiStringFixedLength:
				case System.Data.DbType.String:
				case System.Data.DbType.StringFixedLength:
					return $"{this.Name} {this.Type.ToString()}({_length}) [{nullable}]";
			}

			return $"{this.Name} {this.Type.ToString()} [{nullable}]";
		}
		#endregion

		#region 私有方法
		private object GetToday()
		{
			return DateTime.Today;
		}

		private object GetNow()
		{
			return DateTime.Now;
		}

		private object GetGuid()
		{
			return Guid.NewGuid();
		}

		private object GetRandom()
		{
			switch(this.Type)
			{
				case DbType.Byte:
					return Zongsoft.Common.Randomizer.Generate(1)[0];
				case DbType.SByte:
					return (sbyte)Zongsoft.Common.Randomizer.Generate(1)[0];
				case DbType.Int16:
					return BitConverter.ToInt16(Zongsoft.Common.Randomizer.Generate(2), 0);
				case DbType.UInt16:
					return BitConverter.ToUInt16(Zongsoft.Common.Randomizer.Generate(2), 0);
				case DbType.Int32:
					return Zongsoft.Common.Randomizer.GenerateInt32();
				case DbType.UInt32:
					return (uint)Zongsoft.Common.Randomizer.GenerateInt32();
				case DbType.Int64:
					return Zongsoft.Common.Randomizer.GenerateInt64();
				case DbType.UInt64:
					return (ulong)Zongsoft.Common.Randomizer.GenerateInt64();
				case DbType.Single:
					return BitConverter.ToSingle(Zongsoft.Common.Randomizer.Generate(4), 0);
				case DbType.Double:
					return BitConverter.ToDouble(Zongsoft.Common.Randomizer.Generate(8), 0);
				case DbType.Decimal:
				case DbType.Currency:
					return new Decimal(Zongsoft.Common.Randomizer.GenerateInt64());
				case DbType.Date:
				case DbType.Time:
				case DbType.DateTime:
				case DbType.DateTime2:
					return new DateTime(Zongsoft.Common.Randomizer.GenerateInt64());
				case DbType.DateTimeOffset:
					return new DateTimeOffset(Zongsoft.Common.Randomizer.GenerateInt64(), TimeSpan.Zero);
				case DbType.Binary:
					return Zongsoft.Common.Randomizer.Generate(this.Length > 0 ? this.Length : 8);
				case DbType.Guid:
					return Guid.NewGuid();
			}

			return Zongsoft.Common.Randomizer.GenerateString();
		}
		#endregion
	}
}
