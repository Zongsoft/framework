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

namespace Zongsoft.Configuration.Profiles;

public class ProfileComment : ProfileItem
{
	#region 私有变量
	private readonly string _text;
	private readonly string[] _lines;
	#endregion

	#region 构造函数
	internal ProfileComment(Profile profile, string text, int lineNumber = -1) : base(profile, lineNumber)
	{
		_text = text ?? string.Empty;
		_lines = string.IsNullOrEmpty(text) ? [] : text.Replace("\r\n", "\n").Split('\n');
	}

	internal ProfileComment(ProfileSection section, string text, int lineNumber = -1) : base(section, lineNumber)
	{
		_text = text ?? string.Empty;
		_lines = string.IsNullOrEmpty(text) ? [] : text.Replace("\r\n", "\n").Split('\n');
	}
	#endregion

	#region 公共属性
	public bool IsEmpty => string.IsNullOrEmpty(_text);
	public string Text => _text;
	public string[] Lines => _lines;
	public override ProfileItemType ItemType => ProfileItemType.Comment;
	#endregion

	#region 重写方法
	public override string ToString() => _text;
	#endregion
}
