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
using System.Text;

namespace Zongsoft.Components;

/// <summary>
/// 表示命令输出内容链的类。
/// </summary>
public class CommandOutletContent
{
	#region 嵌套枚举
	public enum Direction
	{
		Previous,
		Next,
	}
	#endregion

	#region 成员字段
	private string _text;
	private CommandOutletStyles _style;
	private CommandOutletColor? _foregroundColor;
	private CommandOutletColor? _backgroundColor;
	private CommandOutletContent _next;
	private CommandOutletContent _previous;
	#endregion

	#region 私有构造
	private CommandOutletContent(CommandOutletContent previous, string text, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null) : this(previous, text, CommandOutletStyles.None, foregroundColor, backgroundColor) { }
	private CommandOutletContent(CommandOutletContent previous, string text, CommandOutletStyles style, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null)
	{
		_text = text;
		_style = style;
		_foregroundColor = foregroundColor;
		_backgroundColor = backgroundColor;
		_previous = previous;
		_next = null;
	}

	private CommandOutletContent(CommandOutletContent previous, string text, CommandOutletContent next, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null) : this(previous, text, next, CommandOutletStyles.None, foregroundColor, backgroundColor) { }
	private CommandOutletContent(CommandOutletContent previous, string text, CommandOutletContent next, CommandOutletStyles style, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null)
	{
		_text = text;
		_style = style;
		_foregroundColor = foregroundColor;
		_backgroundColor = backgroundColor;
		_previous = previous;
		_next = next;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置内容段文本。</summary>
	public string Text { get => _text; set => _text = value; }

	/// <summary>获取或设置内容段的样式。</summary>
	public CommandOutletStyles Style { get => _style; set => _style = value; }

	/// <summary>获取或设置内容段的文本前景颜色，如果为空(<c>null</c>)则使用默认前景色。</summary>
	public CommandOutletColor? ForegroundColor { get => _foregroundColor; set => _foregroundColor = value; }

	/// <summary>获取或设置内容段的文本背景颜色，如果为空(<c>null</c>)则使用默认背景色。</summary>
	public CommandOutletColor? BackgroundColor { get => _backgroundColor; set => _backgroundColor = value; }

	/// <summary>获取一个值，指示内容链是否为空（即内容链的所有节点文本都为空）。</summary>
	public bool IsEmpty
	{
		get
		{
			if(!string.IsNullOrEmpty(_text))
				return false;

			var current = _previous;
			while(current != null)
			{
				if(!IsEmpty(current))
					return false;

				current = current.Previous;
			}

			current = _next;
			while(current != null)
			{
				if(!IsEmpty(current))
					return false;

				current = current.Next;
			}

			return true;

			static bool IsEmpty(CommandOutletContent content)
			{
				return content == null || string.IsNullOrEmpty(content.Text);
			}
		}
	}

	/// <summary>获取或设置当前游标，即显示装置的定位。</summary>
	public CommandOutletContent Cursor { get; set; }

	/// <summary>获取当前内容链的首段。</summary>
	public CommandOutletContent First
	{
		get
		{
			var current = this;

			while(current._previous != null)
				current = current._previous;

			return current;
		}
	}

	/// <summary>获取当前内容链的末段。</summary>
	public CommandOutletContent Last
	{
		get
		{
			var current = this;

			while(current._next != null)
				current = current._next;

			return current;
		}
	}

	/// <summary>获取当前内容链的下一段。</summary>
	public CommandOutletContent Next => _next;

	/// <summary>获取当前内容链的上一段。</summary>
	public CommandOutletContent Previous => _previous;
	#endregion

	#region 公共方法
	/// <summary>获取指定方向的内容节点数。</summary>
	/// <param name="direction">指定的计数方向。</param>
	/// <returns>返回的内容节点数量。</returns>
	public int Count(Direction direction)
	{
		return Count(this, direction);

		static int Count(CommandOutletContent current, Direction direction)
		{
			int count = 0;

			while(current != null)
			{
				count++;

				current = direction switch
				{
					Direction.Previous => current._previous,
					Direction.Next => current._next,
					_ => null,
				};
			}

			return count;
		}
	}

	/// <summary>追加一个指定值的内容段。</summary>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent Append<T>(T value) => this.AfterCore($"{value}");

	/// <summary>追加一个指定颜色和值的内容段。</summary>
	/// <param name="foregroundColor">指定的内容文本颜色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent Append<T>(CommandOutletColor foregroundColor, T value) => this.AfterCore($"{value}", foregroundColor);

	/// <summary>追加一个指定颜色和值的内容段。</summary>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="backgroundColor">指定的内容背景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent Append<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => this.AfterCore($"{value}", foregroundColor, backgroundColor);

	/// <summary>追加一个指定样式和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent Append<T>(CommandOutletStyles style, T value) => this.AfterCore($"{value}", style);

	/// <summary>追加一个指定样式、颜色和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent Append<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value) => this.AfterCore($"{value}", style, foregroundColor);

	/// <summary>追加一个指定样式、颜色和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="backgroundColor">指定的内容背景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent Append<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => this.AfterCore($"{value}", style, foregroundColor, backgroundColor);

	/// <summary>追加一个指定的内容段。</summary>
	/// <param name="content">指定的内容段。</param>
	/// <returns>返回当前内容段。</returns>
	public CommandOutletContent Append(CommandOutletContent content) => this.AfterCore(content);

	/// <summary>追加一个空行段。</summary>
	/// <returns>返回当前内容段。</returns>
	public CommandOutletContent AppendLine()
	{
		//将本节点文本内容追加一个换行符
		_text += Environment.NewLine;

		//返回当前内容段
		return this;
	}

	/// <summary>追加一个指定值的内容段。</summary>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent AppendLine<T>(T value) => value is null ?
		this.AppendLine() :
		this.Append($"{value}{Environment.NewLine}");

	/// <summary>追加一个指定颜色和值的内容段。</summary>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent AppendLine<T>(CommandOutletColor foregroundColor, T value) => value == null ?
		this.AppendLine() :
		this.Append(foregroundColor, $"{value}{Environment.NewLine}");

	/// <summary>追加一个指定颜色和值的内容段。</summary>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="backgroundColor">指定的内容背景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent AppendLine<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => value == null ?
		this.AppendLine() :
		this.Append(foregroundColor, backgroundColor, $"{value}{Environment.NewLine}");

	/// <summary>追加一个指定样式和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent AppendLine<T>(CommandOutletStyles style, T value) => value == null ?
		this.AppendLine() :
		this.Append(style, $"{value}{Environment.NewLine}");

	/// <summary>追加一个指定样式、颜色和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent AppendLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value) => value == null ?
		this.AppendLine() :
		this.Append(style, foregroundColor, $"{value}{Environment.NewLine}");

	/// <summary>追加一个指定样式、颜色和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="backgroundColor">指定的内容背景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent AppendLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => value == null ?
		this.AppendLine() :
		this.Append(style, foregroundColor, backgroundColor, $"{value}{Environment.NewLine}");

	/// <summary>新增一个指定值的内容段。</summary>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回新增的首部内容段。</returns>
	public CommandOutletContent Prepend<T>(T value) => this.BeforeCore($"{value}");

	/// <summary>新增一个指定颜色和值的内容段。</summary>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回新增的首部内容段。</returns>
	public CommandOutletContent Prepend<T>(CommandOutletColor foregroundColor, T value) => this.BeforeCore($"{value}", foregroundColor);

	/// <summary>新增一个指定颜色和值的内容段。</summary>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="backgroundColor">指定的内容背景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回新增的首部内容段。</returns>
	public CommandOutletContent Prepend<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => this.BeforeCore($"{value}", foregroundColor, backgroundColor);

	/// <summary>新增一个指定样式和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回新增的首部内容段。</returns>
	public CommandOutletContent Prepend<T>(CommandOutletStyles style, T value) => this.BeforeCore($"{value}", style);

	/// <summary>新增一个指定样式、颜色和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回新增的首部内容段。</returns>
	public CommandOutletContent Prepend<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value) => this.BeforeCore($"{value}", style, foregroundColor);

