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
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Externals.Wechat.Paying;

public abstract class FallbackHandlerBase<TRequest> : IFallbackHandler
{
	#region 构造函数
	protected FallbackHandlerBase(IServiceProvider serviceProvider) => this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	#endregion

	#region 属性定义
	protected IServiceProvider ServiceProvider { get; }
	public abstract FallbackKind Kind { get; }
	#endregion

	#region 公共方法
	//仅供已取得可信业务对象的本地调用；外部通知必须经 ReceiveAsync 验证。
	public ValueTask HandleAsync(TRequest request, CancellationToken cancellation = default) => this.OnHandleAsync(request, null, cancellation);
	public ValueTask HandleAsync(TRequest request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default) =>
		this.OnHandleAsync(request, parameters == null ? null : new Dictionary<string, object>(parameters), cancellation);

	public async ValueTask<FallbackReceipt> ReceiveAsync(FallbackRequest request, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(request);
		if(string.IsNullOrEmpty(request.Merchant) || request.Merchant.Length > 32 || !request.Merchant.All(char.IsAsciiDigit))
			throw new System.Security.Cryptography.CryptographicException();

		var authority = await this.GetAuthorityAsync(request.Merchant, cancellation);
		var notification = await FallbackNotification.Parser.ParseAsync(this.Kind, request, authority, cancellation);
		var payload = (TRequest)notification.Payload.Deserialize(this.GetRequestType(notification.Format), Json.Options);

		if(payload == null)
			throw new JsonException();
		return await this.OnReceiveAsync(payload, notification, cancellation);
	}
	#endregion

	#region 抽象方法
	internal protected abstract Type GetRequestType(string format);
	protected abstract ValueTask OnHandleAsync(TRequest request, Parameters parameters, CancellationToken cancellation);
	#endregion

	#region 虚拟方法
	protected virtual async ValueTask<FallbackReceipt> OnReceiveAsync(TRequest request, FallbackNotification notification, CancellationToken cancellation)
	{
		await this.OnHandleAsync(request, new Parameters { [typeof(FallbackNotification)] = notification }, cancellation);
		return FallbackReceipt.Completed;
	}

	protected virtual ValueTask<IAuthority> GetAuthorityAsync(string code, CancellationToken cancellation) =>
		FallbackNotification.Parser.GetAuthorityAsync(this.ServiceProvider, code, cancellation);
	#endregion
}

public abstract class FallbackHandlerBase<TRequest, TResult> : IFallbackHandler
{
	#region 构造函数
	protected FallbackHandlerBase(IServiceProvider serviceProvider) => this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	#endregion

	#region 属性定义
	protected IServiceProvider ServiceProvider { get; }
	public abstract FallbackKind Kind { get; }
	#endregion

	#region 公共方法
	//仅供已取得可信业务对象的本地调用；外部通知必须经 ReceiveAsync 验证。
	public ValueTask<TResult> HandleAsync(TRequest request, CancellationToken cancellation = default) => this.OnHandleAsync(request, null, cancellation);
	public ValueTask<TResult> HandleAsync(TRequest request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default) =>
		this.OnHandleAsync(request, parameters == null ? null : new Dictionary<string, object>(parameters), cancellation);

	public async ValueTask<FallbackReceipt> ReceiveAsync(FallbackRequest request, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(request);
		if(string.IsNullOrEmpty(request.Merchant) || request.Merchant.Length > 32 || !request.Merchant.All(char.IsAsciiDigit))
			throw new System.Security.Cryptography.CryptographicException();

		var authority = await this.GetAuthorityAsync(request.Merchant, cancellation);
		var notification = await FallbackNotification.Parser.ParseAsync(this.Kind, request, authority, cancellation);
		var payload = (TRequest)notification.Payload.Deserialize(this.GetRequestType(notification.Format), Json.Options);

		if(payload == null)
			throw new JsonException();
		return await this.OnReceiveAsync(payload, notification, cancellation);
	}
	#endregion

	#region 抽象方法
	internal protected abstract Type GetRequestType(string format);
	protected abstract ValueTask<TResult> OnHandleAsync(TRequest request, Parameters parameters, CancellationToken cancellation);
	#endregion

	#region 虚拟方法
	protected virtual async ValueTask<FallbackReceipt> OnReceiveAsync(TRequest request, FallbackNotification notification, CancellationToken cancellation)
	{
		await this.OnHandleAsync(request, new Parameters { [typeof(FallbackNotification)] = notification }, cancellation);
		return FallbackReceipt.Completed;
	}

	protected virtual ValueTask<IAuthority> GetAuthorityAsync(string code, CancellationToken cancellation) =>
		FallbackNotification.Parser.GetAuthorityAsync(this.ServiceProvider, code, cancellation);
	#endregion
}
