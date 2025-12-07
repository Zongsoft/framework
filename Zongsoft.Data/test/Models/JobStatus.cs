using System;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示就职状态的枚举。
/// </summary>
public enum JobStatus : byte
{
	/// <summary>在职</summary>
	Active,

	/// <summary>试用期</summary>
	Probation,

	/// <summary>离职</summary>
	Quitted,

	/// <summary>辞退</summary>
	Dismission,

	/// <summary>退休</summary>
	Retirement,

	/// <summary>休假</summary>
	Vacationing,
}
