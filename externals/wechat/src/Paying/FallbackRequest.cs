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
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
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
