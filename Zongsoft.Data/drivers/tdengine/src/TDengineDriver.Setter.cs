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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.TDengine library.
 *
 * The Zongsoft.Data.TDengine is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.TDengine is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.TDengine library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;

using Zongsoft.Data.Common;

namespace Zongsoft.Data.TDengine;

partial class TDengineDriver
{
	private sealed class TDengineSetter : IDataParameterSetter
	{
		public void SetValue(DbParameter parameter, object value, DataType type = null)
		{
			var dbType = type == null ? parameter.DbType : type.DbType;

			switch(dbType)
			{
				case DbType.DateTime:
				case DbType.DateTime2:
					parameter.DbType = DbType.DateTime;
					parameter.Value = TDengineUtility.Convert(value);
					break;
				default:
					parameter.DbType = dbType;
					parameter.Value = Zongsoft.Common.Convert.ConvertValue(value, DataUtility.AsType(dbType));
					break;
			}
		}
	}
}
