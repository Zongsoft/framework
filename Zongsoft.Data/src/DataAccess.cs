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

namespace Zongsoft.Data
{
	public class DataAccess : DataAccessBase
	{
		#region 成员字段
		private readonly IDataProvider _provider;
		#endregion

		#region 构造函数
		public DataAccess(string name) : this(name, null) { }
		public DataAccess(string name, IEnumerable<object> filters) : base(name)
		{
			if(filters != null)
			{
				foreach(var filter in filters)
				{
					if(filter != null)
						this.Filters.Add(filter);
				}
			}

			_provider = DataEnvironment.Providers.GetProvider(this.Name);
			if(_provider != null)
				_provider.Error += this.Provider_Error;
		}
		#endregion

		#region 公共属性
		public IDataProvider Provider => _provider;
		public override IDataMetadataContainer Metadata => _provider.Metadata;
		#endregion

		#region 执行方法
		protected override void OnExecute(DataExecuteContextBase context) => this.Provider.Execute((IDataAccessContext)context);
		protected override Task OnExecuteAsync(DataExecuteContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
		#endregion

		#region 导入方法
		protected override void OnImport(DataImportContextBase context) => this.Provider.Import((DataImportContext)context);
		protected override ValueTask OnImportAsync(DataImportContextBase context, CancellationToken cancellation = default) => this.Provider.ImportAsync((DataImportContext)context, cancellation);
		#endregion

		#region 存在方法
		protected override void OnExists(DataExistContextBase context) => this.Provider.Execute((IDataAccessContext)context);
		protected override Task OnExistsAsync(DataExistContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
		#endregion

		#region 聚合方法
		protected override void OnAggregate(DataAggregateContextBase context) => this.Provider.Execute((IDataAccessContext)context);
		protected override Task OnAggregateAsync(DataAggregateContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
		#endregion

		#region 递增方法
		protected override void OnIncrement(DataIncrementContextBase context) => this.Provider.Execute((IDataAccessContext)context);
		protected override Task OnIncrementAsync(DataIncrementContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
		#endregion

		#region 删除方法
		protected override void OnDelete(DataDeleteContextBase context) => this.Provider.Execute((IDataAccessContext)context);
		protected override Task OnDeleteAsync(DataDeleteContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
		#endregion

		#region 插入方法
		protected override void OnInsert(DataInsertContextBase context) => this.Provider.Execute((IDataAccessContext)context);
		protected override Task OnInsertAsync(DataInsertContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
		#endregion

		#region 增改方法
		protected override void OnUpsert(DataUpsertContextBase context) => this.Provider.Execute((IDataAccessContext)context);
		protected override Task OnUpsertAsync(DataUpsertContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
		#endregion

		#region 更新方法
		protected override void OnUpdate(DataUpdateContextBase context) => this.Provider.Execute((IDataAccessContext)context);
		protected override Task OnUpdateAsync(DataUpdateContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
		#endregion

		#region 查询方法
		protected override void OnSelect(DataSelectContextBase context) => this.Provider.Execute((IDataAccessContext)context);
		protected override Task OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation) => this.Provider.ExecuteAsync((IDataAccessContext)context, cancellation);
		#endregion

		#region 模式解析
		protected override ISchemaParser CreateSchema() => new SchemaParser(this.Provider);
		#endregion

		#region 序号构建
		protected override ISequence CreateSequence()
		{
			var sequence = base.CreateSequence();

			if(sequence == null)
				return null;

			return new DataSequenceProvider(this.Provider, sequence);
		}
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

		protected override DataExecuteContextBase CreateExecuteContext(string name, bool isScalar, Type resultType, IDictionary<string, object> inParameters, IDataExecuteOptions options) =>
			new DataExecuteContext(this, name, isScalar, resultType, inParameters, null, options);

		protected override DataAggregateContextBase CreateAggregateContext(string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options) =>
			new DataAggregateContext(this, name, aggregate, criteria.Flatten(), options);

		protected override DataIncrementContextBase CreateIncrementContext(string name, string member, ICondition criteria, int interval, IDataIncrementOptions options) =>
			new DataIncrementContext(this, name, member, criteria.Flatten(), interval, options);

		protected override DataImportContextBase CreateImportContext(string name, IEnumerable data, IEnumerable<string> members, IDataImportOptions options) =>
			new DataImportContext(this, name, data, members, options);

		protected override DataDeleteContextBase CreateDeleteContext(string name, ICondition criteria, ISchema schema, IDataDeleteOptions options) =>
			new DataDeleteContext(this, name, criteria.Flatten(), schema, options);

		protected override DataInsertContextBase CreateInsertContext(string name, bool isMultiple, object data, ISchema schema, IDataInsertOptions options) =>
			new DataInsertContext(this, name, isMultiple, data, schema, options);

		protected override DataUpsertContextBase CreateUpsertContext(string name, bool isMultiple, object data, ISchema schema, IDataUpsertOptions options) =>
			new DataUpsertContext(this, name, isMultiple, data, schema, options);

		protected override DataUpdateContextBase CreateUpdateContext(string name, bool isMultiple, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options) =>
			new DataUpdateContext(this, name, isMultiple, data, criteria.Flatten(), schema, options);

		protected override DataSelectContextBase CreateSelectContext(string name, Type entityType, ICondition criteria, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options) =>
			new DataSelectContext(this, name, entityType, grouping, criteria.Flatten(), schema, paging, sortings, options);
		#endregion

		#region 内部方法
		internal long Increase(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data)
		{
			if(this.Sequence == null)
				throw new InvalidOperationException($"Missing required sequence of the '{this.Name}' DataAccess.");

			return ((DataSequenceProvider)this.Sequence).Increase(context, sequence, data);
		}

		internal Task<long> IncreaseAsync(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data, CancellationToken cancellation)
		{
			if(this.Sequence == null)
				throw new InvalidOperationException($"Missing required sequence of the '{this.Name}' DataAccess.");

			return ((DataSequenceProvider)this.Sequence).IncreaseAsync(context, sequence, data, cancellation);
		}
		#endregion

		#region 异常事件
		private void Provider_Error(object sender, DataAccessErrorEventArgs args) => this.OnError(args);
		#endregion

		#region 嵌套子类
		private class DataSequenceProvider : ISequence
		{
			#region 常量定义
			private const string SEQUENCE_KEY = "Zongsoft.Sequence:";
			#endregion

			#region 成员字段
			private readonly ISequence _sequence;
			private readonly IDataProvider _provider;
			#endregion

			#region 构造函数
			public DataSequenceProvider(IDataProvider provider, ISequence sequence)
			{
				_provider = provider ?? throw new ArgumentNullException(nameof(provider));
				_sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
			}
			#endregion

			#region 公共方法
			public long Increase(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data)
			{
				if(sequence == null)
					throw new ArgumentNullException(nameof(sequence));

				return _sequence.Increase(this.GetSequenceKey(context, sequence, data), sequence.Interval, sequence.Seed);
			}

			public Task<long> IncreaseAsync(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data, CancellationToken cancellation)
			{
				if(sequence == null)
					throw new ArgumentNullException(nameof(sequence));

				return _sequence.IncreaseAsync(this.GetSequenceKey(context, sequence, data), sequence.Interval, sequence.Seed, null, cancellation);
			}
			#endregion

			#region 显式实现
			long ISequence.Increase(string key, int interval, int seed, TimeSpan? expiry)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.Increase(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed,
					expiry);
			}

			double ISequence.Increase(string key, double interval, double seed, TimeSpan? expiry)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.Increase(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed,
					expiry);
			}

			long ISequence.Decrease(string key, int interval, int seed, TimeSpan? expiry)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.Decrease(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed,
					expiry);
			}

			double ISequence.Decrease(string key, double interval, double seed, TimeSpan? expiry)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.Decrease(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed,
					expiry);
			}

			Task<long> ISequence.IncreaseAsync(string key, int interval, int seed, TimeSpan? expiry, CancellationToken cancellation)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.IncreaseAsync(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed,
					expiry,
					cancellation);
			}

			Task<double> ISequence.IncreaseAsync(string key, double interval, double seed, TimeSpan? expiry, CancellationToken cancellation)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.IncreaseAsync(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed,
					expiry,
					cancellation);
			}

			Task<long> ISequence.DecreaseAsync(string key, int interval, int seed, TimeSpan? expiry, CancellationToken cancellation)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.DecreaseAsync(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed,
					expiry,
					cancellation);
			}

			Task<double> ISequence.DecreaseAsync(string key, double interval, double seed, TimeSpan? expiry, CancellationToken cancellation)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.DecreaseAsync(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed,
					expiry,
					cancellation);
			}

