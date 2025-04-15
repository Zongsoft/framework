﻿/*
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
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>
/// 表示数据查询操作选项的接口。
/// </summary>
public interface IDataSelectOptions : IDataOptions
{
	/// <summary>获取或设置一个值，指示是否进行去重查询。</summary>
	bool IsDistinct { get; set; }

	/// <summary>获取或设置一个值，指示是否禁用对子集的延迟加载。</summary>
	bool LazySuppressed { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否禁用当前数据访问操作的验证器，默认不禁用。
	/// </summary>
	bool ValidatorSuppressed { get; set; }
}

/// <summary>
/// 表示数据查询操作选项的类。
/// </summary>
public class DataSelectOptions : DataOptionsBase, IDataSelectOptions
{
	#region 构造函数
	public DataSelectOptions() { }
	public DataSelectOptions(Collections.Parameters parameters) : base(parameters) { }
	public DataSelectOptions(IEnumerable<KeyValuePair<string, object>> parameters) : base(parameters) { }
	#endregion

	#region 公共属性
	/// <inheritdoc />
	public bool IsDistinct { get; set; }

	/// <inheritdoc />
	public bool LazySuppressed { get; set; }

	/// <inheritdoc />
	public bool ValidatorSuppressed { get; set; }
	#endregion

	#region 静态方法
	/// <summary>创建一个带参数的数据操作选项构建器。</summary>
	/// <param name="name">指定的参数名称。</param>
	/// <param name="value">指定的参数值。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder Parameter(string name, object value = null) => new(new[] { new KeyValuePair<string, object>(name, value) });

	/// <summary>创建一个带参数的数据操作选项构建器。</summary>
	/// <param name="parameters">指定的多个附加参数。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder Parameter(params KeyValuePair<string, object>[] parameters) => new(parameters);

	/// <summary>创建一个带参数的数据操作选项构建器。</summary>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder Parameter(IEnumerable<KeyValuePair<string, object>> parameters) => new(parameters);

	/// <summary>创建一个去重的查询选项构建器。</summary>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder Distinct(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(parameters) { IsDistinct = true };

	/// <summary>创建一个禁用延迟加载的查询选项构建器。</summary>
	/// <param name="distinct">指示是否进行去重查询。</param>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder SuppressLazy(bool distinct, IEnumerable<KeyValuePair<string, object>> parameters = null) => new(parameters)
	{
		IsDistinct = distinct,
		LazySuppressed = true,
	};

	/// <summary>创建一个禁用数据验证器的查询选项构建器。</summary>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder SuppressValidator(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(parameters) { ValidatorSuppressed = true };

	/// <summary>创建一个禁用数据验证器的查询选项构建器。</summary>
	/// <param name="distinct">指示是否进行去重查询。</param>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder SuppressValidator(bool distinct, IEnumerable<KeyValuePair<string, object>> parameters = null) => new(parameters)
	{
		IsDistinct = distinct,
		ValidatorSuppressed = true,
	};
	#endregion

	#region 嵌套子类
	public class Builder : DataOptionsBuilder<DataSelectOptions>
	{
		#region 构造函数
		public Builder(IEnumerable<KeyValuePair<string, object>> parameters) => this.Parameter(parameters);
		#endregion

		#region 公共属性
		/// <summary>获取或设置一个值，指示是否进行去重查询。</summary>
		public bool IsDistinct { get; set; }
		/// <summary>获取或设置一个值，指示是否禁用对子集的延迟加载。</summary>
		public bool LazySuppressed { get; set; }
		/// <summary>获取或设置一个值，指示是否禁用当前数据访问操作的验证器，默认不禁用。</summary>
		public bool ValidatorSuppressed { get; set; }
		#endregion

		#region 设置方法
		public Builder Parameter(string name, object value = null) { this.Parameters.SetValue(name, value); return this; }
		public Builder Parameter(params KeyValuePair<string, object>[] parameters) { this.Parameters.SetValue(parameters); return this; }
		public Builder Parameter(IEnumerable<KeyValuePair<string, object>> parameters) { this.Parameters.SetValue(parameters); return this; }
		public Builder Distinct() { this.IsDistinct = true; return this; }
		public Builder Distinct(bool value) { this.IsDistinct = value; return this; }
		public Builder SuppressLazy() { this.LazySuppressed = true; return this; }
		public Builder UnsuppressLazy() { this.LazySuppressed = false; return this; }
		public Builder SuppressValidator() { this.ValidatorSuppressed = true; return this; }
		public Builder UnsuppressValidator() { this.ValidatorSuppressed = false; return this; }
		#endregion

		#region 构建方法
		public override DataSelectOptions Build() => new DataSelectOptions(this.Parameters)
		{
			IsDistinct = this.IsDistinct,
			LazySuppressed = this.LazySuppressed,
			ValidatorSuppressed = this.ValidatorSuppressed,
		};
		#endregion

		#region 类型转换
		public static implicit operator DataSelectOptions(Builder builder) => builder.Build();
		#endregion
	}
	#endregion
}
