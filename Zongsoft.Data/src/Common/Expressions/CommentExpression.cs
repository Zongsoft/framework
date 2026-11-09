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

namespace Zongsoft.Data.Common.Expressions;

public class CommentExpression : Expression
{
	#region 构造函数
	public CommentExpression(string text)
	{
		this.Text = text;
	}
	#endregion

	#region 公共属性
	public string Text { get; set; }
	#endregion

	#region 重写方法
	public override string ToString() => string.IsNullOrEmpty(this.Text) ? string.Empty : $"/* {this.Text} */";
	#endregion
}