			void ISequence.Reset(string key, int value, TimeSpan? expiry)
			{
				key = this.GetSequenceKey(key, out var sequence);
				_sequence.Reset(key, value == 0 ? sequence.Seed : value, expiry);
			}

			void ISequence.Reset(string key, double value, TimeSpan? expiry)
			{
				key = this.GetSequenceKey(key, out var sequence);
				_sequence.Reset(key, value == 0 ? sequence.Seed : value, expiry);
			}

			Task ISequence.ResetAsync(string key, int value, TimeSpan? expiry, CancellationToken cancellation)
			{
				key = this.GetSequenceKey(key, out var sequence);
				return _sequence.ResetAsync(key, value == 0 ? sequence.Seed : value, expiry, cancellation);
			}

			Task ISequence.ResetAsync(string key, double value, TimeSpan? expiry, CancellationToken cancellation)
			{
				key = this.GetSequenceKey(key, out var sequence);
				return _sequence.ResetAsync(key, value == 0 ? sequence.Seed : value, expiry, cancellation);
			}
			#endregion

			#region 私有方法
			private string GetSequenceKey(string key, out IDataEntityPropertySequence sequence)
			{
				sequence = null;

				if(string.IsNullOrEmpty(key))
					throw new ArgumentNullException(nameof(key));

				var index = key.LastIndexOfAny(new[] { ':', '.', '@' });
				object data = null;

				if(index > 0 && key[index] == '@')
				{
					data = key.Substring(index + 1).Split(',', '|', '-');
					index = key.LastIndexOfAny(new[] { ':', '.' }, index);
				}

				if(index < 0)
					throw new ArgumentException($"Invalid sequence key, the sequence key must separate the entity name and property name with a colon or a dot.");

				if(!_provider.Metadata.Entities.TryGetValue(key.Substring(0, index), out var entity))
					throw new ArgumentException($"The '{key.Substring(0, index)}' entity specified in the sequence key does not exist.");

				if(!entity.Properties.TryGet(key.Substring(index + 1), out var found) || found.IsComplex)
					throw new ArgumentException($"The '{key.Substring(index + 1)}' property specified in the sequence key does not exist or is not a simplex property.");

				sequence = ((IDataEntitySimplexProperty)found).Sequence;

				if(sequence == null)
					throw new ArgumentException($"The '{found.Name}' property specified in the sequence key is undefined.");

				return this.GetSequenceKey(null, sequence, data);
			}

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private string GetSequenceKey(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data)
			{
				var key = SEQUENCE_KEY + sequence.Property.Entity.Name + "." + sequence.Property.Name;

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
								if(!model.TryGetValue(reference.Name, out value) && !this.GetRequiredValue(context, reference, out value))
									throw new InvalidOperationException($"The required '{reference.Name}' reference of sequence is not included in the data.");

								break;
							case IDictionary<string, object> genericDictionary:
								if(!genericDictionary.TryGetValue(reference.Name, out value) && !this.GetRequiredValue(context, reference, out value))
									throw new InvalidOperationException($"The required '{reference.Name}' reference of sequence is not included in the data.");

								break;
							case IDictionary classicDictionary:
								if(!classicDictionary.Contains(reference.Name) && !this.GetRequiredValue(context, reference, out value))
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
									if(Reflection.Reflector.GetValue(ref data, reference.Name) == null && !this.GetRequiredValue(context, reference, out value))
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
			private bool GetRequiredValue(IDataMutateContextBase context, IDataEntitySimplexProperty property, out object value)
			{
				value = null;
				var validator = context.Options.ValidatorSuppressed ? null : context.Validator;
				return validator != null && validator.OnInsert(context, property, out value);
			}
			#endregion
		}
		#endregion
	}
}
