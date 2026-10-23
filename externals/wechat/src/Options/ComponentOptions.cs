using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Options;

public class ComponentOptions
{
	public string Name { get; set; }
	[JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public string Secret { get; set; }
	[JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public string Token { get; set; }
	[JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public string EncodingAESKey { get; set; }
}
