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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>
/// 表示数据增改操作选项的接口。
/// </summary>
public interface IDataUpsertOptions : IDataMutateOptions
{
	/// <summary>获取或设置一个值，指示是否忽略写操作中的数据库约束（主键、唯一索引、外键约束等）。</summary>
	bool ConstraintIgnored { get; set; }

	/// <summary>获取或设置一个值，指示是否强制应用新增序号器来生成序号值，默认不强制。</summary>
	bool SequenceSuppressed { get; set; }

	/// <summary>获取或设置一个值，指示是否获取数据库自增序号器的返回值，默认为获取。</summary>
	bool SequenceRetrieverSuppressed { get; set; }

	/// <summary>获取或设置增改操作的返回设置。</summary>
	Returning Returning { get; set; }

	/// <summary>获取当前增改操作是否指定了返回设置。</summary>
	/// <param name="returning">输出参数，返回指定的返回设置。</param>
	/// <returns>如果返回真(<c>True</c>)则表示指定了返回设置，否则返回假(<c>False</c>)。</returns>
	bool HasReturning(out Returning returning);
}

/// <summary>
/// 表示数据增改操作选项的类。
/// </summary>
public class DataUpsertOptions : DataMutateOptions, IDataUpsertOptions
{
	#region 构造函数
	public DataUpsertOptions() { }
	public DataUpsertOptions(Collections.Parameters parameters) : base(parameters) { }
	public DataUpsertOptions(IEnumerable<KeyValuePair<string, object>> parameters) : base(parameters) { }
	#endregion

	#region 公共属性
	/// <inheritdoc />
	public bool ConstraintIgnored { get; set; }
	/// <inheritdoc />
	public bool SequenceSuppressed { get; set; }
	/// <inheritdoc />
	public bool SequenceRetrieverSuppressed { get; set; }
	/// <inheritdoc />
	public Returning Returning { get; set; }
	#endregion

	#region 公共方法
	public bool HasReturning(out Returning returning)
	{
		returning = this.Returning;
		return returning != null && !returning.Columns.IsEmpty;
	}
	#endregion

	#region 静态方法
	/// <summary>创建一个带返回设置的数据操作选项构建器。</summary>
	/// <param name="kind">指定的增改后的成员返回种类。</param>
	/// <param name="names">指定的增改后的成员名集合。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder Return(ReturningKind kind, params IEnumerable<string> names) => new(null) { Returning = new(names.Select(name => new Returning.Column(name, kind))) };

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

	/// <summary>创建一个忽略数据库约束的增改选项构建器。</summary>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder IgnoreConstraint() => new(null) { ConstraintIgnored = true };

	/// <summary>创建一个禁用序号器的增改选项构建器。</summary>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder SuppressSequence() => new(null) { SequenceSuppressed = true };

	/// <summary>创建一个禁用数据库序号器返回值的增改选项构建器。</summary>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder SuppressSequenceRetriever() => new(null) { SequenceRetrieverSuppressed = true };

	/// <summary>创建一个禁用数据验证器的增改选项构建器。</summary>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder SuppressValidator() => new(null) { ValidatorSuppressed = true };
	#endregion

	#region 嵌套子类
	public class Builder : DataMutateOptionsBuilder<DataUpsertOptions>
	{
		#region 构造函数
		public Builder(IEnumerable<KeyValuePair<string, object>> parameters) => this.Parameter(parameters);
		#endregion

		#region 公共属性
		/// <summary>获取或设置一个值，指示是否忽略写操作中的数据库约束（主键、唯一索引、外键约束等）。</summary>
		public bool ConstraintIgnored { get; set; }

		/// <summary>获取或设置一个值，指示是否强制应用新增序号器来生成序号值，默认不强制。</summary>
		public bool SequenceSuppressed { get; set; }

		/// <summary>获取或设置一个值，指示是否获取数据库自增序号器的返回值，默认为获取。</summary>
		public bool SequenceRetrieverSuppressed { get; set; }

		/// <summary>获取或设置增改操作的返回设置。</summary>
		public Returning Returning { get; set; }
		#endregion

		#region 设置方法
		public Builder Return(ReturningKind kind, params IEnumerable<string> names)
		{
			if(names == null)
				this.Returning = null;
			else if(this.Returning == null)
				this.Returning = new(names.Select(name => new Returning.Column(name, kind)));
			else
			{
				foreach(var name in names)
					this.Returning.Columns.Append(name, kind);
			}

			return this;
		}

		public Builder Parameter(string name, object value = null) { this.Parameters.SetValue(name, value); return this; }
		public Builder Parameter(params KeyValuePair<string, object>[] parameters) { this.Parameters.SetValue(parameters); return this; }
		public Builder Parameter(IEnumerable<KeyValuePair<string, object>> parameters) { this.Parameters.SetValue(parameters); return this; }
		public Builder IgnoreConstraint() { this.ConstraintIgnored = true; return this; }
		public Builder UnignoreConstraint() { this.ConstraintIgnored = false; return this; }
		public Builder SuppressSequence() { this.SequenceSuppressed = true; return this; }
		public Builder UnsuppressSequence() { this.SequenceSuppressed = false; return this; }
		public Builder SuppressSequenceRetriever() { this.SequenceRetrieverSuppressed = true; return this; }
		public Builder UnsuppressSequenceRetriever() { this.SequenceRetrieverSuppressed = false; return this; }
		public Builder SuppressValidator() { this.ValidatorSuppressed = true; return this; }
		public Builder UnsuppressValidator() { this.ValidatorSuppressed = false; return this; }
		#endregion

		#region 构建方法
		public override DataUpsertOptions Build() => new DataUpsertOptions(this.Parameters)
		{
			ConstraintIgnored = this.ConstraintIgnored,
			SequenceSuppressed = this.SequenceSuppressed,
			SequenceRetrieverSuppressed = this.SequenceRetrieverSuppressed,
			ValidatorSuppressed = this.ValidatorSuppressed,
		};
		#endregion

		#region 类型转换
		public static implicit operator DataUpsertOptions(Builder builder) => builder.Build();
		#endregion
	}
	#endregion
}
