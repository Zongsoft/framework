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
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据访问上下文的接口。
	/// </summary>
	public interface IDataAccessContext : IDataAccessContextBase
	{
		/// <summary>获取当前上下文对应的数据源。</summary>
		IDataSource Source { get; }

		/// <summary>获取数据提供程序。</summary>
		IDataProvider Provider { get; }

		/// <summary>获取当前上下文关联的数据会话。</summary>
		DataSession Session { get; }
	}

	/// <summary>
	/// 表示数据写入操作上下文的接口。
	/// </summary>
	public interface IDataMutateContext : IDataAccessContext, IDataMutateContextBase { }

	public class DataExistContext : DataExistContextBase, IDataAccessContext, Common.Expressions.IAliasable
	{
		#region 构造函数
		public DataExistContext(IDataAccess dataAccess, string name, ICondition criteria, IDataExistsOptions options = null) : base(dataAccess, name, criteria, options)
		{
			this.Aliaser = new Common.Expressions.Aliaser();
			this.Provider = DataEnvironment.Providers.GetProvider(dataAccess.Name);
			this.Session = DataAccessContextUtility.GetSession(() => this.Provider.Multiplexer.GetSource(this));
		}
		#endregion

		#region 公共属性
		public Common.Expressions.Aliaser Aliaser { get; }

		public IDataSource Source
		{
			get => this.Session.Source;
		}

		public IDataProvider Provider { get; }

		public DataSession Session { get; }
		#endregion
	}

	public class DataExecuteContext : DataExecuteContextBase, IDataAccessContext, Common.Expressions.IAliasable
	{
		#region 构造函数
		public DataExecuteContext(IDataAccess dataAccess, string name, bool isScalar, Type resultType, IDictionary<string, object> inParameters, IDictionary<string, object> outParameters, IDataExecuteOptions options = null) : base(dataAccess, name, isScalar, resultType, inParameters, outParameters, options)
		{
			this.Aliaser = new Common.Expressions.Aliaser();
			this.Provider = DataEnvironment.Providers.GetProvider(dataAccess.Name);
			this.Session = DataAccessContextUtility.GetSession(() => this.Provider.Multiplexer.GetSource(this));
		}
		#endregion

		#region 公共属性
		public Common.Expressions.Aliaser Aliaser { get; }

		public IDataSource Source
		{
			get => this.Session.Source;
		}

		public IDataProvider Provider { get; }

		public DataSession Session { get; }
		#endregion
	}

	public class DataAggregateContext : DataAggregateContextBase, IDataAccessContext, Common.Expressions.IAliasable
	{
		#region 构造函数
		public DataAggregateContext(IDataAccess dataAccess, string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options = null) : base(dataAccess, name, aggregate, criteria, options)
		{
			this.Aliaser = new Common.Expressions.Aliaser();
			this.Provider = DataEnvironment.Providers.GetProvider(dataAccess.Name);
			this.Session = DataAccessContextUtility.GetSession(() => this.Provider.Multiplexer.GetSource(this));
		}
		#endregion

		#region 公共属性
		public Common.Expressions.Aliaser Aliaser { get; }

		public IDataSource Source
		{
			get => this.Session.Source;
		}

		public IDataProvider Provider { get; }

		public DataSession Session { get; }
		#endregion
	}

	public class DataIncrementContext : DataIncrementContextBase, IDataMutateContext, Common.Expressions.IAliasable
	{
		#region 构造函数
		public DataIncrementContext(IDataAccess dataAccess, string name, string member, ICondition criteria, int interval, IDataIncrementOptions options = null) : base(dataAccess, name, member, criteria, interval, options)
		{
			this.Aliaser = new Common.Expressions.Aliaser();
			this.Provider = DataEnvironment.Providers.GetProvider(dataAccess.Name);
			this.Session = DataAccessContextUtility.GetSession(() => this.Provider.Multiplexer.GetSource(this));
		}
		#endregion

		#region 公共属性
		public Common.Expressions.Aliaser Aliaser { get; }

		public IDataSource Source
		{
			get => this.Session.Source;
		}

		public IDataProvider Provider { get; }

		public DataSession Session { get; }
		#endregion
	}

	public class DataDeleteContext : DataDeleteContextBase, IDataMutateContext, Common.Expressions.IAliasable
	{
		#region 构造函数
		public DataDeleteContext(IDataAccess dataAccess, string name, ICondition criteria, ISchema schema, IDataDeleteOptions options = null) : base(dataAccess, name, criteria, schema, options)
		{
			this.Aliaser = new Common.Expressions.Aliaser();
			this.Provider = DataEnvironment.Providers.GetProvider(dataAccess.Name);
			this.Session = DataAccessContextUtility.GetSession(() => this.Provider.Multiplexer.GetSource(this));
		}
		#endregion

		#region 公共属性
		public Common.Expressions.Aliaser Aliaser { get; }

		public IDataSource Source
		{
			get => this.Session.Source;
		}

		public IDataProvider Provider { get; }

		public new Schema Schema
		{
			get => (Schema)base.Schema;
		}

		public DataSession Session { get; }
		#endregion
	}

	public class DataInsertContext : DataInsertContextBase, IDataMutateContext, Common.Expressions.IAliasable
	{
		#region 构造函数
		public DataInsertContext(IDataAccess dataAccess, string name, bool isMultiple, object data, ISchema schema, IDataInsertOptions options = null) : base(dataAccess, name, isMultiple, data, schema, options)
		{
			this.Aliaser = new Common.Expressions.Aliaser();
			this.Provider = DataEnvironment.Providers.GetProvider(dataAccess.Name);
			this.Session = DataAccessContextUtility.GetSession(() => this.Provider.Multiplexer.GetSource(this));
		}
		#endregion

		#region 公共属性
		public Common.Expressions.Aliaser Aliaser { get; }

		public IDataSource Source
		{
			get => this.Session.Source;
		}

		public IDataProvider Provider { get; }

		public new Schema Schema
		{
			get => (Schema)base.Schema;
		}

		public DataSession Session { get; }
		#endregion
	}

	public class DataUpdateContext : DataUpdateContextBase, IDataMutateContext, Common.Expressions.IAliasable
	{
		#region 构造函数
		public DataUpdateContext(IDataAccess dataAccess, string name, bool isMultiple, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options = null) : base(dataAccess, name, isMultiple, data, criteria, schema, options)
		{
			this.Aliaser = new Common.Expressions.Aliaser();
			this.Provider = DataEnvironment.Providers.GetProvider(dataAccess.Name);
			this.Session = DataAccessContextUtility.GetSession(() => this.Provider.Multiplexer.GetSource(this));
		}
		#endregion

		#region 公共属性
		public Common.Expressions.Aliaser Aliaser { get; }

		public IDataSource Source
		{
			get => this.Session.Source;
		}

		public IDataProvider Provider { get; }

		public new Schema Schema
		{
			get => (Schema)base.Schema;
		}

		public DataSession Session { get; }
		#endregion
	}

	public class DataUpsertContext : DataUpsertContextBase, IDataMutateContext, Common.Expressions.IAliasable
	{
		#region 构造函数
		public DataUpsertContext(IDataAccess dataAccess, string name, bool isMultiple, object data, ISchema schema, IDataUpsertOptions options = null) : base(dataAccess, name, isMultiple, data, schema, options)
		{
			this.Aliaser = new Common.Expressions.Aliaser();
			this.Provider = DataEnvironment.Providers.GetProvider(dataAccess.Name);
			this.Session = DataAccessContextUtility.GetSession(() => this.Provider.Multiplexer.GetSource(this));
		}
		#endregion

		#region 公共属性
		public Common.Expressions.Aliaser Aliaser { get; }

		public IDataSource Source
		{
			get => this.Session.Source;
		}

		public IDataProvider Provider { get; }

		public new Schema Schema
		{
			get => (Schema)base.Schema;
		}

		public DataSession Session { get; }
		#endregion
	}

	public class DataSelectContext : DataSelectContextBase, IDataAccessContext, Common.Expressions.IAliasable
	{
		#region 构造函数
		public DataSelectContext(IDataAccess dataAccess, string name, Type entityType, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options = null) : base(dataAccess, name, entityType, grouping, criteria, schema, paging, sortings, options)
		{
			this.Aliaser = new Common.Expressions.Aliaser();
			this.Provider = DataEnvironment.Providers.GetProvider(dataAccess.Name);
			this.Session = DataAccessContextUtility.GetSession(() => this.Provider.Multiplexer.GetSource(this));
		}
		#endregion

		#region 公共属性
		public Common.Expressions.Aliaser Aliaser { get; }

		public IDataSource Source
		{
			get => this.Session.Source;
		}

		public IDataProvider Provider { get; }

		public new Schema Schema
		{
			get => (Schema)base.Schema;
		}

		public DataSession Session { get; }
		#endregion
	}

	internal static class DataAccessContextUtility
	{
		#region 公共方法
		public static DataSession GetSession(Func<IDataSource> sourceFactory)
		{
			var ambient = Zongsoft.Transactions.Transaction.Current;

			if(ambient == null || ambient.IsCompleted)
				return new DataSession(sourceFactory());

			return (DataSession)ambient.Information.Parameters.GetOrAdd("Zongsoft.Data:DataSession", _ => new DataSession(sourceFactory(), ambient));
		}
		#endregion
	}
}
