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
	public ProfileSection Section { get; internal set; }
}

public class ProfileReadingContext : ProfileContextBase
{
	public ProfileReadingContext(Profile profile, Stream input, int lineNumber = 0) : base(profile)
	{
		this.Input = input ?? throw new ArgumentNullException(nameof(input));
		this.LineNumber = lineNumber;
	}

	public Stream Input { get; }
	public int LineNumber { get; internal set; }
}

public class ProfileWritingContext : ProfileContextBase
{
	public ProfileWritingContext(Profile profile, TextWriter writer) : base(profile)
	{
		this.Writer = writer ?? throw new ArgumentNullException(nameof(writer));
	}

	public TextWriter Writer { get; }
}