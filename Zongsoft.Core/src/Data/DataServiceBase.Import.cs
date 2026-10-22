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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Collections;
using Zongsoft.Data.Archiving;

namespace Zongsoft.Data;

partial class DataServiceBase<TModel> : IDataImportable
{
	#region 公共属性
	public virtual bool CanImport => this.CanInsert;
	#endregion

	#region 公共方法
	public ValueTask<int> ImportAsync(Stream input, CancellationToken cancellation) => this.ImportAsync(input, default(DataImportOptions), cancellation);
	public ValueTask<int> ImportAsync(Stream input, DataImportOptions options = null, CancellationToken cancellation = default) => this.ImportAsync(input, null, options, cancellation);
	public ValueTask<int> ImportAsync(Stream input, string format, CancellationToken cancellation) => this.ImportAsync(input, format, default(DataImportOptions), cancellation);
	public async ValueTask<int> ImportAsync(Stream input, string format, DataImportOptions options = null, CancellationToken cancellation = default)
	{
		if(input == null)
			return 0;

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.Import(), options, cancellation);

		//执行导出操作
		return await this.OnImportAsync(input, format, options, cancellation);
	}
	#endregion

	#region 保护方法
	protected virtual IDataArchiveExtractor GetExtractor(string format, DataImportOptions options, out IDataArchiveExtractorOptions extracting)
	{
		extracting = new DataArchiveExtractorOptions(this.Descriptor.Model, options?.Parameters);
		return this.ServiceProvider.Find<IDataArchiveExtractor>(format) ?? throw OperationException.Unsupported($"The '{format}' format data archive import is not supported.");
	}

	protected virtual ValueTask<int> OnImportAsync(Stream input, string format, DataImportOptions options = null, CancellationToken cancellation = default)
	{
		var extractor = this.GetExtractor(format, options, out var extracting);
		var data = extractor.ExtractAsync<TModel>(input, extracting, cancellation);
		return this.OnImportAsync(data, extracting.Members, options, cancellation);
	}

	protected virtual ValueTask<int> OnImportAsync(IAsyncEnumerable<TModel> items, string[] members, DataImportOptions options, CancellationToken cancellation) => this.DataAccess.ImportAsync(this.Name, items, members, options, cancellation);
	#endregion
}
