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

			_sheet.Worksheet.Elements<Columns>();

			this.SheetId = id;
			this.Name = name;
			this.Visible = visible;
		}
		#endregion

		#region 公共属性
		public uint SheetId { get; init; }
		public string Name { get; init; }
		public bool Visible { get; set; }
		#endregion

		#region 公共方法
		public string GetCellText(int row, int column) => this.GetCellText(new CellAddress(row, column));
		public string GetCellText(CellAddress address)
		{
			var cell = GetCell(address, out _);
			if(cell == null)
				return null;

			if(cell.DataType == null)
				return cell.InnerText;

			switch(cell.DataType.Value)
			{
				case CellValues.SharedString:
					return _sheets.GetSharedString(cell.CellValue.InnerText) ?? cell.InnerText;
				case CellValues.Boolean:
					return cell.CellValue.TryGetBoolean(out var value) ? value.ToString() : cell.InnerText;
			}

			return cell.InnerText;
		}

		public bool TryGetCellValue(int row, int column, out string value) => this.TryGetCellValue(new CellAddress(row, column), out value);
		public bool TryGetCellValue(CellAddress address, out string value)
		{
			value = this.GetCellText(address);
			return value != null;
		}

		public bool TryGetCellValue(int row, int column, out bool value) => this.TryGetCellValue(new CellAddress(row, column), out value);
		public bool TryGetCellValue(CellAddress address, out bool value)
		{
			var text = this.GetCellText(address);
			return bool.TryParse(text, out value);
		}

		public bool TryGetCellValue(int row, int column, out int value) => this.TryGetCellValue(new CellAddress(row, column), out value);
		public bool TryGetCellValue(CellAddress address, out int value)
		{
			var text = this.GetCellText(address);
			return int.TryParse(text, out value);
		}

		public bool TryGetCellValue(int row, int column, out double value) => this.TryGetCellValue(new CellAddress(row, column), out value);
		public bool TryGetCellValue(CellAddress address, out double value)
		{
			var text = this.GetCellText(address);
			return double.TryParse(text, out value);
		}

		public bool TryGetCellValue(int row, int column, out decimal value) => this.TryGetCellValue(new CellAddress(row, column), out value);
		public bool TryGetCellValue(CellAddress address, out decimal value)
		{
			var text = this.GetCellText(address);
			return decimal.TryParse(text, out value);
		}

		public bool TryGetCellValue(int row, int column, out DateTime value) => this.TryGetCellValue(new CellAddress(row, column), out value);
		public bool TryGetCellValue(CellAddress address, out DateTime value)
		{
			var text = this.GetCellText(address);
			return DateTime.TryParse(text, out value);
		}

		public bool TryGetCellValue(int row, int column, out DateTimeOffset value) => this.TryGetCellValue(new CellAddress(row, column), out value);
		public bool TryGetCellValue(CellAddress address, out DateTimeOffset value)
		{
			var text = this.GetCellText(address);
			return DateTimeOffset.TryParse(text, out value);
		}

		public void SetCellValue(int row, int column, string value) => this.SetCellValue(new CellAddress(row, column), value);
		public void SetCellValue(CellAddress address, string value)
		{
			var cell = GetCell(address, out var row) ?? CreateCell(address, row);

			if(cell == null)
				return;

			if(cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
				_sheets.RemoveSharedString(cell.InnerText);

			if(string.IsNullOrEmpty(value))
			{
				cell.Remove();
			}
			else
			{
				cell.DataType = new EnumValue<CellValues>(CellValues.String);
				cell.CellValue = new CellValue(value);
			}
		}

		public void SetCellValue(int row, int column, bool value) => this.SetCellValue(new CellAddress(row, column), value);
		public void SetCellValue(CellAddress address, bool value)
		{
			this.SetCellValue(address, cell =>
			{
				cell.DataType = new EnumValue<CellValues>(CellValues.Boolean);
				cell.CellValue = new CellValue(value);
			});
		}

		public void SetCellValue(int row, int column, int value) => this.SetCellValue(new CellAddress(row, column), value);
		public void SetCellValue(CellAddress address, int value)
		{
			this.SetCellValue(address, cell =>
			{
				cell.DataType = new EnumValue<CellValues>(CellValues.Number);
				cell.CellValue = new CellValue(value);
			});
		}

		public void SetCellValue(int row, int column, double value) => this.SetCellValue(new CellAddress(row, column), value);
		public void SetCellValue(CellAddress address, double value)
		{
			this.SetCellValue(address, cell =>
			{
				cell.DataType = new EnumValue<CellValues>(CellValues.Number);
				cell.CellValue = new CellValue(value);
			});
		}

		public void SetCellValue(int row, int column, decimal value) => this.SetCellValue(new CellAddress(row, column), value);
		public void SetCellValue(CellAddress address, decimal value)
		{
			this.SetCellValue(address, cell =>
			{
				cell.DataType = new EnumValue<CellValues>(CellValues.Number);
				cell.CellValue = new CellValue(value);
			});
		}

		public void SetCellValue(int row, int column, DateTime value) => this.SetCellValue(new CellAddress(row, column), value);
		public void SetCellValue(CellAddress address, DateTime value)
		{
			this.SetCellValue(address, cell =>
			{
				cell.DataType = new EnumValue<CellValues>(CellValues.Date);
				cell.CellValue = new CellValue(value);
			});
		}

		public void SetCellValue(int row, int column, DateTimeOffset value) => this.SetCellValue(new CellAddress(row, column), value);
		public void SetCellValue(CellAddress address, DateTimeOffset value)
		{
			this.SetCellValue(address, cell =>
			{
				cell.DataType = new EnumValue<CellValues>(CellValues.Date);
				cell.CellValue = new CellValue(value);
			});
		}

		private void SetCellValue(CellAddress address, Action<Cell> onValue)
		{
			var cell = GetCell(address, out var row) ?? CreateCell(address, row);

			if(cell == null)
				return;

			if(cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
				_sheets.RemoveSharedString(cell.InnerText);

			onValue(cell);
		}
		#endregion

		#region 私有方法
		private Cell GetCell(CellAddress address, out Row row)
		{
			row = _sheet.Worksheet.GetFirstChild<SheetData>().Elements<Row>().Where(line => line.RowIndex.Value == (uint)address.Row).FirstOrDefault();
			return row?.Elements<Cell>().FirstOrDefault(cell => string.Equals(cell.CellReference.Value, address.ToString()));
		}

		private Cell CreateCell(CellAddress address, Row row)
		{
			var data = _sheet.Worksheet.GetFirstChild<SheetData>();

			if(data == null)
				return null;

			if(row == null)
			{
				row = new Row() { RowIndex = (uint)address.Row, };
				data.Append(row);
			}

			Cell marker = null;

			foreach(var cell in row.Elements<Cell>())
			{
				if(string.Compare(cell.CellReference.Value, address, true) > 0)
				{
					marker = cell;
					break;
				}
			}

			return row.InsertBefore(new Cell()
			{
				CellReference = address.ToString()
			}, marker);
		}
		#endregion
	}
}
