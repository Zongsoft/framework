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
using System.IO;
using System.Collections.Generic;

namespace Zongsoft.Options.Configuration
{
	public static class OptionConfigurationManager
	{
		#region 私有变量
		private readonly static Zongsoft.Collections.ObjectCache<OptionConfiguration> _cache = new Collections.ObjectCache<OptionConfiguration>(0);
		#endregion

		#region 公共方法
		public static OptionConfiguration Open(string filePath, bool createNotExists = false)
		{
			if(string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException("filePath");

			return _cache.Get(filePath.Trim(), key =>
			{
				if(File.Exists(key))
					return OptionConfiguration.Load(key);

				if(createNotExists)
					return new OptionConfiguration(key);

				return null;
			});
		}

		public static void Close(string filePath)
		{
			if(string.IsNullOrWhiteSpace(filePath))
				return;

			_cache.Remove(filePath.Trim());
		}
		#endregion
	}
}
