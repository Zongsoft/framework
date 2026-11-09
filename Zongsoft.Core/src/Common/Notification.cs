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

using Microsoft.Extensions.Primitives;

namespace Zongsoft.Common;

public static class Notification
{
	#region 单例字段
	public static readonly IChangeToken Notified = new ChangedToken();
	#endregion

	#region 公共方法
	public static IChangeToken GetToken(CancellationToken cancellation) => cancellation.CanBeCanceled ? new CancellationChangeToken(cancellation) : Notified;
	public static IChangeToken GetToken(CancellationTokenSource cancellation = null) => cancellation == null ? Notified : new CancellationChangeToken(cancellation.Token);
	#endregion

	#region 嵌套子类
	private sealed class ChangedToken : IChangeToken
	{
		public bool HasChanged => true;
		public bool ActiveChangeCallbacks => true;
		public IDisposable RegisterChangeCallback(Action<object> callback, object state) => Nothing.Instance;

		private sealed class Nothing : IDisposable
		{
			public static readonly Nothing Instance = new();
			public void Dispose() { }
		}
	}
	#endregion
}
