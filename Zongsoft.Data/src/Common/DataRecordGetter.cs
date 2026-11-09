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
using System.Data;

namespace Zongsoft.Data.Common;

public class DataRecordGetter : IDataRecordGetter
{
	#region 单例字段
	public static readonly DataRecordGetter Default = new();
	#endregion

	#region 私有构造
	private DataRecordGetter() { }
	#endregion

	#region 公共方法
	public T GetValue<T>(IDataRecord record, int ordinal) => record.GetValue<T>(ordinal);
	#endregion
}
