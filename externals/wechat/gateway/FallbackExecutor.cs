/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;

using Zongsoft.Web.Http;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Externals.Wechat.Gateway
{
	public class FallbackExecutor : ExecutorBase<Stream, object>
	{
		#region 单例字段
		public static readonly FallbackExecutor Instance = new();
		#endregion

		#region 构造函数
		public FallbackExecutor() => this.Handlers = new Dictionary<string, IHandler>(StringComparer.OrdinalIgnoreCase);
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
}