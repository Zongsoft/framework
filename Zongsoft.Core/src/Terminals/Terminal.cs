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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Components;

namespace Zongsoft.Terminals;

public static partial class Terminal
{
	#region 常量定义
	internal const string SPLASH = @"
     _____                                ___ __
    /_   /  ____  ____  ____  ____ ____  / __/ /_
      / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
     / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
    /____/\____/_/ /_/\__  /____/\____/_/  \__/
                     /____/
";
	#endregion

	#region 单例字段
	public static ITerminal Console => ConsoleTerminal.Instance;
	#endregion

	#region 静态属性
	private static ITerminal _default;
	public static ITerminal Default
	{
		get => _default ?? Console;
		set => _default = value;
	}
	#endregion

	#region 公共方法
	public static void Clear() => Default.Clear();
	public static void Reset() => Default.Reset();
	public static void ResetStyles(TerminalStyles styles) => Default.ResetStyles(styles);

	public static void Write(char character) => Default.Write(character);
	public static void Write(string text) => Default.Write(text);
	public static void Write(object value) => Default.Write(value);
	public static void Write(CommandOutletColor foregroundColor, char character) => Default.Write(foregroundColor, character);
	public static void Write(CommandOutletColor foregroundColor, string text) => Default.Write(foregroundColor, text);
	public static void Write(CommandOutletColor foregroundColor, object value) => Default.Write(foregroundColor, value);
	public static void Write(CommandOutletContent content) => Default.Write(content);
	public static void Write(CommandOutletColor foregroundColor, CommandOutletContent content) => Default.Write(foregroundColor, content);

	public static void WriteLine() => Default.WriteLine();
	public static void WriteLine(char character) => Default.WriteLine(character);
	public static void WriteLine(string text) => Default.WriteLine(text);
	public static void WriteLine(object value) => Default.WriteLine(value);
	public static void WriteLine(CommandOutletColor foregroundColor, char character) => Default.WriteLine(foregroundColor, character);
	public static void WriteLine(CommandOutletColor foregroundColor, string text) => Default.WriteLine(foregroundColor, text);
	public static void WriteLine(CommandOutletColor foregroundColor, object value) => Default.WriteLine(foregroundColor, value);
	public static void WriteLine(CommandOutletContent content) => Default.WriteLine(content);
	public static void WriteLine(CommandOutletColor foregroundColor, CommandOutletContent content) => Default.WriteLine(foregroundColor, content);
	#endregion
}
