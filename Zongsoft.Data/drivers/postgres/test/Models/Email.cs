using System;
using System.Globalization;
using System.ComponentModel;

namespace Zongsoft.Data.PostgreSql.Tests.Models;

[TypeConverter(typeof(EmailConverter))]
public readonly struct Email(string name, string domain) : IEquatable<Email>
{
	#region 公共字段
	public readonly string Name = name?.ToLowerInvariant();
	public readonly string Domain = domain?.ToLowerInvariant();
	#endregion

	#region 重写方法
	public bool Equals(Email other) => string.Equals(this.Name, other.Name) && string.Equals(this.Domain, other.Domain);
	public override bool Equals(object obj) => obj is Email other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.Name, this.Domain);
	public override string ToString() => $"{this.Name}@{this.Domain}";
	#endregion

	#region 符号重写
	public static implicit operator string(Email email) => email.ToString();
	public static bool operator ==(Email left, Email right) => left.Equals(right);
	public static bool operator !=(Email left, Email right) => !(left == right);
	#endregion
}

internal sealed class EmailConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => value.ToString();
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if(value is null || Convert.IsDBNull(value))
			return default;

		if(value is string text)
		{
			var index = text.LastIndexOf('@');

			return index < 0 ?
				new Email(text, string.Empty) :
				new Email(text[..index], text[(index + 1)..]);
		}

		throw new InvalidCastException($"The conversion of the ‘{value.GetType()}’ type to the {nameof(Email)} type is not supported.");
	}
}