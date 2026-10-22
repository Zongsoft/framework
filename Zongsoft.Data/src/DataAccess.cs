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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data;

public class DataAccess : DataAccessBase
{
	#region 成员字段
	private IDataProvider _provider;
	#endregion

	#region 构造函数
	public DataAccess(string name, IDataAccessOptions options = null) : base(name)
	{
		foreach(var filter in DataEnvironment.Filters)
			this.Filters.Add(filter);

		if(options?.Filters != null)
		{
			foreach(var filter in options.Filters)
				this.Filters.Add(filter);
		}

		_provider = new DataProvider(name, options?.Settings);
		_provider.Error += this.Provider_ErrorAsync;
	}
	#endregion

	#region 公共属性
	public IDataProvider Provider => _provider;
	#endregion

	#region 执行方法
	protected override ValueTask OnExecuteAsync(DataExecuteContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 导入方法
	protected override ValueTask OnImportAsync(DataImportContextBase context, CancellationToken cancellation = default) => this.Provider.ImportAsync((DataImportContext)context, cancellation);
	#endregion

	#region 存在方法
	protected override ValueTask OnExistsAsync(DataExistContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 聚合方法
	protected override ValueTask OnAggregateAsync(DataAggregateContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 删除方法
	protected override ValueTask OnDeleteAsync(DataDeleteContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 插入方法
	protected override ValueTask OnInsertAsync(DataInsertContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 增改方法
	protected override ValueTask OnUpsertAsync(DataUpsertContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 更新方法
	protected override ValueTask OnUpdateAsync(DataUpdateContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 查询方法
	protected override ValueTask OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 模式解析
	protected override ISchemaParser CreateSchema() => SchemaParser.Instance;
	#endregion

	#region 序号构建
	protected override IDataSequencer CreateSequencer() => new DataSequencer(this);
	#endregion

	#region 调用过滤
	protected override async ValueTask OnFilteredAsync(IDataAccessContextBase context, CancellationToken cancellation)
	{
		//首先调用本数据访问器的过滤器后趋部分
		await base.OnFilteredAsync(context, cancellation);

		//最后调用全局过滤器的前趋部分
		await DataEnvironment.Filters.InvokeFilteredAsync(context, cancellation);
	}

	protected override async ValueTask OnFilteringAsync(IDataAccessContextBase context, CancellationToken cancellation)
	{
		//首先调用全局过滤器的前趋部分
		await DataEnvironment.Filters.InvokeFilteringAsync(context, cancellation);

		//最后调用本数据访问器的过滤器前趋部分
		await base.OnFilteringAsync(context, cancellation);
	}
	#endregion

	#region 上下文法
	protected override DataExistContextBase CreateExistContext(string name, ICondition criteria, IDataExistsOptions options) =>
		new DataExistContext(this, name, criteria.Flatten(), options);

	protected override DataExecuteContextBase CreateExecuteContext(string name, bool isScalar, Type resultType, IEnumerable<Parameter> parameters, IDataExecuteOptions options) =>
		new DataExecuteContext(this, name, isScalar, resultType, parameters, options);

	protected override DataAggregateContextBase CreateAggregateContext(string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options) =>
		new DataAggregateContext(this, name, aggregate, criteria.Flatten(), options);

	protected override DataImportContextBase CreateImportContext(string name, object data, IEnumerable<string> members, IDataImportOptions options) =>
		new DataImportContext(this, name, data, members, options);

	protected override DataDeleteContextBase CreateDeleteContext(string name, ICondition criteria, ISchema schema, IDataDeleteOptions options) =>
		new DataDeleteContext(this, name, criteria.Flatten(), schema, options);

	protected override DataInsertContextBase CreateInsertContext(string name, bool isMultiple, object data, ISchema schema, IDataInsertOptions options) =>
		new DataInsertContext(this, name, isMultiple, data, schema, options);

	protected override DataUpsertContextBase CreateUpsertContext(string name, bool isMultiple, object data, ISchema schema, IDataUpsertOptions options) =>
		new DataUpsertContext(this, name, isMultiple, data, schema, options);

	protected override DataUpdateContextBase CreateUpdateContext(string name, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options) =>
		new DataUpdateContext(this, name, data, criteria.Flatten(), schema, options);

	protected override DataSelectContextBase CreateSelectContext(string name, Type entityType, ICondition criteria, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options) =>
		new DataSelectContext(this, name, entityType, grouping, criteria.Flatten(), schema, paging, sortings, options);
	#endregion

	#region 内部方法
	internal ValueTask<long> IncreaseAsync(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data, CancellationToken cancellation)
	{
		if(this.Sequencer == null)
			throw new InvalidOperationException(string.Format(Properties.Resources.DataAccess_MissingSequencer_Message, this.Name));

		return ((DataSequencer)this.Sequencer).IncreaseAsync(context, sequence, data, cancellation);
	}
	#endregion

	#region 异常事件
	private ValueTask Provider_ErrorAsync(object sender, DataAccessErrorEventArgs args, CancellationToken cancellation) => this.OnErrorAsync(args, cancellation);
	#endregion

	#region 处置方法
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			var provider = Interlocked.Exchange(ref _provider, null);
			if(provider != null)
				provider.Error -= this.Provider_ErrorAsync;
		}
	}
	#endregion

	#region 嵌套子类
	private sealed class DataSequencer(DataAccess accessor) : DataSequencerBase(accessor), ISequenceBase
	{
		#region 常量定义
		private const string SEQUENCE_KEY = "Zongsoft.Sequence:";
		#endregion

		#region 公共方法
		public override async ValueTask<long> IncreaseAsync(IDataEntitySimplexProperty property, int interval, CancellationToken cancellation = default)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			var sequence = property.Sequence ?? throw new InvalidOperationException(string.Format(Properties.Resources.Sequence_Undefined_Message, property.Entity.Name, property.Name));
			if(sequence.IsBuiltin)
				return 0L;

			return await this.Sequence.IncreaseAsync(
				await GetSequenceKeyAsync(null, sequence, null, cancellation),
				interval == 1 ? sequence.Interval : interval,
				sequence.Seed, cancellation);
		}

		public async ValueTask<long> IncreaseAsync(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data, CancellationToken cancellation)
		{
			if(sequence == null)
				throw new ArgumentNullException(nameof(sequence));

			return await this.Sequence.IncreaseAsync(
				await GetSequenceKeyAsync(context, sequence, data, cancellation),
				sequence.Interval,
				sequence.Seed,
				cancellation);
		}
		#endregion

		#region 显式实现
		long ISequenceBase.Increase(string key, int interval, int seed) => throw new NotSupportedException();

		long ISequenceBase.Decrease(string key, int interval, int seed) => throw new NotSupportedException();

		async ValueTask<long> ISequenceBase.IncreaseAsync(string key, int interval, int seed, CancellationToken cancellation)
		{
			var sequence = ParseSequenceKey(key, out var data);
			key = await GetSequenceKeyAsync(null, sequence, data, cancellation);

			return await this.Sequence.IncreaseAsync(key,
				interval == 1 ? sequence.Interval : interval,
				seed == 0 ? sequence.Seed : seed,
				cancellation);
		}

		async ValueTask<long> ISequenceBase.DecreaseAsync(string key, int interval, int seed, CancellationToken cancellation)
		{
			var sequence = ParseSequenceKey(key, out var data);
			key = await GetSequenceKeyAsync(null, sequence, data, cancellation);

			return await this.Sequence.DecreaseAsync(key,
				interval == 1 ? sequence.Interval : interval,
				seed == 0 ? sequence.Seed : seed,
				cancellation);
		}

		void ISequenceBase.Reset(string key, int value) => throw new NotSupportedException();

		async ValueTask ISequenceBase.ResetAsync(string key, int value, CancellationToken cancellation)
		{
			var sequence = ParseSequenceKey(key, out var data);
			key = await GetSequenceKeyAsync(null, sequence, data, cancellation);
			await this.Sequence.ResetAsync(key, value == 0 ? sequence.Seed : value, cancellation);
		}
		#endregion

		#region 私有方法
		private static IDataEntityPropertySequence ParseSequenceKey(string key, out object data)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			data = null;
			var suffix = key.LastIndexOf('@');

			if(suffix > 0)
			{
				data = key[(suffix + 1)..].Split(',', '|', '-');
				key = key[..suffix];
			}

			var index = key.LastIndexOfAny([':', '.']);
			if(index < 0)
				throw new ArgumentException(Properties.Resources.Sequence_InvalidKey_Message);

			if(!Mapping.Entities.TryGetValue(key[..index], out var entity))
				throw new ArgumentException(string.Format(Properties.Resources.Sequence_EntityNotFound_Message, key[..index]));

			if(!entity.Properties.TryGetValue(key[(index + 1)..], out var found) || found.IsComplex)
				throw new ArgumentException(string.Format(Properties.Resources.Sequence_PropertyNotFound_Message, key[(index + 1)..]));

			return ((IDataEntitySimplexProperty)found).Sequence ??
				throw new ArgumentException(string.Format(Properties.Resources.Sequence_PropertyUndefined_Message, found.Name));
		}

		private static async ValueTask<string> GetSequenceKeyAsync(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data, CancellationToken cancellation)
		{
			var key = $"{SEQUENCE_KEY}{sequence.Property.Entity.Name}.{sequence.Property.Name}";

			if(sequence.References != null && sequence.References.Length > 0)
			{
				if(data == null)
					throw new InvalidOperationException(string.Format(Properties.Resources.Sequence_MissingReferences_Message, sequence.Name));

				var index = 0;

				foreach(var reference in sequence.References)
				{
					cancellation.ThrowIfCancellationRequested();
					object value = null;
					bool found;

					switch(data)
					{
						case IModel model:
							found = model.TryGetValue(reference.Name, out value);
							break;
						case IDictionary<string, object> genericDictionary:
							found = genericDictionary.TryGetValue(reference.Name, out value);
							break;
						case IDictionary classicDictionary:
							found = classicDictionary.Contains(reference.Name);
							if(found)
								value = classicDictionary[reference.Name];
							break;
						default:
							if(Zongsoft.Common.TypeExtension.IsScalarType(data.GetType()))
							{
								value = data.GetType().IsArray ? ((Array)data).GetValue(index) : data.ToString();
								found = value != null;
							}
							else
							{
								value = Reflection.Reflector.GetValue(ref data, reference.Name);
								found = value != null;
							}
							break;
					}

					if(!found)
					{
						var result = await GetRequiredValueAsync(context, reference, cancellation);
						if(!result.HasValue)
							throw new InvalidOperationException(string.Format(Properties.Resources.Sequence_ReferenceMissing_Message, reference.Name));

						value = result.Value;
					}

					key += index++ == 0 ? ":" : "-";
					key += value.ToString().Trim();
				}
			}

			return key;
		}

		private static ValueTask<DataValidatorResult> GetRequiredValueAsync(IDataMutateContextBase context, IDataEntitySimplexProperty property, CancellationToken cancellation)
		{
			var validator = context == null || context.Options.ValidatorSuppressed ? null : context.Validator;
			return validator == null ? ValueTask.FromResult(default(DataValidatorResult)) : validator.OnInsertAsync(context, property, cancellation);
		}
		#endregion
	}
	#endregion
}
