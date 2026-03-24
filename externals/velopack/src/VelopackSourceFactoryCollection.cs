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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Velopack library.
 *
 * The Zongsoft.Externals.Velopack is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Velopack is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Velopack library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.ObjectModel;

namespace Zongsoft.Externals.Velopack;

public sealed class VelopackSourceFactoryCollection : KeyedCollection<string, IVelopackSourceFactory>
{
	public VelopackSourceFactoryCollection() : base(StringComparer.OrdinalIgnoreCase)
	{
		this.Add(VelopackSourceFactory.Web);
		this.Add(VelopackSourceFactory.File);
		this.Add(VelopackSourceFactory.Gitea);
		this.Add(VelopackSourceFactory.Github);
		this.Add(VelopackSourceFactory.Gitlab);
	}

	protected override string GetKeyForItem(IVelopackSourceFactory factory) => factory.Name;
}
