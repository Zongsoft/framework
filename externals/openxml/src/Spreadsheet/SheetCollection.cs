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
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Zongsoft.Externals.OpenXml.Spreadsheet
{
	public class SheetCollection : ICollection<Sheet>
	{
		#region 成员字段
		private List<Sheet> _sheets;
		private WorkbookPart _workbook;
		#endregion

		#region 构造函数
		public SheetCollection(WorkbookPart workbook)
		{
			_workbook = workbook;
			_sheets = new List<Sheet>();

			foreach(var sheet in _workbook.Workbook.Sheets.Cast<DocumentFormat.OpenXml.Spreadsheet.Sheet>())
			{
				if(workbook.TryGetPartById(sheet.Id, out var value) && value is WorksheetPart part)
					_sheets.Add(new Sheet(this, part, sheet.SheetId, sheet.Name, (sheet.State == null || sheet.State.Value == SheetStateValues.Visible)));
			}
		}
		#endregion

		#region 公共属性
		public int Count => _sheets.Count;
		public bool IsReadOnly => false;
		public Sheet this[int index] => _sheets[index];
		public Sheet this[string name] => _sheets.FirstOrDefault(sheet => sheet.Name == name);
		#endregion

		#region 公共方法
		public void Add(string name)
		{
			var worksheetPart = _workbook.AddNewPart<WorksheetPart>();
			worksheetPart.Worksheet = new Worksheet(new SheetData());
			var relationshipId = _workbook.GetIdOfPart(worksheetPart);

			var sheets = _workbook.Workbook.GetFirstChild<Sheets>();

			uint sheetId = 1;
			if(sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>().Any())
				sheetId = sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>().Select(sheet => sheet.SheetId.Value).Max() + 1;

			var sheet = new DocumentFormat.OpenXml.Spreadsheet.Sheet()
			{
				Id = relationshipId,
				SheetId = sheetId,
				Name = name,
			};

			sheets.Append(sheet);
			_sheets.Add(new Sheet(this, worksheetPart, sheet.SheetId, sheet.Name));
		}

		public void Clear()
		{
			foreach(var sheet in _workbook.Workbook.Sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>())
			{
				if(_workbook.TryGetPartById(sheet.Id.Value, out var part))
					_workbook.DeletePart(part);

				sheet.Remove();
			}

			_sheets.Clear();
		}

		public bool Remove(Sheet item)
		{
			if(item == null)
				return false;

			if(_sheets.Remove(item))
			{
				foreach(var sheet in _workbook.Workbook.Sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>())
				{
					if(sheet.SheetId != item.SheetId)
						continue;

					if(_workbook.TryGetPartById(sheet.Id.Value, out var part))
						_workbook.DeletePart(part);

					sheet.Remove();
				}

				return true;
			}

			return false;
		}

		public bool Contains(Sheet item) => _sheets.Contains(item);
		public bool Contains(string name) => _sheets.Any(sheet => string.Equals(sheet.Name, name));
		#endregion

		#region 内部方法
		internal string GetSharedString(string id)
		{
			return int.TryParse(id, out var value) ?
				_workbook.SharedStringTablePart?.SharedStringTable.ElementAt(value).InnerText : null;
		}

		internal bool RemoveSharedString(string id)
		{
			if(string.IsNullOrEmpty(id) || _workbook.SharedStringTablePart == null)
				return false;

			if(!int.TryParse(id, out var number))
				return false;

			if(this.HasSharedStringReferenced(id))
				return false;

			var entry = _workbook.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(number);
			if(entry == null)
				return false;

			entry.Remove();
			_workbook.SharedStringTablePart.SharedStringTable.Save();

			//更新所有对该共享字符串的引用
			foreach(var part in _workbook.GetPartsOfType<WorksheetPart>())
			{
				var worksheet = part.Worksheet;

				foreach(var cell in worksheet.GetFirstChild<SheetData>().Descendants<Cell>())
				{
					if(cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
					{
						int referenceId = int.Parse(cell.CellValue.Text);

						if(referenceId == number)
							cell.Remove();
						else if(referenceId > number)
							cell.CellValue.Text = (referenceId - 1).ToString();
					}
				}

				worksheet.Save();
			}

			return true;
		}

		internal bool HasSharedStringReferenced(string id)
		{
			foreach(var part in _workbook.GetPartsOfType<WorksheetPart>())
			{
				foreach(var cell in part.Worksheet.GetFirstChild<SheetData>().Descendants<Cell>())
				{
					if(cell.DataType != null && cell.DataType.Value == CellValues.SharedString && cell.CellValue.Text == id)
						return true;
				}
			}

			return false;
		}
		#endregion

		#region 显式实现
		void ICollection<Sheet>.Add(Sheet item) => throw new NotSupportedException();
		void ICollection<Sheet>.CopyTo(Sheet[] array, int arrayIndex) => throw new NotSupportedException();
		#endregion

		#region 枚举遍历
		public IEnumerator<Sheet> GetEnumerator() => _sheets.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion
	}
}
