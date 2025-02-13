﻿/*
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
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles
{
	internal class ProfileItemCollection : System.Collections.ObjectModel.ObservableCollection<ProfileItem>
	{
		#region 成员字段
		private readonly object _owner;
		#endregion

		#region 构造函数
		public ProfileItemCollection(object owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}
		#endregion

		#region 内部属性
		internal object Owner
		{
			get => _owner;
			set
			{
				if(object.ReferenceEquals(_owner, value))
					return;

				foreach(var item in this.Items)
					item.Owner = value;
			}
		}
		#endregion

		#region 重写方法
		protected override void InsertItem(int index, ProfileItem item)
		{
			if(item != null && item.Owner == null)
				item.Owner = _owner;

			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, ProfileItem item)
		{
			if(item != null && item.Owner == null)
				item.Owner = _owner;

			base.SetItem(index, item);
		}
		#endregion
	}
}
