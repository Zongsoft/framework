/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
	[Serializable]
	public class FileInfo : PathInfo
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
			get
			{
				return _size;
			}
			set
			{
				_size = value;
			}
		}

		public string Type
		{
			get
			{
				return _type;
			}
			set
			{
				_type = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
			}
		}

		public override bool IsFile
		{
			get
			{
				return true;
			}
		}

		public override bool IsDirectory
		{
			get
			{
				return false;
			}
		}
		#endregion

		#region 重写方法
		public override int GetHashCode()
		{
			var path = this.Path;
			return path == null ? 0 : path.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			var other = (FileInfo)obj;

			return _size == other._size && string.Equals(this.Path, other.Path);
		}
		#endregion
	}
}
