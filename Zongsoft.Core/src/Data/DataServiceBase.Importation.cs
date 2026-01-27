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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
	public virtual bool CanImport => this.CanInsert;

	public int Import(Stream input, DataImportOptions options = null) => this.Import(input, null, options);
	public int Import(Stream input, string format, DataImportOptions options = null)
	{
		if(input == null)
			return 0;

		//进行授权验证
		this.Authorize(DataServiceMethod.Import(), options);

		//执行导出操作
		return this.OnImport(input, format, options);
	}

	public ValueTask<int> ImportAsync(Stream input, DataImportOptions options = null, CancellationToken cancellation = default) => this.ImportAsync(input, null, options, cancellation);
	public ValueTask<int> ImportAsync(Stream input, string format, DataImportOptions options = null, CancellationToken cancellation = default)
	{
		if(input == null)
			return ValueTask.FromResult(0);

		//进行授权验证
		this.Authorize(DataServiceMethod.Import(), options);

		//执行导出操作
		return this.OnImportAsync(input, format, options, cancellation);
	}

	protected virtual IDataArchiveExtractor GetExtractor(string format, DataImportOptions options, out IDataArchiveExtractorOptions extracting)
	{
		extracting = new DataArchiveExtractorOptions(this.Descriptor.Model, options?.Parameters);
		return this.ServiceProvider.Find<IDataArchiveExtractor>(format) ?? throw OperationException.Unsupported($"The '{format}' format data archive import is not supported.");
	}

	protected virtual int OnImport(Stream input, string format, DataImportOptions options = null)
	{
		var extractor = this.GetExtractor(format, options, out var extracting);
		var data = extractor.ExtractAsync<TModel>(input, extracting).Synchronize();
		return this.OnImport(data, extracting.Members, options);
	}

	protected virtual ValueTask<int> OnImportAsync(Stream input, string format, DataImportOptions options = null, CancellationToken cancellation = default)
	{
		var extractor = this.GetExtractor(format, options, out var extracting);
		var data = extractor.ExtractAsync<TModel>(input, extracting, cancellation);
		return this.OnImportAsync(data, extracting.Members, options, cancellation);
	}

	protected virtual int OnImport(IEnumerable<TModel> items, string[] members, DataImportOptions options) => this.DataAccess.Import(this.Name, items, members, options);
	protected virtual ValueTask<int> OnImportAsync(IAsyncEnumerable<TModel> items, string[] members, DataImportOptions options, CancellationToken cancellation) => this.DataAccess.ImportAsync(this.Name, items.Synchronize(), members, options, cancellation);
}