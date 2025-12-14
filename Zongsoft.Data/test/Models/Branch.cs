using System;
using System.Collections.Generic;

using Zongsoft.Common;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示分支机构业务的实体类。
/// </summary>
public abstract class Branch
{
	#region 普通属性
	/// <summary>获取或设置租户编号。</summary>
	public abstract uint TenantId { get; set; }

	/// <summary>获取或设置租户对象。</summary>
	public abstract Tenant Tenant { get; set; }

	/// <summary>获取或设置所属分支机构编号。</summary>
	public abstract uint BranchId { get; set; }

	/// <summary>获取或设置分支机构代号。</summary>
	public abstract string BranchNo { get; set; }

	/// <summary>获取或设置分支机构名称。</summary>
	public abstract string Name { get; set; }

	/// <summary>获取或设置名称缩写。</summary>
	public abstract string Acronym { get; set; }

	/// <summary>获取或设置分支机构简称。</summary>
	public abstract string Abbr { get; set; }

	/// <summary>获取或设置标志图片路径。</summary>
	public abstract Zongsoft.IO.PathLocation LogoPath { get; set; }

	/// <summary>获取或设排列顺序。</summary>
	public abstract short Ordinal { get; set; }

	/// <summary>获取或设置语言代号。</summary>
	public abstract string Language { get; set; }

	/// <summary>获取或设置所属地区国别。</summary>
	public abstract ushort Country { get; set; }

	/// <summary>获取或设置地址编号。</summary>
	public abstract uint AddressId { get; set; }

	/// <summary>获取或设置详细地址。</summary>
	public abstract string AddressDetail { get; set; }

	/// <summary>获取或设置经度坐标。</summary>
	public abstract double? Longitude { get; set; }

	/// <summary>获取或设置纬度坐标。</summary>
	public abstract double? Latitude { get; set; }

	/// <summary>获取或设置工商执照号。</summary>
	public abstract string BusinessLicenseNo { get; set; }

	/// <summary>获取或设置工商执照种类。</summary>
	public abstract BusinessLicenseKind BusinessLicenseKind { get; set; }

	/// <summary>获取或设置工商执照发证机关。</summary>
	public abstract string BusinessLicenseAuthority { get; set; }

	/// <summary>获取或设置工商执照相片路径。</summary>
	public abstract string BusinessLicensePhotoPath { get; set; }

	/// <summary>获取或设置工商执照登记日期。</summary>
	public abstract DateTime? BusinessLicenseIssueDate { get; set; }

	/// <summary>获取或设置工商执照过期日期。</summary>
	public abstract DateTime? BusinessLicenseExpiryDate { get; set; }

	/// <summary>获取或设置工商执照经营范围。</summary>
	public abstract string BusinessLicenseDescription { get; set; }

	/// <summary>获取或设置注册资本。</summary>
	public abstract ushort? RegisteredCapital { get; set; }

	/// <summary>获取或设置注册地址。</summary>
	public abstract string RegisteredAddress { get; set; }

	/// <summary>获取或设置人员规模。</summary>
	public abstract StaffScale StaffScale { get; set; }

	/// <summary>获取或设置法定代表人名。</summary>
	public abstract string LegalRepresentativeName { get; set; }

	/// <summary>获取或设置法定代表性别。</summary>
	public abstract bool? LegalRepresentativeGender { get; set; }

	/// <summary>获取或设置法定代表人邮箱地址。</summary>
	public abstract string LegalRepresentativeEmail { get; set; }

	/// <summary>获取或设置法定代表人身份证号。</summary>
	public abstract string LegalRepresentativeIdentityId { get; set; }

	/// <summary>获取或设置法人身份证种类。</summary>
	public abstract IdentityKind LegalRepresentativeIdentityKind { get; set; }

	/// <summary>获取或设置法人身份证签发日期。</summary>
	public abstract DateTime? LegalRepresentativeIdentityIssued { get; set; }

	/// <summary>获取或设置法人身份证过期日期。</summary>
	public abstract DateTime? LegalRepresentativeIdentityExpiry { get; set; }

	/// <summary>获取或设置法人代表移动电话。</summary>
	public abstract string LegalRepresentativeMobilePhone { get; set; }

	/// <summary>获取或设置法人证件照片路径1。</summary>
	public abstract string LegalRepresentativeIdentityPath1 { get; set; }

