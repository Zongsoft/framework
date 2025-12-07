using System;
using System.Collections.Generic;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示班组(工作组)业务的实体类。
/// </summary>
public abstract class Team
{
	#region 普通属性
	/// <summary>获取或设置所属租户编号。</summary>
	public abstract uint TenantId { get; set; }

	/// <summary>获取或设置所属分支机构编号。</summary>
	public abstract uint BranchId { get; set; }

	/// <summary>获取或设置所属分支机构对象。</summary>
	public abstract Branch Branch { get; set; }

	/// <summary>获取或设置班组编号。</summary>
	public abstract ushort TeamId { get; set; }

	/// <summary>获取或设置班组代号。</summary>
	public abstract string TeamNo { get; set; }

	/// <summary>获取或设置班组名称。</summary>
	public abstract string Name { get; set; }

	/// <summary>获取或设置名称缩写。</summary>
	public abstract string Acronym { get; set; }

	/// <summary>获取或设置图标名。</summary>
	public abstract string Icon { get; set; }

	/// <summary>获取或设置组长编号。</summary>
	public abstract uint LeaderId { get; set; }

	/// <summary>获取或设置组长对象。</summary>
	public abstract UserModel Leader { get; set; }

	/// <summary>获取或设置所属部门编号。</summary>
	public abstract ushort DepartmentId { get; set; }

	/// <summary>获取或设置所属部门对象。</summary>
	public abstract Department Department { get; set; }

	/// <summary>获取或设置是否可用。</summary>
	[System.ComponentModel.DefaultValue(true)]
	public abstract bool Visible { get; set; }

	/// <summary>获取或设置排列顺序。</summary>
	public abstract short Ordinal { get; set; }

	/// <summary>获取或设置备注。</summary>
	public abstract string Remark { get; set; }
	#endregion

	#region 集合属性
	/// <summary>获取或设置班组成员集。</summary>
	public abstract IEnumerable<TeamMember> Members { get; set; }
	#endregion
}

/// <summary>
/// 表示小组查询条件的实体类。
/// </summary>
public abstract class TeamCriteria : CriteriaBase
{
	#region 公共属性
	/// <summary>获取或设置小组名称。</summary>
	[Condition(ConditionOperator.Like, nameof(Name), nameof(Team.Acronym))]
	public abstract string Name { get; set; }

	/// <summary>获取或设置所属分支机构范围。</summary>
	public abstract Mixture<uint>? BranchId { get; set; }

	/// <summary>获取或设置组长编号。</summary>
	public abstract uint? LeaderId { get; set; }
	#endregion
}
