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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data.Common
{
	/// <summary>
	/// 表示数据提供程序的接口。
	/// </summary>
	public interface IDataProvider
	{
		#region 事件定义
		/// <summary>提供数据访问错误的事件。</summary>
		event EventHandler<DataAccessErrorEventArgs> Error;

		/// <summary>提供数据访问开始执行的事件。</summary>
		event EventHandler<DataAccessEventArgs<IDataAccessContext>> Executing;

		/// <summary>提供数据访问执行完成的事件。</summary>
		event EventHandler<DataAccessEventArgs<IDataAccessContext>> Executed;
		#endregion

		#region 属性定义
		/// <summary>获取数据提供程序的名称。</summary>
		string Name { get; }

		/// <summary>获取或设置数据执行器。</summary>
		IDataExecutor Executor { get; set; }

		/// <summary>获取或设置数据提供程序的连接器。</summary>
		IDataMultiplexer Multiplexer { get; set; }

		/// <summary>获取或设置数据提供程序的元数据管理器。</summary>
		Metadata.IDataMetadataManager Metadata { get; set; }
		#endregion

		#region 方法定义
		/// <summary>执行数据操作。</summary>
		/// <param name="context">数据操作的上下文。</param>
		void Execute(IDataAccessContext context);

		/// <summary>异步执行数据操作。</summary>
		/// <param name="context">数据操作的上下文。</param>
		/// <param name="cancellation">指定的异步取消标记。</param>
		/// <returns>返回的异步操作任务。</returns>
		Task ExecuteAsync(IDataAccessContext context, CancellationToken cancellation = default);

		/// <summary>导入数据操作。</summary>
		/// <param name="context">数据导入的上下文。</param>
		void Import(DataImportContext context);

		/// <summary>异步导入数据操作。</summary>
		/// <param name="context">数据导入的上下文。</param>
		/// <param name="cancellation">指定的异步取消标记。</param>
		/// <returns>返回的异步操作任务。</returns>
		ValueTask ImportAsync(DataImportContext context, CancellationToken cancellation = default);
		#endregion
	}
}
