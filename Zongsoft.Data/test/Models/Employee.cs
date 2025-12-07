using System;
using System.Collections.Generic;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示员工的实体类。
/// </summary>
public abstract class Employee
{
	#region 普通属性
	/// <summary>获取或设置用户编号。</summary>
	public abstract uint UserId { get; set; }

	/// <summary>获取或设置用户对象。</summary>
	public abstract UserModel User { get; set; }

	/// <summary>获取或设置租户编号。</summary>
	public abstract uint TenantId { get; set; }

	/// <summary>获取或设置租户对象。</summary>
	public abstract Tenant Tenant { get; set; }

	/// <summary>获取或设置分支机构编号。</summary>
	public abstract uint BranchId { get; set; }

	/// <summary>获取或设置分支机构对象。</summary>
	public abstract Branch Branch { get; set; }

	/// <summary>获取或设置员工代号(工号)。</summary>
	public abstract string EmployeeNo { get; set; }

	/// <summary>获取或设置员工内部代号。</summary>
	public abstract string EmployeeCode { get; set; }

	/// <summary>获取或设置员工种类。</summary>
	public abstract EmployeeKind EmployeeKind { get; set; }

	/// <summary>获取或设置员工全称。</summary>
	public abstract string FullName { get; set; }

	/// <summary>获取或设置名称缩写。</summary>
	public abstract string Acronym { get; set; }

	/// <summary>获取或设置员工性别。</summary>
	public abstract bool? Gender { get; set; }

	/// <summary>获取或设置个人简介。</summary>
	public abstract string Summary { get; set; }

	/// <summary>获取或设置员工职称。</summary>
	public abstract string JobTitle { get; set; }

	/// <summary>获取或设置就职状态。</summary>
	public abstract JobStatus JobStatus { get; set; }

	/// <summary>获取或设置入职日期。</summary>
	public abstract DateTime? Hiredate { get; set; }

	/// <summary>获取或设置离职日期。</summary>
	public abstract DateTime? Leavedate { get; set; }

	/// <summary>获取或设置开户银行。</summary>
	public abstract string BankName { get; set; }

	/// <summary>获取或设置银行账号。</summary>
	public abstract string BankCode { get; set; }

	/// <summary>获取或设置出生日期。</summary>
	public abstract DateTime? Birthdate { get; set; }

	/// <summary>获取或设置照片路径。</summary>
	public abstract string PhotoPath { get; set; }

	/// <summary>获取或设置身份证号。</summary>
	public abstract string IdentityId { get; set; }

	/// <summary>获取或设置身份证种类。</summary>
	public abstract IdentityKind IdentityKind { get; set; }

	/// <summary>获取或设置身份证签发日期。</summary>
	public abstract DateTime? IdentityIssued { get; set; }

	/// <summary>获取或设置身份证过期日期。</summary>
	public abstract DateTime? IdentityExpiry { get; set; }

	/// <summary>获取或设置证件照片路径#1。</summary>
	public abstract string IdentityPath1 { get; set; }

	/// <summary>获取或设置证件照片路径#2。</summary>
	public abstract string IdentityPath2 { get; set; }

	/// <summary>获取或设置婚姻状况。</summary>
	public abstract MaritalStatus MaritalStatus { get; set; }

	/// <summary>获取或设置受教育等级(学历)。</summary>
	public abstract EducationDegree EducationDegree { get; set; }

	/// <summary>获取或设置籍贯。</summary>
	public abstract string NativePlace { get; set; }

	/// <summary>获取或设置移动电话。</summary>
	public abstract string MobilePhone { get; set; }

	/// <summary>获取或设置家庭电话。</summary>
	public abstract string HomePhone { get; set; }

	/// <summary>获取或设置家庭国别地区。</summary>
	public abstract ushort HomeCountry { get; set; }

	/// <summary>获取或设置家庭地址编号。</summary>
	public abstract uint HomeAddressId { get; set; }

	/// <summary>获取或设置家庭住址。</summary>
	public abstract string HomeAddressDetail { get; set; }

	/// <summary>获取或设置办公单位。</summary>
	public abstract string OfficeTitle { get; set; }

	/// <summary>获取或设置办公电话。</summary>
	public abstract string OfficePhone { get; set; }

	/// <summary>获取或设置办公国别地区。</summary>
	public abstract ushort OfficeCountry { get; set; }

	/// <summary>获取或设置办公地址编号。</summary>
	public abstract uint OfficeAddressId { get; set; }

	/// <summary>获取或设置办公详细地址。</summary>
	public abstract string OfficeAddressDetail { get; set; }

	/// <summary>获取或设置创建人编号。</summary>
	public abstract uint CreatorId { get; set; }

	/// <summary>获取或设置创建人对象。</summary>
	public abstract UserModel Creator { get; set; }

	/// <summary>获取或设置创建时间。</summary>
	public abstract DateTime CreatedTime { get; set; }

	/// <summary>获取或设置修改人编号。</summary>
	public abstract uint? ModifierId { get; set; }

	/// <summary>获取或设置修改人对象。</summary>
	public abstract UserModel Modifier { get; set; }

	/// <summary>获取或设置修改时间。</summary>
	public abstract DateTime? ModifiedTime { get; set; }

	/// <summary>获取或设置描述信息。</summary>
	public abstract string Remark { get; set; }
	#endregion

