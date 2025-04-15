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

namespace Zongsoft.IO;

[Serializable]
public class FileInfo : PathInfo, IEquatable<FileInfo>
{
	#region 成员字段
	private long _size;
	private string _type;
	#endregion

	#region 构造函数
	protected FileInfo()
	{
		_size = -1;
		_type = string.Empty;
	}

	public FileInfo(string path, long size, DateTime? createdTime = null, DateTime? modifiedTime = null, string url = null)
		: base(path, createdTime, modifiedTime, url)
	{
		_size = size;
	}

	public FileInfo(Path path, long size, DateTime? createdTime = null, DateTime? modifiedTime = null, string url = null)
		: base(path, createdTime, modifiedTime, url)
	{
		_size = size;
	}
	#endregion

	#region 公共属性
	public long Size
	{
		get => _size;
		set => _size = value;
	}

	public string Type
	{
		get => _type;
		set => _type = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
	}

	public override bool IsFile => true;
	public override bool IsDirectory => false;
	#endregion

	#region 重写方法
	public bool Equals(FileInfo info) => info != null && _size == info._size && base.Equals(info);
	public override bool Equals(object obj) => obj is FileInfo info && this.Equals(info);
	public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), _size);
	#endregion
}