	/// <summary>新增一个指定样式、颜色和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="backgroundColor">指定的内容背景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回新增的首部内容段。</returns>
	public CommandOutletContent Prepend<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => this.BeforeCore($"{value}", style, foregroundColor, backgroundColor);

	/// <summary>将指定内容段置为当前内容链的首部。</summary>
	/// <param name="content">指定的内容段。</param>
	/// <returns>返回当前内容链的新首部。</returns>
	public CommandOutletContent Prepend(CommandOutletContent content) => this.BeforeCore(content);

	/// <summary>前插一个空行段。</summary>
	/// <returns>返回当前内容段。</returns>
	public CommandOutletContent PrependLine()
	{
		//将本节点文本内容前插一个换行符
		_text = Environment.NewLine + _text;

		//返回当前内容段
		return this;
	}

	/// <summary>前插一个指定值的内容段。</summary>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回前插后的内容段。</returns>
	public CommandOutletContent PrependLine<T>(T value) => value == null ?
		this.PrependLine() :
		this.Prepend($"{value}{Environment.NewLine}");

	/// <summary>前插一个指定颜色和值的内容段。</summary>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回前插后的内容段。</returns>
	public CommandOutletContent PrependLine<T>(CommandOutletColor foregroundColor, T value) => value == null ?
		this.PrependLine() :
		this.Prepend(foregroundColor, $"{value}{Environment.NewLine}");

	/// <summary>前插一个指定颜色和值的内容段。</summary>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="backgroundColor">指定的内容背景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回前插后的内容段。</returns>
	public CommandOutletContent PrependLine<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => value == null ?
		this.PrependLine() :
		this.Prepend(foregroundColor, backgroundColor, $"{value}{Environment.NewLine}");