	/// <summary>获取或设置法人证件照片路径2。</summary>
	public abstract string LegalRepresentativeIdentityPath2 { get; set; }

	/// <summary>获取或设置开户银行代号。</summary>
	public abstract string BankCode { get; set; }

	/// <summary>获取或设置开户银行名称。</summary>
	public abstract string BankName { get; set; }

	/// <summary>获取或设置开户银行账户号码。</summary>
	public abstract string BankAccountCode { get; set; }

	/// <summary>获取或设置开户银行账户设置。</summary>
	public abstract string BankAccountSetting { get; set; }

	/// <summary>获取或设置电话号码。</summary>
	public abstract string PhoneNumber { get; set; }

	/// <summary>获取或设置负责人编号。</summary>
	public abstract uint? PrincipalId { get; set; }

	/// <summary>获取或设置负责人对象。</summary>
	public abstract UserModel Principal { get; set; }

	/// <summary>获取或设置联系人姓名。</summary>
	public abstract string ContactName { get; set; }

	/// <summary>获取或设置联系人性别。</summary>
	public abstract bool? ContactGender { get; set; }

	/// <summary>获取或设置联系人邮箱。</summary>
	public abstract string ContactEmail { get; set; }

	/// <summary>获取或设置联系人移动电话。</summary>
	public abstract string ContactMobilePhone { get; set; }

	/// <summary>获取或设置联系人办公电话。</summary>
	public abstract string ContactOfficePhone { get; set; }

	/// <summary>获取或设置联系人身份证号。</summary>
	public abstract string ContactIdentityId { get; set; }

	/// <summary>获取或设置联系人身份证种类。</summary>
	public abstract IdentityKind ContactIdentityKind { get; set; }

	/// <summary>获取或设置联系人身份证签发日期。</summary>
	public abstract DateTime? ContactIdentityIssued { get; set; }

	/// <summary>获取或设置联系人身份证过期日期。</summary>
	public abstract DateTime? ContactIdentityExpiry { get; set; }

	/// <summary>获取或设置联系人证件照片路径1。</summary>
	public abstract string ContactIdentityPath1 { get; set; }

	/// <summary>获取或设置联系人证件照片路径2。</summary>
	public abstract string ContactIdentityPath2 { get; set; }

	/// <summary>获取或设置分支机构状态。</summary>
	public abstract BranchStatus Status { get; set; }

	/// <summary>获取或设置状态变更时间。</summary>
	public abstract DateTime? StatusTimestamp { get; set; }

	/// <summary>获取或设置状态描述。</summary>
	public abstract string StatusDescription { get; set; }

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

	/// <summary>获取或设置备注。</summary>
	public abstract string Remark { get; set; }
	#endregion

	#region 集合属性
	/// <summary>获取或设置成员集。</summary>
	public abstract IEnumerable<Employee> Members { get; set; }
	/// <summary>获取或设置班组集。</summary>
	public abstract ICollection<Team> Teams { get; set; }
	/// <summary>获取或设置部门集。</summary>
	public abstract ICollection<Department> Departments { get; set; }
	#endregion
}

/// <summary>
/// 表示分支机构查询条件的实体类。
/// </summary>
public abstract class BranchCriteria : CriteriaBase
{
	#region 公共属性
	/// <summary>获取或设置查询常用键值。</summary>
	[Condition(nameof(BranchNo))]
	public abstract string Key { get; set; }

	/// <summary>获取或设置租户编号。</summary>
	public abstract uint? TenantId { get; set; }

	/// <summary>获取或设置分支机构代号。</summary>
	public abstract string BranchNo { get; set; }

	/// <summary>获取或设置分支机构名称。</summary>
	[Condition(ConditionOperator.Like, nameof(Branch.BranchNo), nameof(Branch.Acronym), nameof(Branch.Abbr))]
	public abstract string Name { get; set; }

	/// <summary>获取或设置所属地区国别。</summary>
	public abstract ushort? Country { get; set; }

	/// <summary>获取或设置语言代码。</summary>
	public abstract string Language { get; set; }

	/// <summary>获取或设置租户所在地址行政区划代码。</summary>
	[Condition(typeof(AddressConditionConverter))]
	public abstract uint? AddressId { get; set; }
	#endregion
}

