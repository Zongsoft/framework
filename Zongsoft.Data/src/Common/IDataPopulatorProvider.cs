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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;

namespace Zongsoft.Data.Common
{
	/// <summary>
	/// 表示数据实体装配提供程序的接口。
	/// </summary>
	public interface IDataPopulatorProvider
	{
		/// <summary>确认装配提供程序是否支持指定的元素类型。</summary>
		/// <param name="type">指定要装配的元素类型。</param>
		/// <returns>如果支持指定的装配元素类型则返回真(True)，否则返回假(False)。</returns>
		bool CanPopulate(Type type);

		/// <summary>获取或创建一个数据实体装配器。</summary>
		/// <param name="record">指定要获取或构建的数据记录。</param>
		/// <param name="entity">指定组装的实体元素。</param>
		/// <returns>返回的数据实体装配器对象。</returns>
		IDataPopulator<T> GetPopulator<T>(IDataRecord record, Metadata.IDataEntity entity = null);

		/// <summary>获取或创建一个数据实体装配器。</summary>
		/// <param name="type">指定要获取或创建的装配元素类型。</param>
		/// <param name="record">指定要获取或构建的数据记录。</param>
		/// <param name="entity">指定组装的实体元素。</param>
		/// <returns>返回的数据实体装配器对象。</returns>
		IDataPopulator GetPopulator(Type type, IDataRecord record, Metadata.IDataEntity entity = null);
	}
}
