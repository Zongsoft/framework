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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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

	protected virtual ValueTask<int> OnImportAsync(IAsyncEnumerable<TModel> items, string[] members, DataImportOptions options, CancellationToken cancellation) => this.DataAccess.ImportAsync(this.Name, items, members, options,
		(context, token) => this.OnFilteringAsync(DataServiceMethod.Import(), context, token),
		(context, token) => this.OnFilteredAsync(DataServiceMethod.Import(), context, token), cancellation);
	#endregion
}
