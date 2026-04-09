using System;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Data.Tests;

public class MappingTest
{
	[Fact]
	public void Script()
	{
		const int COUNT = 10000;

		Assert.Empty(Mapping.Commands);
		Assert.Empty(Mapping.Entities);

		var command = Mapping.Commands.Script("MySql", "SELECT * FROM Table");
		Assert.NotNull(command);
		Assert.NotNull(command.Name);
		Assert.NotEmpty(command.Name);
		Assert.NotNull(command.Scriptor);
		Assert.Single(Mapping.Commands);

		var other = Mapping.Commands.Script("MySql", "SELECT * FROM Table");
		Assert.NotNull(other);
		Assert.Equal(command, other);
		Assert.Same(command, other);
		Assert.Single(Mapping.Commands);

		Assert.True(Mapping.Commands.Remove(command.QualifiedName));
		Assert.Empty(Mapping.Commands);

		//清空命令映射集合
		Mapping.Commands.Clear();
		Assert.Empty(Mapping.Commands);
		Parallel.For(0, COUNT, index =>
		{
			command = Mapping.Commands.Script("MySql", $"SELECT * FROM Table{index}");
			Assert.NotNull(command);
		});
		Assert.Equal(COUNT, Mapping.Commands.Count);

		//清空命令映射集合
		Mapping.Commands.Clear();
		Assert.Empty(Mapping.Commands);
		Parallel.For(0, COUNT, index =>
		{
			var script = $"SELECT * FROM Table{index}";
			var command = Mapping.Commands.Script("MySql", script);

			Assert.NotNull(command);
			Assert.NotNull(command.QualifiedName);
			Assert.NotEmpty(command.QualifiedName);
			Assert.True(Mapping.Commands.Remove(command.QualifiedName));
		});

		Assert.Empty(Mapping.Commands);
	}
}
