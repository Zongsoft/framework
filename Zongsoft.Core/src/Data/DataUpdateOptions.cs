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
using System.Collections.Generic;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示更新行为的枚举。
	/// </summary>
	[Flags]
	public enum UpdateBehaviors
	{
		/// <summary>未定义</summary>
		None,

		/// <summary>更新主键</summary>
		PrimaryKey,
	}

	/// <summary>
	/// 表示数据更新操作选项的接口。
	/// </summary>
	public interface IDataUpdateOptions : IDataMutateOptions
	{
		/// <summary>获取或设置过滤表达式文本。</summary>
		string Filter { get; set; }

		/// <summary>获取或设置更新行为。</summary>
		UpdateBehaviors Behaviors { get; set; }
	}

	/// <summary>
	/// 表示数据更新操作选项的类。
	/// </summary>
	public class DataUpdateOptions : DataMutateOptions, IDataUpdateOptions
	{
		#region 构造函数
		public DataUpdateOptions(in Collections.Parameters parameters) : base(parameters) { }
		public DataUpdateOptions(IEnumerable<KeyValuePair<string, object>> parameters = null) : base(parameters) { }
		public DataUpdateOptions(string filter, in Collections.Parameters parameters) : base(parameters) => this.Filter = filter;
		public DataUpdateOptions(string filter, IEnumerable<KeyValuePair<string, object>> parameters = null) : base(parameters) => this.Filter = filter;

		public DataUpdateOptions(UpdateBehaviors behaviors, in Collections.Parameters parameters) : base(parameters) => this.Behaviors = behaviors;
		public DataUpdateOptions(UpdateBehaviors behaviors, IEnumerable<KeyValuePair<string, object>> parameters = null) : base(parameters) => this.Behaviors = behaviors;

		public DataUpdateOptions(UpdateBehaviors behaviors, string filter, in Collections.Parameters parameters) : base(parameters)
		{
			this.Filter = filter;
			this.Behaviors = behaviors;
		}

		public DataUpdateOptions(UpdateBehaviors behaviors, string filter, IEnumerable<KeyValuePair<string, object>> parameters = null) : base(parameters)
		{
			this.Filter = filter;
			this.Behaviors = behaviors;
		}
		#endregion

		#region 公共属性
		/// <inheritdoc />
		public string Filter { get; set; }

		/// <inheritdoc />
		public UpdateBehaviors Behaviors { get; set; }
		#endregion

		#region 静态方法
		/// <summary>创建一个带参数的数据操作选项构建器。</summary>
		/// <param name="name">指定的参数名称。</param>
		/// <param name="value">指定的参数值。</param>
		/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
		public static Builder Parameter(string name, object value = null) => new(new KeyValuePair<string, object>(name, value));

		/// <summary>创建一个带参数的数据操作选项构建器。</summary>
		/// <param name="parameters">指定的多个附加参数。</param>
		/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
		public static Builder Parameter(params KeyValuePair<string, object>[] parameters) => new(parameters);

		/// <summary>创建一个禁用数据验证器的更新选项构建器。</summary>
		/// <param name="filter">更新过滤表达式。</param>
		/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
		public static Builder SuppressValidator(string filter = null) => new(filter)
		{
			ValidatorSuppressed = true
		};

		/// <summary>创建一个禁用数据验证器的更新选项构建器。</summary>
		/// <param name="filter">更新过滤表达式。</param>
		/// <param name="behaviors">指定的更新操作行为。</param>
		/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
		public static Builder SuppressValidator(UpdateBehaviors behaviors, string filter = null) => new(behaviors, filter)
		{
			ValidatorSuppressed = true
		};
		#endregion

		#region 嵌套子类
		public class Builder : DataMutateOptionsBuilder<DataUpdateOptions>
		{
			#region 成员字段
			private string _filter;
			private UpdateBehaviors _behaviors;
			#endregion

			#region 构造函数
			public Builder(string filter) => _filter = filter;
			public Builder(UpdateBehaviors behaviors, string filter = null) { _behaviors = behaviors; _filter = filter; }
			public Builder(params KeyValuePair<string, object>[] parameters) => this.Parameter(parameters);
			#endregion

			#region 设置方法
			public Builder Filter(string filter) { _filter = filter; return this; }
			public Builder Behaviors(UpdateBehaviors behaviors) { _behaviors = behaviors; return this; }
			public Builder Parameter(string name, object value = null) { this.Parameters.SetValue(name, value); return this; }
			public Builder Parameter(params KeyValuePair<string, object>[] parameters) { this.Parameters.SetValue(parameters); return this; }
			public Builder SuppressValidator() { this.ValidatorSuppressed = true; return this; }
			public Builder UnsuppressValidator() { this.ValidatorSuppressed = false; return this; }
			#endregion

			#region 构建方法
			public override DataUpdateOptions Build() => new DataUpdateOptions(_behaviors, _filter, this.Parameters) { ValidatorSuppressed = this.ValidatorSuppressed, };
			#endregion

			#region 类型转换
			public static implicit operator DataUpdateOptions(Builder builder) => builder.Build();
			#endregion
		}
		#endregion
	}

	/// <summary>
	/// 提供数据更新操作选项的扩展方法的静态类。
	/// </summary>
	public static class DataUpdateOptionsExtension
	{
		public static bool HasBehaviors(this IDataUpdateOptions options, UpdateBehaviors behaviors)
		{
			if(options == null)
				return false;

			return (options.Behaviors & behaviors) == behaviors;
		}

		public static IDataUpdateOptions EnableBehaviors(this IDataUpdateOptions options, UpdateBehaviors behaviors)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));

			options.Behaviors |= behaviors;
			return options;
		}

		public static IDataUpdateOptions DisableBehaviors(this IDataUpdateOptions options, UpdateBehaviors behaviors)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));

			options.Behaviors &= ~behaviors;
			return options;
		}
	}
}
