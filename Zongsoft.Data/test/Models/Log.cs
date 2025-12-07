using System;
using System.Collections.Generic;

using Zongsoft.Data;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示系统日志的实体类。
/// </summary>
public abstract class Log
{
	#region 普通属性
	/// <summary>获取或设置日志编号。</summary>
	public abstract ulong LogId { get; set; }

	/// <summary>获取或设置用户编号。</summary>
	public abstract uint UserId { get; set; }

	/// <summary>获取或设置用户对象。</summary>
	public abstract UserModel User { get; set; }

	/// <summary>获取或设置所属租户编号。</summary>
	public abstract uint TenantId { get; set; }

	/// <summary>获取或设置所属租户对象。</summary>
	public abstract Tenant Tenant { get; set; }

	/// <summary>获取或设置所属分支机构编号。</summary>
	public abstract uint BranchId { get; set; }

	/// <summary>获取或设置所属分支机构对象。</summary>
	public abstract Branch Branch { get; set; }

	/// <summary>获取或设置领域标识。</summary>
	public abstract string Domain { get; set; }

	/// <summary>获取或设置操作目标。</summary>
	public abstract string Target { get; set; }

	/// <summary>获取或设置操作行为。</summary>
	public abstract string Action { get; set; }

	/// <summary>获取或设置严重级别。</summary>
	public abstract LogSeverity Severity { get; set; }

	/// <summary>获取或设置日志标题。</summary>
	public abstract string Caption { get; set; }

	/// <summary>获取或设置日志内容。</summary>
	public abstract string Content { get; set; }

	/// <summary>获取或设置时间戳。</summary>
	public abstract DateTime Timestamp { get; set; }

	/// <summary>获取或设置备注描述。</summary>
	public abstract string Description { get; set; }
	#endregion
}

/// <summary>
/// 表示日志查询条件的实体类。
/// </summary>
public abstract class LogCriteria : CriteriaBase
{
	#region 公共属性
	/// <summary>获取或设置用户编号。</summary>
	public abstract uint? UserId { get; set; }

	/// <summary>获取或设置所属分支机构范围。</summary>
	[Zongsoft.Components.Alias("Branch")]
	public abstract Mixture<uint>? BranchId { get; set; }

	/// <summary>获取或设置领域标识。</summary>
	public abstract string Domain { get; set; }

	/// <summary>获取或设置操作目标。</summary>
	public abstract string Target { get; set; }

	/// <summary>获取或设置操作行为。</summary>
	public abstract string Action { get; set; }

	/// <summary>获取或设置严重程度的数据。</summary>
	public abstract LogSeverity? Severity { get; set; }

	/// <summary>获取或设置日志时间范围。</summary>
	public abstract Range<DateTime>? Timestamp { get; set; }

	/// <summary>获取或设置日志标题。</summary>
	public abstract string Caption { get; set; }
	#endregion
}
