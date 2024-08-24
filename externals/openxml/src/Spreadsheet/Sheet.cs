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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.OpenXml library.
 *
 * The Zongsoft.Externals.OpenXml is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.OpenXml is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.OpenXml library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Zongsoft.Externals.OpenXml.Spreadsheet
{
	public class Sheet
	{
		#region 成员字段
		private readonly WorksheetPart _sheet;
		private readonly SheetCollection _sheets;
		#endregion

		#region 构造函数
		internal Sheet(SheetCollection sheets, WorksheetPart sheet, uint id, string name, bool visible = true)
		{
			_sheet = sheet;
			_sheets = sheets;

			this.SheetId = id;
			this.Name = name;
			this.Visible = visible;
			this.Cells = new CellAssistant(sheet, sheets);
		}
		#endregion

		#region 公共属性
		public uint SheetId { get; init; }
		public string Name { get; init; }
		public bool Visible { get; set; }
		public CellAssistant Cells { get; }
		#endregion
	}
}