/// <summary>
/// 表示层次化的分支机构实体类。
/// </summary>
public class HierarchicalBranch : Branch
{
	#region 成员字段
	private readonly Branch _branch;
	#endregion

	#region 构造函数
	public HierarchicalBranch(Branch branch)
	{
		_branch = branch ?? throw new ArgumentNullException(nameof(branch));
		this.Level = HierarchyVector32.GetDepth(branch.BranchId);
		this.Children = new List<HierarchicalBranch>();
	}
	#endregion

	#region 公共属性
	public int Level { get; }
	public IList<HierarchicalBranch> Children { get; }

	public override uint TenantId { get => _branch.TenantId; set => _branch.TenantId = value; }
	public override Tenant Tenant { get => _branch.Tenant; set => _branch.Tenant = value; }
	public override uint BranchId { get => _branch.BranchId; set => _branch.BranchId = value; }
	public override string BranchNo { get => _branch.BranchNo; set => _branch.BranchNo = value; }
	public override string Name { get => _branch.Name; set => _branch.Name = value; }
	public override string Abbr { get => _branch.Abbr; set => _branch.Abbr = value; }
	public override string Acronym { get => _branch.Acronym; set => _branch.Acronym = value; }
	public override Zongsoft.IO.PathLocation LogoPath { get => _branch.LogoPath; set => _branch.LogoPath = value; }
	public override short Ordinal { get => _branch.Ordinal; set => _branch.Ordinal = value; }
	public override ushort Country { get => _branch.Country; set => _branch.Country = value; }
	public override string Language { get => _branch.Language; set => _branch.Language = value; }
	public override uint AddressId { get => _branch.AddressId; set => _branch.AddressId = value; }
	public override string AddressDetail { get => _branch.AddressDetail; set => _branch.AddressDetail = value; }
	public override double? Longitude { get => _branch.Longitude; set => _branch.Longitude = value; }
	public override double? Latitude { get => _branch.Latitude; set => _branch.Latitude = value; }
	public override string BusinessLicenseNo { get => _branch.BusinessLicenseNo; set => _branch.BusinessLicenseNo = value; }
	public override BusinessLicenseKind BusinessLicenseKind { get => _branch.BusinessLicenseKind; set => _branch.BusinessLicenseKind = value; }
	public override string BusinessLicenseAuthority { get => _branch.BusinessLicenseAuthority; set => _branch.BusinessLicenseAuthority = value; }
	public override string BusinessLicensePhotoPath { get => _branch.BusinessLicensePhotoPath; set => _branch.BusinessLicensePhotoPath = value; }
	public override DateTime? BusinessLicenseIssueDate { get => _branch.BusinessLicenseIssueDate; set => _branch.BusinessLicenseIssueDate = value; }
	public override DateTime? BusinessLicenseExpiryDate { get => _branch.BusinessLicenseExpiryDate; set => _branch.BusinessLicenseExpiryDate = value; }
	public override string BusinessLicenseDescription { get => _branch.BusinessLicenseDescription; set => _branch.BusinessLicenseDescription = value; }
	public override ushort? RegisteredCapital { get => _branch.RegisteredCapital; set => _branch.RegisteredCapital = value; }
	public override string RegisteredAddress { get => _branch.RegisteredAddress; set => _branch.RegisteredAddress = value; }
	public override StaffScale StaffScale { get => _branch.StaffScale; set => _branch.StaffScale = value; }
	public override string LegalRepresentativeName { get => _branch.LegalRepresentativeName; set => _branch.LegalRepresentativeName = value; }
	public override bool? LegalRepresentativeGender { get => _branch.LegalRepresentativeGender; set => _branch.LegalRepresentativeGender = value; }
	public override string LegalRepresentativeEmail { get => _branch.LegalRepresentativeEmail; set => _branch.LegalRepresentativeEmail = value; }
	public override string LegalRepresentativeIdentityId { get => _branch.LegalRepresentativeIdentityId; set => _branch.LegalRepresentativeIdentityId = value; }
	public override IdentityKind LegalRepresentativeIdentityKind { get => _branch.LegalRepresentativeIdentityKind; set => _branch.LegalRepresentativeIdentityKind = value; }
	public override DateTime? LegalRepresentativeIdentityIssued { get => _branch.LegalRepresentativeIdentityIssued; set => _branch.LegalRepresentativeIdentityIssued = value; }
	public override DateTime? LegalRepresentativeIdentityExpiry { get => _branch.LegalRepresentativeIdentityExpiry; set => _branch.LegalRepresentativeIdentityExpiry = value; }
	public override string LegalRepresentativeMobilePhone { get => _branch.LegalRepresentativeMobilePhone; set => _branch.LegalRepresentativeMobilePhone = value; }
	public override string LegalRepresentativeIdentityPath1 { get => _branch.LegalRepresentativeIdentityPath1; set => _branch.LegalRepresentativeIdentityPath1 = value; }
	public override string LegalRepresentativeIdentityPath2 { get => _branch.LegalRepresentativeIdentityPath2; set => _branch.LegalRepresentativeIdentityPath2 = value; }
	public override string BankCode { get => _branch.BankCode; set => _branch.BankCode = value; }
	public override string BankName { get => _branch.BankName; set => _branch.BankName = value; }
	public override string BankAccountCode { get => _branch.BankAccountCode; set => _branch.BankAccountCode = value; }
	public override string BankAccountSetting { get => _branch.BankAccountSetting; set => _branch.BankAccountSetting = value; }
	public override string PhoneNumber { get => _branch.PhoneNumber; set => _branch.PhoneNumber = value; }
	public override uint? PrincipalId { get => _branch.PrincipalId; set => _branch.PrincipalId = value; }
	public override UserModel Principal { get => _branch.Principal; set => _branch.Principal = value; }
	public override string ContactName { get => _branch.ContactName; set => _branch.ContactName = value; }
	public override bool? ContactGender { get => _branch.ContactGender; set => _branch.ContactGender = value; }
	public override string ContactEmail { get => _branch.ContactEmail; set => _branch.ContactEmail = value; }
	public override string ContactMobilePhone { get => _branch.ContactMobilePhone; set => _branch.ContactMobilePhone = value; }
	public override string ContactOfficePhone { get => _branch.ContactOfficePhone; set => _branch.ContactOfficePhone = value; }
	public override string ContactIdentityId { get => _branch.ContactIdentityId; set => _branch.ContactIdentityId = value; }
	public override IdentityKind ContactIdentityKind { get => _branch.ContactIdentityKind; set => _branch.ContactIdentityKind = value; }
	public override DateTime? ContactIdentityIssued { get => _branch.ContactIdentityIssued; set => _branch.ContactIdentityIssued = value; }
	public override DateTime? ContactIdentityExpiry { get => _branch.ContactIdentityExpiry; set => _branch.ContactIdentityExpiry = value; }
	public override string ContactIdentityPath1 { get => _branch.ContactIdentityPath1; set => _branch.ContactIdentityPath1 = value; }
	public override string ContactIdentityPath2 { get => _branch.ContactIdentityPath2; set => _branch.ContactIdentityPath2 = value; }
	public override BranchStatus Status { get => _branch.Status; set => _branch.Status = value; }
	public override DateTime? StatusTimestamp { get => _branch.StatusTimestamp; set => _branch.StatusTimestamp = value; }
	public override string StatusDescription { get => _branch.StatusDescription; set => _branch.StatusDescription = value; }
	public override uint CreatorId { get => _branch.CreatorId; set => _branch.CreatorId = value; }
	public override UserModel Creator { get => _branch.Creator; set => _branch.Creator = value; }
	public override DateTime CreatedTime { get => _branch.CreatedTime; set => _branch.CreatedTime = value; }
	public override uint? ModifierId { get => _branch.ModifierId; set => _branch.ModifierId = value; }
	public override UserModel Modifier { get => _branch.Modifier; set => _branch.Modifier = value; }
	public override DateTime? ModifiedTime { get => _branch.ModifiedTime; set => _branch.ModifiedTime = value; }
	public override string Remark { get => _branch.Remark; set => _branch.Remark = value; }

	public override IEnumerable<Employee> Members { get => _branch.Members; set => _branch.Members = value; }
	public override ICollection<Team> Teams { get => _branch.Teams; set => _branch.Teams = value; }
	public override ICollection<Department> Departments { get => _branch.Departments; set => _branch.Departments = value; }
	#endregion
}
