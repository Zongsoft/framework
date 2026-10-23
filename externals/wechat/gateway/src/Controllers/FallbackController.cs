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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Web;
using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Gateway.Controllers;

[AllowAnonymous]
[Area("Externals/Wechat")]
[ControllerName("Fallback")]
[RequestSizeLimit(2 * 1024 * 1024)]
public class FallbackController : ControllerBase
{
	[HttpPost("payment/{key}")]
	public Task<IActionResult> PaymentAsync(string key, CancellationToken cancellation = default) => this.ReceiveAsync(FallbackKind.Payment, key, cancellation);

	[HttpPost("refundment/{key}")]
	public Task<IActionResult> RefundmentAsync(string key, CancellationToken cancellation = default) => this.ReceiveAsync(FallbackKind.Refundment, key, cancellation);

	[HttpPost("offlinefaceagreement/{key}")]
	public Task<IActionResult> OfflineFaceAgreementAsync(string key, CancellationToken cancellation = default) => this.ReceiveAsync(FallbackKind.OfflineFaceAgreement, key, cancellation);

	private async Task<IActionResult> ReceiveAsync(FallbackKind kind, string key, CancellationToken cancellation)
	{
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		timeout.CancelAfter(TimeSpan.FromSeconds(4));

		try
		{
			var receipt = await FallbackExecutor.Instance.ExecuteAsync(kind, key, this.Request, timeout.Token);
			return receipt is FallbackReceipt.Completed or FallbackReceipt.Accepted or FallbackReceipt.Duplicate ? this.NoContent() : Fail(StatusCodes.Status503ServiceUnavailable);
		}
		catch(Exception exception) when(exception is CryptographicException or JsonException or FormatException or ArgumentException or System.Collections.Generic.KeyNotFoundException)
		{
			return Fail(StatusCodes.Status400BadRequest);
		}
		catch(Exception exception)
		{
			Zongsoft.Diagnostics.Logging.GetLogging<FallbackController>().Warn(exception.GetType().Name, exception.StackTrace);
			return Fail(StatusCodes.Status503ServiceUnavailable);
		}

		ObjectResult Fail(int status) => this.StatusCode(status, new { code = "FAIL", message = "Notification was not accepted." });
	}
}
