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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Gateway;

[System.Reflection.DefaultMember(nameof(Handlers))]
public class FallbackExecutor
{
	public static readonly FallbackExecutor Instance = new();

	public IDictionary<string, IFallbackHandler> Handlers { get; } = new Dictionary<string, IFallbackHandler>(StringComparer.OrdinalIgnoreCase);

	public async ValueTask<FallbackReceipt> ExecuteAsync(FallbackKind kind, string merchant, HttpRequest request, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		if(!this.Handlers.TryGetValue(kind.ToString(), out var handler) || handler.Kind != kind)
			throw new InvalidOperationException();
		if(request.QueryString.HasValue || !request.HasJsonContentType() || request.ContentLength > 2 * 1024 * 1024)
			throw new System.Security.Cryptography.CryptographicException();

		var headers = request.Headers
			.Where(entry => entry.Key.StartsWith("Wechatpay-", StringComparison.OrdinalIgnoreCase))
			.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

		return await handler.ReceiveAsync(new FallbackRequest(merchant, request.Body, headers), cancellation);
	}
}
