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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Diagnostics.Protocols.Client library.
 *
 * The Zongsoft.Diagnostics.Protocols.Client is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Diagnostics.Protocols.Client is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Diagnostics.Protocols.Client library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Diagnostics.Protocols.Client;

/// <summary>为直接 gRPC 客户端调用添加声明的字面基路径。</summary>
/// <remarks>
///		<para>每条 HTTP 处理链只安装一次；不缓冲消息或改变传输设置。</para>
///		<para>释放本处理器时一并释放内部处理器，通道所有权由 <see cref="Grpc.Net.Client.GrpcChannelOptions.DisposeHttpClient"/> 决定。</para>
/// </remarks>
public sealed class GrpcClientHandler : DelegatingHandler
{
	#region 静态字段
	private static readonly HttpRequestOptionsKey<bool> _applied = new("Zongsoft.Diagnostics.Protocols.Client.GrpcClientHandler.Applied");
	#endregion

	#region 成员字段
	private readonly string _prefix;
	#endregion

	#region 构造函数
	/// <summary>创建基路径处理器。</summary>
	/// <param name="path">以斜杠开头的字面基路径，例如 /opentelemetry；根路径为 /，允许一个尾斜杠。</param>
	/// <param name="innerHandler">由本处理器持有的内部 HTTP 处理器。</param>
	/// <exception cref="ArgumentNullException">参数为空。</exception>
	/// <exception cref="ArgumentException">基路径无效。</exception>
	/// <exception cref="InvalidOperationException">内部处理链存在循环或已经包含本类型的处理器。</exception>
	public GrpcClientHandler(string path, HttpMessageHandler innerHandler) : base(innerHandler ?? throw new ArgumentNullException(nameof(innerHandler)))
	{
		ArgumentNullException.ThrowIfNull(path);

		if(!IsPath(path))
			throw new ArgumentException(Properties.Resources.GrpcPath_Invalid_Message, nameof(path));

		var handlers = new HashSet<HttpMessageHandler>(ReferenceEqualityComparer.Instance);

		for(var handler = innerHandler; handler != null; handler = (handler as DelegatingHandler)?.InnerHandler)
		{
			if(handler is GrpcClientHandler || !handlers.Add(handler))
				throw new InvalidOperationException(Properties.Resources.GrpcPath_Duplicate_Message);
		}

		this.Path = path;
		_prefix = path.EndsWith('/') ? path[..^1] : path;
	}
	#endregion

	#region 公共属性
	/// <summary>获取声明的基路径，保留原有大小写和尾斜杠。</summary>
	public string Path { get; }
	#endregion

	#region 重写方法
	/// <summary>同步发送添加基路径后的请求。</summary>
	/// <param name="request">包含原生 RPC 路径的 HTTP 请求。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>内部处理器返回的原始响应。</returns>
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		this.Prepare(request);
		return base.Send(request, cancellationToken);
	}

	/// <summary>异步发送添加基路径后的请求，不缓冲消息内容。</summary>
	/// <param name="request">包含原生 RPC 路径的 HTTP 请求。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>内部处理器返回的响应任务。</returns>
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		this.Prepare(request);
		return base.SendAsync(request, cancellationToken);
	}
	#endregion

	#region 私有方法
	private void Prepare(HttpRequestMessage request)
	{
		ArgumentNullException.ThrowIfNull(request);

		if(request.Options.TryGetValue(_applied, out var applied) && applied)
			throw new InvalidOperationException(Properties.Resources.GrpcPath_Duplicate_Message);

		var address = request.RequestUri;

		if(address == null || !address.IsAbsoluteUri || address.Scheme is not ("http" or "https") || address.Fragment.Length > 0)
			throw new ArgumentException(Properties.Resources.GrpcPath_RequestInvalid_Message, nameof(request));

		if(_prefix.Length > 0)
		{
			request.RequestUri = new Uri(address.GetLeftPart(UriPartial.Authority) + _prefix + address.PathAndQuery,
				new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true });
		}

		request.Options.Set(_applied, true);
	}

	private static bool IsPath(string path)
	{
		if(path.Length == 0 || path[0] != '/')
			return false;

		foreach(var character in path)
		{
			if(!char.IsAsciiLetterOrDigit(character) && character is not ('/' or '-' or '.' or '_' or '~'))
				return false;
		}

		if(path == "/")
			return true;

		var segments = path.EndsWith('/') ? path[1..^1] : path[1..];

		foreach(var segment in segments.Split('/'))
		{
			if(segment is "" or "." or "..")
				return false;
		}

		return true;
	}
	#endregion
}
