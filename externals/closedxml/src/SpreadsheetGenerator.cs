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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.ClosedXml library.
 *
 * The Zongsoft.Externals.ClosedXml is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.ClosedXml is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.ClosedXml library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using ClosedXML;
using ClosedXML.Excel;

using Zongsoft.Common;
using Zongsoft.Data;
using Zongsoft.Data.Archiving;

namespace Zongsoft.Externals.ClosedXml;

[Zongsoft.Services.Service(typeof(IDataArchiveGenerator))]
public class SpreadsheetGenerator : IDataArchiveGenerator, Services.IMatchable
{
	#region 常量定义
	private const string FONT_NAME = "Arial Narrow"; //偏爱的字体：适用于主键、代号、枚举、电话号码、邮箱地址等
	private const double COLUMN_MIN_WIDTH = 8;
	private const double COLUMN_MAX_WIDTH = 50;
	private const double TEXT_COLUMN_MIN_WIDTH = 10;
	private const double TEXT_COLUMN_DEFAULT_WIDTH = 20;
	#endregion

	#region 公共属性
	public string Name => Spreadsheet.Format.Name;
	public DataArchiveFormat Format => Spreadsheet.Format;
	#endregion

	#region 公共方法
	public ValueTask GenerateAsync(Stream output, ModelDescriptor model, object data, CancellationToken cancellation = default) => this.GenerateAsync(output, model, data, null, cancellation);
	public ValueTask GenerateAsync(Stream output, ModelDescriptor model, object data, IDataArchiveGeneratorOptions options, CancellationToken cancellation = default)
	{
		const int DATA_RANGE_FIRST_ROW = 4;
		const int DATA_RANGE_EMPTY_ROWS = 2;

		if(output == null)
			throw new ArgumentNullException(nameof(output));
		if(model == null)
			throw new ArgumentNullException(nameof(model));

		//获取要导出的数据列
		var columns = GetColumns(model, options?.Fields).ToArray();

		if(columns == null || columns.Length == 0)
			return ValueTask.CompletedTask;

		if(!VerifyTableName(model.Name))
			throw OperationException.Argument(
				string.Format(Properties.Resources.SpreadsheetGenerator_InvalidTableName_Message, model.Name),
				new ArgumentException(string.Format(Properties.Resources.SpreadsheetGenerator_InvalidTableName_Message, model.Name), nameof(model)));

		using var workbook = new XLWorkbook();
		var caption = string.IsNullOrWhiteSpace(model.Title) ? model.Name : model.Title;
		var worksheet = workbook.AddWorksheet(caption);
		worksheet.RowHeight = 20;
		worksheet.Style.Font.SetFontSize(11);
		worksheet.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

		if(columns.Length > 5)
			worksheet.PageSetup.SetPageOrientation(XLPageOrientation.Landscape);

		//生成数据文件标题
		worksheet.Cell(1, 1).SetValue(caption);
		worksheet.Row(1).Height = 45;
		worksheet.Row(1).Style.Font.SetFontSize(18);
		worksheet.Row(1).Style.Font.SetBold(true);
		worksheet.Row(1).Style.Font.FontColor = XLColor.DarkSlateGray;
		worksheet.Row(1).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
		worksheet.Row(1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
		var range = worksheet.Range(1, 1, 1, columns.Length);
		range.Style.Fill.SetPatternType(XLFillPatternValues.Gray125);
		range.Style.Fill.SetPatternColor(XLColor.LightGreen);
		range.Style.Fill.SetBackgroundColor(XLColor.TeaGreen);
		range.Style.Border.TopBorder = XLBorderStyleValues.Medium;
		range.Style.Border.TopBorderColor = XLColor.Green;
		range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
		range.Style.Border.BottomBorderColor = XLColor.Green;
		range.Merge();

		//生成数据文件时间
		worksheet.Cell(2, 1).SetValue(DateTime.Now);
		worksheet.Row(2).Height = 25;
		worksheet.Row(2).Style.Font.SetFontSize(13);
		worksheet.Row(2).Style.Font.SetItalic(true);
		worksheet.Row(2).Style.Font.SetFontName(FONT_NAME);
		worksheet.Row(2).Style.Font.SetFontColor(XLColor.Gray);
		worksheet.Row(2).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
		worksheet.Row(2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
		range = worksheet.Range(2, 1, 2, columns.Length);
		range.Style.Fill.SetPatternType(XLFillPatternValues.Gray0625);
		range.Style.Fill.SetPatternColor(XLColor.White);
		range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(230, 230, 230));
		range.Merge();

		var index = 1;

		//生成数据字段标题行
		foreach(var column in columns)
		{
			//获取字段单元
			var cell = worksheet.Cell(3, index++).AddToNamed(column.Name, XLScope.Worksheet, column.Label);

			//设置字段标题
			cell.SetValue(string.IsNullOrEmpty(column.Label) ? column.Name : column.Label);

			//设置字段标题栏的备注
			if(!string.IsNullOrEmpty(column.Description))
				cell.CreateComment().AddText(column.Description);

			//获取当前列的数据类型
			if(!Common.TypeExtension.IsNullable(column.Type, out var type))
				type = column.Type;

			//为枚举列添加说明
			if(type.IsEnum)
			{
				var comment = cell.CreateComment();
				var entries = Common.EnumUtility.GetEnumEntries(type, true);

				for(int i = 0; i < entries.Length; i++)
				{
					if(!string.IsNullOrEmpty(comment.Text))
						comment.AddNewLine();

					comment.AddText($"[{entries[i].Value}] {entries[i].Name}");
					if(!string.IsNullOrWhiteSpace(entries[i].Description))
					{
						comment.AddNewLine();
						comment.AddText(entries[i].Description).SetItalic().SetFontColor(XLColor.Gray);
					}
				}
			}

			//自动调整批注的尺寸以适配其文本内容
			if(cell.HasComment)
				cell.GetComment().Style.Size.SetAutomaticSize();
		}

		//设置数据字段标题行样式
		worksheet.Row(3).Height = 30;
		worksheet.Row(3).Style.Font.SetFontSize(12);
		worksheet.Row(3).Style.Font.SetBold(false);
		worksheet.Row(3).Style.Font.SetFontColor(XLColor.Navy);
		worksheet.Row(3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
		worksheet.Row(3).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
		range = worksheet.Range(3, 1, 3, columns.Length);
		range.Style.Fill.SetPatternType(XLFillPatternValues.Gray0625);
		range.Style.Fill.SetPatternColor(XLColor.Orange);
		range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(252, 213, 180));
		range.Style.Border.TopBorder = XLBorderStyleValues.Medium;
		range.Style.Border.TopBorderColor = XLColor.DarkRed;
		range.Style.Border.BottomBorder = XLBorderStyleValues.Double;
		range.Style.Border.BottomBorderColor = XLColor.DarkRed;

		//数据区起始行号
		var row = DATA_RANGE_FIRST_ROW;

		//处理 IAsynEnumerable 异步可枚举接口类型
		var items = Collections.Enumerable.IsAsyncEnumerable(data, out var elementType) ?
				Collections.Enumerable.Enumerate(data, elementType) : data as IEnumerable;

		//遍历生成数据区
		if(items != null)
		{
			foreach(var item in items)
				GenerateRow(worksheet, row++, item, columns, options);
		}
		else if(data != null)
		{
			GenerateRow(worksheet, row++, data, columns, options);
		}

		//有数据时按实际记录数确定范围；没有数据时保留空记录供用户录入
		var lastRow = row > DATA_RANGE_FIRST_ROW ? row - 1 : DATA_RANGE_FIRST_ROW + DATA_RANGE_EMPTY_ROWS - 1;

		//固定当前数据区行高；工作表默认行高负责未来扩展行
		worksheet.Rows(DATA_RANGE_FIRST_ROW, lastRow).Height = worksheet.RowHeight;

		//根据内容预先调整各个字段列宽，随后由模型元数据覆盖
		worksheet.ColumnsUsed().AdjustToContents();

		//设置数据区各列的样式
		foreach(var column in columns)
		{
			range = worksheet.Range(DATA_RANGE_FIRST_ROW, column.Index, lastRow, column.Index);
			SetDataColumnStyle(range, column.Property);
		}

		try
		{
			//创建模型数据表（包含字段标题行）
			var table = worksheet.Range(DATA_RANGE_FIRST_ROW - 1, 1, lastRow, columns.Length).CreateTable(model.Name);
			table.Theme = XLTableTheme.None;
			SetDataRangeStyle(table.DataRange);
		}
		catch(ArgumentException exception)
		{
			throw OperationException.Argument(string.Format(Properties.Resources.SpreadsheetGenerator_InvalidTableName_Message, model.Name), exception);
		}

		//写入到输出流
		workbook.SaveAs(output);

		return ValueTask.CompletedTask;
	}
	#endregion

	#region 私有方法
	private static bool VerifyTableName(string name)
	{
		if(string.IsNullOrWhiteSpace(name) || name.Length > 255 ||
		   (!char.IsLetter(name[0]) && name[0] != '_' && name[0] != '\\') ||
		   string.Equals(name, "C", StringComparison.OrdinalIgnoreCase) ||
		   string.Equals(name, "R", StringComparison.OrdinalIgnoreCase) ||
		   XLHelper.IsValidA1Address(name) || XLHelper.IsValidRCAddress(name))
			return false;

		for(int index = 1; index < name.Length; index++)
		{
			if(!char.IsLetterOrDigit(name[index]) && name[index] != '_' && name[index] != '.')
				return false;
		}

		return true;
	}

	private static IEnumerable<TableColumn> GetColumns(ModelDescriptor model, DataArchiveField[] fields)
	{
		int index = 1;

		if(fields != null && fields.Length > 0)
		{
			for(int i = 0; i < fields.Length; i++)
			{
				var field = fields[i];
				if(field == null)
					continue;

				if(field.Name == "*")
				{
					foreach(var property in model.Properties)
					{
						if(property.IsSimplex(out var simplex))
							yield return new TableColumn(index++, simplex);
					}
				}
				else
					yield return new TableColumn(index++, model, field);
			}
		}
		else
		{
			foreach(var property in model.Properties)
			{
				if(property.IsSimplex(out var simplex))
					yield return new TableColumn(index++, simplex);
			}
		}
	}

	private static void SetDataRangeStyle(IXLRange range)
	{
		var style = range.AddConditionalFormat().WhenIsTrue("MOD(ROW(),2)=1");
		style.Fill.SetPatternType(XLFillPatternValues.Gray0625);
		style.Fill.SetPatternColor(XLColor.LightGray);
		style.Fill.SetBackgroundColor(XLColor.FromArgb(240, 240, 240));

		style = range.AddConditionalFormat().WhenIsTrue("TRUE");
		style.Border.BottomBorder = XLBorderStyleValues.Thin;
		style.Border.BottomBorderColor = XLColor.LightGray;

		for(int i = 2; i <= range.ColumnCount(); i++)
		{
			var column = range.Column(i);
			column.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
			column.Style.Border.LeftBorderColor = XLColor.LightGray;
		}
	}

	private static void SetDataColumnStyle(IXLRange column, ModelPropertyDescriptor property)
	{
		var descriptor = property as ModelPropertyDescriptor.SimplexPropertyDescriptor;
		if(descriptor != null)
			column.FirstColumn().WorksheetColumn().Width = GetColumnWidth(descriptor);

		//如果是特定类型则调整其样式
		var nullable = Common.TypeExtension.IsNullable(property.Type, out var type);
		if(!nullable)
			type = property.Type;

		//为枚举类型设置下拉列表
		if(type.IsEnum)
		{
			var entries = Common.EnumUtility.GetEnumEntries(type, false);
			SetColumnSuggestion(column, entries.Select(entry => (XLCellValue)entry.Name), nullable, property);
		}
		else if(type == typeof(bool))
			SetColumnSuggestion(column, [true, false], nullable, property);
		else if(descriptor != null)
			SetColumnValidation(column, descriptor);

		//设置特定类型的字体
		if(type.IsEnum || type == typeof(bool) || Common.TypeExtension.IsNumeric(type) || type == typeof(Guid) ||
		   type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan) ||
		   type == typeof(DateTime) || type == typeof(DateTimeOffset))
		{
			column.Style.Font.SetFontName(FONT_NAME);
		}

		//特定类型则设置其水平居中
		if(type.IsEnum || type == typeof(bool) || type == typeof(byte) || type == typeof(Guid) || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
			SetHorizontalAlignment(column, XLAlignmentHorizontalValues.Center);

		//设置日期时间类型的格式
		if(type == typeof(DateTime) || type == typeof(DateTimeOffset))
		{
			if(property.Role == ModelPropertyRole.Birthday)
				column.Style.DateFormat.SetFormat("yyyy-MM-dd");
			else
				column.Style.DateFormat.SetFormat("yyyy-MM-dd HH:mm:ss");
		}
		else if(type == typeof(DateOnly))
			column.Style.DateFormat.SetFormat("yyyy-MM-dd");
		else if(type == typeof(TimeOnly))
			column.Style.DateFormat.SetFormat("HH:mm:ss");

		//设置特定语义角色的样式
		if(property.Role == ModelPropertyRole.Code ||
		   property.Role == ModelPropertyRole.Phone ||
		   property.Role == ModelPropertyRole.Email ||
		   property.Role == ModelPropertyRole.Status ||
		   property.Role == ModelPropertyRole.Identifier ||
		   property.Role == ModelPropertyRole.PostalCode)
		{
			column.Style.Font.SetFontName(FONT_NAME);
			SetHorizontalAlignment(column, XLAlignmentHorizontalValues.Center);
		}
		else if(property.Role == ModelPropertyRole.Currency)
		{
			column.Style.Font.SetFontName(FONT_NAME);

			//货币格式随当前文化显示货币符号；负数标红，且负号置于货币符号之后（如：$1.23、$-1.23、€1.23、￥-1.23）
			//但 ClosedXML 使用符合要求的 8 号内置格式模板时会在差异样式中丢失必需的 formatCode，导致 Excel 加载样式表失败，故改用等效的自定义格式
			column.Style.NumberFormat.SetFormat(GetCurrencyFormat());
		}

		//设置主键的样式
		if(property.IsSimplex(out var simplex) && simplex.IsPrimaryKey)
		{
			column.Style.Font.SetBold(true);
			column.Style.Font.SetFontName(FONT_NAME);
			column.Style.Font.SetFontColor(XLColor.Maroon);
			SetHorizontalAlignment(column, XLAlignmentHorizontalValues.Center);
		}

		static void SetHorizontalAlignment(IXLRange column, XLAlignmentHorizontalValues alignment)
		{
			column.Style.Alignment.SetHorizontal(alignment);
			column.FirstColumn().WorksheetColumn().Style.Alignment.SetHorizontal(alignment);
		}

		static string GetCurrencyFormat()
		{
			var culture = System.Globalization.CultureInfo.CurrentCulture;
			if(culture.IsNeutralCulture)
				culture = System.Globalization.CultureInfo.CreateSpecificCulture(culture.Name);

			var symbol = culture.NumberFormat.CurrencySymbol;
			return $"\"{symbol}\"#,##0.00;[Red]\"{symbol}\"-#,##0.00";
		}

		static double GetColumnWidth(ModelPropertyDescriptor.SimplexPropertyDescriptor property)
		{
			var width = property.DataType == null ? TEXT_COLUMN_DEFAULT_WIDTH : property.DataType.IsArray ? COLUMN_MAX_WIDTH : property.DataType.DbType switch
			{
				DbType.AnsiString or DbType.AnsiStringFixedLength or
				DbType.String or DbType.StringFixedLength =>
					property.Length > 0 ? Math.Clamp(property.Length + 2d, TEXT_COLUMN_MIN_WIDTH, COLUMN_MAX_WIDTH) : TEXT_COLUMN_DEFAULT_WIDTH,
				DbType.Boolean => 12,
				DbType.Byte or DbType.SByte => 10,
				DbType.Int16 or DbType.UInt16 => 10,
				DbType.Int32 or DbType.UInt32 => 12,
				DbType.Int64 or DbType.UInt64 => 14,
				DbType.Currency or DbType.Decimal or DbType.Double or DbType.Single or DbType.VarNumeric => 16,
				DbType.Date or DbType.Time => 12,
				DbType.DateTime or DbType.DateTime2 => 20,
				DbType.DateTimeOffset => 26,
				DbType.Guid => 38,
				DbType.Xml => COLUMN_MAX_WIDTH,
				DbType.Binary or DbType.Object => TEXT_COLUMN_DEFAULT_WIDTH,
				_ => TEXT_COLUMN_DEFAULT_WIDTH,
			};

			if(property.Role == ModelPropertyRole.Code || property.Role == ModelPropertyRole.Identifier)
				width = 16;
			else if(property.Role == ModelPropertyRole.Name)
				width = 20;
			else if(property.Role == ModelPropertyRole.Email)
				width = 32;
			else if(property.Role == ModelPropertyRole.Gender)
				width = 10;
			else if(property.Role == ModelPropertyRole.Birthday)
				width = 12;
			else if(property.Role == ModelPropertyRole.Phone)
				width = 18;
			else if(property.Role == ModelPropertyRole.Address)
				width = 40;
			else if(property.Role == ModelPropertyRole.Currency)
				width = 16;
			else if(property.Role == ModelPropertyRole.Password)
				width = 24;
			else if(property.Role == ModelPropertyRole.Description)
				width = COLUMN_MAX_WIDTH;

			return Math.Clamp(width, COLUMN_MIN_WIDTH, COLUMN_MAX_WIDTH);
		}
	}

	private static void SetColumnSuggestion(IXLRange column, IEnumerable<XLCellValue> entries, bool nullable, ModelPropertyDescriptor property)
	{
		const string SUGGESTION_SHEET_NAME = "__Suggestion_Sheet__";

		var items = entries.ToArray();
		var validation = column.CreateDataValidation();

		if(nullable || items.Any(item => !item.IsText))
		{
			var workbook = column.FirstCell().Worksheet.Workbook;
			var worksheet = workbook.Worksheets.FirstOrDefault(worksheet =>
				worksheet.Visibility == XLWorksheetVisibility.VeryHidden &&
				worksheet.Name.StartsWith(SUGGESTION_SHEET_NAME, StringComparison.Ordinal));

			if(worksheet == null)
			{
				var index = 0;
				var worksheetName = SUGGESTION_SHEET_NAME;

				while(workbook.Worksheets.TryGetWorksheet(worksheetName, out _))
					worksheetName = $"{SUGGESTION_SHEET_NAME}{++index}";

				worksheet = workbook.AddWorksheet(worksheetName);
				worksheet.Visibility = XLWorksheetVisibility.VeryHidden;
			}

			var sourceColumn = (worksheet.LastColumnUsed()?.ColumnNumber() ?? 0) + 1;
			var source = worksheet.Range(1, sourceColumn, items.Length + (nullable ? 1 : 0), sourceColumn);

			for(int index = 0; index < items.Length; index++)
				source.Cell(index + 1, 1).SetValue(items[index]);

			validation.List(source);
		}
		else
			validation.List($"\"{string.Join(',', items.Select(item => item.GetText()))}\"");

		var label = string.IsNullOrEmpty(property.Label) ? property.Name : property.Label;
		SetValidationError(validation, nullable, string.Format(Properties.Resources.SpreadsheetGenerator_ValidationError_List_Message, label));
	}

	private static void SetColumnValidation(IXLRange column, ModelPropertyDescriptor.SimplexPropertyDescriptor property)
	{
		var label = string.IsNullOrEmpty(property.Label) ? property.Name : property.Label;
		var type = property.DataType?.DbType;
		var nullable = property.Nullable ||
			Common.TypeExtension.IsNullable(property.Type) ||
			!property.Sequence.IsEmpty ||
			property.DefaultValue != null;

		if(type == DbType.Date || type == DbType.DateTime || type == DbType.DateTime2)
		{
			var validation = column.CreateDataValidation();
			//ClosedXML 以 OLE 自动化日期保存验证边界，而序号 1 对应 Excel 的 1900-01-01。
			validation.Date.Between(DateTime.FromOADate(1), DateTime.MaxValue.Date);
			SetValidationError(validation, nullable, string.Format(Properties.Resources.SpreadsheetGenerator_ValidationError_Date_Message, label));
			return;
		}

		if(type is DbType.AnsiString or DbType.AnsiStringFixedLength or DbType.String or DbType.StringFixedLength)
		{
			if(property.Length <= 0)
				return;

			var validation = column.CreateDataValidation();

			if(nullable)
				validation.TextLength.EqualOrLessThan(property.Length);
			else
				validation.TextLength.Between(1, property.Length);

			var message = nullable ?
				Properties.Resources.SpreadsheetGenerator_ValidationError_TextLength_Message :
				Properties.Resources.SpreadsheetGenerator_ValidationError_RequiredTextLength_Message;
			SetValidationError(validation, nullable, string.Format(message, label, property.Length));

			return;
		}

		(string Minimum, string Maximum) integerRange = type switch
		{
			DbType.Byte => ("0", byte.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)),
			DbType.SByte => (sbyte.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture), sbyte.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)),
			DbType.Int16 => (short.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture), short.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)),
			DbType.UInt16 => ("0", ushort.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)),
			DbType.Int32 => (int.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture), int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)),
			DbType.UInt32 => ("0", uint.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)),
			_ => (null, null),
		};

		if(integerRange.Minimum != null)
		{
			var validation = column.CreateDataValidation();
			validation.AllowedValues = XLAllowedValues.WholeNumber;
			validation.Operator = XLOperator.Between;
			validation.MinValue = integerRange.Minimum;
			validation.MaxValue = integerRange.Maximum;
			SetValidationError(validation, nullable, string.Format(Properties.Resources.SpreadsheetGenerator_ValidationError_Integer_Message, label, integerRange.Minimum, integerRange.Maximum));
			return;
		}

		(double Minimum, double Maximum) numericRange = type switch
		{
			DbType.Currency or DbType.Decimal or DbType.VarNumeric => ((double)decimal.MinValue, (double)decimal.MaxValue),
			DbType.Single => (-(double)float.MaxValue, (double)float.MaxValue),
			DbType.Double => (-1E+307, 1E+307),
			_ => (0d, 0d),
		};

		if(numericRange != default)
		{
			var validation = column.CreateDataValidation();
			validation.Decimal.Between(numericRange.Minimum, numericRange.Maximum);
			SetValidationError(validation, nullable, string.Format(Properties.Resources.SpreadsheetGenerator_ValidationError_Number_Message, label));
		}
	}

	private static void SetValidationError(IXLDataValidation validation, bool nullable, string message)
	{
		validation.IgnoreBlanks = nullable;
		validation.ShowErrorMessage = true;
		validation.ErrorStyle = XLErrorStyle.Stop;
		validation.ErrorTitle = Properties.Resources.SpreadsheetGenerator_ValidationError_Title;
		validation.ErrorMessage = message;
	}

	private static void GenerateRow(IXLWorksheet worksheet, int row, object record, TableColumn[] columns, IDataArchiveGeneratorOptions options)
	{
		if(record == null)
			return;

		int index = 1;

		foreach(var column in columns)
		{
			//获取表格单元
			var cell = worksheet.Cell(row, index++);

			//获取当前列对应的属性值
			var value = GetValue(ref record, column, options);

			//设置字段内容
			if(value != null)
			{
				if(value.GetType().IsEnum)
					cell.SetCellValue(value.ToString());
				else if(value is DateTime date && date.Year < 1900)
					cell.SetCellValue(date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
				else
					cell.SetCellValue(value);
			}
		}

		static object GetValue(ref object target, TableColumn column, IDataArchiveGeneratorOptions options) =>
			options?.Formatter != null ? options.Formatter.Format(target, column.Property) : column.GetValue(ref target);
	}
	#endregion

	#region 服务匹配
	bool Services.IMatchable.Match(object parameter) => parameter switch
	{
		string format => Spreadsheet.Format.Equals(format),
		IDataTemplate template => Spreadsheet.Format.Equals(template.Format),
		_ => false,
	};
	#endregion

	#region 嵌套子类
	private readonly struct TableColumn
	{
		private readonly Reflection.Expressions.IMemberExpression _expression;

		public TableColumn(int index, ModelPropertyDescriptor.SimplexPropertyDescriptor property)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			this.Index = index > 0 ? index : throw new ArgumentOutOfRangeException(nameof(index));
			this.Label = property.Label;
			this.Description = property.Description;
			this.Property = property;
		}

		public TableColumn(int index, ModelDescriptor model, DataArchiveField descriptor)
		{
			if(model == null)
				throw new ArgumentNullException(nameof(model));
			if(descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));

			this.Index = index > 0 ? index : throw new ArgumentOutOfRangeException(nameof(index));
			this.Label = descriptor?.Label;
			this.Description = descriptor?.Description;

			if(Reflection.Expressions.MemberExpression.TryParse(descriptor.Name, out var expression))
			{
				_expression = expression;

				while(expression != null)
				{
					if(expression.ExpressionType == Reflection.Expressions.MemberExpressionType.Identifier &&
					   model.Properties.TryGetValue(((Reflection.Expressions.IdentifierExpression)expression).Name, out var property))
					{
						this.Property = property;

						if(expression.Next == null && string.IsNullOrEmpty(this.Label))
							this.Label = property.Label;
						if(expression.Next == null && string.IsNullOrEmpty(this.Description))
							this.Description = property.Description;

						if(property.IsComplex(out var complex))
							model = complex.Target;
					}

					expression = expression.Next;
				}
			}
		}

		public Type Type => this.Property.Type;
		public string Name => this.Property.Name;
		public readonly int Index;
		public readonly string Label;
		public readonly string Description;
		public readonly ModelPropertyDescriptor Property;

		public object GetValue(ref object target)
		{
			if(target == null || string.IsNullOrEmpty(this.Name))
				return null;

			return _expression == null ?
				Reflection.Reflector.TryGetValue(ref target, this.Name, out var value) ? value : null :
				Reflection.Expressions.MemberExpressionEvaluator.Default.GetValue(_expression, target);
		}
	}
	#endregion
}
