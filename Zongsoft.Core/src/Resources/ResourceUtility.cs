/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Resources;
using System.Reflection;

namespace Zongsoft.Resources
{
	public static class ResourceUtility
	{
		public static string GetResourceString(this Assembly assembly, string name, string resourceName = null)
		{
			return GetResourceValue(assembly, name, resourceName) as string;
		}

		public static object GetResourceValue(this Assembly assembly, string name, string resourceName = null)
		{
			if(assembly == null)
				throw new ArgumentNullException(nameof(assembly));

			if(string.IsNullOrEmpty(name))
				return null;

			if(string.IsNullOrEmpty(resourceName))
			{
				var resourceNames = assembly.GetManifestResourceNames();
				for(int i = 0; i < resourceNames.Length; i++)
				{
					using var reader = new ResourceReader(assembly.GetManifestResourceStream(resourceNames[i]));
					var iterator = reader.GetEnumerator();
					while(iterator.MoveNext())
					{
						if(iterator.Key is string key && string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
							return iterator.Value;
					}
				}
			}
			else
			{
				using var stream = assembly.GetManifestResourceStream(resourceName);
				if(stream == null)
					return null;

				using var reader = new ResourceReader(stream);
				var iterator = reader.GetEnumerator();
				while(iterator.MoveNext())
				{
					if(iterator.Key is string key && string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
						return iterator.Value;
				}
			}

			return null;
		}
	}
}
