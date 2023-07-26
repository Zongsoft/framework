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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using ClosedXML;
using ClosedXML.Excel;

using Zongsoft.Data;
using Zongsoft.Data.Templates;

namespace Zongsoft.Externals.ClosedXml
{
	[Zongsoft.Services.Service(typeof(IDataArchiveGenerator))]
	public class SpreadsheetGenerator : IDataArchiveGenerator, Services.IMatchable
	{
		#region 常量定义
		private const string FONT_NAME = "Arial Narrow"; //偏爱的字体：适用于主键、代号、枚举、电话号码、邮箱地址等
		#endregion

		#region 公共属性
		public string Format => SpreadsheetFormat.Name;
		#endregion

		#region 公共方法
		public ValueTask GenerateAsync(Stream output, ModelDescriptor model, object data, CancellationToken cancellation = default) => this.GenerateAsync(output, model, data, null, cancellation);
		public ValueTask GenerateAsync(Stream output, ModelDescriptor model, object data, IDataArchiveGeneratorOptions options, CancellationToken cancellation = default)
		{
			if(output == null)
				throw new ArgumentNullException(nameof(output));
			if(model == null)
				throw new ArgumentNullException(nameof(model));

			//获取要导出的数据列
			var columns = GetColumns(model, options?.Fields).ToArray();

			if(columns == null || columns.Length == 0)
				return ValueTask.CompletedTask;

			using var workbook = new XLWorkbook();
			var worksheet = workbook.AddWorksheet(model.Title ?? model.Name);
			worksheet.Style.Font.SetFontSize(12);
			worksheet.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

			if(columns.Length > 5)
				worksheet.PageSetup.SetPageOrientation(XLPageOrientation.Landscape);

			//生成数据文件标题
			worksheet.Cell(1, 1).SetValue(model.Title);
			worksheet.Row(1).Height = 45;
			worksheet.Row(1).Style.Font.SetFontSize(18);
			worksheet.Row(1).Style.Font.SetBold(true);
			worksheet.Row(1).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
			worksheet.Row(1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
			var range = worksheet.Range(1, 1, 1, columns.Length);
			range.Style.Fill.SetPatternType(XLFillPatternValues.Gray125);
			range.Style.Fill.SetPatternColor(XLColor.Green);
			range.Style.Fill.SetBackgroundColor(XLColor.TeaGreen);
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
			range.Style.Border.BottomBorder = XLBorderStyleValues.Thick;
			range.Style.Border.BottomBorderColor = XLColor.CoolBlack;
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

						if(string.IsNullOrEmpty(entries[i].Description))
							comment.AddText($"[{entries[i].Value}] {entries[i].Name}");
						else
							comment.AddText($"[{entries[i].Value}] {entries[i].Name}:{entries[i].Description}");
					}
				}
			}

			//设置数据字段标题行样式
			worksheet.Row(3).Height = 30;
			worksheet.Row(3).Style.Font.SetFontSize(13);
			worksheet.Row(3).Style.Font.SetBold(true);
			worksheet.Row(3).Style.Font.SetFontColor(XLColor.Navy);
			worksheet.Row(3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
			worksheet.Row(3).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
			range = worksheet.Range(3, 1, 3, columns.Length);
			range.Style.Fill.SetPatternType(XLFillPatternValues.Gray0625);
			range.Style.Fill.SetPatternColor(XLColor.Orange);
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(252, 213, 180));
			range.Style.Border.BottomBorder = XLBorderStyleValues.Double;
			range.Style.Border.BottomBorderColor = XLColor.DarkRed;

			//数据区起始行号
			var row = 4;

			//遍历生成数据区
			if(data is IEnumerable items)
			{
				foreach(var item in items)
					GenerateRow(worksheet, row++, item, columns, options);
			}
			else
			{
				GenerateRow(worksheet, row, data, columns, options);
			}

			//设置数据区各列的样式
			foreach(var column in columns)
			{
				range = worksheet.Range(4, column.Index, row, column.Index);
				SetDataColumnStyle(range, column.Property);
			}

			if(row > 4)
				row--;

			//创建数据区域范围
			var table = worksheet.Range(4, 1, row, columns.Length)
				.AddToNamed(model.Name, XLScope.Worksheet, model.Title);

			//设置数据取悦的外边框
			range = worksheet.Range(1, 1, row, columns.Length);
			range.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
			range.Style.Border.OutsideBorderColor = XLColor.CoolBlack;

			//调整各个字段列宽
			worksheet.ColumnsUsed().AdjustToContents();

			//写入到输出流
			workbook.SaveAs(output);

