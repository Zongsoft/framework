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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Zongsoft.Plugins;

//一次卸载共享释放记录；构件引用由各自的所有者释放，集合遍历不接管这些引用。
internal sealed class PluginDisposal
{
	private readonly HashSet<object> _released = new(ReferenceEqualityComparer.Instance);
	private readonly HashSet<object> _builtins = new(ReferenceEqualityComparer.Instance);
	public readonly List<Exception> Errors = new();

	public PluginDisposal(IEnumerable<Plugin> plugins = null)
	{
		if(plugins != null)
		{
			foreach(var plugin in plugins)
				this.Register(plugin);
		}
	}

	private void Register(Plugin plugin)
	{
		foreach(var builtin in plugin.InnerBuiltins)
		{
			if(builtin.HasValue)
				_builtins.Add(builtin.Value);
		}

		foreach(var child in plugin.Children)
			this.Register(child);
	}

	public async ValueTask ReleaseAsync(object value)
	{
		if(value == null || !_released.Add(value))
			return;

		try
		{
			if(value is IAsyncDisposable asynchronous)
				await asynchronous.DisposeAsync().ConfigureAwait(false);
			else if(value is IDisposable synchronous)
				synchronous.Dispose();
			else if(value is System.Collections.IEnumerable collection)
			{
				foreach(var item in collection)
				{
					if(item != null && !_builtins.Contains(item))
						await this.ReleaseAsync(item).ConfigureAwait(false);
				}
			}
		}
		catch(Exception exception)
		{
			this.Errors.Add(exception);
		}
	}

	public void ThrowIfFailed()
	{
		if(this.Errors.Count == 1)
			ExceptionDispatchInfo.Capture(this.Errors[0]).Throw();
		if(this.Errors.Count > 1)
			throw new AggregateException(this.Errors);
	}
}
