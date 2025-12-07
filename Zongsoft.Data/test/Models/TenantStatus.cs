using System;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示租户状态的枚举。
/// </summary>
public enum TenantStatus : byte
{
	/// <summary>0: 正常。</summary>
	Normal = 0,

	/// <summary>1: 已禁用。</summary>
	Disabled,

	/// <summary>2: 待审核。</summary>
	Unapproved,

	/// <summary>3: 被挂起(可能因为用户欠费)。</summary>
	Suspended,
}
