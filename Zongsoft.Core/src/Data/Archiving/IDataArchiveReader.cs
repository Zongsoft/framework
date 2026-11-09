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

namespace Zongsoft.Data.Archiving;

public interface IDataArchiveReader : IDataArchiveRecord, IDisposable
{
	#region 公共属性
	bool IsEmpty { get; }
	#endregion
	#region 公共方法
	bool Read();
	#endregion
}

public interface IDataArchiveRecord
{
	#region 公共属性
	int FieldCount { get; }

	object this[int ordinal] { get; }
	object this[string name] { get; }
	#endregion

	#region 公共方法
	string GetName(int ordinal);
	object GetValue(int ordinal);
	object GetValue(string name);
	T GetValue<T>(int ordinal);
	T GetValue<T>(string name);
	#endregion
}
