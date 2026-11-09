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

namespace Zongsoft.Security;

/// <summary>提供数字签名功能的接口。</summary>
public interface ISignaturer
{
	/// <summary>获取数字签名器名称。</summary>
	string Name { get; }

	/// <summary>计算指定数据的数字签名。</summary>
	/// <param name="data">待签名的数据。</param>
	/// <param name="algorithm">签名的算法名。</param>
	/// <returns>返回成功的签名内容，如果失败则返回空。</returns>
	byte[] Signature(ReadOnlySpan<byte> data, string algorithm = null);

	/// <summary>校验指定的数据签名。</summary>
	/// <param name="signature">待验证的签名。</param>
	/// <param name="data">待验证的数据内容。</param>
	/// <param name="algorithm">签名的算法名。</param>
	/// <returns>如果校验成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool Verify(ReadOnlySpan<byte> signature, ReadOnlySpan<byte> data, string algorithm = null);
}
