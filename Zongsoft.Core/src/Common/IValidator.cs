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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Common
{
	/// <summary>
	/// 表示提供有效性验证功能的接口。
	/// </summary>
	/// <typeparam name="T">指定</typeparam>
	public interface IValidator<in T>
	{
		bool Validate(T data, Action<string> failure = null)
		{
			return this.Validate(data, null, failure);
		}

		Task<bool> ValidateAsync(T data, Action<string> failure = null, CancellationToken cancellation = default)
		{
			return this.ValidateAsync(data, null, failure, cancellation);
		}

		/// <summary>
		/// 验证指定的数据是否有效。
		/// </summary>
		/// <param name="data">指定的待验证的数据。</param>
		/// <param name="parameter">指定的自定义参数对象。</param>
		/// <param name="failure">当内部验证失败的回调处理函数，传入的字符串参数表示验证失败的消息文本。</param>
		/// <returns>如果验证通过则返回真(True)，否则返回假(False)。</returns>
		bool Validate(T data, object parameter, Action<string> failure = null);

		/// <summary>
		/// 验证指定的数据是否有效。
		/// </summary>
		/// <param name="data">指定的待验证的数据。</param>
		/// <param name="parameter">指定的自定义参数对象。</param>
		/// <param name="failure">当内部验证失败的回调处理函数，传入的字符串参数表示验证失败的消息文本。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>如果验证通过则返回真(True)，否则返回假(False)。</returns>
		Task<bool> ValidateAsync(T data, object parameter, Action<string> failure = null, CancellationToken cancellation = default);
	}
}
