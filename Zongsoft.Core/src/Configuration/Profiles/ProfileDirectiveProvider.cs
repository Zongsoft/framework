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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.ObjectModel;

namespace Zongsoft.Configuration.Profiles;

public class ProfileDirectiveProvider : IProfileDirectiveProvider
{
	#region 单例字段
	public static readonly ProfileDirectiveProvider Default = new();
	#endregion

	#region 成员字段
	private readonly ProfileDirectiveCollection _directives;
	#endregion

	#region 构造函数
	public ProfileDirectiveProvider(params IEnumerable<IProfileDirective> directives)
	{
		_directives = new(directives ?? [Profiles.Directives.ImportDirective.Instance]);
	}

	public ProfileDirectiveProvider(params ReadOnlySpan<IProfileDirective> directives)
	{
		_directives = new(directives.IsEmpty ? [Profiles.Directives.ImportDirective.Instance] : directives);
	}
	#endregion

	#region 公共属性
	public ICollection<IProfileDirective> Directives => _directives;
	#endregion

	#region 公共方法
	public IProfileDirective GetDirective(string name) => name != null && _directives.TryGetValue(name, out var directive) ? directive : null;
	#endregion

	#region 嵌套子类
	private sealed class ProfileDirectiveCollection : KeyedCollection<string, IProfileDirective>
	{
		public ProfileDirectiveCollection(params IEnumerable<IProfileDirective> directives) : base(StringComparer.OrdinalIgnoreCase)
		{
			if(directives != null)
			{
				foreach(var directive in directives)
					this.Items.Add(directive);
			}
		}

		public ProfileDirectiveCollection(params ReadOnlySpan<IProfileDirective> directives) : base(StringComparer.OrdinalIgnoreCase)
		{
			if(directives != null && directives.Length > 0)
			{
				for(int i = 0; i < directives.Length; i++)
					this.Items.Add(directives[i]);
			}
		}

		protected override string GetKeyForItem(IProfileDirective directive) => directive.Name;
	}
	#endregion
}
