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
	public bool Exists(string key, DataExistsOptions options = null) => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key, options, out _), options);
	public bool Exists<TKey1>(TKey1 key1, DataExistsOptions options = null)
		where TKey1 : IEquatable<TKey1> => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, options, out _), options);
	public bool Exists<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataExistsOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, options, out _), options);
	public bool Exists<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataExistsOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, options, out _), options);
	public bool Exists<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataExistsOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, options, out _), options);
	public bool Exists<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataExistsOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, key5, options, out _), options);

	public bool Exists(ICondition criteria, DataExistsOptions options = null)
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataExistsOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Exists(), options);

		//修整查询条件
		criteria = this.OnValidate(DataServiceMethod.Exists(), criteria, options);

		//执行存在操作
		return this.OnExists(criteria, options);
	}

	public bool Exists(Data.Condition criteria, DataExistsOptions options = null) => this.Exists((ICondition)criteria, options);
	public bool Exists(ConditionCollection criteria, DataExistsOptions options = null) => this.Exists((ICondition)criteria, options);

	protected virtual bool OnExists(ICondition criteria, DataExistsOptions options)
	{
		return this.DataAccess.Exists(this.Name, criteria, options, ctx => this.OnExisting(ctx), ctx => this.OnExisted(ctx));
	}

	public ValueTask<bool> ExistsAsync(string key, DataExistsOptions options = null, CancellationToken cancellation = default) => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key, options, out _), options, cancellation);
	public ValueTask<bool> ExistsAsync<TKey1>(TKey1 key1, DataExistsOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, options, out _), options, cancellation);
	public ValueTask<bool> ExistsAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataExistsOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, options, out _), options, cancellation);
	public ValueTask<bool> ExistsAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataExistsOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, options, out _), options, cancellation);
	public ValueTask<bool> ExistsAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataExistsOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, options, out _), options, cancellation);
	public ValueTask<bool> ExistsAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataExistsOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, key5, options, out _), options, cancellation);

	public ValueTask<bool> ExistsAsync(ICondition criteria, DataExistsOptions options = null, CancellationToken cancellation = default)
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataExistsOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Exists(), options);

		//修整查询条件
		criteria = this.OnValidate(DataServiceMethod.Exists(), criteria, options);

		//执行存在操作
		return this.OnExistsAsync(criteria, options, cancellation);
	}

	protected virtual ValueTask<bool> OnExistsAsync(ICondition criteria, DataExistsOptions options, CancellationToken cancellation)
	{
		return this.DataAccess.ExistsAsync(this.Name, criteria, options, ctx => this.OnExisting(ctx), ctx => this.OnExisted(ctx), cancellation);
	}
}
