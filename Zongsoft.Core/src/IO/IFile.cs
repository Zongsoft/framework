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
using System.Collections.Generic;

namespace Zongsoft.IO;

/// <summary>
/// 提供用于创建、复制、删除、移动和打开文件等功能的抽象接口，该接口将提供不同文件系统的文件支持。
/// </summary>
public interface IFile
{
	/// <summary>获取指定文件路径对应的<see cref="FileInfo"/>描述信息。</summary>
	/// <param name="path">指定的文件路径。</param>
	/// <returns>如果指定的路径是存在的则返回对应的<see cref="FileInfo"/>，否则返回空(<c>null</c>)。</returns>
	FileInfo GetInfo(string path);
	ValueTask<FileInfo> GetInfoAsync(string path, CancellationToken cancellation = default);

	bool SetInfo(string path, IEnumerable<KeyValuePair<string, string>> properties);
	ValueTask<bool> SetInfoAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default);

	bool Delete(string path);
	ValueTask<bool> DeleteAsync(string path, CancellationToken cancellation = default);

	bool Exists(string path);
	ValueTask<bool> ExistsAsync(string path, CancellationToken cancellation = default);

	void Copy(string source, string destination, bool overwrite = true);
	ValueTask CopyAsync(string source, string destination, CancellationToken cancellation = default) => this.CopyAsync(source, destination, true, cancellation);
	ValueTask CopyAsync(string source, string destination, bool overwrite, CancellationToken cancellation = default);

	void Move(string source, string destination);
	ValueTask MoveAsync(string source, string destination, CancellationToken cancellation = default);

	Stream Open(string path, FileMode mode, IEnumerable<KeyValuePair<string, string>> properties = null) => this.Open(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None, properties);
	Stream Open(string path, FileMode mode, FileAccess access, IEnumerable<KeyValuePair<string, string>> properties = null) => this.Open(path, mode, access, FileShare.None, properties);
	Stream Open(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties = null);

	ValueTask<Stream> OpenAsync(string path, FileMode mode, CancellationToken cancellation = default) => this.OpenAsync(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None, null, cancellation);
	ValueTask<Stream> OpenAsync(string path, FileMode mode, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default) => this.OpenAsync(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None, properties, cancellation);
	ValueTask<Stream> OpenAsync(string path, FileMode mode, FileAccess access, CancellationToken cancellation = default) => this.OpenAsync(path, mode, access, FileShare.None, null, cancellation);
	ValueTask<Stream> OpenAsync(string path, FileMode mode, FileAccess access, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default) => this.OpenAsync(path, mode, access, FileShare.None, properties, cancellation);
	ValueTask<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, CancellationToken cancellation = default) => this.OpenAsync(path, mode, access, share, null, cancellation);
	ValueTask<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default);
}
