using System;

namespace Zongsoft.Data.Tests.Models;

/// <summary>
/// 表示日志严重程度的枚举。
/// </summary>
public enum LogSeverity : byte
{
	/// <summary>信息</summary>
	Info,
	/// <summary>警告</summary>
	Warn,
	/// <summary>错误</summary>
	Error,
	/// <summary>崩溃</summary>
	Fatal
}
