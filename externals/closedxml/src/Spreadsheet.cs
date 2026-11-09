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

namespace Zongsoft.Externals.ClosedXml;

public static class Spreadsheet
{
	#region 静态字段
	public static readonly Data.Archiving.DataArchiveFormat Format = new("Spreadsheet", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx");
	#endregion

	#region 内部方法
	internal static string GetTableName(Data.ModelDescriptor model) => $"__{model.QualifiedName}__";
	#endregion
}
