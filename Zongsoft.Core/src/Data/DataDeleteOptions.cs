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
/// 表示数据删除操作选项的接口。
/// </summary>
public interface IDataDeleteOptions : IDataMutateOptions
{
	/// <summary>获取或设置删除操作的返回设置。</summary>
	DataDeleteReturning Returning { get; set; }

	/// <summary>获取当前删除操作是否指定了返回设置。</summary>
	/// <param name="returning">输出参数，返回指定的返回设置。</param>
	/// <returns>如果返回真(<c>True</c>)则表示指定了返回设置，否则返回假(<c>False</c>)。</returns>
	bool HasReturning(out DataDeleteReturning returning);
}

/// <summary>
/// 表示数据删除操作选项的类。
/// </summary>
public class DataDeleteOptions : DataMutateOptions, IDataDeleteOptions
{
	#region 构造函数
	public DataDeleteOptions() { }
	public DataDeleteOptions(Collections.Parameters parameters) : base(parameters) { }
	public DataDeleteOptions(IEnumerable<KeyValuePair<string, object>> parameters) : base(parameters) { }
	#endregion

	#region 公共属性
	/// <inheritdoc />
	public DataDeleteReturning Returning { get; set; }
	#endregion

	#region 公共方法
	public bool HasReturning(out DataDeleteReturning returning)
	{
		returning = this.Returning;
		return returning != null && returning.HasValue;
	}
	#endregion

	#region 静态方法
	/// <summary>创建一个带返回设置的数据操作选项构建器。</summary>
	/// <param name="names">指定的删除前的返回成员名数组。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder Return(params string[] names) => new(null) { Returning = new DataDeleteReturning(names) };

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

	/// <summary>创建一个禁用数据验证器的删除选项构建器。</summary>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder SuppressValidator(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(parameters) { ValidatorSuppressed = true };
	#endregion

	#region 嵌套子类
	public class Builder : DataMutateOptionsBuilder<DataDeleteOptions>
	{
		#region 构造函数
		public Builder(IEnumerable<KeyValuePair<string, object>> parameters) => this.Parameter(parameters);
		#endregion

		#region 公共属性
		/// <summary>获取或设置删除操作的返回设置。</summary>
		public DataDeleteReturning Returning { get; set; }
		#endregion

		#region 设置方法
		public Builder Return(params string[] names)
		{
			if(names == null || names.Length == 0)
				this.Returning = null;
			else if(this.Returning == null)
				this.Returning = new(names);
			else
			{
				for(int i = 0; i < names.Length; i++)
					this.Returning.Add(names[i]);
			}

			return this;
		}

		public Builder Return(IEnumerable<string> names)
		{
			if(names == null)
				this.Returning = null;
			else if(this.Returning == null)
				this.Returning = new(names);
			else
			{
				foreach(var name in names)
					this.Returning.Add(name);
			}

			return this;
		}

		public Builder Parameter(string name, object value = null) { this.Parameters.SetValue(name, value); return this; }
		public Builder Parameter(params KeyValuePair<string, object>[] parameters) { this.Parameters.SetValue(parameters); return this; }
		public Builder Parameter(IEnumerable<KeyValuePair<string, object>> parameters) { this.Parameters.SetValue(parameters); return this; }
		public Builder SuppressValidator() { this.ValidatorSuppressed = true; return this; }
		public Builder UnsuppressValidator() { this.ValidatorSuppressed = false; return this; }
		#endregion

		#region 构建方法
		public override DataDeleteOptions Build() => new DataDeleteOptions(this.Parameters)
		{
			Returning = this.Returning,
			ValidatorSuppressed = this.ValidatorSuppressed,
		};
		#endregion

		#region 类型转换
		public static implicit operator DataDeleteOptions(Builder builder) => builder.Build();
		#endregion
	}
	#endregion
}
