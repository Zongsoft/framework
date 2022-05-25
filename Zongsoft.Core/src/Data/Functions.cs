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

namespace Zongsoft.Data
{
	/// <summary>
	/// 提供数据库常用函数的定义。
	/// </summary>
	public static class Functions
	{
		public const string Cast = nameof(Cast);
		public const string IsNull = nameof(IsNull);
		public const string IsDate = nameof(IsDate);
		public const string IsNumeric = nameof(IsNumeric);

		public const string Choose = nameof(Choose);
		public const string Coalesce = nameof(Coalesce);
		public const string Greatest = nameof(Greatest);
		public const string Least = nameof(Least);

		public const string Abs = nameof(Abs);
		public const string Floor = nameof(Floor);
		public const string Ceiling = nameof(Ceiling);
		public const string Power = nameof(Power);
		public const string Sqrt = nameof(Sqrt);
		public const string Round = nameof(Round);
		public const string Random = nameof(Random);
		public const string Sin = nameof(Sin);
		public const string Cos = nameof(Cos);
		public const string Tan = nameof(Tan);
		public const string Asin = nameof(Asin);
		public const string Acos = nameof(Acos);
		public const string Atan = nameof(Atan);

		public const string Guid = nameof(Guid);
		public const string Date = nameof(Date);
		public const string Time = nameof(Time);
		public const string Now = nameof(Now);
		public const string Day = nameof(Day);
		public const string Month = nameof(Month);
		public const string Year = nameof(Year);
		public const string Week = nameof(Week);
		public const string Hour = nameof(Hour);
		public const string Minute = nameof(Minute);
		public const string Second = nameof(Second);

		public const string Concat = nameof(Concat);
		public const string Format = nameof(Format);
		public const string Length = nameof(Length);
		public const string Trim = nameof(Trim);
		public const string TrimEnd = nameof(TrimEnd);
		public const string TrimStart = nameof(TrimStart);
		public const string Substring = nameof(Substring);
		public const string Stuff = nameof(Stuff);
		public const string Reverse = nameof(Reverse);
		public const string Replace = nameof(Replace);
		public const string Replicate = nameof(Replicate);
		public const string Lower = nameof(Lower);
		public const string Upper = nameof(Upper);
	}
}
