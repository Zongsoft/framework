/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Zongsoft.Reflection;
using Zongsoft.Reflection.Expressions;

namespace Zongsoft.Configuration;

[Configuration(nameof(Properties), ResolverType = typeof(ConnectionSettingsResolver))]
public abstract class ConnectionSettingsBase : Settings, IConnectionSettings, IEquatable<ConnectionSettingsBase>
{
	#region 成员字段
	private PropertyDictionary _properties;
	#endregion

	#region 构造函数
	protected ConnectionSettingsBase(string settings) : this(string.Empty, settings) { }
	protected ConnectionSettingsBase(string name, string settings) : base(name, settings, false) { }
	#endregion

	#region 公共属性
	public bool HasProperties => _properties?.Count > 0;
	public IDictionary<string, string> Properties => System.Threading.LazyInitializer.EnsureInitialized(ref _properties, () => new PropertyDictionary(this));
	#endregion

	#region 显式实现
	IConnectionSettingsDriver IConnectionSettings.Driver => this.GetDriver();
	#endregion

	#region 保护属性
	protected virtual bool IsConstrained => false;
	#endregion

	#region 抽象方法
	protected abstract IConnectionSettingsDriver GetDriver();
	#endregion

	#region 公共方法
	public bool IsDriver(string name) => ConnectionSettingsUtility.IsDriver(this, name);
	public bool IsDriver(IConnectionSettingsDriver driver) => ConnectionSettingsUtility.IsDriver(this, driver);

	public T GetValue<T>([CallerMemberName]string name = null)
	{
		ArgumentNullException.ThrowIfNull(name);

		if(this.GetDriver().Descriptors.TryGetValue(name, out var descriptor))
			return this.GetValue<T>(descriptor);

		if(this.TryGetValue(name, out var text))
			return Common.Convert.ConvertValue<T>(text);

		return default;
	}

	public bool TryGetValue<T>(string name, out T value)
	{
		value = default;

		if(name == null)
			return false;

		if(!this.GetDriver().Descriptors.TryGetValue(name, out var descriptor))
			return this.TryGetValue(name, out var text) && Common.Convert.TryConvertValue(text, out value);

		if(this.Entries.TryGetValue(descriptor.Name, out var entry))
			return Common.Convert.TryConvertValue(entry, () => descriptor.Converter, out value);

		try
		{
			if(Composer.Compose(this.Entries, descriptor, out var composite))
				return Common.Convert.TryConvertValue(composite, () => descriptor.Converter, out value);
		}
		catch(InvalidOperationException)
		{
			return false;
		}

		return descriptor.HasDefaultValue(out var defaultValue) && Common.Convert.TryConvertValue(defaultValue, () => descriptor.Converter, out value);
	}
	#endregion

	#region 保护方法
	protected bool SetValue<T>(T value, [CallerMemberName]string name = null) => this.SetValue(name, value);
	protected bool SetValue<T>(string name, T value)
	{
		if(this.GetDriver().Descriptors.TryGetValue(name, out var descriptor))
			return this.SetValue(descriptor, value);

		if(Common.Convert.TryConvertValue<string>(value, out var text))
		{
			this[name] = text;
			return true;
		}

		return false;
	}

	protected virtual T GetValue<T>(ConnectionSettingDescriptor descriptor)
	{
		if(this.Entries.TryGetValue(descriptor.Name, out var text))
			return Common.Convert.ConvertValue<T>(text, () => descriptor.Converter);

		if(Composer.Compose(this.Entries, descriptor, out var result))
			return Common.Convert.ConvertValue<T>(result, () => descriptor.Converter);

		return Common.Convert.ConvertValue<T>(descriptor.DefaultValue, () => descriptor.Converter);
	}

	protected virtual object GetValue(ConnectionSettingDescriptor descriptor)
	{
		if(this.Entries.TryGetValue(descriptor.Name, out var text))
			return Common.Convert.ConvertValue(text, descriptor.Type, () => descriptor.Converter);

		if(Composer.Compose(this.Entries, descriptor, out var result))
			return result;

		return Common.Convert.ConvertValue(descriptor.DefaultValue, descriptor.Type, () => descriptor.Converter);
	}

	protected virtual bool SetValue<T>(ConnectionSettingDescriptor descriptor, T value)
	{
		if(!Common.Convert.TryConvertValue<string>(value, () => descriptor.Converter, out var text))
			return false;

		this[descriptor.Name] = text;
		return true;
	}

	protected override string GetKey(string key, bool validate = false)
	{
		if(this.TryGetKey(key, out var name))
			return name;

		if(validate && this.IsConstrained)
			throw this.InvalidName(key);

		return key;
	}

	protected override void OnReset()
	{
		base.OnReset();
		_properties?.Clear();
	}

	protected override void OnEntryChanged(string key, string value)
	{
		if(this.TryGetKey(key, out var name))
			this.Entries[name] = value;
		else
			this.OnUnrecognized(key, value);
	}

	protected virtual void OnUnrecognized(string name, string value)
	{
		if(this.IsConstrained)
			this.Properties[name] = value;
		else
			this.Entries[name] = value;
	}

