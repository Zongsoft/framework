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
		public DataUpdateOptions(IEnumerable<KeyValuePair<string, object>> states = null) : base(states) { }
		public DataUpdateOptions(string filter, IEnumerable<KeyValuePair<string, object>> states = null) : base(states) => this.Filter = filter;

		public DataUpdateOptions(UpdateBehaviors behaviors, IEnumerable<KeyValuePair<string, object>> states = null) : base(states)
		{
			this.Behaviors = behaviors;
		}

		public DataUpdateOptions(UpdateBehaviors behaviors, string filter, IEnumerable<KeyValuePair<string, object>> states = null) : base(states)
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
		/// <summary>
		/// 创建一个禁用数据验证器的更新选项。
		/// </summary>
		/// <param name="filter">更新过滤表达式。</param>
		/// <returns>返回创建的<see cref="DataUpdateOptions"/>更新选项对象。</returns>
		public static DataUpdateOptions SuppressValidator(string filter = null)
		{
			return new DataUpdateOptions(filter)
			{
				ValidatorSuppressed = true
			};
		}

		/// <summary>
		/// 创建一个禁用数据验证器的更新选项。
		/// </summary>
		/// <param name="filter">更新过滤表达式。</param>
		/// <param name="behaviors">指定的更新操作行为。</param>
		/// <returns>返回创建的<see cref="DataUpdateOptions"/>更新选项对象。</returns>
		public static DataUpdateOptions SuppressValidator(UpdateBehaviors behaviors, string filter = null)
		{
			return new DataUpdateOptions(behaviors, filter)
			{
				ValidatorSuppressed = true
			};
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
