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
using System.Collections.ObjectModel;

namespace Zongsoft.Configuration.Xml;

/// <summary>表示按指令名称索引的选项集合，名称忽略大小写且不可重复。</summary>
/// <remarks>集合顺序不影响指令执行顺序；未配置的指令采用内置默认设置。</remarks>
public class XmlConfigurationInstructionOptionsCollection : KeyedCollection<string, XmlConfigurationInstructionOptions>
{
	#region 构造函数
	internal XmlConfigurationInstructionOptionsCollection(XmlConfigurationOptions options) : base(StringComparer.OrdinalIgnoreCase) => this.Options = options ?? throw new ArgumentNullException(nameof(options));
	#endregion

	#region 公共属性
	/// <summary>获取本集合所属的配置选项。</summary>
	public XmlConfigurationOptions Options { get; }

	/// <summary>获取或设置指令处理前的回调，可改写参数或通过 <see cref="XmlConfigurationInstructionContext.Handled"/> 接管执行。</summary>
	/// <remarks>忽略或禁止的指令不触发此回调。</remarks>
	public Action<XmlConfigurationInstructionContext> Processing { get; set; }
	/// <summary>获取或设置指令成功处理后的回调，包括递归读取与合并。</summary>
	/// <remarks>处理失败时不触发；回调中的参数修改不会重新执行指令。</remarks>
	public Action<XmlConfigurationInstructionContext> Processed { get; set; }
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(XmlConfigurationInstructionOptions item) => item.Name;
	protected override void InsertItem(int index, XmlConfigurationInstructionOptions item)
	{
		ArgumentNullException.ThrowIfNull(item);
		base.InsertItem(index, item);
	}

	protected override void SetItem(int index, XmlConfigurationInstructionOptions item)
	{
		ArgumentNullException.ThrowIfNull(item);
		base.SetItem(index, item);
	}
	#endregion
}
