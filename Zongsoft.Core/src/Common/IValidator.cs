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

namespace Zongsoft.Common;

/// <summary>表示提供有效性验证功能的接口。</summary>
/// <typeparam name="T">指定的验证数据类型。</typeparam>
public interface IValidator<in T>
{
	bool Validate(T data, Action<string> failure = null) => this.Validate(data, null, failure);
	Task<bool> ValidateAsync(T data, Action<string> failure = null, CancellationToken cancellation = default) => this.ValidateAsync(data, null, failure, cancellation);

	/// <summary>验证指定的数据是否有效。</summary>
	/// <param name="data">指定的待验证的数据。</param>
	/// <param name="argument">指定的自定义参数对象。</param>
	/// <param name="failure">当内部验证失败的回调处理函数，传入的字符串参数表示验证失败的消息文本。</param>
	/// <returns>如果验证通过则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool Validate(T data, object argument, Action<string> failure = null);

	/// <summary>验证指定的数据是否有效。</summary>
	/// <param name="data">指定的待验证的数据。</param>
	/// <param name="argument">指定的自定义参数对象。</param>
	/// <param name="failure">当内部验证失败的回调处理函数，传入的字符串参数表示验证失败的消息文本。</param>
	/// <param name="cancellation">监视取消请求的令牌。</param>
	/// <returns>如果验证通过则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	Task<bool> ValidateAsync(T data, object argument, Action<string> failure = null, CancellationToken cancellation = default);
}
