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

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration.Xml;

/// <summary>加载 XML 配置文件、执行处理指令并跟踪导入文件的变化。</summary>
public class XmlConfigurationProvider : FileConfigurationProvider
{
	#region 成员字段
	private readonly XmlConfigurationOptions _options;
	private readonly XmlConfigurationFileProvider _fileProvider;
	#endregion

	#region 构造函数
	/// <summary>初始化 <see cref="XmlConfigurationProvider"/> 类的新实例。</summary>
	/// <param name="source">XML 配置来源及其加载选项。</param>
	public XmlConfigurationProvider(XmlConfigurationSource source) : base(Prepare(source))
	{
		_options = source.Options;
		_fileProvider = this.Source.FileProvider as XmlConfigurationFileProvider;
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前配置数据对应的处理指令只读快照，按导入位置展开并保留重复声明。</summary>
	/// <remarks>遵循文件配置基类的数据生命周期；基类清空配置数据时，此集合同时为空。</remarks>
	public IReadOnlyList<XmlConfigurationInstruction> Instructions => XmlStreamConfigurationProvider.GetInstructions(this.Data);
	#endregion

	#region 重写方法
	/// <summary>读取 XML 配置并执行处理指令，成功后替换当前快照。</summary>
	/// <param name="stream">配置输入流；此方法不会关闭它。</param>
	public override void Load(Stream stream)
	{
		var session = new XmlConfigurationSession(_options);
		IDictionary<string, string> data;

		try
		{
			var source = XmlConfigurationSession.Source.Create(stream, _fileProvider?.Provider ?? this.Source.FileProvider, this.Source.Path);
			data = session.Read(stream, source);
		}
		finally
		{
			//失败时也保留本次已经发现的依赖，以便缺失文件补齐或错误修复后恢复加载。
			_fileProvider?.UpdateDependencies(session.Dependencies);
		}

		this.Data = data;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if(disposing)
			_fileProvider?.Dispose();
	}
	#endregion

	#region 私有方法
	private static XmlConfigurationSource Prepare(XmlConfigurationSource source)
	{
		ArgumentNullException.ThrowIfNull(source);
		if(!source.ReloadOnChange || source.FileProvider == null)
			return source;

		//每个提供程序独享依赖通知，不修改可被其它构建器复用的来源或调用方的文件提供器。
		return new XmlConfigurationSource(source.Options)
		{
			Path = source.Path,
			FileProvider = new XmlConfigurationFileProvider(source.FileProvider),
			Optional = source.Optional,
			ReloadOnChange = source.ReloadOnChange,
			ReloadDelay = source.ReloadDelay,
			OnLoadException = source.OnLoadException,
		};
	}
	#endregion
}
