using System;
using System.Collections;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data;
using Zongsoft.Security.Membership;

namespace Zongsoft.Serialization
{
	public class JsonSerializerTest
	{
		private const int INTEGER = int.MaxValue;
		private const double DOUBLE = double.MaxValue;
		private const string STRING = nameof(String);

		[Fact]
		public void TestSerialize()
		{
			var user = CreateUser();
			var json = Serializer.Json.Serialize(user);
			Assert.NotEmpty(json);
			var userResult = Serializer.Json.Deserialize<IUserModel>(json);
			Assert.NotNull(userResult);
			Assert.Equal(((IModel)user).GetCount(), ((IModel)userResult).GetCount());
			Assert.True(UserComparer.Instance.Equals(user, userResult));

			var credential = CreateCredential();
			json = Serializer.Json.Serialize(credential, new TextSerializationOptions()
			{
				IgnoreNull = true,
				Indented = true,
				NamingConvention = SerializationNamingConvention.Camel,
			});
			Assert.NotEmpty(json);
			var credentialResult = Serializer.Json.Deserialize<Credential>(json);
			Assert.NotNull(credentialResult);
			Assert.Equal(credential, credentialResult);
		}

		[Fact]
		public void TestSerializeDataDictionary()
		{
			var dictionary = DataDictionary.GetDictionary(CreateUser());
			var json = Serializer.Json.Serialize(dictionary);
			Assert.NotEmpty(json);

			var result = Serializer.Json.Deserialize<IDataDictionary>(json);
			Assert.NotNull(result);
			Assert.Equal(((ICollection)dictionary).Count, ((ICollection)result).Count);
		}

		[Fact]
		public void TestSerializeClassicDictionary()
		{
			var dictionary = new Hashtable
			{
				["Null"] = null,
				["Byte"] = byte.MaxValue,
				["SByte"] = sbyte.MaxValue,
				["Int16"] = short.MaxValue,
				["UInt16"] = ushort.MaxValue,
				["Int32"] = int.MaxValue,
				["UInt32"] = uint.MaxValue,
				["Int64"] = long.MaxValue,
				["UInt64"] = ulong.MaxValue,
				["TimeSpan"] = TimeSpan.Parse("1.23:30:59.500"),
				["Date"] = DateOnly.FromDateTime(DateTime.Now),
				["Time"] = TimeOnly.FromDateTime(DateTime.Now),
				["DateTime"] = DateTime.Now,
				["DateTimeOffset"] = DateTimeOffset.UtcNow,
				["Guid"] = Guid.NewGuid(),
				["Double"] = Double.MaxValue,
				["Single"] = Single.MaxValue,
				["Decimal"] = decimal.MaxValue,
				["Boolean.True"] = true,
				["Boolean.False"] = false,
				["String"] = typeof(string).Name,
				["User"] = CreateUser(),
			};

			var json = Serializer.Json.Serialize(dictionary);
			Assert.NotEmpty(json);

			var result = Serializer.Json.Deserialize<Hashtable>(json);
			Assert.NotNull(result);
			Assert.NotEmpty(result);
			Assert.Equal(dictionary.Count, result.Count);

			foreach(DictionaryEntry entry in dictionary)
			{
				Assert.True(result.Contains(entry.Key));
				var value = result[entry.Key];

				if(value is IUserModel user)
					Assert.True(UserComparer.Instance.Equals((IUserModel)entry.Value, user));
				else
					Assert.Equal(entry.Value, value);
			}

			var model = CreateUser();
			json = Serializer.Json.Serialize(model);
			Assert.NotEmpty(json);
			var result2 = Serializer.Json.Deserialize<IDictionary>(json);
			Assert.NotNull(result2);
			Assert.NotEmpty(result2);
			Assert.Equal(((IModel)model).GetCount(), result2.Count);
		}

