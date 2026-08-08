using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

using Xunit;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

public class ModelDescriptorServiceTest
{
	[Fact]
	public void GetDescriptorByType_ShouldShareTheIntrinsicDescriptor()
	{
		var generic = Model.GetDescriptor<ServiceModel>();
		var runtime = Model.GetDescriptor(typeof(ServiceModel));

		Assert.Same(generic, runtime);
	}

	[Fact]
	public void GetDescriptorByService_ShouldApplyMappingAndIsolateTheTypeCache()
	{
		var intrinsic = Model.GetDescriptor<ServiceModel>();
		var parser = new TestSchemaParser(CreateSchema());
		var service = CreateService<ServiceModel>("Mapping.ServiceModel", parser);

		var generic = service.GetDescriptor();
		var runtime = ((IDataService)service).GetDescriptor();

		Assert.NotSame(intrinsic, generic);
		Assert.NotSame(generic, runtime);
		Assert.Equal(generic.QualifiedName, runtime.QualifiedName);
		Assert.Equal("Mapping.ServiceModel", parser.Name);
		Assert.Equal("*", parser.Expression);
		Assert.Equal(typeof(ServiceModel), parser.ModelType);

		var identifier = Assert.IsType<ModelPropertyDescriptor.SimplexPropertyDescriptor>(generic.Properties[nameof(ServiceModel.Identifier)]);
		Assert.Equal("MappedIdentifier", identifier.Alias);
		Assert.Equal("Mapped identifier", identifier.Hint);
		Assert.Equal(DataType.Int64, identifier.DataType);
		Assert.True(identifier.IsPrimaryKey);
		Assert.True(identifier.Immutable);
		Assert.True(identifier.Sortable);
		Assert.False(identifier.Nullable);
		Assert.Equal(19, identifier.Precision);
		Assert.Equal(0, identifier.Scale);
		Assert.Equal(100L, identifier.DefaultValue);
		Assert.True(identifier.Sequence.IsEmpty);

		var runtimeIdentifier = Assert.IsType<ModelPropertyDescriptor.SimplexPropertyDescriptor>(runtime.Properties[nameof(ServiceModel.Identifier)]);
		Assert.Equal(identifier.DataType, runtimeIdentifier.DataType);
		Assert.Equal(identifier.Alias, runtimeIdentifier.Alias);
		Assert.Equal(identifier.Sequence, runtimeIdentifier.Sequence);

		var name = Assert.IsType<ModelPropertyDescriptor.SimplexPropertyDescriptor>(generic.Properties[nameof(ServiceModel.Name)]);
		Assert.Equal("MappedName", name.Alias);
		Assert.Equal(80, name.Length);
		Assert.True(name.Nullable);

		var external = Assert.IsType<ModelPropertyDescriptor.SimplexPropertyDescriptor>(generic.Properties[nameof(ServiceModel.ExternalIdentifier)]);
		Assert.False(external.Sequence.IsEmpty);
		Assert.Equal("#ServiceModel:ExternalIdentifier", external.Sequence.Name);

		var cachedIdentifier = Assert.IsType<ModelPropertyDescriptor.SimplexPropertyDescriptor>(intrinsic.Properties[nameof(ServiceModel.Identifier)]);
		Assert.Equal(DataType.Int32, cachedIdentifier.DataType);
		Assert.False(cachedIdentifier.Sequence.IsEmpty);
	}

	[Fact]
	public void GetDescriptorByServiceWithoutSchema_ShouldPreserveIntrinsicSemantics()
	{
		var service = CreateService<ServiceModel>("Mapping.ServiceModel", null);
		var intrinsic = Model.GetDescriptor<ServiceModel>();
		var contextual = service.GetDescriptor();

		Assert.NotSame(intrinsic, contextual);
		Assert.Equal(intrinsic.QualifiedName, contextual.QualifiedName);
		Assert.Equal(intrinsic.Properties.Count, contextual.Properties.Count);

		var expected = Assert.IsType<ModelPropertyDescriptor.SimplexPropertyDescriptor>(intrinsic.Properties[nameof(ServiceModel.Identifier)]);
		var actual = Assert.IsType<ModelPropertyDescriptor.SimplexPropertyDescriptor>(contextual.Properties[nameof(ServiceModel.Identifier)]);
		Assert.Equal(expected.DataType, actual.DataType);
		Assert.Equal(expected.IsPrimaryKey, actual.IsPrimaryKey);
		Assert.Equal(expected.Sequence, actual.Sequence);
	}

	[Fact]
	public void DataServiceDescriptor_ShouldUseTheUnifiedModelResolver()
	{
		var service = CreateService<ServiceModel>("Mapping.ServiceModel", new TestSchemaParser(CreateSchema()));
		var descriptor = new DataServiceDescriptor<ServiceModel>(service);
		var identifier = Assert.IsType<ModelPropertyDescriptor.SimplexPropertyDescriptor>(descriptor.Model.Properties[nameof(ServiceModel.Identifier)]);

		Assert.Equal(DataType.Int64, identifier.DataType);
		Assert.True(identifier.Sequence.IsEmpty);

		var custom = new ModelDescriptor(typeof(ServiceModel));
		var overridden = new DataServiceDescriptor<ServiceModel>(service, custom);
		Assert.Same(custom, overridden.Model);
	}

