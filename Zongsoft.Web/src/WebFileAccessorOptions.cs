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
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Web
{
	public class WebFileAccessorOptions
	{
		#region 构造函数
		public WebFileAccessorOptions(string name, string path, int index)
		{
			this.Name = name;
			this.Index = index;
			this.Cancel = false;
			this.Overwrite = false;
			this.ExtensionAppend = true;
			this.Path = path;
		}
		#endregion

		#region 公共属性
		/// <summary>获取当前文件的序号，从一开始。</summary>
		public int Index { get; }

		/// <summary>获取或设置一个值，指示是否取消后续的文件写入操作。</summary>
		public bool Cancel { get; set; }

		/// <summary>获取或设置一个值，指示当前文件操作是否为覆盖写入的方式。</summary>
		public bool Overwrite { get; set; }

		/// <summary>获取或设置一个值，指示是否自动添加文件的扩展名。</summary>
		public bool ExtensionAppend { get; set; }

		/// <summary>获取当前上传的表单项名称。</summary>
		public string Name { get; }

		/// <summary>获取当前要写入文件所在的路径。</summary>
		public string Path { get; }

		/// <summary>获取或设置要写入的文件名，如果未包含扩展名则使用上传文件的原始扩展名。</summary>
		public string FileName { get; set; }
		#endregion
	}
}
