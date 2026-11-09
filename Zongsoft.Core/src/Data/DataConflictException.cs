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

/// <summary>表示数据冲突的异常类。</summary>
public class DataConflictException : DataAccessException
{
	#region 构造函数
	public DataConflictException(string driverName, int code, string name, string value, Exception innerException = null) :
		base(driverName, code, string.Format(Properties.Resources.DataConflictException_Message, name, value), innerException)
	{
		this.Name = name;
		this.Value = value;
	}
	#endregion

	#region 公共属性
	/// <summary>获取数据冲突的键名。</summary>
	public string Name { get; }
	/// <summary>获取数据冲突的键值。</summary>
	public string Value { get; }
	#endregion
}
