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
using System.Globalization;

namespace Zongsoft.Text.Templating;

public partial class TemplateEvaluator
{
	/// <summary>提供当前插值的格式化上下文。</summary>
	public class FormattingContext : EventArgs
	{
		#region 构造函数
		internal FormattingContext(ResolutionContext context, object value, string format, CultureInfo culture)
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
}