	protected override IEnumerable<KeyValuePair<string, string>> GetEntries() => this.HasProperties ? this.Entries.Concat(_properties) : this.Entries;
	#endregion

	#region 重写方法
	public bool Equals(IConnectionSettings settings) => settings is Settings other && this.Equals(other);
	public bool Equals(ConnectionSettingsBase settings) => this.Equals((Settings)settings);
	public override bool Equals(Settings other) => other is ConnectionSettingsBase connection &&
		base.Equals(other) &&
		string.Equals(this.GetDriver().Name, connection.GetDriver().Name, StringComparison.OrdinalIgnoreCase) &&
		(_properties?.Count ?? 0) == (connection._properties?.Count ?? 0) &&
		(!this.HasProperties || _properties.All(entry => connection._properties.TryGetValue(entry.Key, out var value) && string.Equals(entry.Value, value, StringComparison.Ordinal)));

	public override bool Equals(object obj) => obj is Settings other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), StringComparer.OrdinalIgnoreCase.GetHashCode(this.GetDriver().Name), this.HasProperties ? GetEntriesHashCode(_properties) : 0);
	public override string ToString() => $"[{this.Name}@{this.GetDriver()}]{this.Value}";
	#endregion

	#region 私有方法
	private ArgumentException InvalidName(string name) => new(string.Format(global::Zongsoft.Properties.Resources.ConnectionSettings_InvalidName_Message, name, this.GetDriver().Name), nameof(name));
	private bool TryGetKey(string key, out string name)
	{
		name = null;

		if(string.IsNullOrEmpty(key))
			return false;

		var descriptors = this.GetDriver().Descriptors;

		if(descriptors.TryGetValue(key, out var descriptor))
		{
			name = descriptor.Name;
			return true;
		}

		var index = key.IndexOf('.');

		if(index <= 0 || !descriptors.TryGetValue(key[..index], out descriptor))
			return false;

		var type = descriptor.Type;
		name = descriptor.Name;

		foreach(var part in key[(index + 1)..].Split('.'))
		{
			if(type == null || string.IsNullOrEmpty(part))
				return false;

			var member = type.GetMember(part, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).FirstOrDefault();

			if(member == null || member is PropertyInfo property && !property.CanWrite || member is FieldInfo field && field.IsInitOnly)
				return false;

			name += "." + member.Name;
			type = member.GetMemberType();
		}

		return true;
	}
	#endregion

	#region 嵌套子类
	private sealed class PropertyDictionary(ConnectionSettingsBase owner) : IDictionary<string, string>
	{
		private readonly Dictionary<string, string> _items = new(StringComparer.OrdinalIgnoreCase);

		public string this[string key]
		{
			get => _items[key];
			set
			{
				this.Validate(key);
				_items[key] = value;
				owner.OnChanged();
			}
		}

		public int Count => _items.Count;
		public bool IsReadOnly => false;
		public ICollection<string> Keys => _items.Keys;
		public ICollection<string> Values => _items.Values;

		public void Add(string key, string value)
		{
			this.Validate(key);
			_items.Add(key, value);
			owner.OnChanged();
		}

		public void Add(KeyValuePair<string, string> item) => this.Add(item.Key, item.Value);
		public bool ContainsKey(string key) => _items.ContainsKey(key);
		public bool TryGetValue(string key, out string value) => _items.TryGetValue(key, out value);
		public bool Contains(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_items).Contains(item);
		public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, string>>)_items).CopyTo(array, arrayIndex);

		public bool Remove(string key)
		{
			if(!_items.Remove(key))
				return false;

			owner.OnChanged();
			return true;
		}

		public bool Remove(KeyValuePair<string, string> item) => this.Contains(item) && this.Remove(item.Key);
		public void Clear()
		{
			_items.Clear();
			owner.OnChanged();
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _items.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		private void Validate(string key)
		{
			ArgumentNullException.ThrowIfNull(key);

			if(owner.IsConstrained && owner.TryGetKey(key, out _))
				throw owner.InvalidName(key);
		}
	}

	private static class Composer
	{
		public static bool Compose(IEnumerable<KeyValuePair<string, string>> entries, ConnectionSettingDescriptor descriptor, out object value)
		{
			var prefix = descriptor.Name + ".";
			var populated = false;
			value = null;

			foreach(var entry in entries)
			{
				//调用方已优先处理直接键名，这里只组装形如 "Cluster.Address" 的复合键。
				if(!entry.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || entry.Key.Length == prefix.Length)
					continue;

				var result = value;
				if(result == null && !TryCreateValue(descriptor.Type, out result))
					return false;

				if(SetMemberValue(ref result, MemberExpression.Parse(entry.Key.AsSpan(prefix.Length)), entry.Value))
				{
					value = result;
					populated = true;
				}
			}

			return populated;
		}

		private static bool TryCreateValue(Type type, out object value)
		{
			value = null;

			if(type == null || type.IsAbstract || type.IsInterface)
				return false;

			try
			{
				value = Activator.CreateInstance(type);
				return value != null;
			}
			catch
			{
				return false;
			}
		}

		private static bool SetMemberValue(ref object target, IMemberExpression expression, string text)
		{
			if(target == null || expression == null)
				return false;

			var member = GetMember(target, expression, out var parameters);
			if(member == null)
				return false;

			if(expression.Next == null)
			{
				var memberType = member.GetMemberType();
				var value = Common.Convert.ConvertValue(text, memberType, () => Common.Convert.GetTypeConverter(member));
				return Reflection.Reflector.TrySetValue(member, ref target, value, parameters);
			}

			var result = Reflection.Reflector.GetValue(member, ref target, parameters);
			if(result == null && !TryCreateValue(member.GetMemberType(), out result))
				return false;

			if(!SetMemberValue(ref result, expression.Next, text))
				return false;

			//子成员可能是结构体，递归赋值完成后必须写回父成员才能保留变更。
			return Reflection.Reflector.TrySetValue(member, ref target, result, parameters);
		}

		private static MemberInfo GetMember(object target, IMemberExpression expression, out object[] parameters)
		{
			parameters = expression is IndexerExpression indexer && indexer.HasArguments ? GetArguments(target, indexer.Arguments) : null;

			var name = expression switch
			{
				IdentifierExpression identifier => identifier.Name,
				IndexerExpression => string.Empty,
				_ => null,
			};

			if(name == null)
				return null;

			var type = (target as Type) ?? target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);

			return members == null || members.Length == 0 ? null : members[0];
		}

		private static object[] GetArguments(object target, IList<IMemberExpression> arguments)
		{
			if(arguments == null || arguments.Count == 0)
				return null;

			var result = new object[arguments.Count];

			for(int i = 0; i < arguments.Count; i++)
			{
				//索引器参数支持常量，也支持相对于当前对象的成员表达式。
				result[i] = arguments[i] is ConstantExpression constant ?
					constant.Value :
					MemberExpressionEvaluator.Default.GetValue(arguments[i], target);
			}

			return result;
		}
	}
	#endregion
}

