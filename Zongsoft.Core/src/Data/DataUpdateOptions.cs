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
	public interface IDataUpdateOptions : IDataOptions
	{
		/// <summary>获取或设置更新行为。</summary>
		UpdateBehaviors Behaviors { get; set; }
	}

	/// <summary>
	/// 表示数据更新操作选项的类。
	/// </summary>
	public class DataUpdateOptions : DataOptionsBase, IDataUpdateOptions
	{
		#region 单例字段
		/// <summary>获取一个空的数据更新操作的选项实例。</summary>
		public static readonly DataUpdateOptions Empty = new DataUpdateOptions();
		#endregion

		#region 公共属性
		/// <inheritdoc />
		public UpdateBehaviors Behaviors { get; set; }
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
