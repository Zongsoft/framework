using System;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示身份证件种类的枚举。
/// </summary>
public enum IdentityKind : byte
{
	/// <summary>其他</summary>
	None,

	/// <summary>身份证</summary>
	Identity,

	/// <summary>居住证</summary>
	Residence,

	/// <summary>护照</summary>
	Passport,

	/// <summary>驾照</summary>
	DrivingLicense,

	/// <summary>统一授信</summary>
	UnifiedLicense,

	/// <summary>工商执照</summary>
	BusinessLicense,
}
