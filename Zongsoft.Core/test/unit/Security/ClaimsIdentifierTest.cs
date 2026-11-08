using System;
using System.Security.Claims;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Security.Tests;

public class ClaimsIdentifierTest
{
	#region 测试方法
	[Fact]
	public void TestObjectCategory()
	{
		var identity = CreateIdentity();
		var model = identity.AsModel<NamedIdentity>(static (_, _) => false);

		Assert.Equal(17u, model.Identifier.Value);
		Assert.Equal("Custom.Identity", model.Identifier.Name);
	}

	[Fact]
	public void TestDynamicModelCategory()
	{
		var model = CreateIdentity().AsModel<NamedModel>();

		Assert.IsAssignableFrom<Data.IModel>(model);
		Assert.Equal(17u, model.Id);
		Assert.Equal("Custom.Model", ((IIdentifiable)model).Identifier.Name);
	}

	[Fact]
	public void TestMissingCategory()
	{
		var exception = Assert.Throws<ArgumentException>(() => CreateIdentity().AsModel<UndefinedIdentity>(static (_, _) => false));

		Assert.Equal("name", exception.ParamName);
	}
	#endregion

	#region 辅助方法
	private static ClaimsIdentity CreateIdentity() => new([
		new Claim(ClaimTypes.Name, "alice"),
		new Claim(ClaimTypes.NameIdentifier, "17", ClaimValueTypes.UInteger32),
	], "Test");
	#endregion

	#region 测试模型
	private sealed class NamedIdentity : IIdentifiable
	{
		public Identifier Identifier { get; set; } = new("Custom.Identity", null);
	}

	private sealed class UndefinedIdentity : IIdentifiable
	{
		public Identifier Identifier { get; set; }
	}

	public abstract class NamedModel : IIdentifiable
	{
		public abstract uint Id { get; set; }

		Identifier IIdentifiable.Identifier
		{
			get => new("Custom.Model", this.Id);
			set => this.Id = value.TryGetValue("Custom.Model", out uint id) ? id : throw new ArgumentException(null, nameof(value));
		}
	}
	#endregion
}
