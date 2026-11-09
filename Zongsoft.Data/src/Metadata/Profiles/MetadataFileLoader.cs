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
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata.Profiles;

[Services.Service<Mapping.Loader>(Members = nameof(Default))]
public class MetadataFileLoader : Mapping.Loader, IEquatable<MetadataFileLoader>
{
	#region 单例字段
	public static readonly MetadataFileLoader Default = new();
	#endregion

	#region 构造函数
	public MetadataFileLoader(string path = null, int priority = 100) : base(priority) => this.Path = path;
	#endregion

	#region 公共属性
	/// <summary>获取或设置要加载的目录地址，支持“|”竖线符分隔的多个目录。</summary>
	public string Path { get; set; }
	#endregion

	#region 加载方法
	protected override IEnumerable<Result> OnLoad()
	{
		var directories = string.IsNullOrEmpty(this.Path) ?
			[Services.ApplicationContext.Current?.ApplicationPath] : this.Path.Split('|');

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
				yield return new(file, metadata.Entities, metadata.Commands);
			}
		}
	}
	#endregion

	#region 重写方法
	public bool Equals(MetadataFileLoader other) => string.Equals(this.Path, other.Path);
	public override bool Equals(object obj) => obj is MetadataFileLoader other && this.Equals(other);
	public override int GetHashCode() => this.Path.GetHashCode();
	public override string ToString() => this.Path;
	#endregion
}
