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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data;

partial class DataServiceBase<TModel>
{
	public int Delete(string key, DataDeleteOptions options = null) => this.Delete(key, null, options);
	public int Delete(string key, string schema, DataDeleteOptions options = null) => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key, options, out _), schema, options);

	public int Delete<TKey1>(TKey1 key1, DataDeleteOptions options = null)
		where TKey1 : IEquatable<TKey1> => this.Delete(key1, null, options);
	public int Delete<TKey1>(TKey1 key1, string schema, DataDeleteOptions options = null)
		where TKey1 : IEquatable<TKey1> => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, options, out _), schema, options);

	public int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataDeleteOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.Delete(key1, key2, null, options);
	public int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataDeleteOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, options, out _), schema, options);

	public int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataDeleteOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.Delete(key1, key2, key3, null, options);
	public int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataDeleteOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, options, out _), schema, options);

	public int Delete<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataDeleteOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.Delete(key1, key2, key3, key4, null, options);
	public int Delete<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataDeleteOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, options, out _), schema, options);

	public int Delete<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataDeleteOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.Delete(key1, key2, key3, key4, key5, null, options);
	public int Delete<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataDeleteOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, key5, options, out _), schema, options);

	public int Delete(ICondition criteria, DataDeleteOptions options = null) => this.Delete(criteria, null, options);
	public int Delete(ICondition criteria, string schema, DataDeleteOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureDelete(options);

		//构建数据操作的选项对象
		if(options == null)
			options = new DataDeleteOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Delete(), options);

		//修整删除条件
		criteria = this.OnValidate(DataServiceMethod.Delete(), criteria, options);

		//执行删除操作
		return this.OnDelete(criteria, this.GetSchema(schema), options);
	}

	public int Delete(Data.Condition criteria, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, options);
	public int Delete(Data.Condition criteria, string schema, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, schema, options);
	public int Delete(ConditionCollection criteria, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, options);
	public int Delete(ConditionCollection criteria, string schema, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, schema, options);

	protected virtual int OnDelete(ICondition criteria, ISchema schema, DataDeleteOptions options)
	{
		if(criteria == null)
			throw new NotSupportedException("The criteria cann't is null on delete operation.");

		return this.DataAccess.Delete(this.Name, criteria, schema, options, ctx => this.OnDeleting(ctx), ctx => this.OnDeleted(ctx));
	}

	public ValueTask<int> DeleteAsync(string key, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync(key, null, options, cancellation);
	public ValueTask<int> DeleteAsync(string key, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync<TKey1>(TKey1 key1, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.DeleteAsync(key1, null, options, cancellation);
	public ValueTask<int> DeleteAsync<TKey1>(TKey1 key1, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.DeleteAsync(key1, key2, null, options, cancellation);
	public ValueTask<int> DeleteAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.DeleteAsync(key1, key2, key3, null, options, cancellation);
	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.DeleteAsync(key1, key2, key3, key4, null, options, cancellation);
	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.DeleteAsync(key1, key2, key3, key4, key5, null, options, cancellation);
	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, key5, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync(ICondition criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync(criteria, null, options, cancellation);
	public ValueTask<int> DeleteAsync(ICondition criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureDelete(options);

		//构建数据操作的选项对象
		if(options == null)
			options = new DataDeleteOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Delete(), options);

		//修整删除条件
		criteria = this.OnValidate(DataServiceMethod.Delete(), criteria, options);

		//执行删除操作
		return this.OnDeleteAsync(criteria, this.GetSchema(schema), options, cancellation);
	}

	public ValueTask<int> DeleteAsync(Data.Condition criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, options, cancellation);
	public ValueTask<int> DeleteAsync(Data.Condition criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, schema, options, cancellation);
	public ValueTask<int> DeleteAsync(ConditionCollection criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, options, cancellation);
	public ValueTask<int> DeleteAsync(ConditionCollection criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, schema, options, cancellation);

	protected virtual ValueTask<int> OnDeleteAsync(ICondition criteria, ISchema schema, DataDeleteOptions options, CancellationToken cancellation)
	{
		if(criteria == null)
			throw new NotSupportedException("The criteria cann't is null on delete operation.");

		return this.DataAccess.DeleteAsync(this.Name, criteria, schema, options, ctx => this.OnDeleting(ctx), ctx => this.OnDeleted(ctx), cancellation);
	}
}
