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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.IO;

[Serializable]
public partial class DirectoryInfo : PathInfo
{
	#region 构造函数
	protected DirectoryInfo() { }
	public DirectoryInfo(string path, DateTime? createdTime = null, DateTime? modifiedTime = null, string url = null) : base(path, createdTime, modifiedTime, url) { }
	public DirectoryInfo(string path, DateTime? createdTime, DateTime? modifiedTime, IEnumerable<KeyValuePair<string, string>> properties, string url = null) : base(path, createdTime, modifiedTime, properties, url) { }
	public DirectoryInfo(Path path, DateTime? createdTime = null, DateTime? modifiedTime = null, string url = null) : base(path, createdTime, modifiedTime, url) { }
	public DirectoryInfo(Path path, DateTime? createdTime, DateTime? modifiedTime, IEnumerable<KeyValuePair<string, string>> properties, string url = null) : base(path, createdTime, modifiedTime, properties, url) { }
	#endregion

	#region 重写属性
	public override bool IsFile => false;
	public override bool IsDirectory => true;
	#endregion
}

partial class DirectoryInfo
{
	public bool Delete() => FileSystem.Directory.Delete(this.Url);
	public ValueTask<bool> DeleteAsync(CancellationToken cancellation = default) => FileSystem.Directory.DeleteAsync(this.Url, cancellation);

	public void Move(string destination) => FileSystem.Directory.Move(this.Url, destination);
	public ValueTask MoveAsync(string destination, CancellationToken cancellation = default) => FileSystem.Directory.MoveAsync(this.Url, destination, cancellation);

	public bool Exists() => FileSystem.Directory.Exists(this.Url);
	public ValueTask<bool> ExistsAsync(CancellationToken cancellation = default) => FileSystem.Directory.ExistsAsync(this.Url, cancellation);

	public IEnumerable<PathInfo> GetChildren() => FileSystem.Directory.GetChildren(this.Url);
	public IEnumerable<PathInfo> GetChildren(string pattern, bool recursive = false) => FileSystem.Directory.GetChildren(this.Url, pattern, recursive);

	public IAsyncEnumerable<PathInfo> GetChildrenAsync(CancellationToken cancellation = default) => FileSystem.Directory.GetChildrenAsync(this.Url, cancellation);
	public IAsyncEnumerable<PathInfo> GetChildrenAsync(string pattern, CancellationToken cancellation = default) => FileSystem.Directory.GetChildrenAsync(this.Url, pattern, cancellation);
	public IAsyncEnumerable<PathInfo> GetChildrenAsync(string pattern, bool recursive, CancellationToken cancellation = default) => FileSystem.Directory.GetChildrenAsync(this.Url, pattern, recursive, cancellation);

	public IEnumerable<DirectoryInfo> GetDirectories() => FileSystem.Directory.GetDirectories(this.Url);
	public IEnumerable<DirectoryInfo> GetDirectories(string pattern, bool recursive = false) => FileSystem.Directory.GetDirectories(this.Url, pattern, recursive);

	public IAsyncEnumerable<DirectoryInfo> GetDirectoriesAsync(CancellationToken cancellation = default) => FileSystem.Directory.GetDirectoriesAsync(this.Url, cancellation);
	public IAsyncEnumerable<DirectoryInfo> GetDirectoriesAsync(string pattern, CancellationToken cancellation = default) => FileSystem.Directory.GetDirectoriesAsync(this.Url, pattern, cancellation);
	public IAsyncEnumerable<DirectoryInfo> GetDirectoriesAsync(string pattern, bool recursive, CancellationToken cancellation = default) => FileSystem.Directory.GetDirectoriesAsync(this.Url, pattern, recursive, cancellation);

	public IEnumerable<FileInfo> GetFiles() => FileSystem.Directory.GetFiles(this.Url);
	public IEnumerable<FileInfo> GetFiles(string pattern, bool recursive = false) => FileSystem.Directory.GetFiles(this.Url, pattern, recursive);

	public IAsyncEnumerable<FileInfo> GetFilesAsync(CancellationToken cancellation = default) => FileSystem.Directory.GetFilesAsync(this.Url, cancellation);
	public IAsyncEnumerable<FileInfo> GetFilesAsync(string pattern, CancellationToken cancellation = default) => FileSystem.Directory.GetFilesAsync(this.Url, pattern, cancellation);
	public IAsyncEnumerable<FileInfo> GetFilesAsync(string pattern, bool recursive, CancellationToken cancellation = default) => FileSystem.Directory.GetFilesAsync(this.Url, pattern, recursive, cancellation);
}