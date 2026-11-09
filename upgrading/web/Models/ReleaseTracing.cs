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

using Zongsoft.Data;

namespace Zongsoft.Upgrading.Models;

/// <summary>表示发布实例跟踪的实体类。</summary>
public abstract class ReleaseTracing
{
	#region 公共属性
	/// <summary>获取或设置发布编号。</summary>
	public abstract uint ReleaseId { get; set; }
	/// <summary>获取或设置实例编号。</summary>
	public abstract uint InstanceId { get; set; }
	/// <summary>获取或设置发布阶段。</summary>
	public abstract string Phase { get; set; }
	/// <summary>获取或设置失败消息。</summary>
	public abstract string Message { get; set; }
	/// <summary>获取或设置更新时间。</summary>
	public abstract DateTime Timestamp { get; set; }
	/// <summary>获取或设置更新描述。</summary>
	public abstract string Description { get; set; }
	/// <summary>获取或设置所属发布。</summary>
	public abstract Release Release { get; set; }
	/// <summary>获取或设置所属实例。</summary>
	public abstract Instance Instance { get; set; }
	#endregion
}

/// <summary>表示发布实例跟踪查询条件的实体类。</summary>
public abstract class ReleaseTracingCriteria : CriteriaBase
{
	#region 公共属性
	/// <summary>获取或设置发布编号。</summary>
	public abstract uint? ReleaseId { get; set; }
	/// <summary>获取或设置实例编号。</summary>
	public abstract uint? InstanceId { get; set; }
	/// <summary>获取或设置发布阶段。</summary>
	public abstract string Phase { get; set; }
	/// <summary>获取或设置更新时间范围。</summary>
	public abstract Range<DateTime>? Timestamp { get; set; }
	#endregion
}
