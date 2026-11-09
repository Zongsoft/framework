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

namespace Zongsoft.IO;

/// <summary>提供用于创建、复制、删除、移动和打开文件等功能的抽象接口，该接口将提供不同文件系统的文件支持。</summary>
public interface IFile
{
	#region 公共方法
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
	#endregion
}
