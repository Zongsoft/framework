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
	private CommandOutletColor? _color;
	private CommandOutletContent _next;
	private CommandOutletContent _previous;
	#endregion

	#region 私有构造
	private CommandOutletContent(CommandOutletContent previous, string text, CommandOutletColor? color = null) : this(previous, text, CommandOutletStyles.None, color) { }
	private CommandOutletContent(CommandOutletContent previous, string text, CommandOutletStyles style, CommandOutletColor? color = null)
	{
		_text = text;
		_style = style;
		_color = color;
		_previous = previous;
		_next = null;
	}

	private CommandOutletContent(CommandOutletContent previous, string text, CommandOutletContent next, CommandOutletColor? color = null) : this(previous, text, next, CommandOutletStyles.None, color) { }
	private CommandOutletContent(CommandOutletContent previous, string text, CommandOutletContent next, CommandOutletStyles style, CommandOutletColor? color = null)
	{
		_text = text;
		_style = style;
		_color = color;
		_previous = previous;
		_next = next;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置内容段文本。</summary>
	public string Text { get => _text; set => _text = value; }

	/// <summary>获取或设置内容段的样式。</summary>
	public CommandOutletStyles Style { get => _style; set => _style = value; }

	/// <summary>获取或设置内容段的文本颜色。</summary>
	public CommandOutletColor? Color { get => _color; set => _color = value; }

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

	/// <summary>追加一个空行段。</summary>
	/// <returns>返回当前内容段。</returns>
	public CommandOutletContent AppendLine()
	{
		//将本节点文本内容追加一个换行符
		_text += Environment.NewLine;

		//返回当前内容段
		return this;
	}

	/// <summary>追加一个指定文本的内容段。</summary>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent AppendLine(string text) => string.IsNullOrEmpty(text) ?
		this.AppendLine() :
		this.Append(text + Environment.NewLine);

	/// <summary>追加一个指定值的内容段。</summary>
	/// <param name="value">指定的内容对象。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent AppendLine(object value) => value == null ?
		this.AppendLine() :
		this.Append($"{value}{Environment.NewLine}");

	/// <summary>追加一个指定颜色和文本的内容段。</summary>
	/// <param name="color">指定的内容文本颜色。</param>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent AppendLine(CommandOutletColor color, string text) => string.IsNullOrEmpty(text) ?
		this.AppendLine() :
		this.Append(color, text + Environment.NewLine);

	/// <summary>追加一个指定颜色和值的内容段。</summary>
	/// <param name="color">指定的内容文本颜色。</param>
	/// <param name="value">指定的内容对象。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent AppendLine(CommandOutletColor color, object value) => value == null ?
		this.AppendLine() :
		this.Append(color, $"{value}{Environment.NewLine}");

	/// <summary>追加一个指定文本的内容段。</summary>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent Append(string text) => this.AfterCore(null, text);

	/// <summary>追加一个指定值的内容段。</summary>
	/// <param name="value">指定的内容对象。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent Append(object value) => this.AfterCore(null, value?.ToString());

	/// <summary>追加一个指定颜色和文本的内容段。</summary>
	/// <param name="color">指定的内容文本颜色。</param>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent Append(CommandOutletColor color, string text) => this.AfterCore(color, text);

	/// <summary>追加一个指定颜色和值的内容段。</summary>
	/// <param name="color">指定的内容文本颜色。</param>
	/// <param name="value">指定的内容对象。</param>
	/// <returns>返回追加后的内容段。</returns>
	public CommandOutletContent Append(CommandOutletColor color, object value) => this.AfterCore(color, value?.ToString());

	/// <summary>追加一个指定的内容段。</summary>
	/// <param name="content">指定的内容段。</param>
	/// <returns>返回当前内容段。</returns>
	public CommandOutletContent Append(CommandOutletContent content) => this.AfterCore(content);

	/// <summary>前插一个空行段。</summary>
	/// <returns>返回当前内容段。</returns>
	public CommandOutletContent PrependLine()
	{
		//将本节点文本内容前插一个换行符
		_text = Environment.NewLine + _text;

		//返回当前内容段
		return this;
	}

	/// <summary>前插一个指定文本的内容段。</summary>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回前插后的内容段。</returns>
	public CommandOutletContent PrependLine(string text) => string.IsNullOrEmpty(text) ?
		this.PrependLine() :
		this.Prepend(text + Environment.NewLine);

	/// <summary>前插一个指定值的内容段。</summary>
	/// <param name="value">指定的内容对象。</param>
	/// <returns>返回前插后的内容段。</returns>
	public CommandOutletContent PrependLine(object value) => value == null ?
		this.PrependLine() :
		this.Prepend($"{value}{Environment.NewLine}");

	/// <summary>前插一个指定颜色和文本的内容段。</summary>
	/// <param name="color">指定的内容文本颜色。</param>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回前插后的内容段。</returns>
	public CommandOutletContent PrependLine(CommandOutletColor color, string text) => string.IsNullOrEmpty(text) ?
		this.PrependLine() :
		this.Prepend(color, text + Environment.NewLine);

	/// <summary>前插一个指定颜色和值的内容段。</summary>
	/// <param name="color">指定的内容文本颜色。</param>
	/// <param name="value">指定的内容对象。</param>
	/// <returns>返回前插后的内容段。</returns>
	public CommandOutletContent PrependLine(CommandOutletColor color, object value) => value == null ?
		this.PrependLine() :
		this.Prepend(color, $"{value}{Environment.NewLine}");

	/// <summary>新增一个指定文本的内容段。</summary>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回新增的首部内容段。</returns>
	public CommandOutletContent Prepend(string text) => this.BeforeCore(null, text);

	/// <summary>新增一个指定值的内容段。</summary>
	/// <param name="value">指定的内容对象。</param>
	/// <returns>返回新增的首部内容段。</returns>
	public CommandOutletContent Prepend(object value) => this.BeforeCore(null, value?.ToString());

	/// <summary>新增一个指定颜色和文本的内容段。</summary>
	/// <param name="color">指定的内容文本颜色。</param>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回新增的首部内容段。</returns>
	public CommandOutletContent Prepend(CommandOutletColor color, string text) => this.BeforeCore(color, text);

	/// <summary>新增一个指定颜色和值的内容段。</summary>
	/// <param name="color">指定的内容文本颜色。</param>
	/// <param name="value">指定的内容对象。</param>
	/// <returns>返回新增的首部内容段。</returns>
	public CommandOutletContent Prepend(CommandOutletColor color, object value) => this.BeforeCore(color, value?.ToString());

	/// <summary>将指定内容段置为当前内容链的首部。</summary>
	/// <param name="content">指定的内容段。</param>
	/// <returns>返回当前内容链的新首部。</returns>
	public CommandOutletContent Prepend(CommandOutletContent content) => this.BeforeCore(content);
	#endregion

	#region 重写方法
	public override string ToString() => _color.HasValue ? $"[{_color}] {_text}" : _text ?? string.Empty;
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

	private CommandOutletContent AfterCore(CommandOutletColor? color, string text)
	{
		if(string.IsNullOrEmpty(text))
			return this;

		if(_next == null)
			return _next = new CommandOutletContent(this, text, color);
		else
			return _next = _next._previous = new CommandOutletContent(this, text, _next, color);
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

	private CommandOutletContent BeforeCore(CommandOutletColor? color, string text)
	{
		if(string.IsNullOrEmpty(text))
			return this;

		if(_previous == null)
			return _previous = new CommandOutletContent(null, text, this, color);
		else
			return _previous._next = new CommandOutletContent(_previous, text, this, color);
	}
	#endregion

	#region 静态方法
	/// <summary>创建一个指定文本的新内容段。</summary>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回新创建的内容段。</returns>
	public static CommandOutletContent Create(string text = null) => new(null, text);

	/// <summary>创建一个指定颜色和文本的新内容段。</summary>
	/// <param name="color">指定的内容文本颜色。</param>
	/// <param name="text">指定的内容文本。</param>
	/// <returns>返回新创建的内容段。</returns>
	public static CommandOutletContent Create(CommandOutletColor color, string text) => new(null, text, color);

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
