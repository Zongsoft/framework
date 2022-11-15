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
using System.IO;
using System.Threading;
using System.Collections.Generic;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Zongsoft.Externals.OpenXml.Spreadsheet
{
	public class SpreadsheetDocument : IDisposable
	{
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
		public void Close() => _document?.Close();
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

		public static SpreadsheetDocument Create(Stream stream)
		{
			return new SpreadsheetDocument(DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook));
		}

		public static SpreadsheetDocument Create(string filePath)
		{
			return new SpreadsheetDocument(DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook));
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
}
