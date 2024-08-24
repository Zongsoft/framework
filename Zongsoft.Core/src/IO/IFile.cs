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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zongsoft.IO
{
	/// <summary>
	/// 提供用于创建、复制、删除、移动和打开文件等功能的抽象接口，该接口将提供不同文件系统的文件支持。
	/// </summary>
	public interface IFile
	{
		/// <summary>
		/// 获取指定文件路径对应的<see cref="FileInfo"/>描述信息。
		/// </summary>
		/// <param name="path">指定的文件路径。</param>
		/// <returns>如果指定的路径是存在的则返回对应的<see cref="FileInfo"/>，否则返回空(null)。</returns>
		FileInfo GetInfo(string path);
		Task<FileInfo> GetInfoAsync(string path);

		bool SetInfo(string path, IDictionary<string, object> properties);
		Task<bool> SetInfoAsync(string path, IDictionary<string, object> properties);

		bool Delete(string path);
		Task<bool> DeleteAsync(string path);

		bool Exists(string path);
		Task<bool> ExistsAsync(string path);

		void Copy(string source, string destination);
		void Copy(string source, string destination, bool overwrite);

		Task CopyAsync(string source, string destination);
		Task CopyAsync(string source, string destination, bool overwrite);

		void Move(string source, string destination);
		Task MoveAsync(string source, string destination);

		Stream Open(string path, IDictionary<string, object> properties = null);
		Stream Open(string path, FileMode mode, IDictionary<string, object> properties = null);
		Stream Open(string path, FileMode mode, FileAccess access, IDictionary<string, object> properties = null);
		Stream Open(string path, FileMode mode, FileAccess access, FileShare share, IDictionary<string, object> properties = null);
	}
}
