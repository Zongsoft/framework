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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data;

partial class DataServiceBase<TModel>
{
	#region 键值查询
	public ValueTask<object> GetAsync(string key, params Sorting[] sortings) => this.GetAsync(key, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, CancellationToken cancellation) => this.GetAsync(key, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync(string key, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, Paging.Disabled, null, sortings, cancellation);
	public ValueTask<object> GetAsync(string key, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, string.Empty, Paging.Disabled, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, DataSelectOptions options, CancellationToken cancellation) => this.GetAsync(key, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync(string key, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, Paging.Disabled, options, sortings, cancellation);

	public ValueTask<object> GetAsync(string key, Paging paging, params Sorting[] sortings) => this.GetAsync(key, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, Paging paging, CancellationToken cancellation) => this.GetAsync(key, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync(string key, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync(string key, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, Paging paging, DataSelectOptions options, CancellationToken cancellation) => this.GetAsync(key, paging, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync(string key, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, paging, options, sortings, cancellation);

	public ValueTask<object> GetAsync(string key, string schema, params Sorting[] sortings) => this.GetAsync(key, schema, Paging.Disabled, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, string schema, CancellationToken cancellation) => this.GetAsync(key, schema, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync(string key, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, schema, Paging.Disabled, null, sortings, cancellation);
	public ValueTask<object> GetAsync(string key, string schema, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, schema, Paging.Disabled, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, string schema, DataSelectOptions options, CancellationToken cancellation) => this.GetAsync(key, schema, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync(string key, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, schema, Paging.Disabled, options, sortings, cancellation);

	public ValueTask<object> GetAsync(string key, string schema, Paging paging, params Sorting[] sortings) => this.GetAsync(key, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, string schema, Paging paging, CancellationToken cancellation) => this.GetAsync(key, schema, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync(string key, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync(string key, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, schema, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) => this.GetAsync(key, schema, paging, options, default(Sorting[]), cancellation);
	public async ValueTask<object> GetAsync(string key, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key, options, out var singular);

		if(singular)
		{
			//进行授权验证
			await this.AuthorizeAsync(DataServiceMethod.Get(), options, cancellation);

			//修整查询条件
			criteria = await this.OnValidateAsync(DataServiceMethod.Get(), criteria, options, cancellation);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}
	#endregion

	#region 单键查询
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, null, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, paging, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, paging, options, default(Sorting[]), cancellation);
	public async ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, options, out var singular);

		if(singular)
		{
			//进行授权验证
			await this.AuthorizeAsync(DataServiceMethod.Get(), options, cancellation);

			//修整查询条件
			criteria = await this.OnValidateAsync(DataServiceMethod.Get(), criteria, options, cancellation);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}
	#endregion

	#region 双键查询
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, null, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, paging, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, paging, options, default(Sorting[]), cancellation);
	public async ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, options, out var singular);

		if(singular)
		{
			//进行授权验证
			await this.AuthorizeAsync(DataServiceMethod.Get(), options, cancellation);

			//修整查询条件
			criteria = await this.OnValidateAsync(DataServiceMethod.Get(), criteria, options, cancellation);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}
	#endregion

	#region 三键查询
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, null, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, paging, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, null, paging, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, options, default(Sorting[]), cancellation);
	public async ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, key3, options, out var singular);

		if(singular)
		{
			//进行授权验证
			await this.AuthorizeAsync(DataServiceMethod.Get(), options, cancellation);

			//修整查询条件
			criteria = await this.OnValidateAsync(DataServiceMethod.Get(), criteria, options, cancellation);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}
	#endregion

	#region 四键查询
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, null, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, paging, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, options, default(Sorting[]), cancellation);
	public async ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, key3, key4, options, out var singular);

		if(singular)
		{
			//进行授权验证
			await this.AuthorizeAsync(DataServiceMethod.Get(), options, cancellation);

			//修整查询条件
			criteria = await this.OnValidateAsync(DataServiceMethod.Get(), criteria, options, cancellation);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}
	#endregion

	#region 五键查询
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, null, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, paging, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, options, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, options, sortings, cancellation);

	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, default(Sorting[]), cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, options, default(Sorting[]), cancellation);
	public async ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, key3, key4, key5, options, out var singular);

		if(singular)
		{
			//进行授权验证
			await this.AuthorizeAsync(DataServiceMethod.Get(), options, cancellation);

			//修整查询条件
			criteria = await this.OnValidateAsync(DataServiceMethod.Get(), criteria, options, cancellation);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}
	#endregion

	#region 保护方法
	protected virtual async ValueTask<TModel> OnGetAsync(ICondition criteria, ISchema schema, DataSelectOptions options, CancellationToken cancellation)
	{
		DataSelectContextBase context = null;
		var result = this.DataAccess.SelectAsync<TModel>(this.Name, criteria, schema, null, options, null, this.OnGettingAsync, FilterAsync, cancellation);

		await using var iterator = result.GetAsyncEnumerator(cancellation);
		var item = await iterator.MoveNextAsync() ? iterator.Current : default;

		if(context != null)
			await this.OnGettedAsync(context, item, cancellation);

		return item;

		ValueTask FilterAsync(DataSelectContextBase selection, CancellationToken token)
		{
			context = selection;
			return this.OnFilteredAsync(DataServiceMethod.Get(), selection, token);
		}
	}
	#endregion
}
