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
using System.Collections.Generic;

namespace Zongsoft.Data;

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
	/// <summary>获取或设置更新行为。</summary>
	UpdateBehaviors Behaviors { get; set; }

	/// <summary>获取或设置更新操作的返回设置。</summary>
	DataUpdateReturning Returning { get; set; }

	/// <summary>获取当前更新操作是否指定了返回设置。</summary>
	/// <param name="returning">输出参数，返回指定的返回设置。</param>
	/// <returns>如果返回真(<c>True</c>)则表示指定了返回设置，否则返回假(<c>False</c>)。</returns>
	bool HasReturning(out DataUpdateReturning returning);
}

/// <summary>
/// 表示数据更新操作选项的类。
/// </summary>
public class DataUpdateOptions : DataMutateOptions, IDataUpdateOptions
{
	#region 静态字段
	private static readonly Lazy<ReturningBuilder> _returning = new(() => new ReturningBuilder());
	#endregion

	#region 构造函数
	public DataUpdateOptions() { }
	public DataUpdateOptions(Collections.Parameters parameters) : base(parameters) { }
	public DataUpdateOptions(IEnumerable<KeyValuePair<string, object>> parameters) : base(parameters) { }
	public DataUpdateOptions(UpdateBehaviors behaviors) => this.Behaviors = behaviors;
	public DataUpdateOptions(UpdateBehaviors behaviors, Collections.Parameters parameters) : base(parameters) => this.Behaviors = behaviors;
	public DataUpdateOptions(UpdateBehaviors behaviors, IEnumerable<KeyValuePair<string, object>> parameters) : base(parameters) => this.Behaviors = behaviors;
	#endregion

	#region 公共属性
	/// <inheritdoc />
	public UpdateBehaviors Behaviors { get; set; }
	/// <inheritdoc />
	public DataUpdateReturning Returning { get; set; }
	#endregion

	#region 公共方法
	public bool HasReturning(out DataUpdateReturning returning)
	{
		returning = this.Returning;
		return returning != null && returning.HasValue;
	}
	#endregion

	#region 静态方法
	/// <summary>创建一个带返回设置的数据操作选项构建器。</summary>
	/// <param name="newer">指定的更新后的成员名集合。</param>
	/// <param name="older">指定的更新前的成员名集合。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder Return(IEnumerable<string> newer = null, IEnumerable<string> older = null) => new(new DataUpdateReturning(newer, older));

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

	/// <summary>创建一个禁用数据验证器的更新选项构建器。</summary>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder SuppressValidator(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(parameters) { ValidatorSuppressed = true };

	/// <summary>创建一个禁用数据验证器的更新选项构建器。</summary>
	/// <param name="behaviors">指定的更新操作行为。</param>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <returns>返回创建的<see cref="Builder"/>构建器对象。</returns>
	public static Builder SuppressValidator(UpdateBehaviors behaviors, IEnumerable<KeyValuePair<string, object>> parameters = null) => new(behaviors, parameters) { ValidatorSuppressed = true };
	#endregion

	#region 嵌套子类
	public class Builder : DataMutateOptionsBuilder<DataUpdateOptions>
	{
		#region 成员字段
		private UpdateBehaviors _behaviors;
		private ReturningBuilder _returningBuilder;
		#endregion

		#region 构造函数
		public Builder(IEnumerable<KeyValuePair<string, object>> parameters) => this.Parameter(parameters);
		public Builder(UpdateBehaviors behaviors, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			_behaviors = behaviors;
			this.Parameter(parameters);
		}
		public Builder(DataUpdateReturning returning, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			this.ReturningSetting = returning;
			this.Parameter(parameters);
		}
		#endregion

		#region 公共属性
		/// <summary>获取更新操作的返回设置构建器。</summary>
		public ReturningBuilder Returning => _returningBuilder ??= new ReturningBuilder(this);
		#endregion

		#region 内部属性
		internal DataUpdateReturning ReturningSetting { get; set; }
		#endregion

		#region 设置方法
		public Builder Behaviors(UpdateBehaviors behaviors) { _behaviors = behaviors; return this; }
		public Builder Parameter(string name, object value = null) { this.Parameters.SetValue(name, value); return this; }
		public Builder Parameter(params KeyValuePair<string, object>[] parameters) { this.Parameters.SetValue(parameters); return this; }
		public Builder Parameter(IEnumerable<KeyValuePair<string, object>> parameters) { this.Parameters.SetValue(parameters); return this; }
		public Builder SuppressValidator() { this.ValidatorSuppressed = true; return this; }
		public Builder UnsuppressValidator() { this.ValidatorSuppressed = false; return this; }
		#endregion

		#region 构建方法
		public override DataUpdateOptions Build() => new DataUpdateOptions(_behaviors, this.Parameters)
		{
			Returning = this.ReturningSetting,
			ValidatorSuppressed = this.ValidatorSuppressed,
		};
		#endregion

		#region 类型转换
		public static implicit operator DataUpdateOptions(Builder builder) => builder.Build();
		#endregion
	}

	public sealed class ReturningBuilder
	{
		#region 私有字段
		private readonly Builder _builder;
		#endregion

		#region 构造函数
		public ReturningBuilder(Builder builder = null) => _builder = builder ?? new Builder(null);
		#endregion

		#region 公共方法
		public Builder Newer(params string[] names)
		{
			if(names == null)
				_builder.ReturningSetting = null;
			else if(_builder.ReturningSetting == null)
				_builder.ReturningSetting = new DataUpdateReturning(names, null);
			else
			{
				for(int i = 0; i < names.Length; i++)
					_builder.ReturningSetting.Newer.Add(names[i], null);
			}

			return _builder;
		}

		public Builder Newer(IEnumerable<string> names)
		{
			if(names == null)
				_builder.ReturningSetting = null;
			else if(_builder.ReturningSetting == null)
				_builder.ReturningSetting = new DataUpdateReturning(names, null);
			else
			{
				foreach(var name in names)
					_builder.ReturningSetting.Newer.Add(name, null);
			}

			return _builder;
		}

		public Builder Older(params string[] names)
		{
			if(names == null)
				_builder.ReturningSetting = null;
			else if(_builder.ReturningSetting == null)
				_builder.ReturningSetting = new DataUpdateReturning(null, names);
			else
			{
				for(int i = 0; i < names.Length; i++)
					_builder.ReturningSetting.Older.Add(names[i], null);
			}

			return _builder;
		}

		public Builder Older(IEnumerable<string> names)
		{
			if(names == null)
				_builder.ReturningSetting = null;
			else if(_builder.ReturningSetting == null)
				_builder.ReturningSetting = new DataUpdateReturning(null, names);
			else
			{
				foreach(var name in names)
					_builder.ReturningSetting.Older.Add(name, null);
			}

			return _builder;
		}
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