		[Fact]
		public void TestSerializeGenericDictionary()
		{
			var dictionary = new Dictionary<string, object>
			{
				["Null"] = null,
				["Byte"] = byte.MaxValue,
				["SByte"] = sbyte.MaxValue,
				["Int16"] = short.MaxValue,
				["UInt16"] = ushort.MaxValue,
				["Int32"] = int.MaxValue,
				["UInt32"] = uint.MaxValue,
				["Int64"] = long.MaxValue,
				["UInt64"] = ulong.MaxValue,
				["TimeSpan"] = TimeSpan.Parse("1.23:30:59.500"),
				["Date"] = DateOnly.FromDateTime(DateTime.Now),
				["Time"] = TimeOnly.FromDateTime(DateTime.Now),
				["DateTime"] = DateTime.Now,
				["DateTimeOffset"] = DateTimeOffset.UtcNow,
				["Guid"] = Guid.NewGuid(),
				["Double"] = Double.MaxValue,
				["Single"] = Single.MaxValue,
				["Decimal"] = decimal.MaxValue,
				["Boolean.True"] = true,
				["Boolean.False"] = false,
				["String"] = typeof(string).Name,
				["User"] = CreateUser(),
			};

			var json = Serializer.Json.Serialize(dictionary, TextSerializationOptions.Pascal(true));
			Assert.NotEmpty(json);

			var result = Serializer.Json.Deserialize<Dictionary<string, object>>(json);
			Assert.NotNull(result);
			Assert.NotEmpty(result);
			Assert.Equal(dictionary.Count, result.Count);

			foreach(var entry in dictionary)
			{
				Assert.True(result.TryGetValue(entry.Key, out var value));

				if(value is IUserModel user)
					Assert.True(UserComparer.Instance.Equals((IUserModel)entry.Value, user));
				else
					Assert.Equal(entry.Value, value);
			}

			var model = CreateUser();
			json = Serializer.Json.Serialize(model);
			Assert.NotEmpty(json);
			var result2 = Serializer.Json.Deserialize<IDictionary<string, object>>(json);
			Assert.NotNull(result2);
			Assert.NotEmpty(result2);
			Assert.Equal(((IModel)model).GetCount(), result2.Count);
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
	""nickname"": ""钟少"",
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
			Assert.Equal("钟少", credential.User.Nickname);
			Assert.Equal(DateTime.Parse("2020-05-12T23:33:51"), credential.User.Creation);
		}

