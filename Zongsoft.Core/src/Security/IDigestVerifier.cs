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

namespace Zongsoft.Security
{
	/// <summary>
	/// 提供摘要校验功能的接口。
	/// </summary>
	public interface IDigestVerifier
	{
		/// <summary>获取校验器名称。</summary>
		string Name { get; }

		/// <summary>
		/// 校验指定的数据摘要。
		/// </summary>
		/// <param name="identifier">数据标识符。</param>
		/// <param name="digest">待验证的数据摘要。</param>
		/// <param name="data">待验证的数据内容。</param>
		/// <returns>如果校验成功则返回真(True)，否则返回假(False)。</returns>
		bool Verify(string identifier, ReadOnlySpan<byte> digest, ReadOnlySpan<byte> data);
	}
}
