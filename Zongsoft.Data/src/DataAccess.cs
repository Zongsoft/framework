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
		private IDataProvider _provider;
		#endregion

		#region 构造函数
		public DataAccess(string name) : base(name)
		{
		}

		public DataAccess(string name, IEnumerable<IDataAccessFilter> filters) : base(name)
		{
			if(filters != null)
			{
				foreach(var filter in filters)
				{
					if(filter != null)
						this.Filters.Add(filter);
				}
			}
		}
		#endregion

		#region 公共属性
		public override IDataMetadataContainer Metadata
		{
			get => this.Provider.Metadata;
		}

		public IDataProvider Provider
		{
			get
			{
				if(_provider == null)
				{
					_provider = DataEnvironment.Providers.GetProvider(this.Name);

					if(_provider != null)
						_provider.Error += this.Provider_Error;
				}

				return _provider;
			}
		}

		private void Provider_Error(object sender, DataAccessErrorEventArgs args)
		{
			this.OnError(args);
		}
		#endregion

		#region 执行方法
		protected override void OnExecute(DataExecuteContextBase context)
		{
			this.Provider.Execute((IDataAccessContext)context);
		}
		#endregion

		#region 存在方法
		protected override void OnExists(DataExistContextBase context)
		{
			this.Provider.Execute((IDataAccessContext)context);
		}
		#endregion

		#region 计数方法
		protected override void OnCount(DataCountContextBase context)
		{
			this.Provider.Execute((IDataAccessContext)context);
		}
		#endregion

		#region 递增方法
		protected override void OnIncrement(DataIncrementContextBase context)
		{
			this.Provider.Execute((IDataAccessContext)context);
		}
		#endregion

		#region 删除方法
		protected override void OnDelete(DataDeleteContextBase context)
		{
			this.Provider.Execute((IDataAccessContext)context);
		}
		#endregion

		#region 插入方法
		protected override void OnInsert(DataInsertContextBase context)
		{
			this.Provider.Execute((IDataAccessContext)context);
		}
		#endregion

		#region 复写方法
		protected override void OnUpsert(DataUpsertContextBase context)
		{
			this.Provider.Execute((IDataAccessContext)context);
		}
		#endregion

		#region 更新方法
		protected override void OnUpdate(DataUpdateContextBase context)
		{
			this.Provider.Execute((IDataAccessContext)context);
		}
		#endregion

		#region 查询方法
		protected override void OnSelect(DataSelectContextBase context)
		{
			this.Provider.Execute((IDataAccessContext)context);
		}
		#endregion

		#region 模式解析
		protected override ISchemaParser CreateSchema()
		{
			return new SchemaParser(this.Provider);
		}
		#endregion

		#region 序号构建
		protected override ISequence CreateSequence()
		{
			var sequence = base.CreateSequence();

			if(sequence == null)
				return null;

			return new DataSequenceProvider(this, this.Provider, sequence);
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
		protected override DataCountContextBase CreateCountContext(string name, ICondition condition, string member, IDictionary<string, object> states)
		{
			return new DataCountContext(this, name, condition, member, states);
		}

		protected override DataExistContextBase CreateExistContext(string name, ICondition condition, IDictionary<string, object> states)
		{
			return new DataExistContext(this, name, condition, states);
		}

		protected override DataExecuteContextBase CreateExecuteContext(string name, bool isScalar, Type resultType, IDictionary<string, object> inParameters, IDictionary<string, object> states)
		{
			return new DataExecuteContext(this, name, isScalar, resultType, inParameters, null, states);
		}

		protected override DataIncrementContextBase CreateIncrementContext(string name, string member, ICondition condition, int interval, IDictionary<string, object> states)
		{
			return new DataIncrementContext(this, name, member, condition, interval, states);
		}

		protected override DataDeleteContextBase CreateDeleteContext(string name, ICondition condition, ISchema schema, IDictionary<string, object> states)
		{
			return new DataDeleteContext(this, name, condition, schema, states);
		}

		protected override DataInsertContextBase CreateInsertContext(string name, bool isMultiple, object data, ISchema schema, IDictionary<string, object> states)
		{
			return new DataInsertContext(this, name, isMultiple, data, schema, states);
		}

		protected override DataUpsertContextBase CreateUpsertContext(string name, bool isMultiple, object data, ISchema schema, IDictionary<string, object> states)
		{
			return new DataUpsertContext(this, name, isMultiple, data, schema, states);
		}

		protected override DataUpdateContextBase CreateUpdateContext(string name, bool isMultiple, object data, ICondition condition, ISchema schema, IDictionary<string, object> states)
		{
			return new DataUpdateContext(this, name, isMultiple, data, condition, schema, states);
		}

		protected override DataSelectContextBase CreateSelectContext(string name, Type entityType, ICondition condition, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDictionary<string, object> states)
		{
			return new DataSelectContext(this, name, entityType, grouping, condition, schema, paging, sortings, states);
		}
		#endregion

		#region 内部方法
		internal long Increase(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data)
		{
			if(this.Sequence == null)
				throw new InvalidOperationException($"Missing required sequence of the '{this.Name}' DataAccess.");

			return ((DataSequenceProvider)this.Sequence).Increase(context, sequence, data);
		}
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
			private readonly IDataAccess _dataAccess;
			#endregion

			#region 构造函数
			public DataSequenceProvider(IDataAccess dataAccess, IDataProvider provider, ISequence sequence)
			{
				_dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
				_provider = provider ?? throw new ArgumentNullException(nameof(provider));
				_sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
			}
			#endregion

			#region 公共方法
			public long Increase(IDataMutateContextBase context, IDataEntityPropertySequence sequence, object data)
			{
				if(sequence == null)
					throw new ArgumentNullException(nameof(sequence));

				return _sequence.Increment(this.GetSequenceKey(context, sequence, data), sequence.Interval, sequence.Seed);
			}
			#endregion

			#region 显式实现
			long ISequence.Increment(string key, int interval, int seed)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.Increment(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed);
			}

			long ISequence.Decrement(string key, int interval, int seed)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.Decrement(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed);
			}

			void ISequence.Reset(string key, int value)
			{
				key = this.GetSequenceKey(key, out var sequence);
				_sequence.Reset(key, value == 0 ? sequence.Seed : value);
			}

			Task<long> ISequence.IncrementAsync(string key, int interval, int seed, CancellationToken cancellation)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.IncrementAsync(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed,
					cancellation);
			}

			Task<long> ISequence.DecrementAsync(string key, int interval, int seed, CancellationToken cancellation)
			{
				key = this.GetSequenceKey(key, out var sequence);

				return _sequence.DecrementAsync(key,
					interval == 1 ? sequence.Interval : interval,
					seed == 0 ? sequence.Seed : seed,
					cancellation);
			}

			Task ISequence.ResetAsync(string key, int value, CancellationToken cancellation)
			{
				key = this.GetSequenceKey(key, out var sequence);
				return _sequence.ResetAsync(key, value == 0 ? sequence.Seed : value, cancellation);
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

				if(!_provider.Metadata.Entities.TryGet(key.Substring(0, index), out var entity))
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
									if(Reflection.Reflector.GetValue(data, reference.Name) == null && !this.GetRequiredValue(context, reference, out value))
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
				var validator = _dataAccess.Validator;
				return validator != null && validator.OnInsert(context, property, out value);
			}
			#endregion
		}
		#endregion
	}
}
