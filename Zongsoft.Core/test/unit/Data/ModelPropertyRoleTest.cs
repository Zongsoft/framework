using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

using Xunit;

namespace Zongsoft.Data.Tests;

public class ModelPropertyRoleTest
{
	[Theory]
	[InlineData("Identifier", "Identifier")]
	[InlineData("Id", "Identifier")]
	[InlineData("Guid", "Identifier")]
	[InlineData("Uuid", "Identifier")]
	[InlineData("Code", "Code")]
	[InlineData("No", "Code")]
	[InlineData("Name", "Name")]
	[InlineData("FullName", "Name")]
	[InlineData("Nickname", "Name")]
	[InlineData("UserName", "Name")]
	[InlineData("DisplayName", "Name")]
	[InlineData("Title", "Title")]
	[InlineData("Label", "Title")]
	[InlineData("Caption", "Title")]
	[InlineData("HeaderName", "Title")]
	[InlineData("Email", "Email")]
	[InlineData("EmailAddress", "Email")]
	[InlineData("Gender", "Gender")]
	[InlineData("Sex", "Gender")]
	[InlineData("Birthday", "Birthday")]
	[InlineData("Birthdate", "Birthday")]
	[InlineData("DateOfBirth", "Birthday")]
	[InlineData("Phone", "Phone")]
	[InlineData("Tel", "Phone")]
	[InlineData("Mobile", "Phone")]
	[InlineData("CellPhone", "Phone")]
	[InlineData("Telephone", "Phone")]
	[InlineData("PhoneNumber", "Phone")]
	[InlineData("Address", "Address")]
	[InlineData("City", "Address")]
	[InlineData("County", "Address")]
	[InlineData("Street", "Address")]
	[InlineData("Country", "Address")]
	[InlineData("Province", "Address")]
	[InlineData("District", "Address")]
	[InlineData("PostalCode", "PostalCode")]
	[InlineData("ZipCode", "PostalCode")]
	[InlineData("Postcode", "PostalCode")]
	[InlineData("Status", "Status")]
	[InlineData("Currency", "Currency")]
	[InlineData("Fee", "Currency")]
	[InlineData("Cost", "Currency")]
	[InlineData("Money", "Currency")]
	[InlineData("Price", "Currency")]
	[InlineData("Amount", "Currency")]
	[InlineData("Balance", "Currency")]
	[InlineData("Percentage", "Percentage")]
	[InlineData("Percent", "Percentage")]
	[InlineData("TaxRate", "Percentage")]
	[InlineData("VatRate", "Percentage")]
	[InlineData("DutyRate", "Percentage")]
	[InlineData("DiscountRate", "Percentage")]
	[InlineData("InterestRate", "Percentage")]
	[InlineData("Url", "Url")]
	[InlineData("Uri", "Url")]
	[InlineData("Link", "Url")]
	[InlineData("Website", "Url")]
	[InlineData("Homepage", "Url")]
	[InlineData("Image", "Image")]
	[InlineData("Icon", "Image")]
	[InlineData("Logo", "Image")]
	[InlineData("Photo", "Image")]
	[InlineData("Avatar", "Image")]
	[InlineData("Picture", "Image")]
	[InlineData("File", "File")]
	[InlineData("Filename", "File")]
	[InlineData("FilePath", "File")]
	[InlineData("Attachment", "File")]
	[InlineData("Password", "Password")]
	[InlineData("Secret", "Password")]
	[InlineData("PinCode", "Password")]
	[InlineData("Passcode", "Password")]
	[InlineData("Description", "Description")]
	[InlineData("Memo", "Description")]
	[InlineData("Note", "Description")]
	[InlineData("Notes", "Description")]
	[InlineData("Remark", "Description")]
	[InlineData("Remarks", "Description")]
	[InlineData("Summary", "Description")]
	[InlineData("Comment", "Description")]
	[InlineData("Comments", "Description")]
	public void PredefinedNameOrAlias_NormalizesToCanonicalRole(string value, string expected)
	{
		var role = new ModelPropertyRole(value);

		Assert.Equal(expected, role.ToString());
		Assert.Equal(expected, ModelPropertyRole.Determine(value).ToString());
	}

	[Theory]
	[InlineData("UserId", "Identifier")]
	[InlineData("userId", "Identifier")]
	[InlineData("user-id", "Identifier")]
	[InlineData("user_id", "Identifier")]
	[InlineData("IdOfUser", "Identifier")]
	[InlineData("id-user", "Identifier")]
	[InlineData("HeaderName", "Title")]
	[InlineData("CountryCode", "Code")]
	[InlineData("ShippingPostalCode", "PostalCode")]
	[InlineData("BillingZipCode", "PostalCode")]
	[InlineData("SalesTaxRate", "Percentage")]
	[InlineData("XMLFile", "File")]
	public void Determine_TokenizedName_UsesDocumentedBoundaryAndPriority(string name, string expected) =>
		Assert.Equal(expected, ModelPropertyRole.Determine(name).ToString());

	[Fact]
	public void Constructor_UnknownCustomRole_PreservesTrimmedValue()
	{
		var role = new ModelPropertyRole("  Audit.Custom  ");

		Assert.False(role.IsEmpty);
		Assert.Equal("Audit.Custom", role.ToString());
		Assert.Equal("Audit.Custom", role.Value.Name);
		Assert.Empty(role.Value.Aliases);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("UnrecognizedRole")]
	[InlineData("Node")]
	[InlineData("Nonce")]
	[InlineData("EndTimeUnixNano")]
	[InlineData("Namespace")]
	[InlineData("Multiplicity")]
	[InlineData("Concurrency")]
	[InlineData("FrameRate")]
	[InlineData("TaxIncluded")]
	public void Determine_UnknownName_ReturnsEmptyRole(string name)
	{
		var role = ModelPropertyRole.Determine(name);

		Assert.True(role.IsEmpty);
		Assert.Equal(default, role);
		Assert.Equal(string.Empty, role.ToString());
	}

