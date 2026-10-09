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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Globalization;

namespace Zongsoft.Expressions;

/// <summary>提供当前插值的格式化上下文。</summary>
public class VariableFormattingContext : EventArgs
{
	#region 构造函数
	internal VariableFormattingContext(VariableEvaluationContext context, object value, string format, CultureInfo culture)
	{
		this.Template = context.Template;
		this.Expression = context.Expression;
		this.Namespace = context.Namespace;
		this.Name = context.Name;
		this.Position = context.Position;
		this.Length = context.Length;
		this.Depth = context.Depth;
		this.Value = value;
		this.Format = format;
		this.Culture = culture;
	}

	#endregion

	#region 公共属性
	public string Template { get; }
	public string Expression { get; }
	public string Namespace { get; }
	public string Name { get; }
	public int Position { get; }
	public int Length { get; }
	public int Depth { get; }
	public object Value { get; set; }
	public string Format { get; set; }
	public CultureInfo Culture { get; set; }
	public string Text { get; set; }
	public bool Handled { get; set; }
	#endregion
}
