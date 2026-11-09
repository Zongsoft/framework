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
