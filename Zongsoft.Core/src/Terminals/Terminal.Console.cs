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
using System.IO;
using System.Text;
using System.ComponentModel;

using Zongsoft.Components;

namespace Zongsoft.Terminals;

partial class Terminal
{
	private class ConsoleTerminal : ITerminal, ICommandOutlet
	{
		#region 单例字段
		public static readonly ConsoleTerminal Instance = new();
		#endregion

		#region 事件定义
		public event EventHandler Resetted;
		public event EventHandler Resetting;
		public event CancelEventHandler Aborting;
		#endregion

		#region 同步变量
		private readonly object _syncRoot;
		#endregion

		#region 私有构造
		private ConsoleTerminal()
		{
			_syncRoot = new object();
			this.Executor = new ConsoleExecutor(this);

			//if(!System.Diagnostics.Debugger.IsAttached)
			{
				System.Console.TreatControlCAsInput = false;
				System.Console.CancelKeyPress += this.Console_CancelKeyPress;
			}
		}
		#endregion

		#region 公共属性
		public ITerminalExecutor Executor { get; }

		public TextWriter Error
		{
			get => System.Console.Error;
			set => System.Console.SetError(value);
		}

		public TextReader Input
		{
			get => System.Console.In;
			set => System.Console.SetIn(value);
		}

		public TextWriter Output
		{
			get => System.Console.Out;
			set => System.Console.SetOut(value);
		}

		public CommandOutletColor BackgroundColor
		{
			get => ConvertColor(System.Console.BackgroundColor, CommandOutletColor.Black);
			set => System.Console.BackgroundColor = ConvertColor(value, ConsoleColor.Black);
		}

		public CommandOutletColor ForegroundColor
		{
			get => ConvertColor(System.Console.ForegroundColor, CommandOutletColor.White);
			set => System.Console.ForegroundColor = ConvertColor(value, ConsoleColor.White);
		}
		#endregion

		#region 公共方法
		public void Clear() => System.Console.Clear();
		public void Reset()
		{
			//激发“Resetting”事件
			this.OnResetting();

			//恢复默认的颜色设置
			System.Console.ResetColor();

			try
			{
				if(System.Console.CursorLeft > 0)
					System.Console.WriteLine();
			}
			catch { }

			//激发“Resetted”事件
			this.OnResetted();
		}

		public void ResetStyles(TerminalStyles styles)
		{
			if((styles & TerminalStyles.ForegroundColor) == TerminalStyles.ForegroundColor ||
			   (styles & TerminalStyles.BackgroundColor) == TerminalStyles.BackgroundColor)
				System.Console.ResetColor();
		}

		public void Write(string text) => System.Console.Write(text);
		public void Write<T>(T value) => System.Console.Write($"{value}");
		public void Write(CommandOutletContent content) => this.WriteContent(content, false);
		public void Write<T>(CommandOutletColor foregroundColor, T value) => Write(value, foregroundColor);
		public void Write<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => Write(value, foregroundColor, backgroundColor);
		public void Write<T>(CommandOutletStyles style, T value) => Write(value, style);
		public void Write<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value) => Write(value, style, foregroundColor);
		public void Write<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => Write(value, style, foregroundColor, backgroundColor);

