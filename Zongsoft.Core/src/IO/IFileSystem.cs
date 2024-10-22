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
using System.Collections.Generic;

namespace Zongsoft.IO
{
	/// <summary>
	/// 表示文件目录系统的接口。
	/// </summary>
	public interface IFileSystem
	{
		/// <summary>获取文件目录系统的方案名。</summary>
		string Scheme { get; }

		/// <summary>获取文件操作服务。</summary>
		IFile File { get; }

		/// <summary>获取目录操作服务。</summary>
		IDirectory Directory { get; }

		/// <summary>获取本地路径对应的外部访问Url地址。</summary>
		/// <param name="path">要解析的本地路径。</param>
		/// <returns>返回对应的Url地址。</returns>
		/// <remarks>
		///		<para>本地路径：是指特定的<see cref="IFileSystem"/>文件目录系统的路径格式。</para>
		///		<para>外部访问Url地址：是指可通过Web方式访问某个文件或目录的URL。</para>
		/// </remarks>
		string GetUrl(string path);

		/// <summary>获取本地路径对应的外部访问Url地址。</summary>
		/// <param name="path">要解析的<see cref="Path"/>路径对象。</param>
		/// <returns>返回对应的Url地址。</returns>
		/// <remarks>
		///		<para>外部访问Url地址：是指可通过Web方式访问某个文件或目录的URL。</para>
		/// </remarks>
		string GetUrl(Path path);
	}
}
