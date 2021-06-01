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
using System.Collections.Generic;

namespace Zongsoft.Security
{
	/// <summary>
	/// 提供身份校验功能的接口。
	/// </summary>
	public interface IIdentityVerifier
	{
		/// <summary>获取身份校验器名称。</summary>
		string Name { get; }

		/// <summary>
		/// 校验身份。
		/// </summary>
		/// <param name="token">指定的校验票据。</param>
		/// <param name="data">指定的校验数据。</param>
		/// <param name="ticket">输出参数，表示验证成功后的票据对象（即校验数据的类型对象）。</param>
		/// <returns>返回的校验结果。</returns>
		Common.OperationResult Verify(string token, object data, out object ticket);
	}

	/// <summary>
	/// 提供身份校验功能的接口。
	/// </summary>
	/// <typeparam name="T">校验数据的类型。</typeparam>
	public interface IIdentityVerifier<in T> : IIdentityVerifier
	{
		/// <summary>
		/// 校验身份。
		/// </summary>
		/// <param name="token">指定的校验票据。</param>
		/// <param name="data">指定的校验数据。</param>
		/// <returns>返回的校验结果。</returns>
		Common.OperationResult Verify(string token, T data);
	}
}
