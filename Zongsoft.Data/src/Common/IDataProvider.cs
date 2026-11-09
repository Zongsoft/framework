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

namespace Zongsoft.Data.Common;

/// <summary>表示数据提供程序的接口。</summary>
public interface IDataProvider
{
	#region 事件定义
	/// <summary>提供数据访问错误的事件。</summary>
	event Func<object, DataAccessErrorEventArgs, CancellationToken, ValueTask> Error;

	/// <summary>提供数据访问开始执行的事件。</summary>
	event Func<object, DataAccessEventArgs<IDataAccessContext>, CancellationToken, ValueTask> Executing;

	/// <summary>提供数据访问执行完成的事件。</summary>
	event Func<object, DataAccessEventArgs<IDataAccessContext>, CancellationToken, ValueTask> Executed;
	#endregion

	#region 属性定义
	/// <summary>获取数据提供程序的名称。</summary>
	string Name { get; }

	/// <summary>获取或设置数据执行器。</summary>
	IDataExecutor Executor { get; set; }

	/// <summary>获取或设置数据提供程序的连接器。</summary>
	IDataMultiplexer Multiplexer { get; set; }
	#endregion

	#region 方法定义
	/// <summary>异步执行数据操作。</summary>
	/// <param name="context">数据操作的上下文。</param>
	/// <param name="cancellation">指定的异步取消标记。</param>
	/// <returns>返回的异步操作任务。</returns>
	ValueTask ExecuteAsync(IDataAccessContext context, CancellationToken cancellation = default);

	/// <summary>异步导入数据操作。</summary>
	/// <param name="context">数据导入的上下文。</param>
	/// <param name="cancellation">指定的异步取消标记。</param>
	/// <returns>返回的异步操作任务。</returns>
	ValueTask ImportAsync(DataImportContext context, CancellationToken cancellation = default);
	#endregion
}
