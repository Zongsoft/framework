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
using System.Collections.Generic;

namespace Zongsoft.Components
{
	public class Executor : IExecutor, IHandler
	{
		#region 同步字段
		private readonly object _syncRoot = new object();
		#endregion

		#region 成员字段
		private volatile ICollection<IExecutionFilter> _filters;
		#endregion

		#region 构造函数
		protected Executor() { }
		protected Executor(IHandler handler)
		{
			this.Handler = handler;
		}
		#endregion

		#region 公共属性
		public IHandler Handler { get; set; }

		public ICollection<IExecutionFilter> Filters
		{
			get
			{
				if(_filters == null)
				{
					lock(_syncRoot)
					{
						if(_filters == null)
							_filters = this.CreateFilters();
					}
				}

				return _filters;
			}
		}
		#endregion

		#region 执行方法
		public bool Execute(object context)
		{
			var filters = this.Filters;

			if(filters != null)
			{
				foreach(var filter in filters)
					this.OnFiltering(filter, context);
			}

			var result = this.OnExecuteAsync(context).GetAwaiter().GetResult();

			if(filters != null)
			{
				foreach(var filter in filters)
					this.OnFiltered(filter, context);
			}

			return result;
		}

		public async ValueTask<bool> ExecuteAsync(object context, CancellationToken cancellation = default)
		{
			var filters = _filters;

			if(filters != null)
			{
				await Task.Run(() =>
				{
					foreach(var filter in filters)
						this.OnFiltering(filter, context);
				});
			}

			var result = await this.OnExecuteAsync(context);

			if(filters != null)
			{
				await Task.Run(() =>
				{
					foreach(var filter in filters)
						this.OnFiltered(filter, context);
				});
			}

			return result;
		}
		#endregion

		#region 虚拟方法
		protected virtual ValueTask<bool> OnExecuteAsync(object context, CancellationToken cancellation = default)
		{
			var handler = this.Handler;

			if(handler != null && handler.CanHandle(context))
				return handler.HandleAsync(context, cancellation);

			return ValueTask.FromResult(false);
		}

		protected virtual void OnFiltered(IExecutionFilter filter, object context)
		{
			filter.OnFiltered(context);
		}

		protected virtual void OnFiltering(IExecutionFilter filter, object context)
		{
			filter.OnFiltering(context);
		}

		protected virtual ICollection<IExecutionFilter> CreateFilters()
		{
			return new List<IExecutionFilter>();
		}
		#endregion

		#region 显式实现
		bool IHandler.CanHandle(object context) => this.Handler?.CanHandle(context) ?? false;
		ValueTask<bool> IHandler.HandleAsync(object caller, object context, CancellationToken cancellation) => this.ExecuteAsync(context, cancellation);
		#endregion
	}
}
