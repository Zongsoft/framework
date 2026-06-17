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
using System.IO;

using Microsoft.ML;
using Microsoft.ML.Data;

namespace Zongsoft.Learning.Data;

internal sealed class FileSource(params string[] paths) : IMultiStreamSource
{
	private readonly string[] _paths = paths ?? [];

	public int Count => _paths.Length;
	public string GetPathOrNull(int index) => index >= 0 && index < _paths.Length ? _paths[index] : null;

	public Stream Open(int index) => IsVirtualPath(_paths[index]) ? Zongsoft.IO.FileSystem.File.Open(_paths[index], FileMode.Open, FileAccess.Read) : File.OpenRead(_paths[index]);
	public TextReader OpenTextReader(int index) => new StreamReader(this.Open(index));

	static bool IsVirtualPath(string path) => path != null && path.Length > 2 && path.IndexOf(':') > 1;
}