	#region 集合属性
	/// <summary>获取或设置所属的部门集。</summary>
	public abstract IEnumerable<Department> Departments { get; set; }

	/// <summary>获取或设置所属的部门成员集。</summary>
	public abstract IEnumerable<DepartmentMember> DepartmentMembers { get; set; }

	/// <summary>获取或设置所属的班组集。</summary>
	public abstract IEnumerable<Team> Teams { get; set; }

	/// <summary>获取或设置所属的班组成员集。</summary>
	public abstract IEnumerable<TeamMember> TeamMembers { get; set; }
	#endregion
}

/// <summary>
/// 表示员工查询条件的实体类。
/// </summary>
public abstract class EmployeeCriteria : CriteriaBase
{
	#region 公共属性
	/// <summary>获取或设置查询常用键值。</summary>
	[Condition(
		nameof(EmployeeNo),
		nameof(EmployeeCode),
		nameof(IdentityId),
		nameof(Employee.User) + "." + nameof(UserModel.Name),
		nameof(Employee.User) + "." + nameof(UserModel.Phone),
		nameof(Employee.User) + "." + nameof(UserModel.Email))]
	public abstract string Key { get; set; }

	/// <summary>获取或设置员工代号(工号)。</summary>
	public abstract string EmployeeNo { get; set; }

	/// <summary>获取或设置内部代号。</summary>
	public abstract string EmployeeCode { get; set; }

	/// <summary>获取或设置员工种类。</summary>
	[Zongsoft.Components.Alias("Kind")]
	public abstract EmployeeKind? EmployeeKind { get; set; }

	/// <summary>获取或设置身份证号。</summary>
	public abstract string IdentityId { get; set; }

	/// <summary>获取或设置员工的名称。</summary>
	[Condition(ConditionOperator.Like,
		nameof(Employee.FullName),
		nameof(Employee.User) + "." + nameof(UserModel.Name),
		nameof(Employee.User) + "." + nameof(UserModel.Nickname))]
	public abstract string Name { get; set; }

	/// <summary>获取或设置安全手机号码。</summary>
	[Condition(nameof(Employee.User) + "." + nameof(UserModel.Phone))]
	public abstract string Phone { get; set; }

	/// <summary>获取或设置安全邮箱地址。</summary>
	[Condition(nameof(Employee.User) + "." + nameof(UserModel.Email))]
	public abstract string Email { get; set; }

	/// <summary>获取或设置所属分支机构范围。</summary>
	public abstract Mixture<uint>? BranchId { get; set; }

	/// <summary>获取或设置所属部门编号。</summary>
	[Condition(typeof(DpeartmentConditionConverter))]
	public abstract Mixture<ushort>? DepartmentId { get; set; }

	/// <summary>获取或设置工作岗位编号。</summary>
	public abstract uint? StationId { get; set; }

	/// <summary>获取或设置就职状态数组。</summary>
	public abstract JobStatus[] JobStatus { get; set; }

	/// <summary>获取或设置教育程度数组。</summary>
	public abstract EducationDegree[] EducationDegree { get; set; }

	/// <summary>获取或设置入职日期范围。</summary>
	public abstract Range<DateTime>? Hiredate { get; set; }

	/// <summary>获取或设置离职日期范围。</summary>
	public abstract Range<DateTime>? Leavedate { get; set; }

	/// <summary>获取或设置出生日期范围。</summary>
	public abstract Range<DateTime>? Birthdate { get; set; }

	/// <summary>获取或设置租户编号。</summary>
	public abstract uint TenantId { get; set; }
	#endregion

	#region 条件转换
	private class DpeartmentConditionConverter : ConditionConverter
	{
		public override ICondition Convert(ConditionConverterContext context)
		{
			var departmentId = Zongsoft.Common.Convert.ConvertValue<ushort>(context.Value);

			//如果部门编号为零，则表示无归属部门(即没有属于任何部门)的员工
			return departmentId == 0 ?
				Condition.NotExists(context.GetFullName(nameof(Employee.DepartmentMembers))) :
				Condition.Exists(context.GetFullName(nameof(Employee.DepartmentMembers)), Condition.Equal(nameof(DepartmentMember.DepartmentId), departmentId));
		}
	}
	#endregion
}

/// <summary>
/// 表示员工主键的结构。
/// </summary>
public struct EmployeeKey : IEquatable<EmployeeKey>
{
	#region 构造函数
	public EmployeeKey(uint tenantId, uint userId)
	{
		this.TenantId = tenantId;
		this.UserId = userId;
	}
	#endregion

	#region 公共字段
	public uint TenantId;
	public uint UserId;
	#endregion

	#region 重写方法
	public bool Equals(EmployeeKey key)
	{
		return this.TenantId == key.TenantId && this.UserId == key.UserId;
	}

	public override bool Equals(object obj)
	{
		if(obj == null || obj.GetType() != this.GetType())
			return false;

		return this.Equals((EmployeeKey)obj);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(this.TenantId, this.UserId);
	}

	public override string ToString()
	{
		return this.TenantId.ToString() + "-" + this.UserId.ToString();
	}

	public static bool operator ==(EmployeeKey left, EmployeeKey right) => left.Equals(right);
	public static bool operator !=(EmployeeKey left, EmployeeKey right) => !(left == right);
	#endregion
}
