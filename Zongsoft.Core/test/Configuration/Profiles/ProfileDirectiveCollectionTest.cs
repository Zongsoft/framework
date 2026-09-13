using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Configuration.Profiles;
using Zongsoft.Configuration.Profiles.Directives;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class ProfileDirectiveCollectionTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Collection_ConstructorsAndMutationUseCaseInsensitiveKeys(bool enumerable)
	{
		var original = new RecordingDirective("original", []);
		var added = new RecordingDirective("added", []);
		var collection = Create(enumerable, original);

		Assert.Same(original, collection["ORIGINAL"]);
		collection.Add(added);
		Assert.True(collection.TryGetValue("ADDED", out var found));
		Assert.Same(added, found);
		Assert.Same(original, collection["Original"]);
		Assert.True(collection.Remove("ORIGINAL"));
		Assert.False(collection.Contains("original"));
		Assert.False(collection.TryGetValue("original", out _));
		Assert.Same(added, Assert.Single(collection));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Collection_DuplicateNamesRejectDuringConstructionAndAdd(bool enumerable)
	{
		var original = new RecordingDirective("trace", []);
		var duplicate = new RecordingDirective("TRACE", []);

		Assert.Throws<ArgumentException>(() => Create(enumerable, original, duplicate));

		var collection = Create(enumerable, original);
		Assert.Throws<ArgumentException>(() => collection.Add(duplicate));
		Assert.Same(original, Assert.Single(collection));
		Assert.Same(original, collection["trace"]);
	}

	[Fact]
	public void Collection_EmptyConstructorFormsAreIndependentWithoutDefault()
	{
		var collections = new[]
		{
			new ProfileDirectiveCollection(),
			new ProfileDirectiveCollection(null),
			new ProfileDirectiveCollection(default),
			new ProfileDirectiveCollection([]),
			new ProfileDirectiveCollection((IEnumerable<IProfileDirective>)Array.Empty<IProfileDirective>()),
		};

		Assert.All(collections, collection => Assert.Empty(collection));
		collections[0].Add(ImportDirective.Default);
		Assert.Same(ImportDirective.Default, Assert.Single(collections[0]));
		for(var index = 1; index < collections.Length; index++)
			Assert.Empty(collections[index]);
		Assert.Empty(typeof(ProfileDirectiveCollection).GetMember("Default", BindingFlags.Public | BindingFlags.Static));
	}

	[Fact]
	public void Collection_ValueConstructorFormsRetainKeyedMembers()
	{
		var first = new RecordingDirective("first", []);
		var second = new RecordingDirective("second", []);
		IProfileDirective[] array = [first, second];
		IEnumerable<IProfileDirective> enumerable = array;
		var collections = new[]
		{
			new ProfileDirectiveCollection(array),
			new ProfileDirectiveCollection(enumerable),
			new ProfileDirectiveCollection([first, second]),
			new ProfileDirectiveCollection(first, second),
		};

		Assert.All(collections, collection =>
		{
			Assert.Equal(2, collection.Count);
			Assert.Same(first, collection["FIRST"]);
			Assert.Same(second, collection["SECOND"]);
		});
		Assert.Same(first, Assert.Single(new ProfileDirectiveCollection(first)));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	public void Options_NullAndEmptyConstructorsRegisterIndependentDefaults(int constructor)
	{
		using var files = new ProfileImportTest.ProfileFiles();
		var child = files.Write("child.ini", "imported=child");
		var root = files.Write("root.ini", "#@import child.ini\nlocal=root");
		var options = CreateOptions(constructor);
		var empty = new ProfileOptions([]);

		var profile = Profile.Load(root, options);
		var emptyProfile = Profile.Load(root, empty);

		Assert.Equal(constructor != 2 && constructor != 3 && constructor != 7, options.ReservedBlanks);
		Assert.Same(ImportDirective.Default, Assert.Single(options.Directives));
		Assert.Same(ImportDirective.Default, Assert.Single(empty.Directives));
		Assert.NotSame(options.Directives, empty.Directives);
		Assert.Equal(2, profile.Entries.Count);
		Assert.Equal("root", profile.Entries["local"].Value);
		Assert.Equal("child", profile.Entries["imported"].Value);
		Assert.Equal(child, profile.Entries["imported"].Profile.FilePath);
		Assert.Equal("child", emptyProfile.Entries["imported"].Value);
		using var output = new StringWriter();
		using var emptyOutput = new StringWriter();
		profile.Save(output, options);
		emptyProfile.Save(emptyOutput, empty);
		Assert.Equal(emptyOutput.ToString(), output.ToString());
		Assert.Contains("#@import child.ini", output.ToString());
		options.Directives.Clear();
		Assert.Empty(options.Directives);
		Assert.Same(ImportDirective.Default, Assert.Single(empty.Directives));
		Assert.Same(ImportDirective.Default, Assert.Single(CreateOptions(constructor).Directives));
	}

	[Fact]
	public void Options_ClearDisablesImportsAndPreservesReadOnlyCollection()
	{
		var options = new ProfileOptions();
		var collection = options.Directives;
		Assert.Null(typeof(ProfileOptions).GetProperty(nameof(ProfileOptions.Directives)).GetSetMethod(nonPublic: true));
		Assert.Same(ImportDirective.Default, Assert.Single(collection));
		collection.Clear();
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("#@import relative.ini\nvalue=local"));

		var profile = Profile.Load(stream, options);

		Assert.Same(collection, options.Directives);
		Assert.Empty(collection);
		Assert.Equal("local", Assert.Single(profile.Entries).Value);
		Assert.IsType<ProfileDirective>(Assert.Single(profile.Comments));
		Assert.False(stream.CanRead);
		using var output = new StringWriter();
		profile.Save(output, options);
		Assert.Contains("#@import relative.ini", output.ToString());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Options_EnumeratesInputOnceAndOnlyDefaultsWhenEmpty(bool empty)
	{
		var events = new List<string>();
		var directive = new RecordingDirective("trace", events);
		var input = new SingleUseDirectives(empty ? [] : [directive]);

		var options = new ProfileOptions(input);

		Assert.Equal(1, input.EnumerationCount);
		if(empty)
			Assert.Same(ImportDirective.Default, Assert.Single(options.Directives));
		else
		{
			Assert.Same(directive, Assert.Single(options.Directives));
			using var stream = new MemoryStream(Encoding.UTF8.GetBytes("#@trace observed\n#@import relative.ini"));
			Profile.Load(stream, options);
			Assert.Equal(["read:observed"], events);
		}
		Assert.Equal(1, input.EnumerationCount);
	}

	[Fact]
	public void Options_ValueConstructorFormsDispatchConfiguredInstruction()
	{
		var events = new List<string>();
		var directive = new RecordingDirective("trace", events);
		IProfileDirective[] array = [directive];
		IEnumerable<IProfileDirective> enumerable = array;
		var options = new[]
		{
			new ProfileOptions(array),
			new ProfileOptions(enumerable),
			new ProfileOptions([directive]),
			new ProfileOptions(directive),
			new ProfileOptions(false, array),
			new ProfileOptions(false, directive),
		};

		for(var index = 0; index < options.Length; index++)
		{
			using var stream = new MemoryStream(Encoding.UTF8.GetBytes("#@trace " + index + "\n#@import relative.ini"));
			Profile.Load(stream, options[index]);
			Assert.Same(directive, Assert.Single(options[index].Directives));
		}

		Assert.Equal(["read:0", "read:1", "read:2", "read:3", "read:4", "read:5"], events);
		Assert.False(options[4].ReservedBlanks);
		Assert.False(options[5].ReservedBlanks);
	}

	[Fact]
	public void Collection_DispatchesReadWriteAndIgnoresUnknownInstructions()
	{
		var events = new List<string>();
		var directive = new RecordingDirective("trace", events);
		var options = new ProfileOptions(directive);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("#@TrAcE observed\n#@unknown ignored\nvalue=local"));

		var profile = Profile.Load(stream, options);
		Assert.Equal(["read:observed"], events);
		using var output = new StringWriter();
		profile.Save(output, options);

		Assert.Equal(["read:observed", "write:observed"], events);
		Assert.Equal("local", profile.Entries["value"].Value);
		Assert.Contains("#@TrAcE observed", output.ToString());
		Assert.Contains("#@unknown ignored", output.ToString());
	}

	private static ProfileDirectiveCollection Create(bool enumerable, params IProfileDirective[] directives) => enumerable ?
		new ProfileDirectiveCollection((IEnumerable<IProfileDirective>)directives) :
		new ProfileDirectiveCollection(directives);

	private static ProfileOptions CreateOptions(int constructor) => constructor switch
	{
		0 => new ProfileOptions(),
		1 => new ProfileOptions(null),
		2 => new ProfileOptions(false),
		3 => new ProfileOptions(false, null),
		4 => new ProfileOptions([]),
		5 => new ProfileOptions(Array.Empty<IProfileDirective>()),
		6 => new ProfileOptions((IEnumerable<IProfileDirective>)Array.Empty<IProfileDirective>()),
		7 => new ProfileOptions(false, []),
		_ => throw new ArgumentOutOfRangeException(nameof(constructor)),
	};

	private sealed class SingleUseDirectives(IProfileDirective[] directives) : IEnumerable<IProfileDirective>
	{
		public int EnumerationCount { get; private set; }

		public IEnumerator<IProfileDirective> GetEnumerator()
		{
			this.EnumerationCount++;
			if(this.EnumerationCount != 1)
				throw new InvalidOperationException("The directive input can only be enumerated once.");
			return ((IEnumerable<IProfileDirective>)directives).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	}

	private sealed class RecordingDirective(string name, List<string> events) : IProfileDirective
	{
		public string Name => name;
		public void OnRead(ProfileReadingContext context, string argument) => events.Add("read:" + argument);
		public void OnWrite(ProfileWritingContext context, string argument) => events.Add("write:" + argument);
	}
}