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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Data;
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Metadata;

public class DataCommandParameterCollection() : KeyedCollection<string, IDataCommandParameter>(StringComparer.OrdinalIgnoreCase)
{
	public DataCommandParameter Add(string name, DataType type, ParameterDirection direction = ParameterDirection.Input) => this.Add(name, type, 0, null, direction);
	public DataCommandParameter Add(string name, DataType type, object value, ParameterDirection direction = ParameterDirection.Input) => this.Add(name, type, 0, value, direction);
	public DataCommandParameter Add(string name, DataType type, int length, ParameterDirection direction = ParameterDirection.Input) => this.Add(name, type, length, null, direction);
	public DataCommandParameter Add(string name, DataType type, int length, object value, ParameterDirection direction = ParameterDirection.Input)
	{
		var parameter = new DataCommandParameter(name, type, length, value, direction);
		this.Add(parameter);
		return parameter;
	}

	protected override string GetKeyForItem(IDataCommandParameter parameter) => parameter.Name;
}