	[Fact]
	public void EqualityAndImplicitConversions_AreCaseInsensitiveAndCanonical()
	{
		ModelPropertyRole known = "id";
		string knownText = known;
		ModelPropertyRole custom = "  Audit  ";
		string customText = custom;

		Assert.Equal(ModelPropertyRole.Identifier, known);
		Assert.True(known == "IDENTIFIER");
		Assert.True("GUID" == known);
		Assert.False(known != "uuid");
		Assert.Equal("Identifier", knownText);
		Assert.Equal(new ModelPropertyRole("audit"), custom);
		Assert.Equal(new ModelPropertyRole("AUDIT").GetHashCode(), custom.GetHashCode());
		Assert.Equal("Audit", customText);
		Assert.Null((string)default(ModelPropertyRole));
	}

	[Fact]
	public void GetEntries_ReturnsDefensiveCopy()
	{
		var entries = ModelPropertyRole.GetEntries();
		var copy = ModelPropertyRole.GetEntries();
		var expected = new[]
		{
			"Identifier", "Code", "Name", "Title", "Email", "Gender", "Birthday", "Phone", "Address",
			"PostalCode", "Status", "Currency", "Percentage", "Url", "Image", "File", "Password", "Description",
		};

		Assert.NotSame(entries, copy);
		Assert.Equal(expected, entries.Select(entry => entry.Name));

		entries[0] = default;

		Assert.True(entries[0].IsEmpty);
		Assert.Equal("Identifier", copy[0].Name);
		Assert.Equal("Identifier", ModelPropertyRole.GetEntries()[0].Name);
	}

	[Fact]
	public void EntryAliases_AreReadOnly()
	{
		var identifier = ModelPropertyRole.GetEntries().Single(entry => entry.Equals("Identifier"));
		var postalCode = ModelPropertyRole.GetEntries().Single(entry => entry.Equals("PostalCode"));
		var percentage = ModelPropertyRole.GetEntries().Single(entry => entry.Equals("Percentage"));
		var aliases = Assert.IsAssignableFrom<IList<string>>(identifier.Aliases);

		Assert.Equal(new[] { "Id", "Guid", "Uuid" }, aliases);
		Assert.Equal(new[] { "ZipCode", "Postcode" }, postalCode.Aliases);
		Assert.Equal(new[] { "Percent", "TaxRate", "VatRate", "DutyRate", "DiscountRate", "InterestRate" }, percentage.Aliases);
		Assert.Throws<NotSupportedException>(() => aliases[0] = "Changed");
		Assert.Equal("Id", ModelPropertyRole.Identifier.Value.Aliases[0]);
	}

	[Fact]
	public void ModelPropertyDescriptor_Role_UsesValueTypeInferenceAndAttributeOverride()
	{
		var descriptor = new ModelDescriptor(typeof(DescriptorModel));

		Assert.True(descriptor.Properties.TryGetValue(nameof(DescriptorModel.UserId), out var inferred));
		Assert.IsType<ModelPropertyRole>((object)inferred.Role);
		Assert.Equal(ModelPropertyRole.Identifier, inferred.Role);
		Assert.Equal("Identifier", inferred.Role.ToString());

		Assert.True(descriptor.Properties.TryGetValue(nameof(DescriptorModel.HeaderName), out var explicitRole));
		Assert.IsType<ModelPropertyRole>((object)explicitRole.Role);
		Assert.Equal(new ModelPropertyRole("Audit.Custom"), explicitRole.Role);
		Assert.NotEqual(ModelPropertyRole.Title, explicitRole.Role);
	}

	[Fact]
	public void JsonConverter_WritesStringsAndReadsKnownCustomAndEmptyRoles()
	{
		Assert.Equal("\"Identifier\"", JsonSerializer.Serialize(ModelPropertyRole.Identifier));
		Assert.Equal("\"Audit.Custom\"", JsonSerializer.Serialize(new ModelPropertyRole("Audit.Custom")));
		Assert.Equal("null", JsonSerializer.Serialize(default(ModelPropertyRole)));

		var known = JsonSerializer.Deserialize<ModelPropertyRole>("\"id\"");
		var custom = JsonSerializer.Deserialize<ModelPropertyRole>("\"Audit.Custom\"");
		var empty = JsonSerializer.Deserialize<ModelPropertyRole>("null");

		Assert.Equal(ModelPropertyRole.Identifier, known);
		Assert.Equal("Identifier", known.ToString());
		Assert.Equal("Audit.Custom", custom.ToString());
		Assert.True(empty.IsEmpty);
		Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ModelPropertyRole>("42"));

		var descriptor = new ModelPropertyDescriptor.SimplexPropertyDescriptor
		{
			Name = "UserId",
			Role = ModelPropertyRole.Identifier,
		};
		var json = JsonSerializer.Serialize(descriptor);

		using var document = JsonDocument.Parse(json);
		var roleElement = document.RootElement.GetProperty(nameof(ModelPropertyDescriptor.Role));
		Assert.Equal(JsonValueKind.String, roleElement.ValueKind);
		Assert.Equal("Identifier", roleElement.GetString());

		var restored = JsonSerializer.Deserialize<ModelPropertyDescriptor.SimplexPropertyDescriptor>(json);
		Assert.NotNull(restored);
		Assert.Equal(ModelPropertyRole.Identifier, restored.Role);
	}

	private class DescriptorModel
	{
		public int UserId { get; set; }

		[ModelProperty(Role = "  Audit.Custom  ")]
		public string HeaderName { get; set; }
	}
}
