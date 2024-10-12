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

namespace Zongsoft.Security
{
	/// <summary>
	/// 表示人机识别程序(Completely Automated Public Turing test to tell Computers and Humans Apart)的接口。
	/// </summary>
	/// <remarks>
	///		<para>人机识别 RESTful API 定义：</para>
	///		<list type="bullet">
	///			<item>
	///				<term>[POST] /security/captcha/{scheme}</term>
	///				<description>发起人机识别。</description>
	///			</item>
	///			<item>
	///				<term>[POST] /security/captcha/{scheme}/verify/{token}?extra={extra?}</term>
	///				<description>请求的内容为识别的数据信息文本。</description>
	///			</item>
	///			<item>
	///				<term>[GET] /security/captcha/{scheme}/{token}</term>
	///				<description>判断指定的识别会话是否存在。</description>
	///			</item>
	///		</list>
	/// </remarks>
	public interface ICaptcha
	{
		/// <summary>获取人机识别程序的标识。</summary>
		string Scheme { get; }

		/// <summary>发起人机识别。</summary>
		/// <param name="argument">指定的签发请求。</param>
		/// <param name="parameters">指定的附加参数集。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的签发结果，如果为空(null)则表示发起失败。</returns>
		ValueTask<object> IssueAsync(object argument, Zongsoft.Collections.Parameters parameters, CancellationToken cancellation = default);

		/// <summary>验证人机识别。</summary>
		/// <param name="argument">指定的验证请求。</param>
		/// <param name="parameters">指定的附加参数集。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回验证结果令牌，如果验证失败则返回空(<c>null</c>)。</returns>
		ValueTask<string> VerifyAsync(object argument, Zongsoft.Collections.Parameters parameters, CancellationToken cancellation = default);

		/// <summary>确认人机识别结果是否有效。</summary>
		/// <param name="token">指定的验证结果令牌。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回一个值，指示验证是否成功。</returns>
		ValueTask<bool> VerifyAsync(string token, CancellationToken cancellation = default);
	}
}
