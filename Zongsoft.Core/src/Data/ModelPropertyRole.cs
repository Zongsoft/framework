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
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Data;

/// <summary>表示模型属性的语义角色。</summary>
/// <remarks>本结构定义了常用预定义角色，同时允许使用方按需使用自定义角色。</remarks>
[TypeConverter(typeof(ModelPropertyRole.TypeConverter))]
[JsonConverter(typeof(ModelPropertyRole.JsonConverter))]
public readonly struct ModelPropertyRole : IEquatable<ModelPropertyRole>
{
	#region 静态变量
	private static readonly Entry[] _entries;
	#endregion

	#region 静态构造
	static ModelPropertyRole()
	{
		Identifier = Create(nameof(Identifier));
		Code = Create(nameof(Code));
		Name = Create(nameof(Name));
		Title = Create(nameof(Title));
		Email = Create(nameof(Email));
		Gender = Create(nameof(Gender));
		Birthday = Create(nameof(Birthday));
		Phone = Create(nameof(Phone));
		Address = Create(nameof(Address));
		PostalCode = Create(nameof(PostalCode));
		Status = Create(nameof(Status));
		Currency = Create(nameof(Currency));
		Percentage = Create(nameof(Percentage));
		Url = Create(nameof(Url));
		Image = Create(nameof(Image));
		File = Create(nameof(File));
		Password = Create(nameof(Password));
		Description = Create(nameof(Description));

		_entries =
		[
			Identifier.Value,
			Code.Value,
			Name.Value,
			Title.Value,
			Email.Value,
			Gender.Value,
			Birthday.Value,
			Phone.Value,
			Address.Value,
			PostalCode.Value,
			Status.Value,
			Currency.Value,
			Percentage.Value,
			Url.Value,
			Image.Value,
			File.Value,
			Password.Value,
			Description.Value,
		];

		var aliases = new Dictionary<string, string>(_entries.Length * 2, StringComparer.OrdinalIgnoreCase);

		for(int i = 0; i < _entries.Length; i++)
		{
			var entry = _entries[i];
			Add(entry.Name, entry.Name);

			for(int j = 0; j < entry.Aliases.Count; j++)
				Add(entry.Aliases[j], entry.Name);
		}

		void Add(string alias, string role)
		{
			if(string.IsNullOrWhiteSpace(alias))
				return;

			if(aliases.TryGetValue(alias, out var existed))
			{
				if(!string.Equals(existed, role, StringComparison.Ordinal))
					throw new InvalidOperationException($"The '{alias}' model property role alias is already assigned to the '{existed}' role.");

				return;
			}

			aliases.Add(alias, role);
		}
	}
	#endregion

	#region 实例字段
	private readonly Entry _value;
	#endregion

	#region 构造函数
	public ModelPropertyRole(string value)
	{
		value = value?.Trim();

		if(string.IsNullOrEmpty(value))
		{
			_value = default;
			return;
		}

		_value = TryNormalize(value, out var entry) ? entry : new Entry(value);
	}

	private ModelPropertyRole(Entry value) => _value = value;
	#endregion

	#region 实例属性
	/// <summary>获取当前角色的描述项。</summary>
	public Entry Value => _value;
	/// <summary>获取一个值，指示当前角色是否为空。</summary>
	public bool IsEmpty => _value.IsEmpty;
	#endregion

	#region 静态字段
	/// <summary>标识符</summary>
	[Components.Alias("Id")]
	[Components.Alias("Guid")]
	[Components.Alias("Uuid")]
	public static readonly ModelPropertyRole Identifier;

	/// <summary>代码</summary>
	[Components.Alias("No")]
	public static readonly ModelPropertyRole Code;

	/// <summary>名称</summary>
	[Components.Alias("FullName")]
	[Components.Alias("Nickname")]
	[Components.Alias("UserName")]
	[Components.Alias("DisplayName")]
	public static readonly ModelPropertyRole Name;

	/// <summary>标题</summary>
	[Components.Alias("Label")]
	[Components.Alias("Caption")]
	[Components.Alias("Subtitle")]
	[Components.Alias("HeaderName")]
	public static readonly ModelPropertyRole Title;

	/// <summary>邮箱</summary>
	[Components.Alias("EmailAddress")]
	public static readonly ModelPropertyRole Email;

	/// <summary>性别</summary>
	[Components.Alias("Sex")]
	public static readonly ModelPropertyRole Gender;

	/// <summary>生日</summary>
	[Components.Alias("Birthdate")]
	[Components.Alias("DateOfBirth")]
	public static readonly ModelPropertyRole Birthday;

	/// <summary>电话</summary>
	[Components.Alias("Tel")]
	[Components.Alias("Mobile")]
	[Components.Alias("CellPhone")]
	[Components.Alias("Telephone")]
	[Components.Alias("PhoneNumber")]
	public static readonly ModelPropertyRole Phone;

	/// <summary>地址</summary>
	[Components.Alias("City")]
	[Components.Alias("County")]
	[Components.Alias("Street")]
	[Components.Alias("Country")]
	[Components.Alias("Province")]
	[Components.Alias("District")]
	public static readonly ModelPropertyRole Address;

	/// <summary>邮政编码</summary>
	[Components.Alias("ZipCode")]
	[Components.Alias("Postcode")]
	public static readonly ModelPropertyRole PostalCode;

	/// <summary>状态</summary>
	public static readonly ModelPropertyRole Status;

	/// <summary>货币金额</summary>
	[Components.Alias("Fee")]
	[Components.Alias("Cost")]
	[Components.Alias("Money")]
	[Components.Alias("Price")]
	[Components.Alias("Amount")]
	[Components.Alias("Balance")]
	public static readonly ModelPropertyRole Currency;

	/// <summary>百分比</summary>
	[Components.Alias("Percent")]
	[Components.Alias("TaxRate")]
	[Components.Alias("VatRate")]
	[Components.Alias("DutyRate")]
	[Components.Alias("DiscountRate")]
	[Components.Alias("InterestRate")]
	public static readonly ModelPropertyRole Percentage;

	/// <summary>网址</summary>
	[Components.Alias("Uri")]
	[Components.Alias("Link")]
	[Components.Alias("Website")]
	[Components.Alias("Homepage")]
	public static readonly ModelPropertyRole Url;

	/// <summary>图像</summary>
	[Components.Alias("Icon")]
	[Components.Alias("Logo")]
	[Components.Alias("Photo")]
	[Components.Alias("Avatar")]
	[Components.Alias("Picture")]
	public static readonly ModelPropertyRole Image;

	/// <summary>文件</summary>
	[Components.Alias("Filename")]
	[Components.Alias("FilePath")]
	[Components.Alias("Attachment")]
	public static readonly ModelPropertyRole File;

	/// <summary>密码</summary>
	[Components.Alias("Secret")]
	[Components.Alias("PinCode")]
	[Components.Alias("Passcode")]
	public static readonly ModelPropertyRole Password;

	/// <summary>描述信息</summary>
	[Components.Alias("Memo")]
	[Components.Alias("Note")]
	[Components.Alias("Notes")]
	[Components.Alias("Remark")]
	[Components.Alias("Remarks")]
	[Components.Alias("Summary")]
	[Components.Alias("Comment")]
	[Components.Alias("Comments")]
	public static readonly ModelPropertyRole Description;
	#endregion

	#region 公共方法
	/// <summary>根据指定的模型属性名称推断其语义角色。</summary>
	/// <param name="name">要推断的模型属性名称。</param>
	/// <returns>返回推断得到的语义角色；如果无法推断则返回空角色。</returns>
	/// <remarks>
	/// 	<para>推断不区分大小写，并支持 PascalCase、camelCase、连字符、下划线和数字分隔的标识符词元。</para>
	/// 	<para>当存在多个匹配时，依次优先选择完整匹配、后缀词元匹配和前缀词元匹配，相同级别则选择更长的名称或别名。</para>
	/// </remarks>
	public static ModelPropertyRole Determine(string name) => string.IsNullOrWhiteSpace(name) || !TryNormalize(name.Trim(), out var entry) ? default : new(entry);

	/// <summary>获取所有预定义角色描述项的副本。</summary>
	public static Entry[] GetEntries() => (Entry[])_entries.Clone();
	#endregion

	#region 重写方法
	public bool Equals(ModelPropertyRole other) => _value.Equals(other._value);
	public override bool Equals(object obj) => obj is ModelPropertyRole other && this.Equals(other);
	public override int GetHashCode() => _value.GetHashCode();
	public override string ToString() => _value.Name ?? string.Empty;
	#endregion

	#region 符号重写
	public static bool operator ==(ModelPropertyRole left, ModelPropertyRole right) => left.Equals(right);
	public static bool operator !=(ModelPropertyRole left, ModelPropertyRole right) => !left.Equals(right);
	public static bool operator ==(ModelPropertyRole left, string right) => left.Equals(new ModelPropertyRole(right));
	public static bool operator !=(ModelPropertyRole left, string right) => !left.Equals(new ModelPropertyRole(right));
	public static bool operator ==(string left, ModelPropertyRole right) => right.Equals(new ModelPropertyRole(left));
	public static bool operator !=(string left, ModelPropertyRole right) => !right.Equals(new ModelPropertyRole(left));

	public static implicit operator ModelPropertyRole(string value) => new(value);
	public static implicit operator string(ModelPropertyRole role) => role._value.Name;
	#endregion

	#region 私有方法
	private static ModelPropertyRole Create(string name)
	{
		var field = typeof(ModelPropertyRole).GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
		return new(new Entry(name, Components.AliasAttribute.GetAliases(field)));
	}

	private static bool TryNormalize(string name, out Entry result)
	{
		var matched = default(Entry);
		var matchedKind = Entry.MatchKind.None;
		var matchedLength = 0;

		for(int i = 0; i < _entries.Length; i++)
		{
			var entry = _entries[i];
			var kind = entry.Match(name, out var length);

			if((int)kind > (int)matchedKind || (kind == matchedKind && kind != Entry.MatchKind.None && length > matchedLength))
			{
				matched = entry;
				matchedKind = kind;
				matchedLength = length;
			}
		}

		result = matched;
		return matchedKind != Entry.MatchKind.None;
	}
	#endregion

	#region 嵌套结构
	/// <summary>表示属性角色的描述项。</summary>
	public readonly struct Entry : IEquatable<Entry>, IEquatable<string>
	{
		#region 内部构造
		internal Entry(string name, params string[] aliases)
		{
			this.Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
			this.Aliases = aliases == null || aliases.Length == 0 ? Array.Empty<string>() : Array.AsReadOnly((string[])aliases.Clone());
		}
		#endregion

		#region 公共属性
		/// <summary>获取属性角色的标准名。</summary>
		public string Name { get; }
		/// <summary>获取属性角色的只读别名集。</summary>
		public IReadOnlyList<string> Aliases => field ?? [];
		/// <summary>获取一个值，指示当前描述项是否为空。</summary>
		public bool IsEmpty => string.IsNullOrEmpty(this.Name);
		#endregion

		#region 重写方法
		public bool Equals(Entry other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		public bool Equals(string other) => string.Equals(this.Name, other, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is Entry other && this.Equals(other) || obj is string text && this.Equals(text);
		public override int GetHashCode() => this.Name == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name);
		public override string ToString() => this.Aliases.Count == 0 ? this.Name ?? string.Empty : $"{this.Name}({string.Join(',', this.Aliases)})";
		#endregion

		#region 符号重写
		public static bool operator ==(Entry left, Entry right) => left.Equals(right);
		public static bool operator !=(Entry left, Entry right) => !(left == right);
		#endregion

		#region 私有方法
		internal MatchKind Match(string name, out int length)
		{
			var result = Match(name, this.Name);
			length = result == MatchKind.None ? 0 : this.Name.Length;

			for(int i = 0; i < this.Aliases.Count; i++)
			{
				var alias = this.Aliases[i];
				var kind = Match(name, alias);

				if((int)kind > (int)result || (kind == result && kind != MatchKind.None && alias.Length > length))
				{
					result = kind;
					length = alias.Length;
				}
			}

			return result;
		}

		private static MatchKind Match(string name, string alias)
		{
			if(string.Equals(name, alias, StringComparison.OrdinalIgnoreCase))
				return MatchKind.Exact;

			if(name.Length <= alias.Length)
				return MatchKind.None;

			if(name.EndsWith(alias, StringComparison.OrdinalIgnoreCase) && IsBoundary(name, name.Length - alias.Length))
				return MatchKind.Suffix;

			if(name.StartsWith(alias, StringComparison.OrdinalIgnoreCase) && IsBoundary(name, alias.Length))
				return MatchKind.Prefix;

			return MatchKind.None;
		}

		private static bool IsBoundary(string name, int index)
		{
			if(index <= 0 || index >= name.Length)
				return true;

			var previous = name[index - 1];
			var current = name[index];

			if(!char.IsLetterOrDigit(previous) || !char.IsLetterOrDigit(current))
				return true;

			if(char.IsDigit(previous) != char.IsDigit(current))
				return true;

			if(char.IsLower(previous) && char.IsUpper(current))
				return true;

			return char.IsUpper(previous) && char.IsUpper(current) && index + 1 < name.Length && char.IsLower(name[index + 1]);
		}
		#endregion

		#region 嵌套枚举
		internal enum MatchKind
		{
			None,
			Prefix,
			Suffix,
			Exact,
		}
		#endregion
	}
	#endregion

	#region 嵌套子类
	private sealed class TypeConverter : System.ComponentModel.TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
			value is string text ? new ModelPropertyRole(text) : base.ConvertFrom(context, culture, value);

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
			destinationType == typeof(string) && value is ModelPropertyRole role ? role.ToString() : base.ConvertTo(context, culture, value, destinationType);
	}

	private sealed class JsonConverter : JsonConverter<ModelPropertyRole>
	{
		public override ModelPropertyRole Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
		{
			JsonTokenType.Null => default,
			JsonTokenType.String => new(reader.GetString()),
			_ => throw new JsonException(),
		};

		public override void Write(Utf8JsonWriter writer, ModelPropertyRole value, JsonSerializerOptions options)
		{
			if(value.IsEmpty)
				writer.WriteNullValue();
			else
				writer.WriteStringValue(value.ToString());
		}
	}
	#endregion

}
