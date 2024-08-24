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

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据访问的异常类。
	/// </summary>
	public class DataAccessException : DataException
	{
		#region 构造函数
		public DataAccessException(string driverName, int code, Exception innerException = null) : base(null, innerException)
		{
			this.Code = code;
			this.DriverName = driverName;
		}

		public DataAccessException(string driverName, int code, string message, Exception innerException = null) : base(message, innerException)
		{
			this.Code = code;
			this.DriverName = driverName;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问的错误代码。
		/// </summary>
		public int Code { get; }

		/// <summary>
		/// 获取数据访问驱动程序的名称。
		/// </summary>
		public string DriverName { get; }
		#endregion

		#region 重写方法
		public override string ToString()
		{
			if(string.IsNullOrEmpty(this.Message))
				return $"{this.DriverName}:{this.Code}";
			else
				return $"{this.DriverName}:{this.Code}{Environment.NewLine}{this.Message}";
		}
		#endregion
	}
}
