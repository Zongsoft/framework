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

namespace Zongsoft.Expressions;

/// <summary>表示词素提取的结果结构。</summary>
public readonly struct TokenResult
{
	#region 公共字段
	/// <summary>获取当前词素消耗的字符数，失败结果不消耗文本。</summary>
	public readonly int Length;

	/// <summary>获取当前词素提取结果对应的词素对象，如果为空(<c>null</c>)则表示当前位置对应的字面量不是对应提取器支持的词素。</summary>
	public readonly Token Token;
	#endregion

	#region 构造函数
	public TokenResult(int length, Token token)
	{
		this.Length = length;
		this.Token = token;
	}
	#endregion

	#region 重写方法
	public override string ToString() => $"[{this.Length}] {this.Token}";
	#endregion

	#region 静态方法
	/// <summary>创建一个失败的提取结果。</summary>
	/// <returns>返回的失败提取结果。</returns>
	public static TokenResult Fail() => default;
	#endregion
}
