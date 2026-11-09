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

public abstract class ProfileItem
{
	#region 构造函数
	protected ProfileItem(Profile profile, int lineNumber = -1)
	{
		this.Profile = profile ?? throw new ArgumentNullException(nameof(profile));
		this.LineNumber = Math.Max(lineNumber, -1);
	}

	protected ProfileItem(ProfileSection section, int lineNumber = -1)
	{
		this.Section = section ?? throw new ArgumentNullException(nameof(section));
		this.Profile = section.Profile;
		this.LineNumber = Math.Max(lineNumber, -1);
	}
	#endregion

	#region 公共属性
	public Profile Profile { get; }
	public ProfileSection Section { get; }
	public abstract ProfileItemType ItemType { get; }
	public int LineNumber { get; }
	#endregion
}
