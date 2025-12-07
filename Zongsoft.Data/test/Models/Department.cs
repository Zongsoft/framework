using System;
using System.Collections.Generic;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示部门业务的实体类。
/// </summary>
public abstract class Department
{
	#region 普通属性
	/// <summary>获取或设置所属租户编号。</summary>
	public abstract uint TenantId { get; set; }

	/// <summary>获取或设置所属分支机构编号。</summary>
	public abstract uint BranchId { get; set; }

	/// <summary>获取或设置所属分支机构对象。</summary>
	public abstract Branch Branch { get; set; }

	/// <summary>获取或设置上级部门编号。</summary>
	public abstract ushort ParentId { get; set; }

	/// <summary>获取或设置部门编号。</summary>
	public abstract ushort DepartmentId { get; set; }

	/// <summary>获取或设置部门代号。</summary>
	public abstract string DepartmentNo { get; set; }

	/// <summary>获取或设置部门名称。</summary>
	public abstract string Name { get; set; }

	/// <summary>获取或设置名称缩写。</summary>
	public abstract string Acronym { get; set; }

	/// <summary>获取或设置图标名。</summary>
	public abstract string Icon { get; set; }

	/// <summary>获取或设置部门电话。</summary>
	public abstract string PhoneNumber { get; set; }

	/// <summary>获取或设置部门办公地址。</summary>
	public abstract string Address { get; set; }

	/// <summary>获取或设置排列顺序。</summary>
	public abstract short Ordinal { get; set; }

	/// <summary>获取或设置负责人编号。</summary>
	public abstract uint? PrincipalId { get; set; }

	/// <summary>获取或设置负责人对象。</summary>
	public abstract UserModel Principal { get; set; }

	/// <summary>获取或设置备注。</summary>
	public abstract string Remark { get; set; }
	#endregion

	#region 集合属性
	/// <summary>获取或设置子部门集。</summary>
	public abstract IEnumerable<Department> Children { get; set; }

	/// <summary>获取或设置班组集。</summary>
	public abstract IEnumerable<Team> Teams { get; set; }

	/// <summary>获取或设置部门成员集。</summary>
	public abstract IEnumerable<DepartmentMember> Members { get; set; }
	#endregion
}

/// <summary>
/// 表示部门主键的结构。
/// </summary>
public struct DepartmentKey : IEquatable<DepartmentKey>
{
	#region 构造函数
	public DepartmentKey(uint tenantId, uint branchId, ushort departmentId)
	{
		this.TenantId = tenantId;
		this.BranchId = branchId;
		this.DepartmentId = departmentId;
	}
	#endregion

	#region 公共字段
	public uint TenantId;
	public uint BranchId;
	public ushort DepartmentId;
	#endregion

	#region 重写方法
	public bool Equals(DepartmentKey key)
	{
		return this.TenantId == key.TenantId &&
		       this.BranchId == key.BranchId &&
		       this.DepartmentId == key.DepartmentId;
	}

	public override bool Equals(object obj)
	{
		if(obj == null || obj.GetType() != typeof(DepartmentKey))
			return false;

		return this.Equals((DepartmentKey)obj);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(this.TenantId, this.BranchId, this.DepartmentId);
	}

	public override string ToString()
	{
		return $"{this.TenantId}-{this.BranchId}-{this.DepartmentId}";
	}
	#endregion
}

/// <summary>
/// 表示部门查询条件的实体类。
/// </summary>
public abstract class DepartmentCriteria : CriteriaBase
{
	#region 公共属性
	/// <summary>获取或设置父部门编号。</summary>
	[Zongsoft.Components.Alias("Parent")]
	public abstract ushort? ParentId { get; set; }

	/// <summary>获取或设置部门名称。</summary>
	[Condition(ConditionOperator.Like, nameof(Department.Name), nameof(Department.Acronym))]
	public abstract string Name { get; set; }

	/// <summary>获取或设置所属分支机构范围。</summary>
	public abstract Mixture<uint>? BranchId { get; set; }

	/// <summary>获取或设置负责人编号。</summary>
	public abstract uint? PrincipalId { get; set; }

	/// <summary>获取或设置是否一个值，指示是否过滤顶级部门。</summary>
	[Condition(typeof(TopmostConverter))]
	public abstract bool? Topmost { get; set; }
	#endregion

	#region 条件转换
	private class TopmostConverter : ConditionConverter
	{
		public override ICondition Convert(ConditionConverterContext context)
		{
			return (bool)context.Value ?
				Condition.Equal(context.GetFullName(nameof(Department.ParentId)), 0) :
				Condition.GreaterThan(context.GetFullName(nameof(Department.ParentId)), 0);
		}
	}
	#endregion
}
