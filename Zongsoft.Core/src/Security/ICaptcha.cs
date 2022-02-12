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
		/// <param name="data">指定的签发数据。</param>
		/// <param name="extra">指定的附加信息。</param>
		/// <returns>返回的签发结果，如果为空(null)则表示发起失败。</returns>
		object Issue(object data, string extra = null);

		/// <summary>验证人机识别。</summary>
		/// <param name="data">指定识别的数据。</param>
		/// <param name="extra">输出参数，签发的附加信息。</param>
		/// <returns>返回一个值，指示是否验证成功。</returns>
		bool Verify(object data, out string extra);
	}
}
