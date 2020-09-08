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
	/// 表示数据约束失败的异常类。
	/// </summary>
	public class DataConstraintException : DataException
	{
		#region 构造函数
		public DataConstraintException(string field, Exception innerException = null) : base(Properties.Resources.Text_DataConstraintException_Message, innerException)
		{
			this.Field = field;
		}

		public DataConstraintException(string field, string message, Exception innerException = null) : base(message, innerException)
		{
			this.Field = field;
		}

		protected DataConstraintException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			this.Field = info.GetString(nameof(Field));
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置不符合约束的字段名。</summary>
		public string Field { get; set; }
		#endregion

		#region 重写方法
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue(nameof(Field), this.Field);
		}
		#endregion
	}
}
