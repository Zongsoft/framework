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
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
