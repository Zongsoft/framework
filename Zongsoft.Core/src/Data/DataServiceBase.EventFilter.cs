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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

partial class DataServiceBase<TModel>
{
	#region 嵌套子类
	/// <summary>为显式启用的写入操作准备事件数据，并调用对应的单条或集合发布回调。</summary>
	/// <remarks>需由服务显式添加到 Filters 集合。事件声明和发布由派生类负责；本类不处理读取操作。</remarks>
	public abstract class EventFilterBase : IDataServiceFilter<TModel>
	{
		#region 成员字段
		private readonly HashSet<DataAccessMethod> _methods;
		#endregion

		#region 构造函数
		/// <summary>指定需要处理的写入操作。</summary>
		/// <param name="methods">至少一个新增、更新、删除、增改或导入操作；重复项只启用一次。</param>
		protected EventFilterBase(params DataAccessMethod[] methods)
		{
			ArgumentNullException.ThrowIfNull(methods);

			if(methods.Length == 0)
				throw new ArgumentException(null, nameof(methods));

			_methods = [];

			foreach(var item in methods)
			{
				if(!IsWriting(item))
					throw new ArgumentOutOfRangeException(nameof(methods));

				_methods.Add(item);
			}
		}
		#endregion

		#region 公共方法
		/// <summary>准备操作独立的返回列及可重复使用的批量输入，不短路数据操作。</summary>
		/// <param name="context">当前数据服务上下文。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>准备完成后返回 false。</returns>
		public async ValueTask<bool> OnFilteringAsync(IDataServiceContext<TModel> context, CancellationToken cancellation = default)
		{
			ArgumentNullException.ThrowIfNull(context);

			if(context.AccessContext is not { } operation || !_methods.Contains(operation.Method))
				return false;

			cancellation.ThrowIfCancellationRequested();

			switch(operation)
			{
				case DataDeleteContextBase deletion:
					this.PrepareReturning(deletion, ReturningKind.Older);
					break;
				case DataUpdateContextBase updation:
					this.PrepareReturning(updation, ReturningKind.Newer);
					break;
				case DataInsertContextBase { IsMultiple: true } insertion:
					insertion.Data = await MaterializeAsync(insertion.Data, cancellation);
					break;
				case DataUpsertContextBase { IsMultiple: true } upsertion:
					upsertion.Data = await MaterializeAsync(upsertion.Data, cancellation);
					break;
				case DataImportContextBase importation:
					importation.Data = importation.Data is IAsyncEnumerable<TModel> items ?
						await ToArrayAsync(items, cancellation) : ((IEnumerable<TModel>)importation.Data).ToArray();
					break;
			}

			return false;
		}

		/// <summary>操作成功且影响记录数大于零时，等待对应发布回调完成。</summary>
		/// <param name="context">当前数据服务上下文。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>发布回调的完成任务。</returns>
		public async ValueTask OnFilteredAsync(IDataServiceContext<TModel> context, CancellationToken cancellation = default)
		{
			ArgumentNullException.ThrowIfNull(context);

			if(context.AccessContext is not { } operation || !_methods.Contains(operation.Method))
				return;

			cancellation.ThrowIfCancellationRequested();

			switch(operation)
			{
				case DataDeleteContextBase { Count: > 0 } deletion when !deletion.Returning.Rows.IsEmpty:
					var deleted = deletion.Returning.Rows.ToDictionaries<TModel>(ReturningKind.Older).ToArray();

					if(IsSingle(deletion.Entity, deletion.Criteria))
						await this.OnDeletedAsync(context, deleted.Single(), cancellation);
					else
						await this.OnDeletedAsync(context, deleted, cancellation);

					break;
				case DataUpdateContextBase { Count: > 0 } updation when !updation.Returning.Rows.IsEmpty:
					var updated = updation.Returning.Rows.ToDictionaries<TModel>(ReturningKind.Newer).ToArray();

					if(IsSingle(updation.Entity, updation.Criteria))
						await this.OnUpdatedAsync(context, updated.Single(), cancellation);
					else
						await this.OnUpdatedAsync(context, updated, cancellation);

					break;
				case DataInsertContextBase { Count: > 0 } insertion:
					if(insertion.IsMultiple)
						await this.OnInsertedAsync(context, insertion.GetDataDictionaries<TModel>().ToArray(), cancellation);
					else
						await this.OnInsertedAsync(context, insertion.GetDataDictionaries<TModel>().Single(), cancellation);

					break;
				case DataUpsertContextBase { Count: > 0 } upsertion:
					if(upsertion.IsMultiple)
						await this.OnUpsertedAsync(context, upsertion.GetDataDictionaries<TModel>().ToArray(), cancellation);
					else
						await this.OnUpsertedAsync(context, upsertion.GetDataDictionaries<TModel>().Single(), cancellation);

					break;
				case DataImportContextBase { Count: > 0 } importation:
					await this.OnImportedAsync(context, DataDictionary.GetDictionaries<TModel>((IEnumerable)importation.Data).ToArray(), cancellation);
					break;
			}
		}
		#endregion

		#region 保护方法
		/// <summary>在操作上下文中追加事件需要的返回列，默认请求主实体全部已映射简单字段。</summary>
		/// <param name="context">当前写入上下文；不得借用或修改调用方 Options.Returning。</param>
		/// <param name="kind">删除使用 Older，更新使用 Newer。</param>
		/// <remarks>派生类可调整请求列；发布回调收到本次操作该种类的返回数据。</remarks>
		protected virtual void PrepareReturning(IDataMutateContextBase context, ReturningKind kind)
		{
			foreach(var property in context.Entity.Properties)
			{
				if(property.IsSimplex)
					context.Returning.Columns.Append(property.Name, kind);
			}
		}

