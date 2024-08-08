/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

namespace Zongsoft.Expressions
{
	/// <summary>
	/// 表示词素提取的结果结构。
	/// </summary>
	public readonly struct TokenResult
	{
		#region 公共字段
		/// <summary>获取当前词素提取结果后需要移动读取器指针的偏移量。</summary>
		public readonly int Offset;

		/// <summary>获取当前词素提取结果对应的词素对象，如果为空(null)则表示当前位置对应的字面量不是对应提取器支持的词素。</summary>
		public readonly Token Token;
		#endregion

		#region 构造函数
		public TokenResult(int offset, Token token)
		{
			this.Offset = offset;
			this.Token = token;
		}
		#endregion

		#region 重写方法
		public override string ToString() => $"[{this.Offset}] {this.Token}";
		#endregion

		#region 静态方法
		/// <summary>创建一个失败的提取结果。</summary>
		/// <param name="offset">指定的偏移量。</param>
		/// <returns>返回的失败提取结果。</returns>
		public static TokenResult Fail(int offset) => new TokenResult(offset, null);
		#endregion
	}
}
