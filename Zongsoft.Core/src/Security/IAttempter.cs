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
	/// 表示身份验证失败的处理器。
	/// </summary>
	public interface IAttempter
	{
		/// <summary>
		/// 校验指定身份是否可以继续验证。
		/// </summary>
		/// <param name="identity">指定待验证的身份标识。</param>
		/// <param name="namespace">表示身份标识所属的命名空间。</param>
		/// <returns>如果校验成功则返回真(True)，否则返回假(False)。</returns>
		bool Verify(string identity, string @namespace = null);

		/// <summary>
		/// 验证成功方法。
		/// </summary>
		/// <param name="identity">指定验证成功的身份标识。</param>
		/// <param name="namespace">表示身份标识所属的命名空间。</param>
		void Done(string identity, string @namespace = null);

		/// <summary>
		/// 验证失败方法。
		/// </summary>
		/// <param name="identity">指定验证失败的身份标识。</param>
		/// <param name="namespace">表示身份标识所属的命名空间。</param>
		/// <returns>返回验证失败是否超过阈值，如果返回真(True)则表示失败次数超过阈值。</returns>
		bool Fail(string identity, string @namespace = null);
	}
}
