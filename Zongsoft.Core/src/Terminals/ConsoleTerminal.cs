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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

public class ConsoleTerminal : ITerminal, ICommandOutlet
{
	#region 单例字段
	public static readonly ConsoleTerminal Instance = new ConsoleTerminal();
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

		if(!System.Diagnostics.Debugger.IsAttached)
		{
			Console.TreatControlCAsInput = false;
			Console.CancelKeyPress += this.Console_CancelKeyPress;
		}
	}
	#endregion

	#region 公共属性
	public TextWriter Error
	{
		get => Console.Error;
		set => Console.SetError(value);
	}

	public TextReader Input
	{
		get => Console.In;
		set => Console.SetIn(value);
	}

	public TextWriter Output
	{
		get => Console.Out;
		set => Console.SetOut(value);
	}

	public CommandOutletColor BackgroundColor
	{
		get => ConvertColor(Console.BackgroundColor, CommandOutletColor.Black);
		set => Console.BackgroundColor = ConvertColor(value, ConsoleColor.Black);
	}

	public CommandOutletColor ForegroundColor
	{
		get => ConvertColor(Console.ForegroundColor, CommandOutletColor.White);
		set => Console.ForegroundColor = ConvertColor(value, ConsoleColor.White);
	}
	#endregion

	#region 公共方法
	public void Clear() => Console.Clear();
	public void Reset()
	{
		//激发“Resetting”事件
		this.OnResetting();

		//恢复默认的颜色设置
		Console.ResetColor();

		try
		{
			if(Console.CursorLeft > 0)
				Console.WriteLine();
		}
		catch
		{
		}

		//激发“Resetted”事件
		this.OnResetted();
	}

	public void ResetStyles(TerminalStyles styles)
	{
		if((styles & TerminalStyles.Color) == TerminalStyles.Color)
			Console.ResetColor();
	}

	public void Write(string text)
	{
		lock(_syncRoot)
		{
			Console.Write(text);
		}
	}

	public void Write(object value)
	{
		lock(_syncRoot)
		{
			Console.Write(value);
		}
	}

	public void Write(CommandOutletColor foregroundColor, string text)
	{
		lock(_syncRoot)
		{
			var originalColor = this.ForegroundColor;
			this.ForegroundColor = foregroundColor;

			Console.Write(text);

			this.ForegroundColor = originalColor;
		}
	}

	public void Write(CommandOutletColor foregroundColor, object value)
	{
		lock(_syncRoot)
		{
			var originalColor = this.ForegroundColor;
			this.ForegroundColor = foregroundColor;

			Console.Write(value);

			this.ForegroundColor = originalColor;
		}
	}

	public void Write(CommandOutletContent content)
	{
		this.WriteContent(content, false, null);
	}

	public void Write(CommandOutletColor foregroundColor, CommandOutletContent content)
	{
		this.WriteContent(content, false, foregroundColor);
	}

	public void Write(string format, params object[] args)
	{
		lock(_syncRoot)
		{
			Console.Write(format, args);
		}
	}

	public void Write(CommandOutletColor foregroundColor, string format, params object[] args)
	{
		lock(_syncRoot)
		{
			var originalColor = this.ForegroundColor;
			this.ForegroundColor = foregroundColor;

			Console.Write(format, args);

			this.ForegroundColor = originalColor;
		}
	}

	public void WriteLine()
	{
		lock(_syncRoot)
		{
			Console.WriteLine();
		}
	}

	public void WriteLine(string text)
	{
		lock(_syncRoot)
		{
			Console.WriteLine(text);
		}
	}

	public void WriteLine(object value)
	{
		lock(_syncRoot)
		{
			Console.WriteLine(value);
		}
	}

	public void WriteLine(CommandOutletColor foregroundColor, string text)
	{
		lock(_syncRoot)
		{
			var originalColor = this.ForegroundColor;
			this.ForegroundColor = foregroundColor;

			Console.WriteLine(text);

			this.ForegroundColor = originalColor;
		}
	}

	public void WriteLine(CommandOutletColor foregroundColor, object value)
	{
		lock(_syncRoot)
		{
			var originalColor = this.ForegroundColor;
			this.ForegroundColor = foregroundColor;

			Console.WriteLine(value);

			this.ForegroundColor = originalColor;
		}
	}

	public void WriteLine(CommandOutletContent content)
	{
		this.WriteContent(content, true, null);
	}

	public void WriteLine(CommandOutletColor foregroundColor, CommandOutletContent content)
	{
		this.WriteContent(content, true, foregroundColor);
	}

	public void WriteLine(string format, params object[] args)
	{
		lock(_syncRoot)
		{
			Console.WriteLine(format, args);
		}
	}

	public void WriteLine(CommandOutletColor foregroundColor, string format, params object[] args)
	{
		lock(_syncRoot)
		{
			var originalColor = this.ForegroundColor;
			this.ForegroundColor = foregroundColor;

			Console.WriteLine(format, args);

			this.ForegroundColor = originalColor;
		}
	}
	#endregion

	#region 显式实现
	Encoding ICommandOutlet.Encoding
	{
		get => Console.OutputEncoding;
		set => Console.OutputEncoding = value;
	}

	TextWriter ICommandOutlet.Writer => Console.Out;
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

	protected virtual void OnResetted()
	{
		this.Resetted?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnResetting()
	{
		this.Resetting?.Invoke(this, EventArgs.Empty);
	}
	#endregion

	#region 中断事件
	private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
	{
		e.Cancel = this.OnAborting();
	}
	#endregion

	#region 私有方法
	private static CommandOutletColor ConvertColor(ConsoleColor color, CommandOutletColor defaultColor)
	{
		if(Enum.TryParse<CommandOutletColor>(color.ToString(), out var result))
			return result;
		else
			return defaultColor;
	}

	private static ConsoleColor ConvertColor(CommandOutletColor color, ConsoleColor defaultColor)
	{
		if(Enum.TryParse<ConsoleColor>(color.ToString(), out var result))
			return result;
		else
			return defaultColor;
	}

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

			while(content != null)
			{
				//设置片段内容指定的颜色值
				this.ForegroundColor = content.Color ?? foregroundColor.Value;

				//输出指定的片段内容文本
				Console.Write(content.Text);

				//将当前片段内容指定为下一个
				content = content.Next;
			}

			//还原原来的前景色
			this.ForegroundColor = originalColor;

			//输出最后的换行
			if(appendLine)
				Console.WriteLine();
		}
	}
	#endregion
}
