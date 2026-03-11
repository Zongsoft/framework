using System;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示系统日志的实体类。
/// </summary>
[Model(Alias = "Log")]
public abstract class LogModel
{
	#region 普通属性
	/// <summary>获取或设置日志编号。</summary>
	[ModelProperty(IsPrimaryKey = true, Sequence = "*")]
	public abstract ulong LogId { get; set; }

	/// <summary>获取或设置用户编号。</summary>
	[ModelProperty(Immutable = true)]
	public abstract uint UserId { get; set; }

	/// <summary>获取或设置用户对象。</summary>
	[ModelProperty($"Security.{nameof(User)}", DataAssociationMultiplicity.ZeroOrOne, [nameof(UserId)])]
	public abstract UserModel User { get; set; }

	/// <summary>获取或设置所属租户编号。</summary>
	[ModelProperty(Immutable = true, Sortable = true)]
	public abstract uint TenantId { get; set; }

	/// <summary>获取或设置所属租户对象。</summary>
	[ModelProperty(nameof(Tenant), DataAssociationMultiplicity.One, [nameof(TenantId)])]
	public abstract Tenant Tenant { get; set; }

	/// <summary>获取或设置所属分支机构编号。</summary>
	[ModelProperty(Immutable = true, Sortable = true)]
	public abstract uint BranchId { get; set; }

	/// <summary>获取或设置所属分支机构对象。</summary>
	[ModelProperty(nameof(Branch), DataAssociationMultiplicity.One, [nameof(TenantId), nameof(BranchId)])]
	public abstract Branch Branch { get; set; }

	/// <summary>获取或设置领域标识。</summary>
	[ModelProperty(System.Data.DbType.AnsiString, 50, false, "_")]
	public abstract string Domain { get; set; }

	/// <summary>获取或设置操作目标。</summary>
	[ModelProperty(Length = 100)]
	public abstract string Target { get; set; }

	/// <summary>获取或设置操作行为。</summary>
	[ModelProperty(Length = 100)]
	public abstract string Action { get; set; }

	/// <summary>获取或设置严重级别。</summary>
	public abstract LogSeverity Severity { get; set; }

	/// <summary>获取或设置日志标题。</summary>
	[ModelProperty(Length = 200)]
	public abstract string Caption { get; set; }

	/// <summary>获取或设置日志内容。</summary>
	[ModelProperty(Length = 4000)]
	public abstract string Content { get; set; }

	/// <summary>获取或设置时间戳。</summary>
	[ModelProperty(DefaultValue = "now()")]
	public abstract DateTime Timestamp { get; set; }

	/// <summary>获取或设置备注描述。</summary>
	[ModelProperty(Length = 500)]
	public abstract string Description { get; set; }
	#endregion
}
