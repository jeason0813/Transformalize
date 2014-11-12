#region License

// /*
// Transformalize - Replicate, Transform, and Denormalize Your Data...
// Copyright (C) 2013 Dale Newman
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// */

#endregion

using System;
using System.Data;
using System.IO;
using System.Numerics;
using Transformalize.Configuration;
using Transformalize.Libs.FileHelpers.Enums;
using Transformalize.Libs.Rhino.Etl.Operations;

namespace Transformalize.Main.Providers {
    public abstract class AbstractConnection {

        private Uri _uri;
        private string _l = string.Empty;
        private string _r = string.Empty;
        private string _server;
        private int _port;
        private string _defaultSchema = string.Empty;
        private ConnectionStringProperties _connectionStringProperties = new ConnectionStringProperties();

        public ConnectionConfigurationElement Source { get; set; }
        public string Name { get; set; }
        public string TypeAndAssemblyName { get; set; }
        public int BatchSize { get; set; }
        public string Process { get; set; }
        public string Path { get; set; }
        public IConnectionChecker ConnectionChecker { get; set; }
        public IEntityRecordsExist EntityRecordsExist { get; set; }
        public IEntityDropper EntityDropper { get; set; }
        public IEntityCreator EntityCreator { get; set; }
        public IScriptRunner ScriptRunner { get; set; }
        public ITableQueryWriter TableQueryWriter { get; set; }
        public ITflWriter TflWriter { get; set; }
        public IViewWriter ViewWriter { get; set; }
        public IDataTypeService DataTypeService { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string PersistSecurityInfo { get; set; }
        public string File { get; set; }
        public string Folder { get; set; }
        public ErrorMode ErrorMode { get; set; }
        public string Delimiter { get; set; }
        public string SearchPattern { get; set; }
        public SearchOption SearchOption { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public ProviderType Type { get; set; }
        public bool IsDatabase { get; set; }
        public bool InsertMultipleRows { get; set; }
        public bool Top { get; set; }
        public bool NoLock { get; set; }
        public bool TableVariable { get; set; }
        public bool NoCount { get; set; }
        public bool MaxDop { get; set; }
        public bool IndexInclude { get; set; }
        public bool Views { get; set; }
        public bool Schemas { get; set; }
        public string DateFormat { get; set; }
        public string Header { get; set; }
        public string Footer { get; set; }
        public bool EnableSsl { get; set; }
        public bool Direct { get; set; }
        public bool TableSample { get; set; }
        public string Encoding { get; set; }
        public string Version { get; set; }
        public ConnectionIs Is { get; set; }
        public string TextQualifier { get; set; }

        public ConnectionStringProperties ConnectionStringProperties {
            get { return _connectionStringProperties; }
            set { _connectionStringProperties = value; }
        }

        public string DefaultSchema {
            get { return _defaultSchema; }
            set { _defaultSchema = value; }
        }

        public string Server {
            get { return _server; }
            set {
                _server = value;
                _uri = null;
            }
        }

        public int Port {
            get { return _port; }
            set {
                _port = value;
                _uri = null;
            }
        }

        public string L {
            get { return _l; }
            set { _l = value; }
        }

        public string R {
            get { return _r; }
            set { _r = value; }
        }

        protected AbstractConnection(ConnectionConfigurationElement element, AbstractConnectionDependencies dependencies) {

            Source = element;
            BatchSize = element.BatchSize;
            Name = element.Name;
            Start = element.Start;
            End = element.End;
            File = element.File;
            Folder = element.Folder;
            Delimiter = element.Delimiter;
            DateFormat = element.DateFormat;
            Header = element.Header;
            Footer = element.Footer;
            EnableSsl = element.EnableSsl;
            Direct = element.Direct;
            Encoding = element.Encoding;
            Version = element.Version;
            Path = element.Path;
            ErrorMode = (ErrorMode)Enum.Parse(typeof(ErrorMode), element.ErrorMode, true);
            SearchOption = (SearchOption)Enum.Parse(typeof(SearchOption), element.SearchOption, true);
            SearchPattern = element.SearchPattern;

            ProcessConnectionString(element);

            TableQueryWriter = dependencies.TableQueryWriter;
            ConnectionChecker = dependencies.ConnectionChecker;
            EntityRecordsExist = dependencies.EntityRecordsExist;
            EntityDropper = dependencies.EntityDropper;
            EntityCreator = dependencies.EntityCreator;
            ViewWriter = dependencies.ViewWriter;
            TflWriter = dependencies.TflWriter;
            ScriptRunner = dependencies.ScriptRunner;
            DataTypeService = dependencies.DataTypeService;

            Is = new ConnectionIs(this);
        }

        private void ProcessConnectionString(ConnectionConfigurationElement element) {
            if (element.ConnectionString != string.Empty) {
                Database = ConnectionStringParser.GetDatabaseName(element.ConnectionString);
                Server = ConnectionStringParser.GetServerName(element.ConnectionString);
                User = ConnectionStringParser.GetUsername(element.ConnectionString);
                Password = ConnectionStringParser.GetPassword(element.ConnectionString);
                PersistSecurityInfo = ConnectionStringParser.GetPersistSecurityInfo(element.ConnectionString);
            } else {
                Server = element.Server;
                Database = element.Database;
                User = element.User;
                Password = element.Password;
                Port = element.Port;
            }
        }

        public IDbConnection GetConnection() {
            var type = System.Type.GetType(TypeAndAssemblyName, false, true);
            var connection = (IDbConnection)Activator.CreateInstance(type);
            connection.ConnectionString = GetConnectionString();
            return connection;
        }

        public IScriptReponse ExecuteScript(string script, int timeOut = 0) {
            return ScriptRunner.Execute(this, script, timeOut);
        }

        public string WriteTemporaryTable(string name, Fields fields, bool useAlias = true) {
            return TableQueryWriter.WriteTemporary(this, name, fields, useAlias);
        }

        public bool IsReady() {
            return ConnectionChecker.Check(this);
        }

        public void AddParameter(IDbCommand command, string name, object val) {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = val ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        public bool RecordsExist(Entity entity) {
            return EntityRecordsExist.RecordsExist(this, entity);
        }

        public void Drop(Entity entity) {
            EntityDropper.Drop(this, entity);
        }

        public Uri Uri() {
            if (_uri != null)
                return _uri;

            var builder = new UriBuilder(Server.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? Server : "http://" + Server);
            if (Port > 0) {
                builder.Port = Port;
            }
            _uri = builder.Uri;
            return _uri;
        }

        public void Create(Process process, Entity entity) {
            EntityCreator.Create(this, process, entity);
        }

        public Entity TflBatchEntity(string processName) {
            return new Entity {
                TflBatchId = 1,
                Name = "TflBatch",
                ProcessName = processName,
                Alias = "TflBatch",
                Schema = "dbo",
                PrimaryKey = new Fields() {
                    new Field(FieldType.PrimaryKey) {
                        Name = "TflBatchId"
                    }
                }
            };
        }

        public bool TflBatchRecordsExist(string processName) {
            return RecordsExist(TflBatchEntity(processName));
        }

        public bool TflBatchExists(string processName) {
            return EntityRecordsExist.EntityExists.Exists(this, TflBatchEntity(processName));
        }

        public string Enclose(string field) {
            return L + field + R;
        }

        public string GetConnectionString() {
            return ConnectionStringProperties.GetConnectionString(this);
        }

        //concrete class may override these
        public virtual string KeyRangeQuery(Entity entity) { throw new NotImplementedException(); }
        public virtual string KeyQuery(Entity entity) { throw new NotImplementedException(); }
        public virtual string KeyAllQuery(Entity entity) { throw new NotImplementedException(); }

        //concrete must implement these

        /// <summary>
        /// Get's the next batch id from the output.  Returns 1 on first run.
        /// </summary>
        /// <param name="processName">a common output may have many processes loading data into it, so you have to pass in the process name to get the right batch id.</param>
        /// <returns></returns>
        public abstract int NextBatchId(string processName);

        /// <summary>
        /// Complete the process by writing a batch record to the output.  Record the max date or rowversion read from input.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="input"></param>
        /// <param name="entity"></param>
        /// <param name="force"></param>
        public abstract void WriteEndVersion(Process process, AbstractConnection input, Entity entity, bool force = false);

        /// <summary>
        /// Try to be clever and pull the matching input and output keys along with the version in order to detect changes.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract IOperation ExtractCorrespondingKeysFromOutput(Entity entity);

        /// <summary>
        /// Just get all the keys from the output, certain assumptions can be made about output
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract IOperation ExtractAllKeysFromOutput(Entity entity);

        /// <summary>
        /// Just get all the keys from the input
        /// </summary>
        /// <param name="process"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract IOperation ExtractAllKeysFromInput(Process process, Entity entity);

        /// <summary>
        /// Insert crap as fast as you can
        /// </summary>
        /// <param name="process"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract IOperation Insert(Process process, Entity entity);

        /// <summary>
        /// Update stuff as fast as you can
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract IOperation Update(Entity entity);

        /// <summary>
        /// Get a correctly typed version for the maximum tflbatchid given an entity and process name
        /// Sets entity.HasRange and entity.Begin.
        /// </summary>
        /// <param name="entity">an entity</param>
        public abstract void LoadBeginVersion(Entity entity);

        /// <summary>
        /// Get maximum version from entity. Sets entity.HasRows and entity.End
        /// </summary>
        /// <param name="entity">an entity</param>
        public abstract void LoadEndVersion(Entity entity);

        public abstract Fields GetEntitySchema(Process process, Entity entity, bool isMaster = false);

        public abstract IOperation Delete(Entity entity);

        public abstract IOperation Extract(Process process, Entity entity, bool firstRun);
    }
}