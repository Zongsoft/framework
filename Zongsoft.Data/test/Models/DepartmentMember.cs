using System;
using System.Collections.Generic;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示部门成员的实体结构。
/// </summary>
public struct DepartmentMember : IEquatable<DepartmentMember>
{
	#region 构造函数
	public DepartmentMember(uint tenantId, uint branchId, ushort departmentId, uint userId)
	{
		this.TenantId = tenantId;
		this.BranchId = branchId;
		this.DepartmentId = departmentId;
		this.UserId = userId;
		this.User = null;
		this.Employee = null;
		this.Department = null;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置租户编号。</summary>
	public uint TenantId { get; set; }

	/// <summary>获取或设置分支机构编号。</summary>
	public uint BranchId { get; set; }

	/// <summary>获取或设置部门编号。</summary>
	public ushort DepartmentId { get; set; }

	/// <summary>获取或设置部门对象。</summary>
	public Department Department { get; set; }

	/// <summary>获取或设置用户编号。</summary>
	public uint UserId { get; set; }

	/// <summary>获取或设置用户对象。</summary>
	public UserModel User { get; set; }

	/// <summary>获取或设置员工对象。</summary>
	public Employee Employee { get; set; }
	#endregion

	#region 重写方法
	public bool Equals(DepartmentMember other)
	{
		return other.TenantId == this.TenantId &&
		       other.BranchId == this.BranchId &&
		       other.DepartmentId == this.DepartmentId &&
		       other.UserId == this.UserId;
	}

	public override bool Equals(object obj) => obj is DepartmentMember member && this.Equals(member);
	public override int GetHashCode() => HashCode.Combine(this.TenantId, this.BranchId, this.DepartmentId, this.UserId);
	public override string ToString() => $"{this.TenantId}-{this.BranchId}-{this.DepartmentId}-{this.UserId}";

	public static bool operator ==(DepartmentMember left, DepartmentMember right) => left.Equals(right);
	public static bool operator !=(DepartmentMember left, DepartmentMember right) => !(left == right);
	#endregion
}
