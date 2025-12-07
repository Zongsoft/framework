using System;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示受教育程度（学历）的枚举。
/// </summary>
public enum EducationDegree : byte
{
	/// <summary>未定义</summary>
	None = 0,

	/// <summary>初等教育(小学)</summary>
	Primary,

	/// <summary>中等教育(中学)</summary>
	Secondary,

	/// <summary>专科教育(大/中专)</summary>
	College,

	/// <summary>本科教育</summary>
	Undergraduate,

	/// <summary>硕士教育</summary>
	Master,

	/// <summary>博士教育</summary>
	Doctor,
}
