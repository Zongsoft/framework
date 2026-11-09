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
using System.Text;

namespace Zongsoft.Components;

public interface ICommandOutlet
{
	#region 公共属性
	Encoding Encoding { get; set; }
	TextWriter Writer { get; }
	#endregion

	#region 公共方法
	void Write(CommandOutletContent content);
	void Write<T>(T value);
	void Write<T>(CommandOutletColor foregroundColor, T value);
	void Write<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value);
	void Write<T>(CommandOutletStyles style, T value);
	void Write<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value);
	void Write<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value);

	void WriteLine();
	void WriteLine(CommandOutletContent content);
	void WriteLine<T>(T value);
	void WriteLine<T>(CommandOutletColor foregroundColor, T value);
	void WriteLine<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value);
	void WriteLine<T>(CommandOutletStyles style, T value);
	void WriteLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value);
	void WriteLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value);
	#endregion
}
