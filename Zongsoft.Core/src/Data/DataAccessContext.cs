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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	public class DataExistContextBase : DataAccessContextBase<IDataExistsOptions>
	{
		#region 构造函数
		protected DataExistContextBase(IDataAccess dataAccess, string name, ICondition criteria, IDataExistsOptions options = null) : base(dataAccess, name, DataAccessMethod.Exists, options ?? new DataExistsOptions())
		{
			this.Criteria = criteria;
			this.Entity = DataContextUtility.GetEntity(name, dataAccess.Metadata);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问对应的实体元数据。
		/// </summary>
		public Metadata.IDataEntity Entity { get; }

		/// <summary>
		/// 获取或设置判断操作的条件。
		/// </summary>
		public ICondition Criteria { get; set; }

		/// <summary>
		/// 获取判断操作的结果，即指定条件的数据是否存在。
		/// </summary>
		public bool Result { get; set; }

		/// <summary>
		/// 获取当前判断操作的验证器。
		/// </summary>
		public virtual IDataValidator Validator
		{
			get => this.DataAccess.Validator;
		}
		#endregion

		#region 公共方法
		public ICondition Validate(ICondition criteria = null)
		{
			var validator = this.Options.ValidatorSuppressed ? null : this.Validator;

			return validator == null ?
				criteria ?? this.Criteria :
				validator.Validate(this, criteria ?? this.Criteria);
		}
		#endregion
	}

	public class DataExecuteContextBase : DataAccessContextBase<IDataExecuteOptions>
	{
		#region 成员字段
		private Type _resultType;
		#endregion

		#region 构造函数
		protected DataExecuteContextBase(IDataAccess dataAccess, string name, bool isScalar, Type resultType, IDictionary<string, object> inParameters, IDictionary<string, object> outParameters, IDataExecuteOptions options = null) : base(dataAccess, name, DataAccessMethod.Execute, options ?? new DataExecuteOptions())
		{
			_resultType = resultType ?? throw new ArgumentNullException(nameof(resultType));
			this.IsScalar = isScalar;
			this.InParameters = inParameters;
			this.OutParameters = outParameters;
			this.Command = DataContextUtility.GetCommand(name, dataAccess.Metadata);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问对应的命令元数据。
		/// </summary>
		public Metadata.IDataCommand Command { get; }

		/// <summary>
		/// 获取一个值，指示是否为返回单值。
		/// </summary>
		public bool IsScalar { get; }

		/// <summary>
		/// 获取或设置执行结果的类型。
		/// </summary>
		public Type ResultType
		{
			get => _resultType;
			set => _resultType = value ?? throw new ArgumentNullException();
		}

		/// <summary>
		/// 获取或设置执行操作的结果。
		/// </summary>
		public object Result { get; set; }

		/// <summary>
		/// 获取执行操作的输入参数。
		/// </summary>
		public IDictionary<string, object> InParameters { get; }

		/// <summary>
		/// 获取或设置执行操作的输出参数。
		/// </summary>
		public IDictionary<string, object> OutParameters { get; set; }
		#endregion
	}

	public class DataAggregateContextBase : DataAccessContextBase<IDataAggregateOptions>
	{
		#region 构造函数
		protected DataAggregateContextBase(IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options = null) : base(dataAccess, name, DataAccessMethod.Aggregate, options ?? new DataAggregateOptions())
		{
			this.Criteria = criteria;
			this.Aggregate = aggregate;
			this.Entity = DataContextUtility.GetEntity(name, dataAccess.Metadata);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问对应的实体元数据。
		/// </summary>
		public Metadata.IDataEntity Entity { get; }

		/// <summary>
		/// 获取或设置聚合操作的结果。
		/// </summary>
		public double? Result { get; set; }

		/// <summary>
		/// 获取或设置聚合操作的条件。
		/// </summary>
		public ICondition Criteria { get; set; }

		/// <summary>
		/// 获取聚合操作的聚合元素。
		/// </summary>
		public DataAggregate Aggregate { get; }

		/// <summary>
		/// 获取当前聚合操作的验证器。
		/// </summary>
		public virtual IDataValidator Validator
		{
			get => this.DataAccess.Validator;
		}
		#endregion

		#region 公共方法
		public ICondition Validate(ICondition criteria = null)
		{
			var validator = this.Options.ValidatorSuppressed ? null : this.Validator;

			return validator == null ?
				criteria ?? this.Criteria :
				validator.Validate(this, criteria ?? this.Criteria);
		}
		#endregion
	}

	public class DataIncrementContextBase : DataAccessContextBase<IDataIncrementOptions>, IDataMutateContextBase
	{
		#region 成员字段
		private string _member;
		private ICondition _criteria;
		#endregion

		#region 构造函数
		protected DataIncrementContextBase(IDataAccess dataAccess, string name, string member, ICondition criteria, int interval = 1, IDataIncrementOptions options = null) : base(dataAccess, name, DataAccessMethod.Increment, options ?? new DataIncrementOptions())
		{
			if(string.IsNullOrEmpty(member))
				throw new ArgumentNullException(nameof(member));

			_member = member;
			_criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));

			this.Interval = interval;
			this.Entity = DataContextUtility.GetEntity(name, dataAccess.Metadata);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问对应的实体元数据。
		/// </summary>
		public Metadata.IDataEntity Entity { get; }

		public int Count { get; set; }

		public string Member
		{
			get => _member;
			set => _member = string.IsNullOrWhiteSpace(value) ? throw new ArgumentNullException() : value;
		}

		public ICondition Criteria
		{
			get => _criteria;
			set => _criteria = value ?? throw new ArgumentNullException();
		}

		public int Interval { get; set; }

		public long Result { get; set; }

		/// <summary>
		/// 获取写入操作的选项对象。
		/// </summary>
		IDataMutateOptions IDataMutateContextBase.Options { get => this.Options; }

		/// <summary>
		/// 获取当前递增(减)操作的验证器。
		/// </summary>
		public virtual IDataValidator Validator
		{
			get => this.DataAccess.Validator;
		}
		#endregion

		#region 公共方法
		public ICondition Validate(ICondition criteria = null)
		{
			var validator = this.Options.ValidatorSuppressed ? null : this.Validator;

			return validator == null ?
				criteria ?? this.Criteria :
				validator.Validate(this, criteria ?? this.Criteria);
		}
		#endregion

		#region 显式实现
		object IDataMutateContextBase.Data
		{
			get => this.Interval;
			set
			{
				if(Common.Convert.TryConvertValue<int>(value, out var interval))
					this.Interval = interval;
			}
		}

		bool IDataMutateContextBase.IsMultiple
		{
			get => false;
		}
		#endregion
	}

	public class DataSelectContextBase : DataAccessContextBase<IDataSelectOptions>
	{
		#region 委托定义
		public delegate bool FilterDelegate(DataSelectContextBase context, ref object data);
		#endregion

		#region 成员字段
		private IEnumerable _result;
		#endregion

		#region 构造函数
		protected DataSelectContextBase(IDataAccess dataAccess, string name, Type modelType, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options = null) : base(dataAccess, name, DataAccessMethod.Select, options ?? new DataSelectOptions())
		{
			this.Grouping = grouping;
			this.Criteria = criteria;
			this.Schema = schema;
			this.Paging = paging;
			this.Sortings = sortings;
			this.Entity = DataContextUtility.GetEntity(name, dataAccess.Metadata);
			this.ModelType = modelType ?? typeof(object);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问对应的实体元数据。
		/// </summary>
		public Metadata.IDataEntity Entity { get; }

		/// <summary>
		/// 获取查询要返回的结果集元素类型。
		/// </summary>
		public Type ModelType { get; }

		/// <summary>
		/// 获取或设置查询操作的条件。
		/// </summary>
		public ICondition Criteria { get; set; }

		/// <summary>
		/// 获取或设置查询操作的结果数据模式（即查询结果的形状结构）。
		/// </summary>
		public ISchema Schema { get; set; }

		/// <summary>
		/// 获取或设置查询操作的分组。
		/// </summary>
		public Grouping Grouping { get; set; }

		/// <summary>
		/// 获取或设置查询操作的分页设置。
		/// </summary>
		public Paging Paging { get; set; }

		/// <summary>
		/// 获取或设置查询操作的排序设置。
		/// </summary>
		public Sorting[] Sortings { get; set; }

		/// <summary>
		/// 获取或设置查询结果的过滤器。
		/// </summary>
		public FilterDelegate ResultFilter { get; set; }

		/// <summary>
		/// 获取或设置查询操作的结果集。
		/// </summary>
		public IEnumerable Result
		{
			get
			{
				bool OnFilter(ref object data)
				{
					return ResultFilter(this, ref data);
				}

				if(ResultFilter != null && _result is IPageable source)
					Pageable.Filter(source, this.ModelType, OnFilter);

				return _result;
			}
			set => _result = value ?? throw new ArgumentNullException();
		}

		/// <summary>
		/// 获取当前查询操作的验证器。
		/// </summary>
		public virtual IDataValidator Validator
		{
			get => this.DataAccess.Validator;
		}
		#endregion

		#region 公共方法
		public ICondition Validate(ICondition criteria = null)
		{
			var validator = this.Options.ValidatorSuppressed ? null : this.Validator;

			return validator == null ?
				criteria ?? this.Criteria :
				validator.Validate(this, criteria ?? this.Criteria);
		}
		#endregion
	}

	public class DataDeleteContextBase : DataAccessContextBase<IDataDeleteOptions>, IDataMutateContextBase
	{
		#region 构造函数
		protected DataDeleteContextBase(IDataAccess dataAccess, string name, ICondition criteria, ISchema schema, IDataDeleteOptions options = null) : base(dataAccess, name, DataAccessMethod.Delete, options ?? new DataDeleteOptions())
		{
			this.Criteria = criteria;
			this.Schema = schema;
			this.Entity = DataContextUtility.GetEntity(name, dataAccess.Metadata);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问对应的实体元数据。
		/// </summary>
		public Metadata.IDataEntity Entity { get; }

		/// <summary>
		/// 获取或设置删除操作的受影响记录数。
		/// </summary>
		public int Count { get; set; }

		/// <summary>
		/// 获取或设置删除操作的条件。
		/// </summary>
		public ICondition Criteria { get; set; }

		/// <summary>
		/// 获取或设置删除操作的数据模式（即删除数据的形状结构）。
		/// </summary>
		public ISchema Schema { get; set; }

		/// <summary>
		/// 获取写入操作的选项对象。
		/// </summary>
		IDataMutateOptions IDataMutateContextBase.Options { get => this.Options; }

		/// <summary>
		/// 获取当前删除操作的验证器。
		/// </summary>
		public virtual IDataValidator Validator
		{
			get => this.DataAccess.Validator;
		}
		#endregion

		#region 公共方法
		public ICondition Validate(ICondition criteria = null)
		{
			var validator = this.Options.ValidatorSuppressed ? null : this.Validator;

			return validator == null ?
				criteria ?? this.Criteria :
				validator.Validate(this, criteria ?? this.Criteria);
		}
		#endregion

		#region 显式实现
		object IDataMutateContextBase.Data
		{
			get => null;
			set { }
		}

		bool IDataMutateContextBase.IsMultiple
		{
			get => false;
		}
		#endregion
	}

	public class DataInsertContextBase : DataAccessContextBase<IDataInsertOptions>, IDataMutateContextBase
	{
		#region 构造函数
		protected DataInsertContextBase(IDataAccess dataAccess, string name, bool isMultiple, object data, ISchema schema, IDataInsertOptions options = null) : base(dataAccess, name, DataAccessMethod.Insert, options ?? new DataInsertOptions())
		{
			this.Data = data ?? throw new ArgumentNullException(nameof(data));
			this.Schema = schema;
			this.IsMultiple = isMultiple;
			this.Entity = DataContextUtility.GetEntity(name, dataAccess.Metadata);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问对应的实体元数据。
		/// </summary>
		public Metadata.IDataEntity Entity { get; }

		/// <summary>
		/// 获取一个值，指示是否为批量新增操作。
		/// </summary>
		public bool IsMultiple { get; }

		/// <summary>
		/// 获取或设置插入操作的受影响记录数。
		/// </summary>
		public int Count { get; set; }

		/// <summary>
		/// 获取或设置插入操作的数据。
		/// </summary>
		public object Data { get; set; }

		/// <summary>
		/// 获取或设置插入操作的数据模式（即插入的数据形状结构）。
		/// </summary>
		public ISchema Schema { get; set; }

		/// <summary>
		/// 获取写入操作的选项对象。
		/// </summary>
		IDataMutateOptions IDataMutateContextBase.Options { get => this.Options; }

		/// <summary>
		/// 获取插入数据的元素类型。
		/// </summary>
		public virtual Type ModelType
		{
			get
			{
				if(IsMultiple && Data is IEnumerable)
					return Common.TypeExtension.GetElementType(Data.GetType());

				return Data.GetType();
			}
		}

		/// <summary>
		/// 获取当前新增操作的验证器。
		/// </summary>
		public virtual IDataValidator Validator
		{
			get => this.DataAccess.Validator;
		}
		#endregion
	}

	public class DataUpdateContextBase : DataAccessContextBase<IDataUpdateOptions>, IDataMutateContextBase
	{
		#region 构造函数
		protected DataUpdateContextBase(IDataAccess dataAccess, string name, bool isMultiple, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options = null) : base(dataAccess, name, DataAccessMethod.Update, options ?? new DataUpdateOptions())
		{
			this.Data = data ?? throw new ArgumentNullException(nameof(data));
			this.Criteria = criteria;
			this.Schema = schema;
			this.IsMultiple = isMultiple;
			this.Entity = DataContextUtility.GetEntity(name, dataAccess.Metadata);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问对应的实体元数据。
		/// </summary>
		public Metadata.IDataEntity Entity { get; }

		/// <summary>
		/// 获取一个值，指示是否为批量更新操作。
		/// </summary>
		public bool IsMultiple { get; }

		/// <summary>
		/// 获取或设置更新操作的受影响记录数。
		/// </summary>
		public int Count { get; set; }

		/// <summary>
		/// 获取或设置更新操作的数据。
		/// </summary>
		public object Data { get; set; }

		/// <summary>
		/// 获取或设置更新操作的条件。
		/// </summary>
		public ICondition Criteria { get; set; }

		/// <summary>
		/// 获取或设置更新操作的数据模式（即更新的数据形状结构）。
		/// </summary>
		public ISchema Schema { get; set; }

		/// <summary>
		/// 获取写入操作的选项对象。
		/// </summary>
		IDataMutateOptions IDataMutateContextBase.Options { get => this.Options; }

		/// <summary>
		/// 获取更新数据的元素类型。
		/// </summary>
		public virtual Type ModelType
		{
			get
			{
				if(IsMultiple && Data is IEnumerable)
					return Common.TypeExtension.GetElementType(Data.GetType());

				return Data.GetType();
			}
		}

		/// <summary>
		/// 获取当前更新操作的验证器。
		/// </summary>
		public virtual IDataValidator Validator
		{
			get => this.DataAccess.Validator;
		}
		#endregion

		#region 公共方法
		public ICondition Validate(ICondition criteria = null)
		{
			var validator = this.Options.ValidatorSuppressed ? null : this.Validator;

			return validator == null ?
				criteria ?? this.Criteria :
				validator.Validate(this, criteria ?? this.Criteria);
		}
		#endregion
	}

	public class DataUpsertContextBase : DataAccessContextBase<IDataUpsertOptions>, IDataMutateContextBase
	{
		#region 构造函数
		protected DataUpsertContextBase(IDataAccess dataAccess, string name, bool isMultiple, object data, ISchema schema, IDataUpsertOptions options = null) : base(dataAccess, name, DataAccessMethod.Upsert, options ?? new DataUpsertOptions())
		{
			this.Data = data ?? throw new ArgumentNullException(nameof(data));
			this.Schema = schema;
			this.IsMultiple = isMultiple;
			this.Entity = DataContextUtility.GetEntity(name, dataAccess.Metadata);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问对应的实体元数据。
		/// </summary>
		public Metadata.IDataEntity Entity { get; }

		/// <summary>
		/// 获取一个值，指示是否为批量操作。
		/// </summary>
		public bool IsMultiple { get; }

		/// <summary>
		/// 获取或设置操作的受影响记录数。
		/// </summary>
		public int Count { get; set; }

		/// <summary>
		/// 获取或设置操作的数据。
		/// </summary>
		public object Data { get; set; }

		/// <summary>
		/// 获取或设置操作的数据模式（即更新或新增的数据形状结构）。
		/// </summary>
		public ISchema Schema { get; set; }

		/// <summary>
		/// 获取写入操作的选项对象。
		/// </summary>
		IDataMutateOptions IDataMutateContextBase.Options { get => this.Options; }

		/// <summary>
		/// 获取操作数据的元素类型。
		/// </summary>
		public virtual Type ModelType
		{
			get
			{
				if(IsMultiple && Data is IEnumerable)
					return Common.TypeExtension.GetElementType(Data.GetType());

				return Data.GetType();
			}
		}

		/// <summary>
		/// 获取当前写入操作的验证器。
		/// </summary>
		public virtual IDataValidator Validator
		{
			get => this.DataAccess.Validator;
		}
		#endregion
	}

	internal static class DataContextUtility
	{
		public static Metadata.IDataEntity GetEntity(string name, Metadata.IDataMetadataContainer metadata)
		{
			if(metadata.Entities.TryGet(name, out var entity))
				return entity;

			throw new DataException($"The specified '{name}' entity mapping does not exist.");
		}

		public static Metadata.IDataCommand GetCommand(string name, Metadata.IDataMetadataContainer metadata)
		{
			if(metadata.Commands.TryGet(name, out var command))
				return command;

			throw new DataException($"The specified '{name}' command mapping does not exist.");
		}
	}

	#region 过滤遍历器类
	internal class DataFilterEnumerable<TContext, TEntity> : IEnumerable<TEntity> where TContext : IDataAccessContextBase
	{
		private TContext _context;
		private IEnumerable _source;
		private Func<TContext, object, bool> _filter;

		public DataFilterEnumerable(TContext context, IEnumerable source, Func<TContext, object, bool> filter)
		{
			if(context == null)
				throw new ArgumentNullException(nameof(context));
			if(source == null)
				throw new ArgumentNullException(nameof(source));

			_context = context;
			_source = source;
			_filter = filter;
		}

		public IEnumerator<TEntity> GetEnumerator()
		{
			return new DataFilterIterator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new DataFilterIterator(this);
		}

		private class DataFilterIterator : IEnumerator<TEntity>
		{
			private DataFilterEnumerable<TContext, TEntity> _owner;
			private IEnumerator _iterator;

			public DataFilterIterator(DataFilterEnumerable<TContext, TEntity> owner)
			{
				_owner = owner;
				_iterator = owner._source.GetEnumerator();
			}

			public TEntity Current
			{
				get
				{
					var iterator = _iterator;

					if(iterator == null)
						throw new ObjectDisposedException(this.GetType().FullName);

					return (TEntity)iterator.Current;
				}
			}

			object IEnumerator.Current
			{
				get
				{
					var iterator = _iterator;

					if(iterator == null)
						throw new ObjectDisposedException(this.GetType().FullName);

					return iterator.Current;
				}
			}

			public bool MoveNext()
			{
				var iterator = _iterator;

				if(iterator == null)
					throw new ObjectDisposedException(this.GetType().FullName);

				var filter = _owner._filter;

				while(iterator.MoveNext())
				{
					if(filter == null || filter(_owner._context, iterator.Current))
						return true;
				}

				return false;
			}

			public void Reset()
			{
				var iterator = _iterator;

				if(iterator == null)
					throw new ObjectDisposedException(this.GetType().FullName);

				iterator.Reset();
			}

			public void Dispose()
			{
				_iterator = null;
				_owner = null;
			}
		}
	}
	#endregion
}
