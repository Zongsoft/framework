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

public class ProfileCommentCollection : ProfileItemCollection<ProfileComment>
{
	#region 构造函数
	public ProfileCommentCollection(Profile profile) : base(profile) { }
	public ProfileCommentCollection(ProfileSection section) : base(section) { }
	#endregion

	#region 公共方法
	public ProfileComment Add(string text, int lineNumber = -1)
	{
		var comment = this.Section == null ?
			new ProfileComment(this.Profile, text, lineNumber) :
			new ProfileComment(this.Section, text, lineNumber);

		this.Add(comment);
		return comment;
	}
	#endregion

	#region 内部方法
	internal ProfileComment AddParsed(string text, int lineNumber)
	{
		var comment = this.Section == null ?
			new ProfileComment(this.Profile, text, lineNumber) :
			new ProfileComment(this.Section, text, lineNumber);

		this.Items.Add(comment);
		this.Profile.Declare(comment, true);
		return comment;
	}
	#endregion
}
