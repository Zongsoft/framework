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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Linq;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Zongsoft.Externals.OpenXml.Spreadsheet;

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
