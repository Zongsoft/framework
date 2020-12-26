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

namespace Zongsoft.Data
{
	/// <summary>
	/// 提供数据服务的条件和数据进行验证功能的接口。
	/// </summary>
	/// <typeparam name="TModel">关于数据服务验证对应的数据模型类型。</typeparam>
	public interface IDataServiceValidator<TModel> : IDataServiceValidator
	{
		/// <summary>
		/// 验证指定数据服务方法的写入数据。
		/// </summary>
		/// <param name="method">待验证的数据服务方法。</param>
		/// <param name="schema">待验证的数据模式。</param>
		/// <param name="data">待验证的写入数据。</param>
		/// <param name="options">待验证方法的选项参数。</param>
		void Validate(DataServiceMethod method, ISchema schema, IDataDictionary<TModel> data, IDataMutateOptions options);
	}
}
