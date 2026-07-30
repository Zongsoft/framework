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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data.Common;

/// <summary>管理指定数据源的数据连接及其连接故障保护。</summary>
public sealed partial class DataConnector
{
	#region 公共事件
	/// <summary>当真实物理连接建立失败时发生。</summary>
	/// <remarks>熔断期间被快速拒绝的连接请求不会重复触发该事件。</remarks>
	public event EventHandler<DataConnectionFailureEventArgs> Failed;
	#endregion

	#region 成员字段
	private readonly IDataSource _source;
	private readonly CircuitBreaker _breaker;
	private readonly SemaphoreSlim _semaphore;
	#endregion

	#region 构造函数
	internal DataConnector(IDataSource source, CircuitBreakerOptions options = null, TimeProvider timeProvider = null)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_breaker = new CircuitBreaker(source, options, timeProvider);
		_breaker.Failed += this.OnFailed;

		/*
		 * 数据源发生故障时，高并发请求可能在首个失败结果返回前同时进入底层提供程序，
		 * 因此这里将连接建立串行化；首个失败会打开内部保护器，后续等待者不会再触发物理连接。
		 */
		_semaphore = new SemaphoreSlim(1, 1);
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前连接器所属的数据源。</summary>
	public IDataSource Source => _source;
	#endregion

	#region 内部属性
	internal CircuitBreaker Breaker => _breaker;
	#endregion

	#region 内部方法
	internal DbConnection CreateConnection() => _source.Driver.CreateConnection(_source.ConnectionString);
	#endregion

	#region 公共方法
	/// <summary>创建并打开当前数据源的数据连接。</summary>
	/// <returns>返回已打开的数据连接。</returns>
	public DbConnection Connect()
	{
		_breaker.EnsureAvailable();
		var connection = this.CreateConnection();

		try
		{
			this.Open(connection);
			return connection;
		}
		catch
		{
			connection?.Dispose();
			throw;
		}
	}

	/// <summary>异步创建并打开当前数据源的数据连接。</summary>
	/// <param name="cancellation">指定的取消标记。</param>
	/// <returns>返回表示异步操作的任务，其结果为已打开的数据连接。</returns>
	public async Task<DbConnection> ConnectAsync(CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		_breaker.EnsureAvailable();
		var connection = this.CreateConnection();

		try
		{
			await this.OpenAsync(connection, cancellation).ConfigureAwait(false);
			return connection;
		}
		catch
		{
			if(connection != null)
				await connection.DisposeAsync().ConfigureAwait(false);

			throw;
		}
	}

	/// <summary>使用指定的连接函数建立原生数据连接。</summary>
	/// <typeparam name="TResult">指定原生连接的类型。</typeparam>
	/// <param name="connector">指定的连接函数。</param>
	/// <returns>返回建立的原生数据连接。</returns>
	public TResult Connect<TResult>(Func<TResult> connector)
	{
		if(connector == null)
			throw new ArgumentNullException(nameof(connector));

		_breaker.EnsureAvailable();
		_semaphore.Wait();

		try
		{
			return _breaker.Execute(connector);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <summary>使用指定的连接函数异步建立原生数据连接。</summary>
	/// <typeparam name="TResult">指定原生连接的类型。</typeparam>
	/// <param name="connector">指定的异步连接函数。</param>
	/// <param name="cancellation">指定的取消标记。</param>
	/// <returns>返回表示异步操作的任务，其结果为建立的原生数据连接。</returns>
	public async Task<TResult> ConnectAsync<TResult>(Func<CancellationToken, Task<TResult>> connector, CancellationToken cancellation = default)
	{
		if(connector == null)
			throw new ArgumentNullException(nameof(connector));

		cancellation.ThrowIfCancellationRequested();
		_breaker.EnsureAvailable();
		await _semaphore.WaitAsync(cancellation).ConfigureAwait(false);

		try
		{
			return await _breaker.ExecuteAsync(connector, cancellation).ConfigureAwait(false);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <summary>打开指定的数据连接。</summary>
	/// <param name="connection">指定要打开的数据连接。</param>
	public void Open(DbConnection connection)
	{
		if(connection == null)
			throw new ArgumentNullException(nameof(connection));

		if(connection.State == ConnectionState.Open)
			return;

		this.Connect(() =>
		{
			if(connection.State == ConnectionState.Open)
				return connection;

			if(connection.State == ConnectionState.Broken)
				connection.Close();

			connection.Open();
			return connection;
		});
	}

	/// <summary>异步打开指定的数据连接。</summary>
	/// <param name="connection">指定要打开的数据连接。</param>
	/// <param name="cancellation">指定的取消标记。</param>
	/// <returns>返回表示异步操作的任务。</returns>
	public async Task OpenAsync(DbConnection connection, CancellationToken cancellation = default)
	{
		if(connection == null)
			throw new ArgumentNullException(nameof(connection));

		if(connection.State == ConnectionState.Open)
			return;

		await this.ConnectAsync(async token =>
		{
			if(connection.State == ConnectionState.Open)
				return connection;

			if(connection.State == ConnectionState.Broken)
				await connection.CloseAsync().ConfigureAwait(false);

			await connection.OpenAsync(token).ConfigureAwait(false);
			return connection;
		}, cancellation).ConfigureAwait(false);
	}
	#endregion

	#region 私有方法
	private void OnFailed(object sender, DataConnectionFailureEventArgs failure)
	{
		var handlers = this.Failed;

		if(handlers != null)
		{
			foreach(EventHandler<DataConnectionFailureEventArgs> handler in handlers.GetInvocationList())
			{
				try
				{
					handler(this, failure);
				}
				catch
				{
					//连接故障订阅者不能影响数据连接操作
				}
			}
		}

		if(failure.ExceptionHandled)
			return;

		try
		{
			var format = failure.IsSuspended ?
				Properties.Resources.DataConnector_ConnectionFailed_Suspended_Message :
				Properties.Resources.DataConnector_ConnectionFailed_Message;

			var message = string.Format(
				format,
				failure.Source.Name,
				failure.Source.Driver?.Name,
				failure.FailureCount,
				failure.RetryAt?.ToLocalTime());

			Zongsoft.Diagnostics.Logging.GetLogging<DataConnector>().Error(message, failure.Exception);
		}
		catch
		{
			//默认日志处理不能掩盖原始连接异常
		}
	}
	#endregion
}
