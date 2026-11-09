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

public class ReportResource : IReportResource, IEquatable<ReportResource>
{
	#region 构造函数
	public ReportResource() { }
	public ReportResource(string name, string type, string title = null, string extra = null, string description = null)
	{
		this.Name = name;
		this.Type = type;
		this.Title = title;
		this.Extra = extra;
		this.Description = description;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Type { get; }
	public string Title { get; set; }
	public string Extra { get; set; }
	public string Description { get; set; }

	public IDictionary<string, ReportResourceEntry> Dictionary { get; set; }
	#endregion

	#region 重写方法
	public bool Equals(ReportResource other) =>
		string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) &
		string.Equals(this.Type, other.Type, StringComparison.OrdinalIgnoreCase);

	public override bool Equals(object obj) => obj is ReportResource info && this.Equals(info);
	public override int GetHashCode() => HashCode.Combine(this.Name.ToUpperInvariant(), this.Type.ToUpperInvariant());
	public override string ToString() => $"{this.Name}@{this.Type}";

	public static bool operator ==(ReportResource left, ReportResource right) => left.Equals(right);
	public static bool operator !=(ReportResource left, ReportResource right) => !(left == right);
	#endregion
}
