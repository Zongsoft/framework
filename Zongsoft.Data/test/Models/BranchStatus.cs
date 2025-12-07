using System;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示分支机构状态的枚举。
/// </summary>
public enum BranchStatus : byte
{
	/// <summary>正常。</summary>
	Normal = 0,

	/// <summary>已撤裁。</summary>
	Disabled,
}
