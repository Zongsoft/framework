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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;

using Zongsoft.Web.Http;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Externals.Aliyun.Gateway;

public class FallbackExecutor : ExecutorBase<Stream, object>
{
	#region 单例字段
	public static readonly FallbackExecutor Instance = new FallbackExecutor();
	#endregion

	#region 私有构造
	private FallbackExecutor() => this.Handlers = new Dictionary<string, IHandler>(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共属性
	public IDictionary<string, IHandler> Handlers { get; }
	#endregion

	#region 公共方法
	public ValueTask<object> ExecuteAsync(HttpRequest request, CancellationToken cancellation = default)
	{
		if(request == null)
			throw new ArgumentNullException(nameof(request));

		return this.ExecuteAsync(request.Body, request.GetParameters().ToDictionary(entry => entry.Key, entry => entry.Value), cancellation);
	}
	#endregion

	#region 重写方法
	protected override IExecutorContext<Stream, object> CreateContext(Stream request, Parameters parameters) => new ExecutorContext<Stream, object>(this, request, parameters);
	protected override IHandler GetHandler(IExecutorContext<Stream, object> context) => context.Parameters.TryGetValue("name", out var value) && value is string name && this.Handlers.TryGetValue(name, out var handler) ? handler : null;
	#endregion
}