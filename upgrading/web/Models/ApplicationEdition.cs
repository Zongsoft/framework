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

using Zongsoft.Data;

namespace Zongsoft.Upgrading.Models;

/// <summary>表示应用版本的实体类。</summary>
public abstract class ApplicationEdition
{
	#region 公共属性
	/// <summary>获取或设置应用编号。</summary>
	public abstract uint ApplicationId { get; set; }
	/// <summary>获取或设置版本标识。</summary>
	public abstract string Name { get; set; }
	/// <summary>获取或设置版本标题。</summary>
	public abstract string Title { get; set; }
	/// <summary>获取或设置是否启用。</summary>
	public abstract bool Enabled { get; set; }
	/// <summary>获取或设置是否授权。</summary>
	public abstract bool Licensed { get; set; }
	/// <summary>获取或设置创建时间。</summary>
	public abstract DateTime Creation { get; set; }
	/// <summary>获取或设置修改时间。</summary>
	public abstract DateTime? Modification { get; set; }
	/// <summary>获取或设置描述信息。</summary>
	public abstract string Description { get; set; }
	/// <summary>获取或设置所属应用。</summary>
	public abstract Application Application { get; set; }
	#endregion
}

/// <summary>表示应用版本查询条件的实体类。</summary>
public abstract class ApplicationEditionCriteria : CriteriaBase
{
	#region 公共属性
	/// <summary>获取或设置应用编号。</summary>
	public abstract uint? ApplicationId { get; set; }
	/// <summary>获取或设置版本标识。</summary>
	[Condition(ConditionOperator.Like)]
	public abstract string Name { get; set; }
	/// <summary>获取或设置版本标题。</summary>
	[Condition(ConditionOperator.Like)]
	public abstract string Title { get; set; }
	/// <summary>获取或设置是否启用。</summary>
	public abstract bool? Enabled { get; set; }
	/// <summary>获取或设置是否授权。</summary>
	public abstract bool? Licensed { get; set; }
	/// <summary>获取或设置创建时间范围。</summary>
	public abstract Range<DateTime>? Creation { get; set; }
	#endregion
}
