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

namespace Zongsoft.Text.Templating;

/// <summary>表示模板格式的类。</summary>
public readonly struct TemplateFormat : IEquatable<TemplateFormat>, IEquatable<string>
{
	#region 构造函数
	public TemplateFormat(string name, string type, string extension = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(string.IsNullOrEmpty(type))
			throw new ArgumentNullException(nameof(type));

		this.Name = name;
		this.Type = type;

		if(!string.IsNullOrWhiteSpace(extension))
			this.Extension = extension.StartsWith('.') ? extension.Trim() : '.' + extension.Trim();
	}
	#endregion

	#region 公共属性
	/// <summary>获取模板的格式名称。</summary>
	public string Name { get; }

	/// <summary>获取模板的 <seealso href="http://developer.mozilla.org/docs/Web/HTTP/Guides/MIME_types">MIME</seealso> 类型。</summary>
	public string Type { get; }

	/// <summary>获取模板文件的扩展名称。</summary>
	/// <remarks>如果该属性不为空，则扩展名始终以 <c>.</c> 打头。</remarks>
	public string Extension { get; }
	#endregion

	#region 重写方法
	public bool Equals(string name) => string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase);
	public bool Equals(TemplateFormat other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is TemplateFormat other && this.Equals(other);
	public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name);
	public override string ToString() => $"{this.Name}({this.Type})";
	#endregion

	#region 符号重写
	public static bool operator ==(TemplateFormat left, TemplateFormat right) => left.Equals(right);
	public static bool operator !=(TemplateFormat left, TemplateFormat right) => !(left == right);
	#endregion
}
