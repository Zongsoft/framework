/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.IO;
using System.ComponentModel;

namespace Zongsoft.Terminals;

/// <summary>表示提供输入、输出和中断通知的终端。</summary>
public interface ITerminal : Components.ICommandOutlet
{
	#region 事件定义
	/// <summary>在终端重置后发生。</summary>
	event EventHandler Resetted;
	/// <summary>在终端重置前发生。</summary>
	event EventHandler Resetting;
	/// <summary>在请求中断时发生；处理器将 <see cref="CancelEventArgs.Cancel"/> 设为 <see langword="true"/> 表示已处理此次请求。</summary>
	/// <remarks>处理器须同步、及时返回；异步清理由命令或执行器的所有者等待。</remarks>
	event CancelEventHandler Aborting;
	#endregion

	#region 属性定义
	/// <summary>获取终端命令执行器。</summary>
	ITerminalExecutor Executor { get; }
	/// <summary>获取或设置错误输出。</summary>
	TextWriter Error { get; set; }
	/// <summary>获取或设置终端输入；默认控制台允许设置 <see langword="null" /> 以恢复标准输入。</summary>
	TextReader Input { get; set; }
	/// <summary>获取或设置文本输出。</summary>
	TextWriter Output { get; set; }
	/// <summary>获取或设置背景颜色。</summary>
	Components.CommandOutletColor BackgroundColor { get; set; }
	/// <summary>获取或设置前景颜色。</summary>
	Components.CommandOutletColor ForegroundColor { get; set; }
	#endregion

	#region 方法定义
	/// <summary>同步触发中断通知，由调用方决定未处理请求的后续行为。</summary>
	/// <returns>通知处理器已处理请求则返回 <see langword="true"/>；没有处理器或未处理时返回 <see langword="false"/>。</returns>
	/// <remarks>本方法不停止输入、执行器或进程，也不等待异步清理。事件处理器的异常向调用方传播。</remarks>
	bool Abort();
	/// <summary>清除终端显示。</summary>
	void Clear();
	/// <summary>重置终端并触发重置前后的通知。</summary>
	void Reset();
	/// <summary>重置指定的显示样式。</summary>
	/// <param name="styles">需要重置的样式。</param>
	void ResetStyles(TerminalStyles styles);
	#endregion
}
