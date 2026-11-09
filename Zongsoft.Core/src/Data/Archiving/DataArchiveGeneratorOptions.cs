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
using System.Linq;

namespace Zongsoft.Data.Archiving;

public class DataArchiveGeneratorOptions : IDataArchiveGeneratorOptions
{
	#region 构造函数
	public DataArchiveGeneratorOptions(params string[] fields) : this(null, fields) { }
	public DataArchiveGeneratorOptions(IDataArchiveFormatter formatter, params string[] fields)
	{
		this.Formatter = formatter;

		if(fields != null && fields.Length > 0)
			this.Fields = fields.Select(field => new DataArchiveField(field)).ToArray();
	}

	public DataArchiveGeneratorOptions(params DataArchiveField[] fields) : this(null, fields) { }
	public DataArchiveGeneratorOptions(IDataArchiveFormatter formatter, params DataArchiveField[] fields)
	{
		this.Formatter = formatter;
		this.Fields = fields;
	}
	#endregion

	#region 公共属性
	public IDataArchiveFormatter Formatter { get; set; }
	public DataArchiveField[] Fields { get; set; }
	#endregion
}