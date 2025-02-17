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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data.Metadata.Profiles;

[Services.Service<Mapping.Loader>(Members = nameof(Default))]
public class MetadataFileLoader : Mapping.Loader
{
	#region 单例字段
	public static readonly MetadataFileLoader Default = new();
	#endregion

	#region 成员字段
	private string _path;
	#endregion

	#region 构造函数
	private MetadataFileLoader() => _path = Services.ApplicationContext.Current?.ApplicationPath;
	public MetadataFileLoader(string path) => _path = path;
	#endregion

	#region 公共属性
	/// <summary>获取或设置要加载的目录地址，支持“|”竖线符分隔的多个目录。</summary>
	public string Path
	{
		get => _path;
		set => _path = value;
	}
	#endregion

	#region 加载方法
	protected override IEnumerable<Result> OnLoad()
	{
		var directories = string.IsNullOrEmpty(_path) ? [Services.ApplicationContext.Current?.ApplicationPath] : _path.Split('|');

		foreach(var directory in directories)
		{
			if(string.IsNullOrWhiteSpace(directory))
				continue;

			//如果指定的目录不存在则忽略
			if(!Directory.Exists(directory))
				continue;

			//查找指定目录下的所有映射文件
			var files = Directory.GetFiles(directory, "*.mapping", SearchOption.AllDirectories);

			foreach(var file in files)
			{
				//加载指定的映射文件
				var metadata = MetadataFile.Load(file);

				//遍历返回加载的结果
				yield return new(metadata.Entities, metadata.Commands);
			}
		}
	}
	#endregion
}
