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

namespace Zongsoft.Expressions;

/// <summary>提供当前完整变量引用的取值上下文。</summary>
public class VariableEvaluationContext : EventArgs
{
	#region 构造函数
	internal VariableEvaluationContext(string template, string expression, string @namespace, string name, int position, int length, int depth, bool isIndex)
	{
		this.Template = template;
		this.Expression = expression;
		this.Namespace = @namespace;
		this.Name = name;
		this.Position = position;
		this.Length = length;
		this.Depth = depth;
		this.IsIndex = isIndex;
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
	public bool IsIndex { get; }
	public object Value { get; set; }
	public bool Handled { get; set; }
	#endregion
}
