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

namespace Zongsoft.Data;

partial class DataServiceBase<TModel>
{
	#region 插入方法
	protected int Insert(object data, ISchema schema, DataInsertOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(data == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Insert(), options);

		//将当前插入数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//修正数据模式对象
		schema = this.Schematic(schema, data.GetType());

		//验证待新增的数据
		this.OnValidate(DataServiceMethod.Insert(), schema, dictionary, options);

		return this.OnInsert(dictionary, schema, options);
	}

	protected int InsertMany(IEnumerable items, ISchema schema, DataInsertOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.InsertMany(), options);

		//将当前插入数据集合对象转换成数据字典集合
		var dictionares = DataDictionary.GetDictionaries<TModel>(items);

		//修正数据模式对象
		schema = this.Schematic(schema, Common.TypeExtension.GetElementType(items.GetType()));

		foreach(var dictionary in dictionares)
		{
			//验证待新增的数据
			this.OnValidate(DataServiceMethod.InsertMany(), schema, dictionary, options);
		}

		return this.OnInsertMany(dictionares, schema, options);
	}
	#endregion

	#region 增改方法
	protected int Upsert(object data, ISchema schema, DataUpsertOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(data == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Upsert(), options);

		//将当前复写数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//修正数据模式对象
		schema = this.Schematic(schema, data.GetType());

		//验证待复写的数据
		this.OnValidate(DataServiceMethod.Upsert(), schema, dictionary, options);

		return this.OnUpsert(dictionary, schema, options);
	}

	protected int UpsertMany(IEnumerable items, ISchema schema, DataUpsertOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.UpsertMany(), options);

		//将当前复写数据集合对象转换成数据字典集合
		var dictionares = DataDictionary.GetDictionaries<TModel>(items);

		//修正数据模式对象
		schema = this.Schematic(schema, Common.TypeExtension.GetElementType(items.GetType()));

		foreach(var dictionary in dictionares)
		{
			//验证待复写的数据
			this.OnValidate(DataServiceMethod.UpsertMany(), schema, dictionary, options);
		}

		return this.OnUpsertMany(dictionares, schema, options);
	}
	#endregion

	#region 更新方法
	protected int Update(object data, ICondition criteria, ISchema schema, DataUpdateOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureUpdate(options);

		if(data == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpdateOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Update(), options);

		//将当前更新数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//如果指定了更新条件，则尝试将条件中的主键值同步设置到数据字典中
		if(criteria != null)
		{
			//获取当前数据服务的实体主键集
			var keys = Mapping.Entities[this.Name].Key;

			if(keys != null && keys.Length > 0)
			{
				foreach(var key in keys)
				{
					criteria.Match(key.Name, c => dictionary.TrySetValue(c.Name, c.Value));
				}
			}
		}

		//修整过滤条件
		criteria = this.OnValidate(DataServiceMethod.Update(), criteria ?? this.GetUpdateKey(dictionary), options);

		//修正数据模式对象
		schema = this.Schematic(schema, data.GetType());

		//验证待更新的数据
		this.OnValidate(DataServiceMethod.Update(), schema, dictionary, options);

		//如果缺少必须的更新条件则抛出异常
		if(criteria == null)
		{
			//再次从数据中获取主键条件
			criteria = this.GetUpdateKey(dictionary);

			if(criteria == null)
				throw new DataOperationException($"The update operation of the specified ‘{this.Name}’ entity missing required conditions.");
		}

		//执行更新操作
		return this.OnUpdate(dictionary, criteria, schema, options);
	}

	protected int UpdateMany(IEnumerable items, ISchema schema, DataUpdateOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureUpdate(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpdateOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.UpdateMany(), options);

		//将当前更新数据集合对象转换成数据字典集合
		var dictionares = DataDictionary.GetDictionaries<TModel>(items);

		//修正数据模式对象
		schema = this.Schematic(schema, Common.TypeExtension.GetElementType(items.GetType()));

		foreach(var dictionary in dictionares)
		{
			//验证待更新的数据
			this.OnValidate(DataServiceMethod.UpdateMany(), schema, dictionary, options);
		}

		return this.OnUpdateMany(dictionares, schema, options);
	}
	#endregion

	#region 私有方法
	private ISchema Schematic(ISchema schema, Type modelType)
	{
		if(schema == null)
			return this.GetSchema(null, modelType);
		else if(schema is Schema.ReadOnlySchema invariant)
			return this.GetSchema(invariant.Text, modelType, true);

		return schema;
	}
	#endregion

	#region 嵌套子类
	public static class Schema
	{
		public static ISchema Invariant(string schema = null) => new ReadOnlySchema(schema);

		internal class ReadOnlySchema : ISchema
		{
			private string _expression;
			internal ReadOnlySchema(string schema) => _expression = schema;

			public string Name => string.Empty;
			public string Text => _expression;
			public Type ModelType => null;
			public bool IsEmpty => true;
			public bool IsReadOnly { get => true; set => throw new InvalidOperationException(); }

			public void Clear() { }
			public bool Contains(string path) => false;
			public SchemaMemberBase Find(string path) => null;
			public ISchema Include(string path) => this;
			public ISchema Exclude(string path) => this;
			public bool Exclude(string path, out SchemaMemberBase member) { member = null; return false; }
		}
	}
	#endregion
}
