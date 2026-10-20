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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Externals.Redis.Configuration;

public class RedisConfigurationSource : IConfigurationSource, IAsyncDisposable
{
	#region 常量定义
	private const string DEFAULT_NAMESPACE = "Zongsoft.Configuration";
	#endregion

	#region 成员字段
	private string _namespace = DEFAULT_NAMESPACE;
	private RedisConfigurationProvider _provider;
	private readonly object _sync = new();
	private bool _disposed;
	#endregion

	#region 构造函数
	public RedisConfigurationSource() { }
	public RedisConfigurationSource(string name) => this.Name = name;
	#endregion

	#region 公共属性
	public string Name { get; set; }
	public string Namespace
	{
		get => _namespace;
		set => _namespace = string.IsNullOrWhiteSpace(value) ? throw new ArgumentNullException() : value.Trim();
	}
	#endregion

	#region 公共方法
	/// <summary>加载远程配置后才能将此配置源交给同步配置构建器。</summary>
	/// <param name="cancellation">取消标记。</param>
	/// <returns>配置加载完成的任务。</returns>
	public ValueTask LoadAsync(CancellationToken cancellation = default)
	{
		lock(_sync)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
			return (_provider ??= new RedisConfigurationProvider(this)).LoadAsync(cancellation);
		}
	}

	public IConfigurationProvider Build(IConfigurationBuilder builder)
	{
		lock(_sync)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
			return _provider is { IsLoaded: true } ? _provider :
				throw new InvalidOperationException(Properties.Resources.Configuration_AsyncLoadRequired_Message);
		}
	}

	/// <summary>等待此配置源拥有的加载、通知和订阅完成释放。</summary>
	/// <returns>释放完成的任务。</returns>
	public ValueTask DisposeAsync()
	{
		lock(_sync)
		{
			_disposed = true;
			return _provider?.DisposeAsync() ?? ValueTask.CompletedTask;
		}
	}
	#endregion
}