		[Fact]
		public void TestDeserializeGenericDictionary()
		{
			var json = $$"""
{
	"integer":{{INTEGER}},
	"double":{{DOUBLE}},
	"string":"{{STRING}}",
	"true":true,
	"false":false,
	"null":null,
	"integers":[1,2,3],
	"booleans":[true, false],
	"strings":["String#1", "String#2"],
	"object":{
		"double":{{DOUBLE}},
		"object":{
			"short":{{short.MaxValue}}
		},
		"null":null
	},
	"objects":[
		null,
		{{INTEGER}},
		{{DOUBLE}},
		"{{STRING}}",
		true,
		false,
		{
			"array":[{},null,{}]
		}
	]
}
""";

			var dictionary = Serializer.Json.Deserialize<Dictionary<string, object>>(json);
			Assert.NotNull(dictionary);
			Assert.NotEmpty(dictionary);
			Assert.Equal(11, dictionary.Count);
			Assert.True(dictionary.TryGetValue("integer", out var value));
			Assert.Equal(INTEGER, value);
			Assert.True(dictionary.TryGetValue("double", out value));
			Assert.Equal(DOUBLE, value);
			Assert.True(dictionary.TryGetValue("string", out value));
			Assert.Equal(STRING, value);
			Assert.True(dictionary.TryGetValue("true", out value));
			Assert.Equal(true, value);
			Assert.True(dictionary.TryGetValue("false", out value));
			Assert.Equal(false, value);
			Assert.True(dictionary.TryGetValue("null", out value));
			Assert.Null(value);

			Assert.True(dictionary.TryGetValue("integers", out value));
			Assert.NotNull(value);
			Assert.IsType<object[]>(value);
			Assert.Equal([1, 2, 3], (object[])value);

			Assert.True(dictionary.TryGetValue("booleans", out value));
			Assert.NotNull(value);
			Assert.IsType<object[]>(value);
			Assert.Equal([true, false], (object[])value);

			Assert.True(dictionary.TryGetValue("strings", out value));
			Assert.NotNull(value);
			Assert.IsType<object[]>(value);
			Assert.Equal(["String#1", "String#2"], (object[])value);

			Assert.True(dictionary.TryGetValue("object", out value));
			Assert.NotNull(value);
			Assert.IsType<Dictionary<string, object>>(value);

			var children = (Dictionary<string, object>)value;
			Assert.NotEmpty(children);
			Assert.Equal(3, children.Count);

			Assert.True(children.TryGetValue("null", out value));
			Assert.Null(value);
			Assert.True(children.TryGetValue("double", out value));
			Assert.IsType<double>(value);
			Assert.Equal(DOUBLE, value);

			Assert.True(children.TryGetValue("object", out value));
			Assert.NotNull(value);
			Assert.IsType<Dictionary<string, object>>(value);

			children = (Dictionary<string, object>)value;
			Assert.NotEmpty(children);
			Assert.Single(children);

			Assert.True(children.TryGetValue("short", out value));
			Assert.IsType<int>(value);
			Assert.Equal((int)short.MaxValue, value);

			Assert.True(dictionary.TryGetValue("objects", out value));
			Assert.NotNull(value);
			Assert.IsType<object[]>(value);

			var array = (object[])value;
			Assert.NotEmpty(array);
			Assert.Equal(7, array.Length);

			Assert.Null(array[0]);
			Assert.Equal(INTEGER, array[1]);
			Assert.Equal(DOUBLE, array[2]);
			Assert.Equal(STRING, array[3]);
			Assert.Equal(true, array[4]);
			Assert.Equal(false, array[5]);

			Assert.NotNull(array[6]);
			Assert.IsType<Dictionary<string, object>>(array[6]);
			children = (Dictionary<string, object>)array[6];
			Assert.Single(children);
			Assert.True(children.TryGetValue("array", out value));
			Assert.NotNull(value);
			Assert.IsType<object[]>(value);

			array = (object[])value;
			Assert.NotEmpty(array);
			Assert.Equal(3, array.Length);
			Assert.Null(array[0]);
			Assert.Null(array[1]);
			Assert.Null(array[2]);
		}

		private static IUserModel CreateUser() => Model.Build<IUserModel>(p =>
		{
			p.UserId = 100;
			p.Name = "Popeye";
			p.Nickname = "钟少";
		});

		private static Credential CreateCredential() => new()
		{
			CredentialId = "123",
			RenewalToken = "666",
			Expiration = TimeSpan.FromHours(4),
			User = CreateUser(),
		};

		public class Credential : IEquatable<Credential>
		{
			public string CredentialId { get; set; }
			public string RenewalToken { get; set; }
			public TimeSpan Expiration { get; set; }
			public int Count { get; set; }
			public IUserModel User { get; set; }

			public bool Equals(Credential other) => other is not null &&
				this.CredentialId == other.CredentialId &&
				this.RenewalToken == other.RenewalToken &&
				this.Expiration == other.Expiration &&
				this.Count == other.Count &&
				UserComparer.Instance.Equals(this.User, other.User);

			public override bool Equals(object obj) => obj is Credential other && this.Equals(other);
			public override int GetHashCode() => HashCode.Combine(this.CredentialId.ToUpperInvariant(), this.RenewalToken.ToUpperInvariant());
			public override string ToString() => $"[{this.Expiration}] {this.CredentialId} ({this.RenewalToken})";
		}

		public sealed class UserComparer : IEqualityComparer<IUserModel>
		{
			public static readonly UserComparer Instance = new();

			public bool Equals(IUserModel x, IUserModel y)
			{
				if(x is null)
					return y is null;
				if(y is null)
					return false;

				return x.UserId == y.UserId &&
					x.Name == y.Name &&
					x.Nickname == y.Nickname &&
					x.Namespace == y.Namespace &&
					x.Email == y.Email &&
					x.Phone == y.Phone &&
					x.Status == y.Status &&
					x.StatusTimestamp == y.StatusTimestamp;
			}

			public int GetHashCode(IUserModel user) => user is null ? 0 : HashCode.Combine(user.UserId, user.Name.ToUpperInvariant(), user.Namespace.ToUpperInvariant());
		}
	}
}
