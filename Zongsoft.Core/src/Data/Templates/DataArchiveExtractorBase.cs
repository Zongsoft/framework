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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data.Templates;

public abstract class DataArchiveExtractorBase(string name, DataArchiveFormat format) : IDataArchiveExtractor, Services.IMatchable
{
	#region 公共属性
	public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
	public DataArchiveFormat Format { get; } = format ?? throw new ArgumentNullException(nameof(format));
	#endregion

	#region 公共方法
	public IAsyncEnumerable<T> ExtractAsync<T>(Stream input, IDataArchiveExtractorOptions options, CancellationToken cancellation = default) => new ExtractorResult<T>(this.Open(input, options), options);
	#endregion

	#region 抽象方法
	protected abstract IDataArchiveReader Open(Stream stream, IDataArchiveExtractorOptions options);
	#endregion

	#region 服务匹配
	bool Services.IMatchable.Match(object parameter) => parameter switch
	{
		string format => this.Format.Equals(format),
		IDataTemplate template => this.Format.Equals(template.Format),
		_ => false,
	};
	#endregion

	#region 嵌套子类
	private sealed class ExtractorResult<T>(IDataArchiveReader reader, IDataArchiveExtractorOptions options) : IAsyncEnumerable<T>
	{
		private readonly IDataArchiveReader _reader = reader;
		private readonly IDataArchiveExtractorOptions _options = options;

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new Iterator(_reader, _options);

		private class Iterator(IDataArchiveReader reader, IDataArchiveExtractorOptions options) : IAsyncEnumerator<T>
		{
			private readonly IDataArchiveReader _reader = reader;
			private readonly IDataArchiveExtractorOptions _options = options;

			public T Current => _reader == null || _options == null ? default : _options.Populator.Populate<T>(_reader, _options.Model);
			public ValueTask DisposeAsync() { _reader?.Dispose(); return ValueTask.CompletedTask; }
			public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_reader != null && _reader.Read());
		}
	}
	#endregion
}
