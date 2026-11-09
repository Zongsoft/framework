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

namespace Zongsoft.Communication;

public partial class TransmitterDescriptor : IEquatable<TransmitterDescriptor>
{
	#region 构造函数
	public TransmitterDescriptor(string name, string title = null, string description = null)
	{
		this.Name = name ?? throw new ArgumentNullException(nameof(name));
		this.Title = title;
		this.Description = description;
		this.Channels = new();
		this.Templates = new();
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Title { get; set; }
	public string Description { get; set; }
	public ChannelCollection Channels { get; }
	public TemplateCollection Templates { get; }
	#endregion

	#region 重写方法
	public bool Equals(TransmitterDescriptor other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is TransmitterDescriptor other && this.Equals(other);
	public override int GetHashCode() => this.Name.ToUpperInvariant().GetHashCode();
	public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.Name : $"[{this.Name}]{this.Title}";
	#endregion
}

public class TransmitterDescriptorCollection() : KeyedCollection<string, TransmitterDescriptor>(StringComparer.OrdinalIgnoreCase)
{
	#region 重写方法
	protected override string GetKeyForItem(TransmitterDescriptor transmitter) => transmitter.Name;
	#endregion
}
