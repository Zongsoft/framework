/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Runtime.Serialization;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据冲突的异常类。
	/// </summary>
	public class DataConflictException : DataAccessException
	{
		#region 构造函数
		public DataConflictException(string driverName, int code, string key, string value, string[] fields, Exception innerException = null) :
			base(driverName, code, string.Format(Properties.Resources.Text_DataConflictException_Message, key, value), innerException)
		{
			this.Key = key;
			this.Value = value;
			this.Fields = fields;
		}

		protected DataConflictException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			this.Key = info.GetString(nameof(Key));
			this.Value = info.GetString(nameof(Value));
			this.Fields = info.GetValue(nameof(Fields), typeof(string[])) as string[];
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置数据冲突的键名。</summary>
		public string Key { get; set; }

		/// <summary>获取或设置数据冲突的键值。</summary>
		public string Value { get; set; }

		/// <summary>获取或设置冲突的字段名数组。</summary>
		public string[] Fields { get; set; }
		#endregion

		#region 重写方法
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue(nameof(Key), this.Key);
			info.AddValue(nameof(Value), this.Value);
			info.AddValue(nameof(Fields), this.Fields);
		}
		#endregion
	}
}
