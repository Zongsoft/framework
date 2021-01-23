using System;
using System.Text.Json;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data;
using Zongsoft.Security.Membership;

namespace Zongsoft.Serialization
{
	public class JsonSerializerTest
	{
		[Fact]
		public void TestSerialize()
		{
			var user = Model.Build<IUser>(p =>
			{
				p.UserId = 100;
				p.Name = "Popeye";
				p.FullName = "钟少";
			});

			var credential = new Credential()
			{
				CredentialId = "123",
				RenewalToken = "666",
				Expiration = TimeSpan.FromHours(4),
				User = user,
			};

			var text = Serializer.Json.Serialize(credential, new TextSerializationOptions()
			{
				IgnoreNull = true,
				Indented = true,
				NamingConvention = SerializationNamingConvention.Camel,
			});

			Assert.NotEmpty(text);
		}

		[Fact]
		public void TestDeserialize()
		{
			var text = @"{
""CredentialId"": ""123"",
""RenewalToken"": ""666"",
""Scenario"": ""web"",
""Expiration"": ""04:00:00"",
""Count"":""69"",
""User"": {
	""creation"": ""2020-05-12T23:33:51"",
	""properties"": {
		""roles"": [""Administrators"", ""Users"" ]
	},
	""userId"": 100,
	""name"": ""Popeye"",
	""fullName"": ""钟少"",
	""namespace"": ""automao"",
	""description"": ""钟峰""
}}";

			var credential = Serializer.Json.Deserialize<Credential>(text);

			Assert.NotNull(credential);
			Assert.Equal("123", credential.CredentialId);
			Assert.Equal("666", credential.RenewalToken);
			Assert.Equal(69, credential.Count);
			Assert.Equal(TimeSpan.FromHours(4), credential.Expiration);

			Assert.NotNull(credential.User);
			Assert.Equal(100u, credential.User.UserId);
			Assert.Equal("Popeye", credential.User.Name);
			Assert.Equal("钟少", credential.User.FullName);
		}

		public class Credential
		{
			public string CredentialId { get; set; }
			public string RenewalToken { get; set; }
			public TimeSpan Expiration { get; set; }
			public int Count { get; set; }

			public IUser User { get; set; }
		}
	}
}
