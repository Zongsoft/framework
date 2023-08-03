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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示提供数据导出功能的接口。
	/// </summary>
	public interface IDataExportable
	{
		/// <summary>获取一个值，指示是否支持导出操作。</summary>
		bool CanExport { get; }

		void Export(Stream output, object data, string format = null, DataExportOptions options = null);
		void Export(Stream output, object data, string[] members, string format = null, DataExportOptions options = null);
		ValueTask ExportAsync(Stream output, object data, string format = null, DataExportOptions options = null, CancellationToken cancellation = default);
		ValueTask ExportAsync(Stream output, object data, string[] members, string format = null, DataExportOptions options = null, CancellationToken cancellation = default);

		void Export(Stream output, string template, object argument, string format = null, DataExportOptions options = null);
		void Export(Stream output, string template, object argument, IEnumerable<KeyValuePair<string, object>> parameters, string format = null, DataExportOptions options = null);
		ValueTask ExportAsync(Stream output, string template, object argument, string format = null, DataExportOptions options = null, CancellationToken cancellation = default);
		ValueTask ExportAsync(Stream output, string template, object argument, IEnumerable<KeyValuePair<string, object>> parameters, string format = null, DataExportOptions options = null, CancellationToken cancellation = default);
	}
}