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

namespace Zongsoft.Data;

/// <summary>
/// 表示数据参数无效的异常类。
/// </summary>
public class DataArgumentException : DataException
{
	#region 构造函数
	public DataArgumentException(string name, Exception innerException = null) : base(Properties.Resources.DataArgumentException_Message, innerException)
	{
		this.Name = name;
	}

	public DataArgumentException(string name, string message, Exception innerException = null) : base(message ?? Properties.Resources.DataArgumentException_Message, innerException)
	{
		this.Name = name;
	}

	public DataArgumentException(string name, object value, Exception innerException = null) : base(Properties.Resources.DataArgumentException_Message, innerException)
	{
		this.Name = name;
		this.Value = value;
	}

	public DataArgumentException(string name, object value, string message, Exception innerException = null) : base(message ?? Properties.Resources.DataArgumentException_Message, innerException)
	{
		this.Name = name;
		this.Value = value;
	}
	#endregion

	#region 公共属性
	/// <summary>获取参数名。</summary>
	public string Name { get; }

	/// <summary>获取或设置参数值。</summary>
	public object Value { get; set; }
	#endregion

	#region 静态方法
	public static DataArgumentException Unnamed(object value, string message = null) => new(string.Empty, value, message);
	#endregion

	#region 重写方法
	public override string ToString() => string.IsNullOrEmpty(this.Name) ? this.Value?.ToString() : $"{this.Name}={this.Value}";
	#endregion
}
