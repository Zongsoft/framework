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
 * Copyright (C) 2025-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Learning library.
 *
 * The Zongsoft.Learning is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Learning is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Learning library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Microsoft.ML;

namespace Zongsoft.Learning.Data;

[Services.Service<IDatasetLoader>(Tags = NAME)]
public class TextFileLoader : IDatasetLoader, Services.IMatchable<string>
{
	const string NAME = "TextFile";
	public string Name => NAME;

	public IDataView Load(MLContext context, IDataset dataset)
	{
		ArgumentNullException.ThrowIfNull(dataset);

		var settings = TextFileLoaderSettingsDriver.Instance.GetSettings(dataset.Settings);
		var options = settings.GetOptions();

		if(string.IsNullOrEmpty(settings?.FilePath))
			throw new ArgumentException("Missing the data source file path.", nameof(settings.FilePath));

		var source = new FileSource(settings.FilePath);
		return context.Data.CreateTextLoader(options, source).Load(source);
	}

	bool Services.IMatchable<string>.Match(string argument) => string.Equals(argument, NAME, StringComparison.OrdinalIgnoreCase);
}
