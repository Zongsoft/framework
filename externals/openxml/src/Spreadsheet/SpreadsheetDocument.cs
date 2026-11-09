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
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Zongsoft.Externals.OpenXml.Spreadsheet;

public class SpreadsheetDocument : IDisposable
{
	#region 常量定义
	private static readonly string[] SHEETS = ["Sheet1"];
	#endregion

	#region 成员字段
	private DocumentFormat.OpenXml.Packaging.SpreadsheetDocument _document;
	private SheetCollection _sheets;
	#endregion

	#region 构造函数
	private SpreadsheetDocument(DocumentFormat.OpenXml.Packaging.SpreadsheetDocument document)
	{
		_document = document ?? throw new ArgumentNullException(nameof(document));
		_sheets = new SheetCollection(_document.WorkbookPart);
	}
	#endregion

	#region 公共属性
	public SheetCollection Sheets => _sheets;
	#endregion

	#region 公共方法
	public void Save() => _document?.Save();
	public void Close() => _document?.Dispose();
	#endregion

	#region 静态方法
	public static SpreadsheetDocument Open(Stream stream, bool editable = false)
	{
		return new SpreadsheetDocument(DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(stream, editable));
	}

	public static SpreadsheetDocument Open(string filePath, bool editable = false)
	{
		return new SpreadsheetDocument(DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(filePath, editable));
	}

	public static SpreadsheetDocument Create(Stream stream, params string[] sheets)
	{
		var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
		Initialize(document, sheets);
		return new SpreadsheetDocument(document);
	}

	public static SpreadsheetDocument Create(string filePath, params string[] sheets)
	{
		var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
		Initialize(document, sheets);
		return new SpreadsheetDocument(document);
	}

	private static void Initialize(DocumentFormat.OpenXml.Packaging.SpreadsheetDocument document, IEnumerable<string> sheets = null)
	{
		var workbook = document.AddWorkbookPart();
		workbook.Workbook = new Workbook();

		var worksheet = workbook.AddNewPart<WorksheetPart>();
		worksheet.Worksheet = new Worksheet(new SheetData());

		if(sheets == null || !sheets.Any())
			sheets = SHEETS;

		var index = 0U;
		var sheetReferences = workbook.Workbook.AppendChild(new Sheets());

		foreach(var sheet in sheets)
		{
			sheetReferences.AddChild(new DocumentFormat.OpenXml.Spreadsheet.Sheet()
			{
				Id = workbook.GetIdOfPart(worksheet),
				SheetId = ++index,
				Name = string.IsNullOrWhiteSpace(sheet) ? $"Sheet{index}" : sheet.Trim(),
			});
		}
	}
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if(disposing)
		{
			var document = Interlocked.Exchange(ref _document, null);

			if(document != null)
				document.Dispose();
		}
	}
	#endregion
}
