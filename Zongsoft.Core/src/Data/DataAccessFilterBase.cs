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
using System.Collections.Generic;

namespace Zongsoft.Data
{
	/// <summary>
	/// 提供数据访问过滤功能的过滤器基类。
	/// </summary>
	public abstract class DataAccessFilterBase : IDataAccessFilter
	{
		#region 构造函数
		protected DataAccessFilterBase()
		{
		}

		protected DataAccessFilterBase(string name, params DataAccessMethod[] methods)
		{
			this.Name = name;
			this.Methods = methods;
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get;
		}

		public DataAccessMethod[] Methods
		{
			get;
		}
		#endregion

		#region 保护属性
		protected virtual System.Security.Claims.ClaimsPrincipal Principal
		{
			get => Services.ApplicationContext.Current?.Principal;
		}
		#endregion

		#region 过滤方法
		protected virtual void OnFiltered(IDataAccessContextBase context)
		{
			switch(context.Method)
			{
				case DataAccessMethod.Aggregate:
					this.OnAggregated((DataAggregateContextBase)context);
					break;
				case DataAccessMethod.Exists:
					this.OnExisted((DataExistContextBase)context);
					break;
				case DataAccessMethod.Increment:
					this.OnIncremented((DataIncrementContextBase)context);
					break;
				case DataAccessMethod.Select:
					this.OnSelected((DataSelectContextBase)context);
					break;
				case DataAccessMethod.Delete:
					this.OnDeleted((DataDeleteContextBase)context);
					break;
				case DataAccessMethod.Insert:
					this.OnInserted((DataInsertContextBase)context);
					break;
				case DataAccessMethod.Update:
					this.OnUpdated((DataUpdateContextBase)context);
					break;
				case DataAccessMethod.Upsert:
					this.OnUpserted((DataUpsertContextBase)context);
					break;
				case DataAccessMethod.Execute:
					this.OnExecuted((DataExecuteContextBase)context);
					break;
			}
		}

		protected virtual void OnFiltering(IDataAccessContextBase context)
		{
			switch(context.Method)
			{
				case DataAccessMethod.Aggregate:
					this.OnAggregating((DataAggregateContextBase)context);
					break;
				case DataAccessMethod.Exists:
					this.OnExisting((DataExistContextBase)context);
					break;
				case DataAccessMethod.Increment:
					this.OnIncrementing((DataIncrementContextBase)context);
					break;
				case DataAccessMethod.Select:
					this.OnSelecting((DataSelectContextBase)context);
					break;
				case DataAccessMethod.Delete:
					this.OnDeleting((DataDeleteContextBase)context);
					break;
				case DataAccessMethod.Insert:
					this.OnInserting((DataInsertContextBase)context);
					break;
				case DataAccessMethod.Update:
					this.OnUpdating((DataUpdateContextBase)context);
					break;
				case DataAccessMethod.Upsert:
					this.OnUpserting((DataUpsertContextBase)context);
					break;
				case DataAccessMethod.Execute:
					this.OnExecuting((DataExecuteContextBase)context);
					break;
			}
		}

		void Services.IExecutionFilter<IDataAccessContextBase>.OnFiltered(IDataAccessContextBase context)
		{
			this.OnFiltered(context);
		}

		void Services.IExecutionFilter<IDataAccessContextBase>.OnFiltering(IDataAccessContextBase context)
		{
			this.OnFiltering(context);
		}

		void Services.IExecutionFilter.OnFiltered(object context)
		{
			if(context is IDataAccessContextBase ctx)
				this.OnFiltered(ctx);
		}

		void Services.IExecutionFilter.OnFiltering(object context)
		{
			if(context is IDataAccessContextBase ctx)
				this.OnFiltering(ctx);
		}
		#endregion

		#region 虚拟方法
		protected virtual void OnAggregating(DataAggregateContextBase context)
		{
		}

		protected virtual void OnAggregated(DataAggregateContextBase context)
		{
		}

		protected virtual void OnExecuting(DataExecuteContextBase context)
		{
		}

		protected virtual void OnExecuted(DataExecuteContextBase context)
		{
		}

		protected virtual void OnExisting(DataExistContextBase context)
		{
		}

		protected virtual void OnExisted(DataExistContextBase context)
		{
		}

		protected virtual void OnIncrementing(DataIncrementContextBase context)
		{
		}

		protected virtual void OnIncremented(DataIncrementContextBase context)
		{
		}

		protected virtual void OnSelecting(DataSelectContextBase context)
		{
		}

		protected virtual void OnSelected(DataSelectContextBase context)
		{
		}

		protected virtual void OnDeleting(DataDeleteContextBase context)
		{
		}

		protected virtual void OnDeleted(DataDeleteContextBase context)
		{
		}

		protected virtual void OnInserting(DataInsertContextBase context)
		{
		}

		protected virtual void OnInserted(DataInsertContextBase context)
		{
		}

		protected virtual void OnUpdating(DataUpdateContextBase context)
		{
		}

		protected virtual void OnUpdated(DataUpdateContextBase context)
		{
		}

		protected virtual void OnUpserting(DataUpsertContextBase context)
		{
		}

		protected virtual void OnUpserted(DataUpsertContextBase context)
		{
		}
		#endregion
	}
}
