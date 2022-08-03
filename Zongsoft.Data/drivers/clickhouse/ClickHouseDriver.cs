/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.ClickHouse library.
 *
 * The Zongsoft.Data.ClickHouse is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.ClickHouse is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.ClickHouse library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Collections;

using ClickHouse.Ado;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.ClickHouse
{
	public class ClickHouseDriver : DataDriverBase
	{
		#region 公共常量
		/// <summary>驱动程序的标识：ClickHouse。</summary>
		public const string Key = "ClickHouse";
		#endregion

		#region 构造函数
		public ClickHouseDriver()
		{
			//添加 MySQL 支持的功能特性集
			this.Features.Add(Feature.Deletion.Multitable);
			this.Features.Add(Feature.Updation.Multitable);
		}
		#endregion

		#region 公共属性
		public override string Name => Key;
		public override IStatementBuilder Builder => ClickHouseStatementBuilder.Default;
		#endregion

		#region 公共方法
		public override Exception OnError(Exception exception)
		{
			if(exception is ClickHouseException error)
			{
				switch(error.Code)
				{
					case 0:
						return error;
				}
			}

			return exception;
		}

		public override DbCommand CreateCommand() => new ClickHouseCommandWrapper();
		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new ClickHouseCommandWrapper(text, commandType);

		public override DbConnection CreateConnection() => new ClickHouseConnectionWrapper();
		public override DbConnection CreateConnection(string connectionString) => new ClickHouseConnectionWrapper(connectionString);

		public override IDataImporter CreateImporter(DataImportContextBase context) => new ClickHouseImporter(context);
		#endregion

		#region 保护方法
		protected override ExpressionVisitorBase CreateVisitor() => new ClickHouseExpressionVisitor();
		#endregion

		#region 嵌套子类
		private class ClickHouseConnectionWrapper : DbConnection, IEquatable<ClickHouseConnectionWrapper>, IEquatable<ClickHouseConnection>
		{
			private readonly ClickHouseConnection _connection;

			public ClickHouseConnectionWrapper() => _connection = new ClickHouseConnection();
			public ClickHouseConnectionWrapper(string connectionString) => _connection = new ClickHouseConnection(connectionString);
			public ClickHouseConnectionWrapper(ClickHouseConnection connection) => _connection = connection;

			internal ClickHouseConnection Connection => _connection;
			public override string ConnectionString { get => _connection.ConnectionString; set => _connection.ConnectionString = value; }
			public override string Database => _connection.Database;
			public override string DataSource => _connection.ConnectionSettings?.Host;
			public override string ServerVersion => $"{_connection.ServerInfo.Major}.{_connection.ServerInfo.Minor}.{_connection.ServerInfo.Build}.{_connection.ServerInfo.Patch}";
			public override ConnectionState State => _connection.State;

			public override void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);
			public override void Close() => _connection.Close();
			public override void Open() => _connection.Open();

			protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
			protected override DbCommand CreateDbCommand() => new ClickHouseCommandWrapper(_connection.CreateCommand());

			public bool Equals(ClickHouseConnection other) => object.Equals(_connection, other);
			public bool Equals(ClickHouseConnectionWrapper other) => object.Equals(_connection, other?._connection);
		}

		private class ClickHouseDataReaderWrapper : DbDataReader
		{
			private readonly ClickHouseDataReader _reader;
			public ClickHouseDataReaderWrapper(ClickHouseDataReader reader) => _reader = reader;

			public override object this[int ordinal] => _reader.GetValue(ordinal);
			public override object this[string name] => _reader.GetValue(_reader.GetOrdinal(name));

			public override int Depth => _reader.Depth;
			public override int FieldCount => _reader.FieldCount;
			public override bool HasRows => _reader.RecordsAffected > 0;
			public override bool IsClosed => _reader.IsClosed;
			public override int RecordsAffected => _reader.RecordsAffected;

			public override string GetName(int ordinal) => _reader.GetName(ordinal);
			public override int GetOrdinal(string name) => _reader.GetOrdinal(name);
			public override string GetDataTypeName(int ordinal) => _reader.GetDataTypeName(ordinal);
			public override Type GetFieldType(int ordinal) => _reader.GetFieldType(ordinal);

			public override bool GetBoolean(int ordinal) => _reader.GetBoolean(ordinal);
			public override byte GetByte(int ordinal) => _reader.GetByte(ordinal);
			public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => _reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
			public override char GetChar(int ordinal) => _reader.GetChar(ordinal);
			public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => _reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
			public override DateTime GetDateTime(int ordinal) => _reader.GetDateTime(ordinal);
			public override decimal GetDecimal(int ordinal) => _reader.GetDecimal(ordinal);
			public override double GetDouble(int ordinal) => _reader.GetDouble(ordinal);
			public override float GetFloat(int ordinal) => _reader.GetFloat(ordinal);
			public override Guid GetGuid(int ordinal) => _reader.GetGuid(ordinal);
			public override short GetInt16(int ordinal) => _reader.GetInt16(ordinal);
			public override int GetInt32(int ordinal) => _reader.GetInt32(ordinal);
			public override long GetInt64(int ordinal) => _reader.GetInt64(ordinal);
			public override string GetString(int ordinal) => _reader.GetString(ordinal);
			public override object GetValue(int ordinal) => _reader.GetValue(ordinal);
			public override int GetValues(object[] values) => _reader.GetValues(values);

			public override IEnumerator GetEnumerator() => new DataRecordIterator(_reader);
			public override bool IsDBNull(int ordinal) => _reader.IsDBNull(ordinal);
			public override bool NextResult() => _reader.NextResult();
			public override bool Read() => _reader.Read();

			private class DataRecordIterator : IEnumerator
			{
				private IDataReader _reader;
				internal DataRecordIterator(IDataReader reader) => _reader = reader;

				public object Current => _reader;
				public bool MoveNext()
				{
					if(_reader.Read())
						return true;

					_reader = null;
					return false;
				}
				public void Reset() { }
			}
		}

		private class ClickHouseCommandWrapper : DbCommand
		{
			private readonly ClickHouseCommand _command;
			private ClickHouseConnectionWrapper _connection;
			private ClickHouseParameterCollectionWrapper _parameters;

			public ClickHouseCommandWrapper()
			{
				_command = new ClickHouseCommand();
				_parameters = new ClickHouseParameterCollectionWrapper(_command.Parameters);
			}

			public ClickHouseCommandWrapper(ClickHouseCommand command)
			{
				_command = command;
				_parameters = new ClickHouseParameterCollectionWrapper(command.Parameters);

				if(command.Connection is ClickHouseConnection connection)
					_connection = new ClickHouseConnectionWrapper(connection);
			}

			public ClickHouseCommandWrapper(string text, CommandType type = CommandType.Text)
			{
				_command = new ClickHouseCommand()
				{
					CommandText = text,
					CommandType = type,
				};

				_parameters = new ClickHouseParameterCollectionWrapper(_command.Parameters);
			}

			public override string CommandText { get => _command.CommandText; set => _command.CommandText = value; }
			public override int CommandTimeout { get => _command.CommandTimeout; set => _command.CommandTimeout = value; }
			public override CommandType CommandType { get => _command.CommandType; set => _command.CommandType = value; }
			public override bool DesignTimeVisible { get => false; set => throw new NotSupportedException(); }
			public override UpdateRowSource UpdatedRowSource { get => _command.UpdatedRowSource; set => _command.UpdatedRowSource = value; }
			protected override DbConnection DbConnection
			{
				get => _connection;
				set
				{
					var connection = value as ClickHouseConnectionWrapper ?? throw new ArgumentException("The specified connection type must be ClickHouseConnection.");

					_command.Connection = connection.Connection;
					_connection = connection;
				}
			}
			protected override DbParameterCollection DbParameterCollection => _parameters;
			protected override DbTransaction DbTransaction { get => _command.Transaction as DbTransaction; set => _command.Transaction = value; }

			public override void Cancel() => _command.Cancel();
			public override int ExecuteNonQuery() => _command.ExecuteNonQuery();
			public override object ExecuteScalar() => _command.ExecuteScalar();
			public override void Prepare() => _command.Prepare();
			protected override DbParameter CreateDbParameter() => new ClickHouseParameterWrapper(_command.CreateParameter());
			protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => new ClickHouseDataReaderWrapper((ClickHouseDataReader)_command.ExecuteReader());
		}

		private class ClickHouseParameterWrapper : DbParameter
		{
			private readonly ClickHouseParameter _parameter;
			public ClickHouseParameterWrapper(ClickHouseParameter parameter) => _parameter = parameter;

			internal ClickHouseParameter Parameter => _parameter;
			public override DbType DbType { get => _parameter.DbType; set => _parameter.DbType = value; }
			public override ParameterDirection Direction { get => ((IDataParameter)_parameter).Direction; set => ((IDataParameter)_parameter).Direction = value; }
			public override bool IsNullable { get => ((IDataParameter)_parameter).IsNullable; set { } }
			public override string ParameterName { get => _parameter.ParameterName; set => _parameter.ParameterName = value; }
			public override int Size { get => ((IDbDataParameter)_parameter).Size; set => ((IDbDataParameter)_parameter).Size = value; }
			public override string SourceColumn { get => ((IDataParameter)_parameter).SourceColumn; set => ((IDataParameter)_parameter).SourceColumn = value; }
			public override bool SourceColumnNullMapping { get => false; set { } }
			public override object Value { get => _parameter.Value; set => _parameter.Value = value; }
			public override void ResetDbType() => _parameter.DbType = DbType.Object;
		}

		private class ClickHouseParameterCollectionWrapper : DbParameterCollection
		{
			private readonly ClickHouseParameterCollection _collection;
			public ClickHouseParameterCollectionWrapper(ClickHouseParameterCollection collection) => _collection = collection;

			public override int Count => _collection.Count;
			public override object SyncRoot => _collection.SyncRoot;

			public override int Add(object value)
			{
				if(value is ClickHouseParameter parameter)
					_collection.Add(parameter);
				else if(value is IDataParameter dataParameter)
					_collection.Add(dataParameter.ParameterName, dataParameter.DbType, dataParameter.Value);
				else
					throw new ArgumentException();

				return _collection.Count - 1;
			}

			public override void AddRange(Array values)
			{
				if(values == null || values.Length == 0)
					return;

				foreach(var value in values)
					this.Add(value);
			}

			public override void Clear() => _collection.Clear();

			public override bool Contains(object value) => value switch
			{
				IDataParameter parameter => _collection.Contains(parameter.ParameterName),
				string name => _collection.Contains(name),
				_ => false,
			};

			public override bool Contains(string name) => _collection.Contains(name);

			public override void CopyTo(Array array, int index) => _collection.CopyTo(array, index);

			public override int IndexOf(object value) => value switch
			{
				IDataParameter parameter => _collection.IndexOf(parameter.ParameterName),
				string name => _collection.IndexOf(name),
				_ => -1,
			};

			public override int IndexOf(string parameterName) => _collection.IndexOf(parameterName);

			public override void Insert(int index, object value)
			{
				if(value is ClickHouseParameter parameter)
					_collection.Insert(index, parameter);
				else if(value is IDataParameter dataParameter)
					_collection.Insert(index, new ClickHouseParameter()
					{
						ParameterName = dataParameter.ParameterName,
						DbType = dataParameter.DbType,
						Value = dataParameter.Value,
					});
				else
					throw new ArgumentException();
			}

			public override void Remove(object value)
			{
				switch(value)
				{
					case ClickHouseParameter parameter:
						_collection.Remove(parameter);
						break;
					case IDataParameter dataParameter:
						_collection.RemoveAt(dataParameter.ParameterName);
						break;
					case string name:
						_collection.RemoveAt(name);
						break;
				}
			}

			public override void RemoveAt(int index) => _collection.RemoveAt(index);
			public override void RemoveAt(string parameterName) => _collection.RemoveAt(parameterName);

			protected override DbParameter GetParameter(int index) => new ClickHouseParameterWrapper(_collection[index]);
			protected override DbParameter GetParameter(string parameterName) => new ClickHouseParameterWrapper(_collection[parameterName]);

			protected override void SetParameter(int index, DbParameter value)
			{
				if(value is ClickHouseParameterWrapper wrapper)
					_collection[index] = wrapper.Parameter;
				else
					_collection[index] = new ClickHouseParameter()
					{
						ParameterName = value.ParameterName,
						DbType = value.DbType,
						Value = value.Value,
					
					};
			}

			protected override void SetParameter(string parameterName, DbParameter value)
			{
				_collection[parameterName] = value is ClickHouseParameterWrapper wrapper ?
					wrapper.Parameter :
					new ClickHouseParameter()
					{
						ParameterName = value.ParameterName,
						DbType = value.DbType,
						Value = value.Value,

					};
			}

			public override IEnumerator GetEnumerator() => _collection.GetEnumerator();
		}
		#endregion
	}
}