			return ValueTask.CompletedTask;
		}
		#endregion

		#region 私有方法
		private static IEnumerable<TableColumn> GetColumns(ModelDescriptor model, DataArchiveField[] fields)
		{
			int index = 1;

			if(fields != null && fields.Length > 0)
			{
				for(int i = 0; i < fields.Length; i++)
				{
					var field = fields[i];
					if(model.Properties.TryGetValue(field.Name, out var property))
						yield return new TableColumn(index++, property, field);
				}
			}
			else
			{
				foreach(var property in model.Properties)
				{
					if(property.Field == null || property.Field.IsSimplex)
						yield return new TableColumn(index++, property);
				}
			}
		}

		private static void SetDataColumnStyle(IXLRange column, ModelPropertyDescriptor property)
		{
			//如果是特定类型则调整其样式
			if(!Common.TypeExtension.IsNullable(property.Type, out var type))
				type = property.Type;

			//设置特定类型的字体
			if(type.IsEnum || Common.TypeExtension.IsNumeric(type) || type == typeof(Guid) ||
			   type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
			{
				column.Style.Font.SetFontName(FONT_NAME);
			}

			//特定类型则设置其水平居中
			if(type.IsEnum || type == typeof(byte) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
				column.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

			//设置日期时间类型的格式
			if(type == typeof(DateTime) || type == typeof(DateTimeOffset))
				column.Style.DateFormat.SetFormat("yyyy-MM-dd");

			//设置特定语义角色的样式
			switch(property.Role)
			{
				case ModelPropertyRole.Code:
				case ModelPropertyRole.Phone:
				case ModelPropertyRole.Email:
					column.Style.Font.SetFontName(FONT_NAME);
					column.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
					break;
				case ModelPropertyRole.Currency:
					column.Style.Font.SetFontName(FONT_NAME);
					column.Style.NumberFormat.SetFormat("0.00");
					break;
				case ModelPropertyRole.Description:
					column.FirstColumn().WorksheetColumn().Width = 20;
					break;
			}

			//设置主键的样式
			if(property.Field != null && property.Field.IsPrimaryKey)
			{
				column.Style.Font.SetBold(true);
				column.Style.Font.SetFontName(FONT_NAME);
				column.Style.Font.SetFontColor(XLColor.Navy);
				column.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
			}
		}

		private static void GenerateRow(IXLWorksheet worksheet, int row, object record, TableColumn[] columns, IDataArchiveGeneratorOptions options)
		{
			int index = 1;

			foreach(var column in columns)
			{
				//获取表格单元
				var cell = worksheet.Cell(row, index++);

				//获取当前列对应的属性值
				var value = GetValue(ref record, column.Property, options);

				//设置字段内容
				if(value != null)
				{
					if(value.GetType().IsEnum)
						cell.SetCellValue(value.ToString());
					else
						cell.SetCellValue(value);
				}
			}

			worksheet.Row(row).Height = 20;
			var range = worksheet.Range(row, 1, row, columns.Length);
			range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
			range.Style.Border.BottomBorderColor = XLColor.LightGray;

			if(row % 2 == 1)
			{
				range.Style.Fill.SetPatternType(XLFillPatternValues.Gray0625);
				range.Style.Fill.SetPatternColor(XLColor.LightGray);
				range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(240, 240, 240));
			}

			static object GetValue(ref object target, ModelPropertyDescriptor property, IDataArchiveGeneratorOptions options)
			{
				if(options != null && options.Formatter != null)
					return options.Formatter(target, property);

				return Reflection.Reflector.TryGetValue(ref target, property.Name, out var value) ? value : null;
			}
		}
		#endregion

		#region 服务匹配
		bool Services.IMatchable.Match(object parameter) => parameter is string format && SpreadsheetFormat.IsFormat(format);
		#endregion

		#region 嵌套子类
		private readonly struct TableColumn
		{
			public TableColumn(int index, ModelPropertyDescriptor property, DataArchiveField field = null)
			{
				this.Index = index > 0 ? index : throw new ArgumentOutOfRangeException(nameof(index));
				this.Property = property ?? throw new ArgumentNullException(nameof(property));
				this.Field = field;
			}

			public readonly int Index;
			public readonly ModelPropertyDescriptor Property;
			public readonly DataArchiveField Field;

			public string Name => this.Property.Name;
			public Type Type => this.Property.Type;
			public string Label => string.IsNullOrEmpty(this.Field?.Label) ? this.Property.Label : this.Field.Label;
			public string Description => string.IsNullOrEmpty(this.Field?.Description) ? this.Property.Description : this.Field.Description;
		}
		#endregion
	}
}