	[Fact]
	public void GetDescriptorByService_ShouldRejectNull()
	{
		Assert.Throws<ArgumentNullException>(() => Model.GetDescriptor<ServiceModel>((IDataService<ServiceModel>)null));
		Assert.Throws<ArgumentNullException>(() => Model.GetDescriptor((IDataService)null));
	}

	private static TestSchema CreateSchema()
	{
		var entity = new DataEntity("Mapping", nameof(ServiceModel));

		var identifier = entity.Properties.Simplex(nameof(ServiceModel.Identifier), DataType.Int64, 19, 0, false, true);
		identifier.Alias = "MappedIdentifier";
		identifier.Hint = "Mapped identifier";
		identifier.IsPrimaryKey = true;
		identifier.Sortable = true;
		identifier.DefaultValue = 100L;

		var name = entity.Properties.Simplex(nameof(ServiceModel.Name), DataType.String, 80, true);
		name.Alias = "MappedName";

		var external = entity.Properties.Simplex(nameof(ServiceModel.ExternalIdentifier), DataType.Int32, false);
		external.Sequence = DataEntityPropertySequence.Create(external, "#");

		return new TestSchema("Mapping.ServiceModel", "*", typeof(ServiceModel), identifier, name, external);
	}

	private static IDataService<TModel> CreateService<TModel>(string name, ISchemaParser parser)
	{
		var dataAccess = parser == null ? null : CreateProxy<IDataAccess>(method => method.Name == "get_Schema" ? parser : null);

		return CreateProxy<IDataService<TModel>>(method => method.Name switch
		{
			"get_Name" => name,
			"get_DataAccess" => dataAccess,
			_ => null,
		});
	}

	private static T CreateProxy<T>(Func<MethodInfo, object> handler) where T : class
	{
		var proxy = DispatchProxy.Create<T, TestProxy>();
		((TestProxy)(object)proxy).Handler = handler;
		return proxy;
	}

	[Model("Tests.ServiceModel")]
	public sealed class ServiceModel
	{
		[ModelProperty(DbType.Int32, false, IsPrimaryKey = true, Sequence = "#")]
		public int Identifier { get; set; }

		[ModelProperty(DbType.String, 16, false)]
		public string Name { get; set; }

		[ModelProperty(DbType.Int32, false)]
		public int ExternalIdentifier { get; set; }
	}

	public class TestProxy : DispatchProxy
	{
		public Func<MethodInfo, object> Handler { get; set; }

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			var result = this.Handler?.Invoke(targetMethod);
			if(result != null || targetMethod.ReturnType == typeof(void))
				return result;

			return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
		}
	}

	private sealed class TestSchemaParser(TestSchema schema) : ISchemaParser
	{
		public string Name { get; private set; }
		public string Expression { get; private set; }
		public Type ModelType { get; private set; }

		public ISchema Parse(string name, string expression, Type entityType = null)
		{
			this.Name = name;
			this.Expression = expression;
			this.ModelType = entityType;
			return schema;
		}
	}

	private sealed class TestSchema : ISchema
	{
		private readonly Dictionary<string, SchemaMemberBase> _members;

		public TestSchema(string name, string text, Type modelType, params IDataEntityProperty[] properties)
		{
			this.Name = name;
			this.Text = text;
			this.ModelType = modelType;
			_members = new(StringComparer.OrdinalIgnoreCase);

			foreach(var property in properties)
				_members.Add(property.Name, new TestSchemaMember(property));
		}

		public string Name { get; }
		public string Text { get; }
		public Type ModelType { get; }
		public bool IsEmpty => _members.Count == 0;
		public bool IsReadOnly { get; set; }

		public void Clear() => _members.Clear();
		public bool Contains(string path) => path != null && _members.ContainsKey(path);
		public SchemaMemberBase Find(string path) => path != null && _members.TryGetValue(path, out var member) ? member : null;
		public ISchema Include(string path) => this;
		public ISchema Exclude(string path)
		{
			_members.Remove(path);
			return this;
		}

		public bool Exclude(string path, out SchemaMemberBase member) => _members.Remove(path, out member);
	}

	private sealed class TestSchemaMember(IDataEntityProperty property) : SchemaMemberBase(property.Name)
	{
		public override IDataEntityProperty Property => property;
		public override bool HasChildren => false;

		protected override SchemaMemberBase GetParent() => null;
		protected override void SetParent(SchemaMemberBase parent) { }
		protected override bool TryGetChild(string name, out SchemaMemberBase child)
		{
			child = null;
			return false;
		}

		protected override void AddChild(SchemaMemberBase child) { }
		protected override void RemoveChild(string name) { }
		protected override void ClearChildren() { }
	}
}
