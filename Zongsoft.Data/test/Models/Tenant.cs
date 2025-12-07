using System;
using System.Collections.Generic;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示租户业务的实体类。
/// </summary>
public abstract class Tenant
{
	#region 普通属性
	/// <summary>获取或设置租户编号。</summary>
	public abstract uint TenantId { get; set; }

	/// <summary>获取或设置租户代号。</summary>
	public abstract string TenantNo { get; set; }

	/// <summary>获取或设置租户名称。</summary>
	public abstract string Name { get; set; }

	/// <summary>获取或设置名称缩写。</summary>
	public abstract string Acronym { get; set; }

	/// <summary>获取或设置简称。</summary>
	public abstract string Abbr { get; set; }

	/// <summary>获取或设置租户LOGO图片路径。</summary>
	public abstract Zongsoft.IO.PathLocation LogoPath { get; set; }

	/// <summary>获取或设置语言代号。</summary>
	public abstract string Language { get; set; }

	/// <summary>获取或设置所属地区国别。</summary>
	public abstract ushort Country { get; set; }

	/// <summary>获取或设置租户所在地址行政区划代码编号。</summary>
	public abstract uint AddressId { get; set; }

	/// <summary>获取或设置经营详细地址。</summary>
	public abstract string AddressDetail { get; set; }

	/// <summary>获取或设置经度坐标。</summary>
	public abstract double? Longitude { get; set; }

	/// <summary>获取或设置纬度坐标。</summary>
	public abstract double? Latitude { get; set; }

	/// <summary>获取或设置租户类型编号。</summary>
	public abstract uint? TenantTypeId { get; set; }

	/// <summary>获取或设置租户子类编号。</summary>
	public abstract byte? TenantSubtypeId { get; set; }

	/// <summary>获取或设置工商执照号。</summary>
	public abstract string BusinessLicenseNo { get; set; }

	/// <summary>获取或设置工商执照种类。</summary>
	public abstract BusinessLicenseKind BusinessLicenseKind { get; set; }

	/// <summary>获取或设置工商执照发证机关。</summary>
	public abstract string BusinessLicenseAuthority { get; set; }

	/// <summary>获取或设置工商执照相片路径。</summary>
	public abstract Zongsoft.IO.PathLocation BusinessLicensePhotoPath { get; set; }

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
	public abstract Zongsoft.IO.PathLocation LegalRepresentativeIdentityPath1 { get; set; }

	/// <summary>获取或设置法人证件照片路径2。</summary>
	public abstract Zongsoft.IO.PathLocation LegalRepresentativeIdentityPath2 { get; set; }

	/// <summary>获取或设置开户银行代号。</summary>
	public abstract string BankCode { get; set; }

	/// <summary>获取或设置开户银行名称。</summary>
	public abstract string BankName { get; set; }

	/// <summary>获取或设置开户银行账户号码。</summary>
	public abstract string BankAccountCode { get; set; }

	/// <summary>获取或设置开户银行账户设置。</summary>
	public abstract string BankAccountSetting { get; set; }

	/// <summary>获取或设置办公电话。</summary>
	public abstract string PhoneNumber { get; set; }

	/// <summary>获取或设置网站网址。</summary>
	public abstract string WebUrl { get; set; }

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
	public abstract Zongsoft.IO.PathLocation ContactIdentityPath1 { get; set; }

	/// <summary>获取或设置联系人证件照片路径2。</summary>
	public abstract Zongsoft.IO.PathLocation ContactIdentityPath2 { get; set; }

	/// <summary>获取或设置标记位。</summary>
	public abstract byte Flags { get; set; }

	/// <summary>获取或设置级别。</summary>
	public abstract byte Grade { get; set; }

	/// <summary>获取或设置租户状态。</summary>
	public abstract TenantStatus Status { get; set; }

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
	/// <summary>获取或设置租户许可授权集。</summary>
	public abstract ICollection<TenantLicense> Licenses { get; set; }
	#endregion
}

/// <summary>
/// 表示租户查询条件的实体类。
/// </summary>
public abstract class TenantCriteria : CriteriaBase
{
	#region 公共属性
	/// <summary>获取或设置查询常用键值。</summary>
	[Condition(nameof(TenantNo), nameof(BusinessLicenseNo))]
	public abstract string Key { get; set; }

