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
using System.Collections.Generic;

namespace Zongsoft.Reporting;

public interface IReportData
{
	IList<ReportModelData> Models { get; }
}

public struct ReportModelData
{
	#region 构造函数
	public ReportModelData(string name, object value = null)
	{
		this.Name = name;
		this.Value = value;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public object Value { get; set; }
	#endregion

	#region 重写方法
	public override string ToString()
	{
		if(string.IsNullOrEmpty(this.Name))
			return this.Value?.ToString();
		else
			return $"[{this.Name}]{this.Value}";
	}
	#endregion
}
