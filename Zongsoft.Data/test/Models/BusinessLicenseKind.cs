using System;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示营业执照种类的枚举。
/// </summary>
public enum BusinessLicenseKind : byte
{
	/// <summary>其他</summary>
	None,

	/// <summary>个人</summary>
	Peronal,

	/// <summary>商业公司/(个体户)</summary>
	Company,

	/// <summary>政府单位</summary>
	Government,

	/// <summary>非政府组织</summary>
	Organization,
}
