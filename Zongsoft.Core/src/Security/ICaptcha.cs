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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Security;

/// <summary>表示人机识别程序(Completely Automated Public Turing test to tell Computers and Humans Apart)的接口。</summary>
/// <remarks>
///		<para>人机识别 RESTful API 定义：</para>
///		<list type="bullet">
///			<item>
///				<term>[POST] /security/captcha/{scheme}</term>
///				<description>发起人机识别，返回特定 scheme 的响应内容。</description>
///			</item>
///			<item>
///				<term>[POST] /security/captcha/{scheme}/verify</term>
///				<description>人机识别验证，请求的内容为识别的数据，如果验证成功则返回成功令牌，否则返回空。</description>
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
	/// <returns>返回的签发结果，如果为空(<c>null</c>)则表示发起失败。</returns>
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