		public void WriteLine() => System.Console.WriteLine();
		public void WriteLine(string text) => System.Console.WriteLine(text);
		public void WriteLine<T>(T value) => System.Console.WriteLine($"{value}");
		public void WriteLine(CommandOutletContent content) => this.WriteContent(content, true);
		public void WriteLine<T>(CommandOutletColor foregroundColor, T value) => WriteLine(value, foregroundColor);
		public void WriteLine<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => WriteLine(value, foregroundColor, backgroundColor);
		public void WriteLine<T>(CommandOutletStyles style, T value) => WriteLine(value, style);
		public void WriteLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value) => WriteLine(value, style, foregroundColor);
		public void WriteLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => WriteLine(value, style, foregroundColor, backgroundColor);
		#endregion

		#region 显式实现
		TextWriter ICommandOutlet.Writer => System.Console.Out;
		Encoding ICommandOutlet.Encoding
		{
			get => System.Console.OutputEncoding;
			set => System.Console.OutputEncoding = value;
		}
		#endregion

		#region 激发事件
		protected virtual bool OnAborting()
		{
			var aborting = this.Aborting;

			if(aborting != null)
			{
				var args = new CancelEventArgs();
				aborting(this, args);
				return args.Cancel;
			}

			return false;
		}

		protected virtual void OnResetted() => this.Resetted?.Invoke(this, EventArgs.Empty);
		protected virtual void OnResetting() => this.Resetting?.Invoke(this, EventArgs.Empty);
		#endregion

		#region 中断事件
		private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			e.Cancel = this.OnAborting();
		}
		#endregion

		#region 私有方法
		private static CommandOutletColor ConvertColor(ConsoleColor color, CommandOutletColor defaultColor) =>
			Enum.TryParse<CommandOutletColor>(color.ToString(), out var result) ? result : defaultColor;

		private static ConsoleColor ConvertColor(CommandOutletColor color, ConsoleColor defaultColor) =>
			Enum.TryParse<ConsoleColor>(color.ToString(), out var result) ? result : defaultColor;

		private static string GetStyle(CommandOutletStyles style)
		{
			if(style == 0)
				return null;

			var code = string.Empty;

			if((style & CommandOutletStyles.Bold) == CommandOutletStyles.Bold)
				code += "1;";
			if((style & CommandOutletStyles.Italic) == CommandOutletStyles.Italic)
				code += "3;";
			if((style & CommandOutletStyles.Underline) == CommandOutletStyles.Underline)
				code += "4;";
			if((style & CommandOutletStyles.Strikeout) == CommandOutletStyles.Strikeout)
				code += "9;";
			if((style & CommandOutletStyles.Blinking) == CommandOutletStyles.Blinking)
				code += "5;";
			if((style & CommandOutletStyles.Highlight) == CommandOutletStyles.Highlight)
				code += "7;";

			return code;
		}

		private static int GetForegroundColor(CommandOutletColor? color) => color switch
		{
			CommandOutletColor.Black => 30,
			CommandOutletColor.White => 97,
			CommandOutletColor.DarkRed => 31,
			CommandOutletColor.DarkGreen => 32,
			CommandOutletColor.DarkYellow => 33,
			CommandOutletColor.DarkBlue => 34,
			CommandOutletColor.DarkMagenta => 35,
			CommandOutletColor.DarkCyan => 36,
			CommandOutletColor.Red => 91,
			CommandOutletColor.Green => 92,
			CommandOutletColor.Yellow => 93,
			CommandOutletColor.Blue => 94,
			CommandOutletColor.Magenta => 95,
			CommandOutletColor.Cyan => 96,
			_ => 39,
		};

		private static int GetBackgroundColor(CommandOutletColor? color) => color switch
		{
			CommandOutletColor.Black => 40,
			CommandOutletColor.White => 47,
			CommandOutletColor.DarkRed => 41,
			CommandOutletColor.DarkGreen => 42,
			CommandOutletColor.DarkYellow => 43,
			CommandOutletColor.DarkBlue => 44,
			CommandOutletColor.DarkMagenta => 45,
			CommandOutletColor.DarkCyan => 46,
			CommandOutletColor.Red => 101,
			CommandOutletColor.Green => 102,
			CommandOutletColor.Yellow => 103,
			CommandOutletColor.Blue => 104,
			CommandOutletColor.Magenta => 105,
			CommandOutletColor.Cyan => 106,
			_ => 49,
		};

		private static void Write<T>(T value, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null) => Write<T>(value, CommandOutletStyles.None, foregroundColor, backgroundColor);
		private static void Write<T>(T value, CommandOutletStyles style, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null)
		{
			if(backgroundColor == null)
				System.Console.Write($"\u001b[{GetStyle(style)}{GetForegroundColor(foregroundColor)}m{value}\u001b[0m");
			else
				System.Console.Write($"\u001b[{GetStyle(style)}{GetForegroundColor(foregroundColor)};{GetBackgroundColor(backgroundColor.Value)}m{value}\u001b[0m");
		}

		private static void WriteLine<T>(T value, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null) => WriteLine<T>(value, CommandOutletStyles.None, foregroundColor, backgroundColor);
		private static void WriteLine<T>(T value, CommandOutletStyles style, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null)
		{
			if(backgroundColor == null)
				System.Console.WriteLine($"\u001b[{GetStyle(style)}{GetForegroundColor(foregroundColor)}m{value}\u001b[0m");
			else
				System.Console.WriteLine($"\u001b[{GetStyle(style)}{GetForegroundColor(foregroundColor)};{GetBackgroundColor(backgroundColor.Value)}m{value}\u001b[0m");
		}

		private void WriteContent(CommandOutletContent content, bool appendLine)
		{
			if(content == null)
				return;

			//设置输出的开始节点
			var cursor = content.Cursor == null ?
				content.First :
				content.Cursor.Next ?? content.First;

			lock(_syncRoot)
			{
				while(cursor != null)
				{
					//输出内容段文本
					Write(cursor.Text, cursor.Style, cursor.ForegroundColor, cursor.BackgroundColor);

					//更新内容段游标
					content.Cursor = cursor;

					//移动当前游标
					cursor = cursor.Next;
				}

				//输出最后的换行
				if(appendLine)
					System.Console.WriteLine();
			}
		}
		#endregion
	}
}
