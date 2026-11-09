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

namespace Zongsoft.Data.Influx.Common;

public class InfluxParameter : DbParameter
{
	public override string ParameterName { get; set; }
	public override ParameterDirection Direction { get; set; }
	public override bool IsNullable { get; set; }
	public override int Size { get; set; }
	public override DbType DbType { get; set; }
	public override object Value { get; set; }
	public override string SourceColumn { get; set; }
	public override bool SourceColumnNullMapping { get; set; }

	public override void ResetDbType() => this.DbType = DbType.String;
}