	/// <summary>获取或设置租户代号。</summary>
	public abstract string TenantNo { get; set; }

	/// <summary>获取或设置工商执照号。</summary>
	public abstract string BusinessLicenseNo { get; set; }

	/// <summary>获取或设置租户名称。</summary>
	[Condition(ConditionOperator.Like, nameof(Name), nameof(Tenant.Acronym), nameof(Tenant.Abbr))]
	public abstract string Name { get; set; }

	/// <summary>获取或设置所属地区国别。</summary>
	public abstract ushort? Country { get; set; }

	/// <summary>获取或设置语言代码。</summary>
	public abstract string Language { get; set; }

	/// <summary>获取或设置租户所在地址行政区划代码。</summary>
	[Condition(typeof(AddressConditionConverter))]
	public abstract uint? AddressId { get; set; }

	/// <summary>获取或设置租户类型编号。</summary>
	public abstract uint? TenantTypeId { get; set; }

	/// <summary>获取或设置租户子类编号。</summary>
	public abstract byte? TenantSubtypeId { get; set; }

	/// <summary>获取或设置工商执照登记种类。</summary>
	public abstract BusinessLicenseKind? BusinessLicenseKind { get; set; }

	/// <summary>获取或设置工商执照登记日期。</summary>
	public abstract Range<DateTime>? BusinessLicenseIssueDate { get; set; }

	/// <summary>获取或设置工商执照过期日期。</summary>
	public abstract Range<DateTime>? BusinessLicenseIssueExpiryDate { get; set; }

	/// <summary>获取或设置注册资本。</summary>
	public abstract Range<short>? RegisteredCapital { get; set; }

	/// <summary>获取或设置人员规模的数组。</summary>
	public abstract StaffScale[] StaffScales { get; set; }

	/// <summary>获取或设置级别。</summary>
	public abstract Range<byte>? Grade { get; set; }

	/// <summary>获取或设置法定代表人邮箱地址。</summary>
	public abstract string LegalRepresentativeEmail { get; set; }

	/// <summary>获取或设置法定代表人身份证号。</summary>
	public abstract string LegalRepresentativeIdentityId { get; set; }

	/// <summary>获取或设置法人代表移动电话。</summary>
	public abstract string LegalRepresentativeMobilePhone { get; set; }

	/// <summary>获取或设置联系人邮箱。</summary>
	public abstract string ContactEmail { get; set; }

	/// <summary>获取或设置联系人移动电话。</summary>
	public abstract string ContactMobilePhone { get; set; }

	/// <summary>获取或设置联系人身份证号。</summary>
	public abstract string ContactIdentityId { get; set; }

	/// <summary>获取或设置租户状态。</summary>
	public abstract TenantStatus[] Status { get; set; }

	/// <summary>获取或设置状态变更时间。</summary>
	public abstract Range<DateTime>? StatusTimestamp { get; set; }

	/// <summary>获取或设置创建人编号。</summary>
	public abstract uint? CreatorId { get; set; }

	/// <summary>获取或设置创建时间。</summary>
	public abstract Range<DateTime>? CreatedTime { get; set; }
	#endregion
}

/// <summary>
/// 表示租户许可的实体类。
/// </summary>
public abstract class TenantLicense
{
	#region 普通属性
	/// <summary>获取或设置租户编号。</summary>
	public abstract uint TenantId { get; set; }

	/// <summary>获取或设置模块标识。</summary>
	public abstract string Module { get; set; }

	/// <summary>获取或设置版本。</summary>
	public abstract byte Edition { get; set; }

	/// <summary>获取或设置是否可用。</summary>
	[System.ComponentModel.DefaultValue(true)]
	public abstract bool Enabled { get; set; }

	/// <summary>获取或设置生效日期。</summary>
	public abstract DateTime Effective { get; set; }

	/// <summary>获取或设置过期日期。</summary>
	public abstract DateTime Expiration { get; set; }

	/// <summary>获取或设置选项设置。</summary>
	public abstract string Settings { get; set; }

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
}
