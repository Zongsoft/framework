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
	/// 表示数据存在操作选项的接口。
	/// </summary>
	public interface IDataExistsOptions : IDataOptions
	{
		/// <summary>
		/// 获取或设置一个值，指示是否禁用当前数据访问操作的验证器，默认不禁用。
		/// </summary>
		bool ValidatorSuppressed { get; set; }
	}

	/// <summary>
	/// 表示数据存在操作选项的类。
	/// </summary>
	public class DataExistsOptions : DataOptionsBase, IDataExistsOptions
	{
		#region 公共属性
		/// <inheritdoc />
		public bool ValidatorSuppressed { get; set; }
		#endregion

		#region 静态方法
		/// <summary>
		/// 创建一个禁用数据验证器的存在选项。
		/// </summary>
		/// <returns>返回创建的<see cref="DataExistsOptions"/>存在选项对象。</returns>
		public static DataExistsOptions SuppressValidator()
		{
			return new DataExistsOptions() { ValidatorSuppressed = true };
		}
		#endregion
	}
}
