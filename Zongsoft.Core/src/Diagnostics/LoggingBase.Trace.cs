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
using System.Threading.Tasks;

namespace Zongsoft.Diagnostics;

partial class LoggingBase<TLog>
{
	#region 公共方法
	public void Trace(Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, exception, data, null, action));
	public void Trace(string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, message, data, null, action));
	public void Trace(string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, message, exception, data, null, action));

	public void Trace<TSource>(Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, exception, data, this.GetSource(typeof(TSource)), action));
	public void Trace<TSource>(string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, message, data, this.GetSource(typeof(TSource)), action));
	public void Trace<TSource>(string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, message, exception, data, this.GetSource(typeof(TSource)), action));

	public void Trace(Type source, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, exception, data, this.GetSource(source), action));
	public void Trace(Type source, string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, message, data, this.GetSource(source), action));
	public void Trace(Type source, string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, message, exception, data, this.GetSource(source), action));

	public void Trace(object source, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, exception, data, this.GetSource(source), action));
	public void Trace(object source, string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, message, data, this.GetSource(source), action));
	public void Trace(object source, string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Trace, message, exception, data, this.GetSource(source), action));

	public ValueTask TraceAsync(Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, exception, null, null, action), cancellation);
	public ValueTask TraceAsync(Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, exception, data, null, action), cancellation);
	public ValueTask TraceAsync(string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, null, null, action), cancellation);
	public ValueTask TraceAsync(string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, data, null, action), cancellation);
	public ValueTask TraceAsync(string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, exception, null, null, action), cancellation);
	public ValueTask TraceAsync(string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, exception, data, null, action), cancellation);

	public ValueTask TraceAsync<TSource>(Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, exception, null, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask TraceAsync<TSource>(Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, exception, data, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask TraceAsync<TSource>(string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, null, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask TraceAsync<TSource>(string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, data, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask TraceAsync<TSource>(string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, exception, null, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask TraceAsync<TSource>(string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, exception, data, this.GetSource(typeof(TSource)), action), cancellation);

	public ValueTask TraceAsync(Type source, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, exception, null, this.GetSource(source), action), cancellation);
	public ValueTask TraceAsync(Type source, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, exception, data, this.GetSource(source), action), cancellation);
	public ValueTask TraceAsync(Type source, string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, null, this.GetSource(source), action), cancellation);
	public ValueTask TraceAsync(Type source, string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, data, this.GetSource(source), action), cancellation);
	public ValueTask TraceAsync(Type source, string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, exception, null, this.GetSource(source), action), cancellation);
	public ValueTask TraceAsync(Type source, string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, exception, data, this.GetSource(source), action), cancellation);

	public ValueTask TraceAsync(object source, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, exception, null, this.GetSource(source), action), cancellation);
	public ValueTask TraceAsync(object source, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, exception, data, this.GetSource(source), action), cancellation);
	public ValueTask TraceAsync(object source, string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, null, this.GetSource(source), action), cancellation);
	public ValueTask TraceAsync(object source, string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, data, this.GetSource(source), action), cancellation);
	public ValueTask TraceAsync(object source, string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, exception, null, this.GetSource(source), action), cancellation);
	public ValueTask TraceAsync(object source, string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Trace, message, exception, data, this.GetSource(source), action), cancellation);
	#endregion
}
