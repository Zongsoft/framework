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

namespace Zongsoft.Text.Templating;

public partial class TemplateEvaluator
{
	/// <summary>提供当前完整变量引用的取值上下文。</summary>
	public class ResolutionContext : EventArgs
	{
		#region 构造函数
		internal ResolutionContext(string template, string expression, string @namespace, string name, int position, int length, int depth, bool isIndex)
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
}
