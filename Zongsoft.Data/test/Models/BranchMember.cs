using System;
using System.Collections.Generic;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示机构成员的实体结构。
/// </summary>
public struct BranchMember : IEquatable<BranchMember>
{
	#region 构造函数
	public BranchMember(uint tenantId, uint branchId, uint userId)
	{
		this.TenantId = tenantId;
		this.BranchId = branchId;
		this.UserId = userId;
		this.User = null;
		this.Tenant = null;
		this.Branch = null;
		this.Employee = null;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置租户编号。</summary>
	public uint TenantId { get; set; }

	/// <summary>获取或设置租户对象。</summary>
	public Tenant Tenant { get; set; }

	/// <summary>获取或设置机构编号。</summary>
	public uint BranchId { get; set; }

	/// <summary>获取或设置机构对象。</summary>
	public Branch Branch { get; set; }

	/// <summary>获取或设置用户编号。</summary>
	public uint UserId { get; set; }

	/// <summary>获取或设置用户对象。</summary>
	public UserModel User { get; set; }

	/// <summary>获取或设置员工对象。</summary>
	public Employee Employee { get; set; }
	#endregion

	#region 重写方法
	public bool Equals(BranchMember other)
	{
		return other.TenantId == this.TenantId &&
		       other.BranchId == this.BranchId &&
		       other.UserId == this.UserId;
	}

	public override bool Equals(object obj) => obj is BranchMember member && this.Equals(member);
	public override int GetHashCode() => HashCode.Combine(this.TenantId, this.BranchId, this.UserId);
	public override string ToString() => $"{this.TenantId}-{this.BranchId}-{this.UserId}";

	public static bool operator ==(BranchMember left, BranchMember right) => left.Equals(right);
	public static bool operator !=(BranchMember left, BranchMember right) => !(left == right);
	#endregion
}
