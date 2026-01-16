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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Diagnostics;

partial class LoggingBase<TLog>
{
	public void Warn(Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, exception, data, null, action));
	public void Warn(string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, message, data, null, action));
	public void Warn(string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, message, exception, data, null, action));

	public void Warn<TSource>(Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, exception, data, this.GetSource(typeof(TSource)), action));
	public void Warn<TSource>(string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, message, data, this.GetSource(typeof(TSource)), action));
	public void Warn<TSource>(string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, message, exception, data, this.GetSource(typeof(TSource)), action));

	public void Warn(Type source, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, exception, data, this.GetSource(source), action));
	public void Warn(Type source, string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, message, data, this.GetSource(source), action));
	public void Warn(Type source, string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, message, exception, data, this.GetSource(source), action));

	public void Warn(object source, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, exception, data, this.GetSource(source?.GetType()), action));
	public void Warn(object source, string message, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, message, data, this.GetSource(source?.GetType()), action));
	public void Warn(object source, string message, Exception exception, object data = null, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.Log(this.CreateLog(LogLevel.Warn, message, exception, data, this.GetSource(source?.GetType()), action));

	public ValueTask WarnAsync(Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, exception, null, null, action), cancellation);
	public ValueTask WarnAsync(Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, exception, data, null, action), cancellation);
	public ValueTask WarnAsync(string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, null, null, action), cancellation);
	public ValueTask WarnAsync(string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, data, null, action), cancellation);
	public ValueTask WarnAsync(string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, exception, null, null, action), cancellation);
	public ValueTask WarnAsync(string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, exception, data, null, action), cancellation);

	public ValueTask WarnAsync<TSource>(Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, exception, null, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask WarnAsync<TSource>(Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, exception, data, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask WarnAsync<TSource>(string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, null, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask WarnAsync<TSource>(string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, data, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask WarnAsync<TSource>(string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, exception, null, this.GetSource(typeof(TSource)), action), cancellation);
	public ValueTask WarnAsync<TSource>(string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, exception, data, this.GetSource(typeof(TSource)), action), cancellation);

	public ValueTask WarnAsync(Type source, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, exception, null, this.GetSource(source), action), cancellation);
	public ValueTask WarnAsync(Type source, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, exception, data, this.GetSource(source), action), cancellation);
	public ValueTask WarnAsync(Type source, string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, null, this.GetSource(source), action), cancellation);
	public ValueTask WarnAsync(Type source, string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, data, this.GetSource(source), action), cancellation);
	public ValueTask WarnAsync(Type source, string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, exception, null, this.GetSource(source), action), cancellation);
	public ValueTask WarnAsync(Type source, string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, exception, data, this.GetSource(source), action), cancellation);

	public ValueTask WarnAsync(object source, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, exception, null, this.GetSource(source?.GetType()), action), cancellation);
	public ValueTask WarnAsync(object source, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, exception, data, this.GetSource(source?.GetType()), action), cancellation);
	public ValueTask WarnAsync(object source, string message, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, null, this.GetSource(source?.GetType()), action), cancellation);
	public ValueTask WarnAsync(object source, string message, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, data, this.GetSource(source?.GetType()), action), cancellation);
	public ValueTask WarnAsync(object source, string message, Exception exception, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, exception, null, this.GetSource(source?.GetType()), action), cancellation);
	public ValueTask WarnAsync(object source, string message, Exception exception, object data, CancellationToken cancellation = default, [System.Runtime.CompilerServices.CallerMemberName] string action = null) => Logging.LogAsync(this.CreateLog(LogLevel.Warn, message, exception, data, this.GetSource(source?.GetType()), action), cancellation);
}
