using System;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示婚姻状态的枚举。
/// </summary>
public enum MaritalStatus : byte
{
	/// <summary>未婚</summary>
	Unmarried,

	/// <summary>已婚</summary>
	Married,

	/// <summary>离异</summary>
	Divorced,

	/// <summary>丧偶</summary>
	Widowed,
}
