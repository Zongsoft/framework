using System;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示人员规模的枚举。
/// </summary>
public enum StaffScale : byte
{
	/// <summary>未定义</summary>
	None,

	/// <summary>10人以内</summary>
	Ten,

	/// <summary>30人以内</summary>
	Thirty,

	/// <summary>50人以内</summary>
	Fifty,

	/// <summary>100人以内</summary>
	OneHundred,

	/// <summary>200人以内</summary>
	TwoHundred,

	/// <summary>500人以内</summary>
	FiveHundred,

	/// <summary>1000人以内</summary>
	OneThousand,

	/// <summary>5000人以内</summary>
	FiveThousand,

	/// <summary>10000人以内</summary>
	TenThousand,

	/// <summary>50000人以内</summary>
	FiftyThousand,

	/// <summary>100000人以内</summary>
	HundredThousand,

	/// <summary>100000人以上</summary>
	More,
}
