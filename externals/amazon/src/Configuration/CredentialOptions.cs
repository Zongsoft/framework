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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Amazon library.
 *
 * The Zongsoft.Externals.Amazon is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Amazon is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Amazon library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.ObjectModel;

namespace Zongsoft.Externals.Amazon.Configuration;

public class CredentialOptions
{
	public string Name { get; set; }
	public string Code { get; set; }
	public string Token { get; set; }
	public string Secret { get; set; }
}

public class CredentialOptionsCollection() : KeyedCollection<string, CredentialOptions>(StringComparer.OrdinalIgnoreCase)
{
	public string Default { get; set; }
	public bool TryGetDefault(out CredentialOptions result)
	{
		if(string.IsNullOrEmpty(this.Default))
		{
			result = null;
			return false; 
		}

		return this.TryGetValue(this.Default, out result);
	}

	public CredentialOptions GetDefault()
	{
		if(string.IsNullOrEmpty(this.Default))
			return this[0];

		return this.TryGetValue(this.Default, out var result) ? result : this[0];
	}

	protected override string GetKeyForItem(CredentialOptions credential) => credential.Name;
}