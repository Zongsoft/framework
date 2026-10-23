using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data;
using Zongsoft.Components;

namespace Zongsoft.Serialization.Tests;

[Collection(Data.Transactions.Tests.TransactionEventDispatchCollection.Name)]
public class DataDictionarySerializationTest
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void GenericDictionaryPreservesModelContract(int source)
	{
		object data = source switch
		{
			0 => new Entry { Id = 401, Name = "Building" },
			1 => new Dictionary<string, object> { ["Id"] = 401U, ["Name"] = "Building" },
			2 => new Hashtable { ["Id"] = 401U, ["Name"] = "Building" },
			_ => Model.Build<IEntry>(entry => { entry.Id = 401; entry.Name = "Building"; }),
		};

		var original = DataDictionary.GetDictionary<IEntry>(data);
		var json = Serializer.Json.Serialize(original);
		var restored = Serializer.Json.Deserialize<IDataDictionary<IEntry>>(json);

		Assert.NotNull(restored);
		Assert.True(restored.TryGetValue(entry => entry.Id, out var id));
		Assert.Equal(401U, id);
		Assert.True(restored.TryGetValue(entry => entry.Name, out var name));
		Assert.Equal("Building", name);
	}

	[Fact]
	public void PartialDictionaryPreservesMissingAndNullFields()
	{
		var original = DataDictionary.GetDictionary<IEntry>(new Dictionary<string, object> { ["Name"] = null });
		var restored = Serializer.Json.Deserialize<IDataDictionary<IEntry>>(Serializer.Json.Serialize(original));

		Assert.False(restored.ContainsKey("Id"));
		Assert.True(restored.ContainsKey("Name"));
		Assert.Null(restored["Name"]);
		Assert.Null(Serializer.Json.Deserialize<IDataDictionary<IEntry>>("null"));
	}

	[Fact]
	public void ArrayAndNonGenericDictionaryRoundTrip()
	{
		IDataDictionary<IEntry>[] original =
		[
			DataDictionary.GetDictionary<IEntry>(new Entry { Id = 1, Name = "First" }),
			null,
			DataDictionary.GetDictionary<IEntry>(new Entry { Id = 2, Name = "Second" }),
		];
		var restored = Serializer.Json.Deserialize<IDataDictionary<IEntry>[]>(Serializer.Json.Serialize(original));

		Assert.Equal(3, restored.Length);
		Assert.Equal("First", restored[0].GetValue<string>("Name", null));
		Assert.Null(restored[1]);
		Assert.Equal("Second", restored[2].GetValue<string>("Name", null));

		var dictionary = Serializer.Json.Deserialize<IDataDictionary>(Serializer.Json.Serialize(original[0]));
		Assert.Equal(1U, dictionary.GetValue<uint>("Id", 0));
		Assert.Equal("First", dictionary.GetValue<string>("Name", null));
	}

	[Fact]
	public void EventMarshalerRestoresTypedDictionaryAndParameters()
	{
		var registry = new Registry();
		var original = DataDictionary.GetDictionary<IEntry>(new Entry { Id = 401, Name = "Building" });
		var context = new EventContext<IDataDictionary<IEntry>>(registry, "Changed", original);
		context.Parameters["source"] = "test";
		var (argument, parameters) = Events.Marshaler.Unmarshal(registry["Changed"], Events.Marshaler.Marshal(context));
		var restored = Assert.IsAssignableFrom<IDataDictionary<IEntry>>(argument);

		Assert.Equal(401U, restored.GetValue<uint>("Id", 0));
		Assert.Equal("Building", restored.GetValue<string>("Name", null));
		Assert.Equal("test", parameters["source"]);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task TransactionDispatchRestoresDictionarySnapshot(bool commit)
	{
		await using var dispatcher = new EventDispatcher();
		var received = new List<IDataDictionary<IEntry>>();
		var executor = Executor.Build<EventContext>(context => received.Add((IDataDictionary<IEntry>)context.GetArgument()));
		await dispatcher.StartAsync();

		await using(var transaction = new Transaction())
		{
			var entry = new Entry { Id = 401, Name = "Before" };
			var argument = DataDictionary.GetDictionary<IEntry>(entry);
			await dispatcher.DispatchAsync(new EventContext<IDataDictionary<IEntry>>(new Registry(), "Changed", argument), executor);
			entry.Name = "After";
			Assert.Empty(received);

			if(commit)
				await transaction.CommitAsync();
			else
				await transaction.RollbackAsync();
		}

		await dispatcher.StopAsync();

		if(commit)
			Assert.Equal("Before", Assert.Single(received).GetValue<string>("Name", null));
		else
			Assert.Empty(received);
	}

	private sealed class Registry : EventRegistryBase
	{
		public Registry() : base("DictionaryTests") => this.Event<IDataDictionary<IEntry>>("Changed");
	}

	public interface IEntry
	{
		uint Id { get; set; }
		string Name { get; set; }
	}

	public class Entry : IEntry
	{
		public uint Id { get; set; }
		public string Name { get; set; }
	}
}
