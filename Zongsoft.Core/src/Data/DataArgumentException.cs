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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

namespace Zongsoft.Data;

/// <summary>表示数据参数无效的异常类。</summary>
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
