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
		_provider.Error += this.Provider_Error;
	}
	#endregion

	#region 公共属性
	public IDataProvider Provider => _provider;
	#endregion

	#region 执行方法
	protected override void OnExecute(DataExecuteContextBase context) => this.Provider.Execute((IDataAccessContext)context);
	protected override ValueTask OnExecuteAsync(DataExecuteContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 导入方法
	protected override void OnImport(DataImportContextBase context) => this.Provider.Import((DataImportContext)context);
	protected override ValueTask OnImportAsync(DataImportContextBase context, CancellationToken cancellation = default) => this.Provider.ImportAsync((DataImportContext)context, cancellation);
	#endregion

	#region 存在方法
	protected override void OnExists(DataExistContextBase context) => this.Provider.Execute((IDataAccessContext)context);
	protected override ValueTask OnExistsAsync(DataExistContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 聚合方法
	protected override void OnAggregate(DataAggregateContextBase context) => this.Provider.Execute((IDataAccessContext)context);
	protected override ValueTask OnAggregateAsync(DataAggregateContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 删除方法
	protected override void OnDelete(DataDeleteContextBase context) => this.Provider.Execute((IDataAccessContext)context);
	protected override ValueTask OnDeleteAsync(DataDeleteContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 插入方法
	protected override void OnInsert(DataInsertContextBase context) => this.Provider.Execute((IDataAccessContext)context);
	protected override ValueTask OnInsertAsync(DataInsertContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 增改方法
	protected override void OnUpsert(DataUpsertContextBase context) => this.Provider.Execute((IDataAccessContext)context);
	protected override ValueTask OnUpsertAsync(DataUpsertContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 更新方法
	protected override void OnUpdate(DataUpdateContextBase context) => this.Provider.Execute((IDataAccessContext)context);
	protected override ValueTask OnUpdateAsync(DataUpdateContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 查询方法
	protected override void OnSelect(DataSelectContextBase context) => this.Provider.Execute((IDataAccessContext)context);
	protected override ValueTask OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
	#endregion

	#region 模式解析
	protected override ISchemaParser CreateSchema() => SchemaParser.Instance;
	#endregion

	#region 序号构建
	protected override IDataSequencer CreateSequencer() => new DataSequencer(this);
	#endregion

	#region 调用过滤
	protected override void OnFiltered(IDataAccessContextBase context)
	{
		//首先调用本数据访问器的过滤器后趋部分
		base.OnFiltered(context);

		//最后调用全局过滤器的前趋部分
		DataEnvironment.Filters.InvokeFiltered(context);
	}

	protected override void OnFiltering(IDataAccessContextBase context)
	{
		//首先调用全局过滤器的前趋部分
		DataEnvironment.Filters.InvokeFiltering(context);

		//最后调用本数据访问器的过滤器前趋部分
		base.OnFiltering(context);
	}
	#endregion

	#region 上下文法
	protected override DataExistContextBase CreateExistContext(string name, ICondition criteria, IDataExistsOptions options) =>
		new DataExistContext(this, name, criteria.Flatten(), options);

	protected override DataExecuteContextBase CreateExecuteContext(string name, bool isScalar, Type resultType, IEnumerable<Parameter> parameters, IDataExecuteOptions options) =>
		new DataExecuteContext(this, name, isScalar, resultType, parameters, options);

	protected override DataAggregateContextBase CreateAggregateContext(string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options) =>
		new DataAggregateContext(this, name, aggregate, criteria.Flatten(), options);

	protected override DataImportContextBase CreateImportContext(string name, IEnumerable data, IEnumerable<string> members, IDataImportOptions options) =>
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
	internal long Increase(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data)
	{
		if(this.Sequencer == null)
			throw new InvalidOperationException($"Missing required sequencer of the '{this.Name}' DataAccess.");

		return ((DataSequencer)this.Sequencer).Increase(context, sequence, data);
	}

	internal ValueTask<long> IncreaseAsync(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data, CancellationToken cancellation)
	{
		if(this.Sequencer == null)
			throw new InvalidOperationException($"Missing required sequencer of the '{this.Name}' DataAccess.");

		return ((DataSequencer)this.Sequencer).IncreaseAsync(context, sequence, data, cancellation);
	}
	#endregion

	#region 异常事件
	private void Provider_Error(object sender, DataAccessErrorEventArgs args) => this.OnError(args);
	#endregion

	#region 处置方法
	protected override void Dispose(bool disposing)
	{
		//取消提供程序的事件监听
		_provider.Error -= this.Provider_Error;

		//置空提供程序
		_provider = null;

		//调用基类同名方法
		base.Dispose(disposing);
	}
	#endregion

	#region 嵌套子类
	private sealed class DataSequencer(DataAccess accessor) : DataSequencerBase(accessor), ISequenceBase
	{
		#region 常量定义
		private const string SEQUENCE_KEY = "Zongsoft.Sequence:";
		#endregion

		#region 公共方法
		public override long Increase(IDataEntitySimplexProperty property, int interval = 1)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			var sequence = property.Sequence ?? throw new InvalidOperationException($"The specified '{property.Entity.Name}.{property.Name}' property does not define a sequence.");
			if(sequence.IsBuiltin)
				return 0L;

			return this.Sequence.Increase(
				GetSequenceKey(null, sequence, null),
				interval == 1 ? sequence.Interval : interval,
				sequence.Seed);
		}

		public override ValueTask<long> IncreaseAsync(IDataEntitySimplexProperty property, int interval, CancellationToken cancellation = default)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			var sequence = property.Sequence ?? throw new InvalidOperationException($"The specified '{property.Entity.Name}.{property.Name}' property does not define a sequence.");
			if(sequence.IsBuiltin)
				return ValueTask.FromResult(0L);

			return this.Sequence.IncreaseAsync(
				GetSequenceKey(null, sequence, null),
				interval == 1 ? sequence.Interval : interval,
				sequence.Seed, cancellation);
		}

		public long Increase(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data)
		{
			if(sequence == null)
				throw new ArgumentNullException(nameof(sequence));

			return this.Sequence.Increase(
				GetSequenceKey(context, sequence, data),
				sequence.Interval,
				sequence.Seed);
		}

		public ValueTask<long> IncreaseAsync(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data, CancellationToken cancellation)
		{
			if(sequence == null)
				throw new ArgumentNullException(nameof(sequence));

			return this.Sequence.IncreaseAsync(
				GetSequenceKey(context, sequence, data),
				sequence.Interval,
				sequence.Seed,
				cancellation);
		}
		#endregion

		#region 显式实现
		long ISequenceBase.Increase(string key, int interval, int seed)
		{
			key = GetSequenceKey(key, out var sequence);

			return this.Sequence.Increase(key,
				interval == 1 ? sequence.Interval : interval,
				seed == 0 ? sequence.Seed : seed);
		}

		long ISequenceBase.Decrease(string key, int interval, int seed)
		{
			key = GetSequenceKey(key, out var sequence);

			return this.Sequence.Decrease(key,
				interval == 1 ? sequence.Interval : interval,
				seed == 0 ? sequence.Seed : seed);
		}

		ValueTask<long> ISequenceBase.IncreaseAsync(string key, int interval, int seed, CancellationToken cancellation)
		{
			key = GetSequenceKey(key, out var sequence);

			return this.Sequence.IncreaseAsync(key,
				interval == 1 ? sequence.Interval : interval,
				seed == 0 ? sequence.Seed : seed,
				cancellation);
		}

		ValueTask<long> ISequenceBase.DecreaseAsync(string key, int interval, int seed, CancellationToken cancellation)
		{
			key = GetSequenceKey(key, out var sequence);

			return this.Sequence.DecreaseAsync(key,
				interval == 1 ? sequence.Interval : interval,
				seed == 0 ? sequence.Seed : seed,
				cancellation);
		}

		void ISequenceBase.Reset(string key, int value)
		{
			key = GetSequenceKey(key, out var sequence);
			this.Sequence.Reset(key, value == 0 ? sequence.Seed : value);
		}

		ValueTask ISequenceBase.ResetAsync(string key, int value, CancellationToken cancellation)
		{
			key = GetSequenceKey(key, out var sequence);
			return this.Sequence.ResetAsync(key, value == 0 ? sequence.Seed : value, cancellation);
		}
		#endregion

		#region 私有方法
		private static string GetSequenceKey(string key, out IDataEntityPropertySequence sequence)
		{
			sequence = null;

			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			var index = key.LastIndexOfAny([':', '.', '@']);
			object data = null;

			if(index > 0 && key[index] == '@')
			{
				data = key[(index + 1)..].Split(',', '|', '-');
				index = key.LastIndexOfAny([':', '.'], index);
			}

			if(index < 0)
				throw new ArgumentException($"Invalid sequence key, the sequence key must separate the entity name and property name with a colon or a dot.");

			if(!Mapping.Entities.TryGetValue(key[..index], out var entity))
				throw new ArgumentException($"The '{key[..index]}' entity specified in the sequence key does not exist.");

			if(!entity.Properties.TryGetValue(key[(index + 1)..], out var found) || found.IsComplex)
				throw new ArgumentException($"The '{key[(index + 1)..]}' property specified in the sequence key does not exist or is not a simplex property.");

			sequence = ((IDataEntitySimplexProperty)found).Sequence;

			if(sequence == null)
				throw new ArgumentException($"The '{found.Name}' property specified in the sequence key is undefined.");

			return GetSequenceKey(null, sequence, data);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetSequenceKey(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data)
		{
			var key = $"{SEQUENCE_KEY}{sequence.Property.Entity.Name}.{sequence.Property.Name}";

			if(sequence.References != null && sequence.References.Length > 0)
			{
				if(data == null)
					throw new InvalidOperationException($"Missing required references data for the '{sequence.Name}' sequence.");

				var index = 0;
				object value = null;

				foreach(var reference in sequence.References)
				{
					switch(data)
					{
						case IModel model:
							if(!model.TryGetValue(reference.Name, out value) && !GetRequiredValue(context, reference, out value))
								throw new InvalidOperationException($"The required '{reference.Name}' reference of sequence is not included in the data.");

							break;
						case IDictionary<string, object> genericDictionary:
							if(!genericDictionary.TryGetValue(reference.Name, out value) && !GetRequiredValue(context, reference, out value))
								throw new InvalidOperationException($"The required '{reference.Name}' reference of sequence is not included in the data.");

							break;
						case IDictionary classicDictionary:
							if(!classicDictionary.Contains(reference.Name) && !GetRequiredValue(context, reference, out value))
								throw new InvalidOperationException($"The required '{reference.Name}' reference of sequence is not included in the data.");

							break;
						default:
							if(Zongsoft.Common.TypeExtension.IsScalarType(data.GetType()))
							{
								if(data.GetType().IsArray)
									value = ((Array)data).GetValue(index) ?? throw new InvalidOperationException($"The required '{reference.Name}' reference of sequence is not included in the data.");
								else
									value = data.ToString();
							}
							else
							{
								if(Reflection.Reflector.GetValue(ref data, reference.Name) == null && !GetRequiredValue(context, reference, out value))
									throw new InvalidOperationException($"The required '{reference.Name}' reference of sequence is not included in the data.");
							}

							break;
					}

					if(index++ == 0)
						key += ":";
					else
						key += "-";

					key += value.ToString().Trim();
				}
			}

			return key;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static bool GetRequiredValue(IDataMutateContextBase context, IDataEntitySimplexProperty property, out object value)
		{
			value = null;
			var validator = context.Options.ValidatorSuppressed ? null : context.Validator;
			return validator != null && validator.OnInsert(context, property, out value);
		}
		#endregion
	}
	#endregion
}
