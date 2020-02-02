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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata.Profiles
{
	public class MetadataFileLoader : IDataMetadataLoader
	{
		#region 成员字段
		private string _path;
		#endregion

		#region 构造函数
		public MetadataFileLoader()
		{
			_path = Zongsoft.Services.ApplicationContext.Current?.ApplicationDirectory;
		}

		public MetadataFileLoader(string path)
		{
			if(string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			_path = path;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置要加载的目录地址，支持“|”竖线符分隔的多个目录。
		/// </summary>
		public string Path
		{
			get
			{
				return _path;
			}
			set
			{
				if(string.IsNullOrEmpty(value))
					throw new ArgumentNullException();

				_path = value;
			}
		}
		#endregion

		#region 加载方法
		public IEnumerable<IDataMetadataProvider> Load(string name)
		{
			if(string.IsNullOrEmpty(_path))
				throw new InvalidOperationException("The file or directory path to load is not specified.");

			var directories = _path.Split('|');

			foreach(var directory in directories)
			{
				if(string.IsNullOrWhiteSpace(directory))
					continue;

				//如果指定的目录不存在则返回初始化失败
				if(!Directory.Exists(directory))
					throw new InvalidOperationException($"The '{directory}' directory path to load does not exist.");

				//查找指定目录下的所有映射文件
				var files = Directory.GetFiles(directory, "*.mapping", SearchOption.AllDirectories);

				foreach(var file in files)
				{
					//加载指定的映射文件
					var metadata = MetadataFile.Load(file, name);

					//将加载成功的映射文件加入到提供程序集中
					if(metadata != null)
						yield return metadata;
				}
			}
		}
		#endregion
	}
}
