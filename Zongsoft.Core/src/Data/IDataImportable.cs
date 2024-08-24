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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示提供数据导入功能的接口。
	/// </summary>
	public interface IDataImportable
	{
		/// <summary>获取一个值，指示是否支持导入操作。</summary>
		bool CanImport { get; }

		int Import(Stream input, DataImportOptions options = null);
		int Import(Stream input, string format, DataImportOptions options = null);

		ValueTask<int> ImportAsync(Stream input, DataImportOptions options = null, CancellationToken cancellation = default);
		ValueTask<int> ImportAsync(Stream input, string format, DataImportOptions options = null, CancellationToken cancellation = default);
	}
}