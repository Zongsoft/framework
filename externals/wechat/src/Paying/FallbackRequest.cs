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
using System.IO;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat.Paying;

/// <summary>一次外部通知的原始输入；调用方拥有正文流。</summary>
public sealed class FallbackRequest
{
	public FallbackRequest(string merchant, Stream body, IReadOnlyDictionary<string, string[]> headers)
	{
		ArgumentNullException.ThrowIfNull(headers);

		this.Merchant = merchant;
		this.Body = body ?? throw new ArgumentNullException(nameof(body));
		var dictionary = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

		foreach(var entry in headers)
			dictionary.Add(entry.Key, entry.Value == null ? [] : (string[])entry.Value.Clone());

		this.Headers = dictionary;
	}

	public string Merchant { get; }
	public Stream Body { get; }
	internal IReadOnlyDictionary<string, string[]> Headers { get; }
}
