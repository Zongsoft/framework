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
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Metadata;

public class DataCommandParameterCollection() : KeyedCollection<string, IDataCommandParameter>(StringComparer.OrdinalIgnoreCase)
{
	#region 公共方法
	public DataCommandParameter Add(string name, DataType type, ParameterDirection direction = ParameterDirection.Input) => this.Add(name, type, 0, null, direction);
	public DataCommandParameter Add(string name, DataType type, object value, ParameterDirection direction = ParameterDirection.Input) => this.Add(name, type, 0, value, direction);
	public DataCommandParameter Add(string name, DataType type, int length, ParameterDirection direction = ParameterDirection.Input) => this.Add(name, type, length, null, direction);
	public DataCommandParameter Add(string name, DataType type, int length, object value, ParameterDirection direction = ParameterDirection.Input)
	{
		var parameter = new DataCommandParameter(name, type, length, value, direction);
		this.Add(parameter);
		return parameter;
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(IDataCommandParameter parameter) => parameter.Name;
	#endregion
}