public abstract class ConnectionSettingsBase<TDriver> : ConnectionSettingsBase, IEquatable<ConnectionSettingsBase<TDriver>> where TDriver : IConnectionSettingsDriver
{
	#region 成员字段
	private readonly TDriver _driver;
	#endregion

	#region 构造函数
	protected ConnectionSettingsBase(TDriver driver, string settings) : this(driver, string.Empty, settings) { }
	protected ConnectionSettingsBase(TDriver driver, string name, string settings) : base(name, settings)
	{
		_driver = driver ?? throw new ArgumentNullException(nameof(driver));

		//驱动初始化之后才解析，避免基类构造期间调用尚未可用的驱动。
		this.OnValueChanged(settings);
	}
	#endregion

	#region 公共属性
	public TDriver Driver => _driver;
	protected override IConnectionSettingsDriver GetDriver() => _driver;
	#endregion

	#region 重写方法
	public bool Equals(ConnectionSettingsBase<TDriver> settings) => this.Equals((Settings)settings);
	#endregion
}

public abstract class ConnectionSettingsBase<TDriver, TOptions> : ConnectionSettingsBase<TDriver>, IConnectionSettings<TOptions> where TDriver : IConnectionSettingsDriver
{
	#region 静态成员
	private static readonly MemberInfo[] _members;
	#endregion

	#region 静态构造
	static ConnectionSettingsBase()
	{
		var properties = typeof(TOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.CanWrite && property.SetMethod.IsPublic);
		var fields = typeof(TOptions).GetFields(BindingFlags.Public | BindingFlags.Instance).Where(field => !field.IsInitOnly);
		_members = [.. properties.Cast<MemberInfo>().Concat(fields.Cast<MemberInfo>())];
	}
	#endregion

	#region 构造函数
	protected ConnectionSettingsBase(TDriver driver, string settings) : base(driver, settings) { }
	protected ConnectionSettingsBase(TDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 保护属性
	protected override bool IsConstrained => true;
	#endregion

	#region 公共方法
	public TOptions GetOptions()
	{
		var options = this.CreateOptions();
		this.Populate(options);
		return options;
	}
	#endregion

	#region 虚拟方法
	protected virtual TOptions CreateOptions() => Activator.CreateInstance<TOptions>();
	protected virtual void Populate(ref TOptions options, ConnectionSettingDescriptor descriptor, MemberInfo member, object value) => Reflector.TrySetValue(member, ref options, value);
	protected virtual void Populate(TOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		for(int i = 0; i < _members.Length; i++)
		{
			if(this.Driver.Descriptors.TryGetValue(_members[i].Name, out var descriptor) && !descriptor.Ignored)
			{
				var value = this.GetValue(descriptor);

				if(descriptor.Populator != null)
					value = Common.Convert.ConvertValue(value, _members[i].GetMemberType(), () => descriptor.Populator);

				this.Populate(ref options, descriptor, _members[i], value);
			}
		}
	}
	#endregion
}
