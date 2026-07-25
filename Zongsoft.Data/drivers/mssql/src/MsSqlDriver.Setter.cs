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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.MsSql library.
 *
 * The Zongsoft.Data.MsSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MsSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MsSql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;

using Zongsoft.Data.Common;

namespace Zongsoft.Data.MsSql;

partial class MsSqlDriver
{
	private sealed class MsSqlSetter : IDataParameterSetter
	{
		public void SetValue(DbParameter parameter, object value, DataType type = null)
		{
			var dbType = type == null ? parameter.DbType : type.DbType;

			if(dbType == DbType.Object && type != null && type.Name.Equals("text", StringComparison.OrdinalIgnoreCase))
				dbType = DbType.String;

			dbType = dbType switch
			{
				DbType.SByte => DbType.Byte,
				DbType.UInt16 => DbType.Int16,
				DbType.UInt32 => DbType.Int32,
				DbType.UInt64 => DbType.Int64,
				_ => dbType,
			};

			parameter.DbType = dbType;

			if(value == null || Convert.IsDBNull(value))
			{
				parameter.Value = DBNull.Value;
				return;
			}

			parameter.Value = Zongsoft.Common.Convert.ConvertValue(value, DataUtility.AsType(dbType));
		}
	}
}
