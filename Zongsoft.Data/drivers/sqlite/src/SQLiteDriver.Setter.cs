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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.SQLite library.
 *
 * The Zongsoft.Data.SQLite is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.SQLite is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.SQLite library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;

using Zongsoft.Data.Common;

namespace Zongsoft.Data.SQLite;

partial class SQLiteDriver
{
	private sealed class SQLiteSetter : IDataParameterSetter
	{
		public void SetValue(DbParameter parameter, object value, DataType type = null)
		{
			var dbType = type == null ? parameter.DbType : type.DbType;

			switch(dbType)
			{
				case DbType.UInt16:
					parameter.DbType = DbType.Int16;
					parameter.Value = Zongsoft.Common.Convert.ConvertValue(value, typeof(short));
					break;
				case DbType.UInt32:
					parameter.DbType = DbType.Int32;
					parameter.Value = Zongsoft.Common.Convert.ConvertValue(value, typeof(int));
					break;
				case DbType.UInt64:
					parameter.DbType = DbType.Int64;
					parameter.Value = Zongsoft.Common.Convert.ConvertValue(value, typeof(long));
					break;
				case DbType.DateTime:
				case DbType.DateTime2:
					parameter.DbType = DbType.DateTime;
					if(value is DateTime datetime && datetime.Kind != DateTimeKind.Utc)
						parameter.Value = datetime.ToUniversalTime();
					else
						parameter.Value = value;
					break;
				default:
					parameter.DbType = dbType;
					parameter.Value = Zongsoft.Common.Convert.ConvertValue(value, DataUtility.AsType(dbType));
					break;
			}
		}
	}
}
