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

		public void Write(char character) => System.Console.Write(character);
		public void Write(string text) => System.Console.Write(text);
		public void Write(object value) => System.Console.Write(value);

		public void Write(CommandOutletColor foregroundColor, char character)
		{
			lock(_syncRoot)
			{
				var originalColor = this.ForegroundColor;
				this.ForegroundColor = foregroundColor;

				System.Console.Write(character);

				this.ForegroundColor = originalColor;
			}
		}

		public void Write(CommandOutletColor foregroundColor, string text)
		{
			lock(_syncRoot)
			{
				var originalColor = this.ForegroundColor;
				this.ForegroundColor = foregroundColor;

				System.Console.Write(text);

				this.ForegroundColor = originalColor;
			}
		}

		public void Write(CommandOutletColor foregroundColor, object value)
		{
			lock(_syncRoot)
			{
				var originalColor = this.ForegroundColor;
				this.ForegroundColor = foregroundColor;

				System.Console.Write(value);

				this.ForegroundColor = originalColor;
			}
		}

		public void Write(CommandOutletContent content) => this.WriteContent(content, false, null);
		public void Write(CommandOutletColor foregroundColor, CommandOutletContent content) => this.WriteContent(content, false, foregroundColor);

		public void WriteLine() => System.Console.WriteLine();
		public void WriteLine(char character) => System.Console.WriteLine(character);
		public void WriteLine(string text) => System.Console.WriteLine(text);
		public void WriteLine(object value) => System.Console.WriteLine(value);

		public void WriteLine(CommandOutletColor foregroundColor, char character)
		{
			lock(_syncRoot)
			{
				var originalColor = this.ForegroundColor;
				this.ForegroundColor = foregroundColor;

				System.Console.WriteLine(character);

				this.ForegroundColor = originalColor;
			}
		}

		public void WriteLine(CommandOutletColor foregroundColor, string text)
		{
			lock(_syncRoot)
			{
				var originalColor = this.ForegroundColor;
				this.ForegroundColor = foregroundColor;

				System.Console.WriteLine(text);

				this.ForegroundColor = originalColor;
			}
		}

		public void WriteLine(CommandOutletColor foregroundColor, object value)
		{
			lock(_syncRoot)
			{
				var originalColor = this.ForegroundColor;
				this.ForegroundColor = foregroundColor;

				System.Console.WriteLine(value);

				this.ForegroundColor = originalColor;
			}
		}

		public void WriteLine(CommandOutletContent content) => this.WriteContent(content, true, null);
		public void WriteLine(CommandOutletColor foregroundColor, CommandOutletContent content) => this.WriteContent(content, true, foregroundColor);
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
			var e = this.Aborting;

			if(e != null)
			{
				var args = new CancelEventArgs();
				e(this, args);
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

		private void WriteContent(CommandOutletContent content, bool appendLine, CommandOutletColor? foregroundColor)
		{
			if(content == null)
				return;

			lock(_syncRoot)
			{
				//获取当前的前景色
				var originalColor = this.ForegroundColor;

				//如果未指定前景色则使用当前前景色
				if(foregroundColor == null)
					foregroundColor = originalColor;

				//设置输出的开始节点
				var cursor = content.Cursor == null ?
					content.First :
					content.Cursor.Next ?? content.First;

				while(cursor != null)
				{
					//设置当前颜色值
					this.ForegroundColor = cursor.Color ?? foregroundColor.Value;

					//输出内容段文本
					System.Console.Write(cursor.Text);

					//更新内容段游标
					content.Cursor = cursor;

					//移动当前游标
					cursor = cursor.Next;
				}

				//还原原来的前景色
				this.ForegroundColor = originalColor;

				//输出最后的换行
				if(appendLine)
					System.Console.WriteLine();
			}
		}
		#endregion
	}
}
