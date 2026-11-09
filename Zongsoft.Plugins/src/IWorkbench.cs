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

namespace Zongsoft.Plugins;

/// <summary>表示工作台的接口，包含对工作台的基本行为特性的定义。</summary>
public interface IWorkbench : IWorkbenchBase
{
	/// <summary>当视图被激活。</summary>
	event EventHandler<ViewEventArgs> ViewActivate;
	/// <summary>当视图失去焦点，当视图被关闭时也会触发该事件。</summary>
	event EventHandler<ViewEventArgs> ViewDeactivate;

	/// <summary>获取当前活动的视图对象。</summary>
	object ActiveView { get; }

	/// <summary>获取当前工作台的所有打开的视图对象。</summary>
	object[] Views { get; }

	/// <summary>获取当前工作台的窗口对象。</summary>
	object Window { get; }

	/// <summary>激活指定名称的视图对象。</summary>
	/// <param name="name">视图名称。</param>
	/// <returns>被激活的视图对象。</returns>
	void ActivateView(string name);
}