	/// <summary>前插一个指定样式和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回前插后的内容段。</returns>
	public CommandOutletContent PrependLine<T>(CommandOutletStyles style, T value) => value == null ?
		this.PrependLine() :
		this.Prepend(style, $"{value}{Environment.NewLine}");

	/// <summary>前插一个指定样式、颜色和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回前插后的内容段。</returns>
	public CommandOutletContent PrependLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value) => value == null ?
		this.PrependLine() :
		this.Prepend(style, foregroundColor, $"{value}{Environment.NewLine}");

	/// <summary>前插一个指定样式、颜色和值的内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="backgroundColor">指定的内容背景色。</param>
	/// <param name="value">指定的内容值。</param>
	/// <returns>返回前插后的内容段。</returns>
	public CommandOutletContent PrependLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => value == null ?
		this.PrependLine() :
		this.Prepend(style, foregroundColor, backgroundColor, $"{value}{Environment.NewLine}");
	#endregion

	#region 重写方法
	public override string ToString() => _text ?? string.Empty;
	#endregion

	#region 私有方法
	private CommandOutletContent AfterCore(CommandOutletContent content)
	{
		if(content == null || string.IsNullOrEmpty(content.Text))
			return this;

		if(_next == null)
		{
			content._previous = this;
			return _next = content;
		}
		else
		{
			content._previous = this;
			content._next = _next;
			return _next = _next._previous = content;
		}
	}

	private CommandOutletContent AfterCore(string text, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null) => this.AfterCore(text, CommandOutletStyles.None, foregroundColor, backgroundColor);
	private CommandOutletContent AfterCore(string text, CommandOutletStyles style, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null)
	{
		if(string.IsNullOrEmpty(text))
			return this;

		if(_next == null)
			return _next = new CommandOutletContent(this, text, style, foregroundColor, backgroundColor);
		else
			return _next = _next._previous = new CommandOutletContent(this, text, _next, style, foregroundColor, backgroundColor);
	}

	private CommandOutletContent BeforeCore(CommandOutletContent content)
	{
		if(content == null || string.IsNullOrEmpty(content.Text))
			return this;

		if(_previous == null)
		{
			content._next = this;
			return _previous = content;
		}
		else
		{
			content._next = this;
			content._previous = _previous;
			return _previous._next = content;
		}
	}

	private CommandOutletContent BeforeCore(string text, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null) => this.BeforeCore(text, CommandOutletStyles.None, foregroundColor, backgroundColor);
	private CommandOutletContent BeforeCore(string text, CommandOutletStyles style, CommandOutletColor? foregroundColor = null, CommandOutletColor? backgroundColor = null)
	{
		if(string.IsNullOrEmpty(text))
			return this;

		if(_previous == null)
			return _previous = new CommandOutletContent(null, text, this, style, foregroundColor, backgroundColor);
		else
			return _previous._next = new CommandOutletContent(_previous, text, this, style, foregroundColor, backgroundColor);
	}
	#endregion

	#region 静态方法
	/// <summary>创建一个指定文本的新内容段。</summary>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回新创建的内容段。</returns>
	public static CommandOutletContent Create(string text = null) => new(null, text);

	/// <summary>创建一个指定颜色和文本的新内容段。</summary>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回新创建的内容段。</returns>
	public static CommandOutletContent Create(CommandOutletColor foregroundColor, string text) => new(null, text, foregroundColor);

	/// <summary>创建一个指定颜色和文本的新内容段。</summary>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="backgroundColor">指定的内容背景色。</param>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回新创建的内容段。</returns>
	public static CommandOutletContent Create(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, string text) => new(null, text, foregroundColor, backgroundColor);

	/// <summary>创建一个指定样式和文本的新内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回新创建的内容段。</returns>
	public static CommandOutletContent Create(CommandOutletStyles style, string text) => new(null, text, style);

	/// <summary>创建一个指定样式、颜色和文本的新内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回新创建的内容段。</returns>
	public static CommandOutletContent Create(CommandOutletStyles style, CommandOutletColor foregroundColor, string text) => new(null, text, style, foregroundColor);

	/// <summary>创建一个指定样式、颜色和文本的新内容段。</summary>
	/// <param name="style">指定的内容样式。</param>
	/// <param name="foregroundColor">指定的内容前景色。</param>
	/// <param name="backgroundColor">指定的内容背景色。</param>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回新创建的内容段。</returns>
	public static CommandOutletContent Create(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, string text) => new(null, text, style, foregroundColor, backgroundColor);

	/// <summary>获取指定输出内容的全文。</summary>
	/// <param name="content">指定的输出内容。</param>
	/// <returns>返回输出内容的全文。</returns>
	public static string GetFullText(CommandOutletContent content)
	{
		if(content == null)
			return null;

		if(content.Previous == null && content.Next == null)
			return content.Text;

		var current = content.First;
		var fulltext = new StringBuilder();

		while(current != null)
		{
			fulltext.Append(current.Text);
			current = current.Next;
		}

		return fulltext.ToString();
	}
	#endregion
}
