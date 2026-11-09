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

namespace Zongsoft.Data.TDengine;

partial class TDengineDriver
{
	private sealed class TDengineSetter : IDataParameterSetter
	{
		public void SetValue(DbParameter parameter, object value, DataType type = null)
		{
			var dbType = type == null ? parameter.DbType : type.DbType;

			if(value == null || System.Convert.IsDBNull(value))
			{
				parameter.DbType = dbType;
				parameter.Value = DBNull.Value;
				return;
			}

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