		/// <summary>发布新增操作的单条事件，默认不执行任何操作。</summary>
		/// <param name="context">当前数据服务上下文。</param>
		/// <param name="argument">本次操作的事件数据。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>发布回调的完成任务。</returns>
		protected virtual ValueTask OnInsertedAsync(IDataServiceContext<TModel> context, IDataDictionary<TModel> argument, CancellationToken cancellation) => ValueTask.CompletedTask;

		/// <summary>发布新增操作的集合事件，默认不执行任何操作。</summary>
		/// <param name="context">当前数据服务上下文。</param>
		/// <param name="argument">本次操作的事件数据。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>发布回调的完成任务。</returns>
		protected virtual ValueTask OnInsertedAsync(IDataServiceContext<TModel> context, IEnumerable<IDataDictionary<TModel>> argument, CancellationToken cancellation) => ValueTask.CompletedTask;

		/// <summary>发布更新操作的单条事件，默认不执行任何操作。</summary>
		/// <param name="context">当前数据服务上下文。</param>
		/// <param name="argument">本次操作的事件数据。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>发布回调的完成任务。</returns>
		protected virtual ValueTask OnUpdatedAsync(IDataServiceContext<TModel> context, IDataDictionary<TModel> argument, CancellationToken cancellation) => ValueTask.CompletedTask;

		/// <summary>发布更新操作的集合事件，默认不执行任何操作。</summary>
		/// <param name="context">当前数据服务上下文。</param>
		/// <param name="argument">本次操作的事件数据。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>发布回调的完成任务。</returns>
		protected virtual ValueTask OnUpdatedAsync(IDataServiceContext<TModel> context, IEnumerable<IDataDictionary<TModel>> argument, CancellationToken cancellation) => ValueTask.CompletedTask;

		/// <summary>发布删除操作的单条事件，默认不执行任何操作。</summary>
		/// <param name="context">当前数据服务上下文。</param>
		/// <param name="argument">本次操作的事件数据。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>发布回调的完成任务。</returns>
		protected virtual ValueTask OnDeletedAsync(IDataServiceContext<TModel> context, IDataDictionary<TModel> argument, CancellationToken cancellation) => ValueTask.CompletedTask;

		/// <summary>发布删除操作的集合事件，默认不执行任何操作。</summary>
		/// <param name="context">当前数据服务上下文。</param>
		/// <param name="argument">本次操作的事件数据。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>发布回调的完成任务。</returns>
		protected virtual ValueTask OnDeletedAsync(IDataServiceContext<TModel> context, IEnumerable<IDataDictionary<TModel>> argument, CancellationToken cancellation) => ValueTask.CompletedTask;

		/// <summary>发布增改操作的单条事件，默认不执行任何操作。</summary>
		/// <param name="context">当前数据服务上下文。</param>
		/// <param name="argument">本次操作的事件数据。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>发布回调的完成任务。</returns>
		protected virtual ValueTask OnUpsertedAsync(IDataServiceContext<TModel> context, IDataDictionary<TModel> argument, CancellationToken cancellation) => ValueTask.CompletedTask;

		/// <summary>发布增改操作的集合事件，默认不执行任何操作。</summary>
		/// <param name="context">当前数据服务上下文。</param>
		/// <param name="argument">本次操作的事件数据。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>发布回调的完成任务。</returns>
		protected virtual ValueTask OnUpsertedAsync(IDataServiceContext<TModel> context, IEnumerable<IDataDictionary<TModel>> argument, CancellationToken cancellation) => ValueTask.CompletedTask;

		/// <summary>发布导入操作的集合事件，默认不执行任何操作。</summary>
		/// <param name="context">当前数据服务上下文。</param>
		/// <param name="argument">本次操作的事件数据。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>发布回调的完成任务。</returns>
		protected virtual ValueTask OnImportedAsync(IDataServiceContext<TModel> context, IEnumerable<IDataDictionary<TModel>> argument, CancellationToken cancellation) => ValueTask.CompletedTask;
		#endregion

		#region 私有方法
		private static bool IsWriting(DataAccessMethod method) => method is DataAccessMethod.Insert or DataAccessMethod.Update or DataAccessMethod.Delete or DataAccessMethod.Upsert or DataAccessMethod.Import;
		private static bool IsSingle(Metadata.IDataEntity entity, ICondition criteria) => entity.Key is { Length: > 0 } keys && keys.All(key => criteria.TryGetValue(key.Name, out var value) && value != null);

		private static async ValueTask<T[]> ToArrayAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellation)
		{
			var items = new List<T>();

			await foreach(var item in source.WithCancellation(cancellation))
				items.Add(item);

			return [.. items];
		}

		private static async ValueTask<IDataDictionary<TModel>[]> MaterializeAsync(object data, CancellationToken cancellation)
		{
			if(data is IAsyncEnumerable<IDataDictionary<TModel>> dictionaries)
				return await ToArrayAsync(dictionaries, cancellation);

			if(data is IAsyncEnumerable<TModel> models)
				return await ToArrayAsync(DataDictionary.GetDictionariesAsync<TModel, TModel>(models, cancellation), cancellation);

			return DataDictionary.GetDictionaries<TModel>((IEnumerable)data).ToArray();
		}
		#endregion
	}
	#endregion
}
