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
using System.IO;

namespace Zongsoft.Configuration.Profiles;

public abstract class ProfileContextBase
{
	protected ProfileContextBase(Profile profile)
	{
		this.Profile = profile ?? throw new ArgumentNullException(nameof(profile));
	}

	public Profile Profile { get; }
}

public class ProfileReadingContext : ProfileContextBase
{
	#region 构造函数
	internal ProfileReadingContext(Profile profile, Stream input, ProfileReader reader) : base(profile)
	{
		this.Input = input ?? throw new ArgumentNullException(nameof(input));
		this.Reader = reader ?? throw new ArgumentNullException(nameof(reader));
	}
	#endregion

	#region 公共属性
	public Stream Input { get; }
	public int LineNumber { get; internal set; }
	public ProfileSection Section { get; internal set; }
	#endregion

	#region 内部属性
	internal ProfileReader Reader { get; }
	#endregion
}

public class ProfileWritingContext : ProfileContextBase
{
	internal ProfileWritingContext(Profile profile, TextWriter writer) : base(profile)
	{
		this.Writer = writer ?? throw new ArgumentNullException(nameof(writer));
	}

	public TextWriter Writer { get; }
}