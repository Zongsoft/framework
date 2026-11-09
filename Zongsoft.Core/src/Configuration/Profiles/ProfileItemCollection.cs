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

namespace Zongsoft.Configuration.Profiles;

public class ProfileItemCollection<TItem> : Collection<TItem> where TItem : ProfileItem
{
	#region 成员字段
	private readonly Profile _profile;
	private readonly ProfileSection _section;
	#endregion

	#region 构造函数
	protected ProfileItemCollection(Profile profile)
	{
		_profile = profile ?? throw new ArgumentNullException(nameof(profile));
	}

	protected ProfileItemCollection(ProfileSection section)
	{
		_section = section ?? throw new ArgumentNullException(nameof(section));
		_profile = section.Profile;
	}
	#endregion

	#region 内部属性
	internal Profile Profile => _profile;
	internal ProfileSection Section => _section;
	#endregion

	#region 重写方法
	protected override void InsertItem(int index, TItem item)
	{
		this.Profile.VerifyOwner(item, this.Section);
		base.InsertItem(index, item);
		this.Profile.Declare(item);
	}

	protected override void SetItem(int index, TItem item)
	{
		this.Profile.VerifyOwner(item, this.Section);
		var previous = this.Items[index];
		this.Profile.VerifyOwner(previous, this.Section);
		base.SetItem(index, item);
		this.Profile.ReplaceDeclaration(previous, item);
	}

	protected override void RemoveItem(int index)
	{
		var item = this.Items[index];
		this.Profile.VerifyOwner(item, this.Section);
		base.RemoveItem(index);
		this.Profile.RemoveDeclaration(item);
	}

	protected override void ClearItems()
	{
		foreach(var item in this.Items)
			this.Profile.VerifyOwner(item, this.Section);

		foreach(var item in this.Items)
			this.Profile.RemoveDeclaration(item);

		base.ClearItems();
	}
	#endregion
}
