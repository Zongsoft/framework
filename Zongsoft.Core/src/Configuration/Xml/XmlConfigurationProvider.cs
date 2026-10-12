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

public class XmlConfigurationProvider : FileConfigurationProvider
{
	#region 构造函数
	public XmlConfigurationProvider(XmlConfigurationSource source) : base(source) { }
	#endregion

	#region 公共属性
	/// <summary>获取当前配置数据对应的处理指令只读快照，按文档顺序保留重复声明。</summary>
	/// <remarks>遵循文件配置基类的数据生命周期；基类清空配置数据时，此集合同时为空。</remarks>
	public IReadOnlyList<XmlConfigurationInstruction> Instructions => XmlStreamConfigurationProvider.GetInstructions(this.Data);
	#endregion

	#region 重写方法
	/// <summary>读取 XML 配置和处理指令，成功后替换当前快照。</summary>
	/// <param name="stream">配置输入流；此方法不会关闭它。</param>
	public override void Load(Stream stream)
	{
		this.Data = XmlStreamConfigurationProvider.Read(stream, out _, stream is FileStream ? null : this.Source.Path);
	}
	#endregion
}
