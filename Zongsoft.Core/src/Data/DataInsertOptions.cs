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
	/// 表示数据新增操作选项的接口。
	/// </summary>
	public interface IDataInsertOptions : IDataMutateOptions
	{
		/// <summary>获取或设置一个值，指示是否强制应用新增序号器来生成序号值，默认不强制。</summary>
		bool SequenceSuppressed { get; set; }
	}

	/// <summary>
	/// 表示数据新增操作选项的类。
	/// </summary>
	public class DataInsertOptions : DataMutateOptions, IDataInsertOptions
	{
		#region 构造函数
		public DataInsertOptions() { }
		public DataInsertOptions(IEnumerable<KeyValuePair<string, object>> states) : base(states) { }
		#endregion

		#region 公共属性
		/// <inheritdoc />
		public bool SequenceSuppressed { get; set; }
		#endregion

		#region 静态方法
		/// <summary>
		/// 创建一个禁用序号器的新增选项。
		/// </summary>
		/// <returns>返回创建的<see cref="DataInsertOptions"/>新增选项对象。</returns>
		public static DataInsertOptions SuppressSequence() => new DataInsertOptions() { SequenceSuppressed = true };

		/// <summary>
		/// 创建一个禁用数据验证器的新增选项。
		/// </summary>
		/// <returns>返回创建的<see cref="DataInsertOptions"/>新增选项对象。</returns>
		public static DataInsertOptions SuppressValidator(bool sequenceSuppressed = false) => new DataInsertOptions() { ValidatorSuppressed = true, SequenceSuppressed = sequenceSuppressed };
		#endregion
	}
}
