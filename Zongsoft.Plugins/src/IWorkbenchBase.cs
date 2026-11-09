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
using System.ComponentModel;

namespace Zongsoft.Plugins;

/// <summary>表示工作台的接口，包含对工作台的基本行为特性的定义。</summary>
public interface IWorkbenchBase
{
	/// <summary>当工作台被打开后。</summary>
	event EventHandler Opened;
	/// <summary>当工作台被打开前。</summary>
	event EventHandler Opening;
	/// <summary>当工作台被关闭后。</summary>
	event EventHandler Closed;
	/// <summary>当工作台被关闭前。</summary>
	event CancelEventHandler Closing;

	/// <summary>获取工作台的当前状态。</summary>
	WorkbenchStatus Status { get; }

	/// <summary>获取或设置工作台标题。</summary>
	string Title { get; set; }

	/// <summary>打开工作台。</summary>
	void Open();

	/// <summary>关闭工作台。</summary>
	void Close();
}
