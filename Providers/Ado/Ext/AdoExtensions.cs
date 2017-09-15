﻿#region license
// Transformalize
// Configurable Extract, Transform, and Load
// Copyright 2013-2017 Dale Newman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//       http://www.apache.org/licenses/LICENSE-2.0
//   
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Transformalize.Configuration;
using Transformalize.Context;
using Transformalize.Contracts;
using Transformalize.Extensions;

namespace Transformalize.Providers.Ado.Ext {
    public static class AdoExtensions {

        public static string SqlControlTableName(this OutputContext c) {
            return Utility.Identifier(c.Process.Name) + "Control";
        }

        private static string DefaultValue(Field field, IConnectionFactory cf) {

            if (field.Default == null)
                return "NULL";

            var d = field.Default == Constants.DefaultSetting ? Constants.StringDefaults()[field.Type] : field.Default;

            if (AdoConstants.StringTypes.Any(t => t == field.Type)) {
                return "'" + d + "'";
            }

            if (!field.Type.StartsWith("bool", StringComparison.Ordinal))
                return d;

            if (cf.AdoProvider == AdoProvider.PostgreSql) {
                return d.Equals("true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
            }
            return d.Equals("true", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
        }

        public static string SqlCreateOutputUniqueIndex(this OutputContext c, IConnectionFactory cf) {
            var tableName = c.Entity.OutputTableName(c.Process.Name);
            var pk = c.Entity.GetAllFields().Where(f => f.PrimaryKey).Select(f => f.FieldName()).ToArray();
            var indexName = ("UX_" + Utility.Identifier(tableName + "_" + SqlKeyName(pk))).Left(128);
            var sql = $"CREATE UNIQUE INDEX {cf.Enclose(indexName)} ON {cf.Enclose(tableName)} ({string.Join(",", pk.Select(cf.Enclose))})";
            if (c.Entity.IgnoreDuplicateKey && c.Connection.Provider == "sqlserver") {
                sql += " WITH (IGNORE_DUP_KEY = ON)";
            }
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlCreateFlatIndex(this OutputContext c, IConnectionFactory cf) {
            var pk = c.Process.Entities.First(e => e.IsMaster).GetAllFields().Where(f => f.PrimaryKey).Select(f => f.Alias).ToArray();
            var indexName = ("UX_" + Utility.Identifier(c.Process.Flat + "_" + SqlKeyName(pk))).Left(128);
            var sql = $"CREATE UNIQUE INDEX {cf.Enclose(indexName)} ON {cf.Enclose(c.Process.Flat)} ({string.Join(",", pk.Select(cf.Enclose))}){cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }
        public static string SqlSelectOutputSchema(this OutputContext c, IConnectionFactory cf) {
            var table = $"{cf.Enclose(c.Entity.OutputTableName(c.Process.Name))}";
            var sql = cf.AdoProvider == AdoProvider.SqlServer || cf.AdoProvider == AdoProvider.SqlCe || cf.AdoProvider == AdoProvider.Access ?
                $"SELECT TOP 0 * FROM {table}{cf.Terminator}" :
                $"SELECT * FROM {table} LIMIT 0";
            c.Debug(() => sql);
            return sql;
        }

        public static string SchemaPrefix(this IContext c, IConnectionFactory f) {
            return c.Entity.Schema == string.Empty ? string.Empty : f.Enclose(c.Entity.Schema) + ".";
        }

        public static string SqlSelectInput(this InputContext c, Field[] fields, IConnectionFactory cf) {
            var fieldList = string.Join(",", fields.Select(f => cf.Enclose(f.Name)));
            var table = SqlInputName(c, cf);
            var filter = c.Entity.Filter.Any() ? " WHERE " + c.ResolveFilter(cf) : string.Empty;
            var orderBy = c.ResolveOrder(cf);

            if (!c.Entity.IsPageRequest())
                return $"SELECT {fieldList} FROM {SqlInputName(c, cf)} {filter} {orderBy}";

            var start = (c.Entity.Page * c.Entity.PageSize) - c.Entity.PageSize;
            var end = start + c.Entity.PageSize;

            switch (cf.AdoProvider) {
                case AdoProvider.SqlServer:
                    if (string.IsNullOrWhiteSpace(orderBy)) {
                        orderBy = GetRequiredOrderBy(fields, cf);
                    }
                    var subQuery = $@"SELECT {fieldList},ROW_NUMBER() OVER ({orderBy}) AS TflRow FROM {table} WITH (NOLOCK) {filter}";
                    return $"WITH p AS ({subQuery}) SELECT {fieldList} FROM p WHERE TflRow BETWEEN {start + 1} AND {end}";
                case AdoProvider.SqlCe:
                    if (string.IsNullOrWhiteSpace(orderBy)) {
                        orderBy = GetRequiredOrderBy(fields, cf);
                    }
                    return $"SELECT {fieldList} FROM {table} {filter} {orderBy} OFFSET {start} ROWS FETCH NEXT {c.Entity.PageSize} ROWS ONLY";
                case AdoProvider.Access:
                    // todo: make sure primary key is always include in sort to avoid wierd access top n behavior, see: https://stackoverflow.com/questions/887787/access-sql-using-top-5-returning-more-than-5-results
                    if (string.IsNullOrWhiteSpace(orderBy)) {
                        orderBy = GetRequiredOrderBy(fields, cf);
                    }
                    var xFieldList = string.Join(",", fields.Select(f => "x." + cf.Enclose(f.Name)));
                    var yFieldList = string.Join(",", fields.Select(f => "y." + cf.Enclose(f.Name)));
                    var flippedOrderBy = orderBy.Replace(" ASC", " ^a").Replace(" DESC"," ^d").Replace(" ^a"," DESC").Replace(" ^d"," ASC");

                    return $@"SELECT {yFieldList}
FROM (
    SELECT TOP {c.Entity.PageSize} {xFieldList}
    FROM (
        SELECT TOP {end} {fieldList} FROM {table} {filter} {orderBy}
    ) x
   {flippedOrderBy}
) y
{orderBy}";
                case AdoProvider.PostgreSql:
                    return $"SELECT {fieldList} FROM {table} {filter} {orderBy} LIMIT {c.Entity.PageSize} OFFSET {start}";
                case AdoProvider.MySql:
                case AdoProvider.SqLite:
                    return $"SELECT {fieldList} FROM {table} {filter} {orderBy} LIMIT {start},{c.Entity.PageSize}";
                case AdoProvider.None:
                    return string.Empty;
                default:
                    return string.Empty;
            }
        }

        public static string SqlInputName(this InputContext c, IConnectionFactory cf) {
            return SchemaPrefix(c, cf) + cf.Enclose(c.Entity.Name);
        }

        public static string SqlSelectInputWithMinVersion(this InputContext c, Field[] fields, IConnectionFactory cf) {
            var versionFilter = $"{cf.Enclose(c.Entity.GetVersionField().Name)} {(c.Entity.Overlap ? ">=" : ">")} @MinVersion";
            var fieldList = string.Join(",", fields.Select(f => cf.Enclose(f.Name)));
            var table = SqlInputName(c, cf);
            var filter = c.Entity.Filter.Any() ? $" WHERE {c.ResolveFilter(cf)} AND {versionFilter}" : $" WHERE {versionFilter}";
            return $"SELECT {fieldList} FROM {table} {filter} {c.ResolveOrder(cf)}";
        }

        public static string SqlCreateOutput(this OutputContext c, IConnectionFactory cf) {
            var columnsAndDefinitions = string.Join(",", c.GetAllEntityOutputFields().Select(f => cf.Enclose(f.FieldName()) + " " + cf.SqlDataType(f) + " NOT NULL"));
            var sql = $"CREATE TABLE {cf.Enclose(c.Entity.OutputTableName(c.Process.Name))}({columnsAndDefinitions}, ";
            if (cf.AdoProvider == AdoProvider.SqLite) {
                sql += $"PRIMARY KEY ({cf.Enclose(c.Entity.TflKey().FieldName())} ASC));";
            } else {
                sql += $"CONSTRAINT {Utility.Identifier("pk_" + c.Entity.OutputTableName(c.Process.Name) + "_tflkey")} PRIMARY KEY ({cf.Enclose(c.Entity.TflKey().FieldName())})){cf.Terminator}";
            }
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlSchemaPrefix(this IContext c, IConnectionFactory cf) {
            return c.Entity.Schema == string.Empty ? string.Empty : cf.Enclose(c.Entity.Schema) + ".";
        }

        public static string SqlInsertIntoOutput(this OutputContext c, IConnectionFactory cf) {
            var fields = c.OutputFields.ToArray();
            var parameters = cf.AdoProvider == AdoProvider.Access ? string.Join(",", fields.Select(f => "?")) : string.Join(",", fields.Select(f => "@" + f.FieldName()));
            var sql = $"INSERT INTO {cf.Enclose(c.Entity.OutputTableName(c.Process.Name))} VALUES({parameters}){cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static IEnumerable<Field> GetUpdateFields(this OutputContext c) {
            var fields = c.Entity.GetAllFields().Where(f => f.Output).ToArray();
            foreach (var field in fields.Where(f => !f.PrimaryKey && f.Name != Constants.TflKey).OrderBy(f => f.Index)) {
                yield return field;
            }
            foreach (var field in fields.Where(f => f.PrimaryKey).OrderBy(f => f.Index)) {
                yield return field;
            }
        }

        public static string SqlUpdateOutput(this OutputContext c, IConnectionFactory cf) {
            var fields = GetUpdateFields(c).ToArray();
            var sets = string.Join(",", fields.Where(f => !f.PrimaryKey && f.Name != Constants.TflKey).OrderBy(f => f.Index).Select(f => f.FieldName()).Select(n => cf.Enclose(n) + (cf.AdoProvider == AdoProvider.Access ? " = ?" : " = @" + n)));
            var criteria = string.Join(" AND ", fields.Where(f => f.PrimaryKey).OrderBy(f => f.Index).Select(f => f.FieldName()).Select(n => cf.Enclose(n) + (cf.AdoProvider == AdoProvider.Access ? " = ?" : " = @" + n)));
            var sql = $"UPDATE {cf.Enclose(c.Entity.OutputTableName(c.Process.Name))} SET {sets} WHERE {criteria}{cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static IEnumerable<Field> GetUpdateCalculatedFields(this OutputContext c) {
            foreach (var field in c.Entity.CalculatedFields.Where(f => f.Output && f.Name != Constants.TflKey).OrderBy(f => f.Index)) {
                yield return field;
            }
            yield return c.Entity.TflKey();
        }

        public static string SqlUpdateCalculatedFields(this OutputContext c, Process original, IConnectionFactory cnf) {
            var master = original.Entities.First(e => e.IsMaster);
            var fields = GetUpdateCalculatedFields(c).Where(f => f.Name != Constants.TflKey).ToArray();
            var sets = string.Join(",", fields.Select(f => cnf.Enclose(original.CalculatedFields.First(cf => cf.Name == f.Name).FieldName()) + (cnf.AdoProvider == AdoProvider.Access ? " = ?" : " = @" + f.FieldName())));
            var key = c.Entity.TflKey().FieldName();
            var sql = $"UPDATE {cnf.Enclose(master.OutputTableName(original.Name))} SET {sets} WHERE {cnf.Enclose(key)} = {(cnf.AdoProvider == AdoProvider.Access ? "?" : "@" + key)}{cnf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlDeleteOutput(this OutputContext c, IConnectionFactory cf, int batchId) {
            var deletedValue = cf.AdoProvider == AdoProvider.PostgreSql ? "true" : "1";
            var criteria = string.Join(" AND ", c.Entity.GetPrimaryKey().Select(f => f.FieldName()).Select(n => cf.Enclose(n) + " = @" + n));
            var sql = $"UPDATE {cf.Enclose(c.Entity.OutputTableName(c.Process.Name))} SET {cf.Enclose(c.Entity.TflDeleted().FieldName())} = {deletedValue} WHERE {criteria}";
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlDropOutput(this OutputContext c, IConnectionFactory cf) {
            var cascade = cf.AdoProvider == AdoProvider.PostgreSql ? " CASCADE" : string.Empty;
            var sql = $"DROP TABLE {cf.Enclose(c.Entity.OutputTableName(c.Process.Name))}{cascade}{cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlDropOutputView(this OutputContext c, IConnectionFactory cf) {
            var sql = $"DROP {(cf.AdoProvider == AdoProvider.Access ? "TABLE" : "VIEW")} {cf.Enclose(c.Entity.OutputViewName(c.Process.Name))}{cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlDropOutputViewAsTable(this OutputContext c, IConnectionFactory cf) {
            var sql = $"DROP TABLE {cf.Enclose(c.Entity.OutputViewName(c.Process.Name))}{cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlDropControl(this OutputContext c, IConnectionFactory cf) {
            var sql = $"DROP TABLE {cf.Enclose(SqlControlTableName(c))}{cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlDeleteEntityFromControl(this OutputContext c, IConnectionFactory cf) {
            var sql = $"DELETE FROM {cf.Enclose(SqlControlTableName(c))} WHERE Entity = @Entity";
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlCount(this OutputContext c, IConnectionFactory cf) {
            return $"SELECT COUNT(*) FROM {cf.Enclose(c.Entity.OutputTableName(c.Process.Name))};";
        }

        public static string SqlControlLastBatchId(this OutputContext c, IConnectionFactory cf) {
            string sql;
            if (cf.AdoProvider == AdoProvider.Access) {
                sql = $"SELECT IIF(ISNULL(MAX({cf.Enclose("BatchId")})),0,MAX({cf.Enclose("BatchId")})) FROM {cf.Enclose(SqlControlTableName(c))}";
            } else {
                sql = $"SELECT COALESCE(MAX({cf.Enclose("BatchId")}),0) FROM {cf.Enclose(SqlControlTableName(c))};";
            }
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlControlStartBatch(this OutputContext c, IConnectionFactory cf) {
            var values = cf.AdoProvider == AdoProvider.Access ? "?,?,0,0,0,?" : "@BatchId,@Entity,0,0,0,@Start";
            var sql = $@"INSERT INTO {cf.Enclose(SqlControlTableName(c))}({cf.Enclose("BatchId")},{cf.Enclose("Entity")},{cf.Enclose("Inserts")},{cf.Enclose("Updates")},{cf.Enclose("Deletes")},{cf.Enclose("Start")}) VALUES({values}){cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlControlEndBatch(this OutputContext c, IConnectionFactory cf) {
            string sql;
            if (cf.AdoProvider == AdoProvider.Access) {
                sql = $"UPDATE {cf.Enclose(SqlControlTableName(c))} SET {cf.Enclose("Inserts")} = ?, {cf.Enclose("Updates")} = ?, {cf.Enclose("Deletes")} = ?, {cf.Enclose("End")} = ? WHERE {cf.Enclose("Entity")} = ? AND {cf.Enclose("BatchId")} = ?";
            } else {
                sql = $"UPDATE {cf.Enclose(SqlControlTableName(c))} SET {cf.Enclose("Inserts")} = @Inserts, {cf.Enclose("Updates")} = @Updates, {cf.Enclose("Deletes")} = @Deletes, {cf.Enclose("End")} = @End WHERE {cf.Enclose("Entity")} = @Entity AND {cf.Enclose("BatchId")} = @BatchId{cf.Terminator}";
            }
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlCreateControl(this OutputContext c, IConnectionFactory cf) {
            var dateType = (cf.AdoProvider == AdoProvider.PostgreSql ? "TIMESTAMP" : "DATETIME");
            var longType = cf.AdoProvider == AdoProvider.Access ? "LONG" : "BIGINT";
            var stringType = (cf.AdoProvider == AdoProvider.SqlServer || cf.AdoProvider == AdoProvider.SqlCe ? "N" : string.Empty);

            var sql = $@"
                CREATE TABLE {cf.Enclose(SqlControlTableName(c))}(
                    {cf.Enclose("BatchId")} INTEGER NOT NULL,
                    {cf.Enclose("Entity")} {stringType}{(cf.AdoProvider == AdoProvider.Access ? "CHAR" : "VARCHAR")}(128) NOT NULL,
                    {cf.Enclose("Inserts")} {longType} NOT NULL,
                    {cf.Enclose("Updates")} {longType} NOT NULL,
                    {cf.Enclose("Deletes")} {longType} NOT NULL,
                    {cf.Enclose("Start")} {dateType} NOT NULL,
                    {cf.Enclose("End")} {dateType},
                    CONSTRAINT PK_{Utility.Identifier(SqlControlTableName(c))}_BatchId PRIMARY KEY ({cf.Enclose("BatchId")}, {cf.Enclose("Entity")})
                ){cf.Terminator}";

            c.Debug(() => sql);

            return sql;
        }

        public static string SqlCreateOutputView(this OutputContext c, IConnectionFactory cf) {
            var columnNames = string.Join(",", c.GetAllEntityOutputFields().Select(f => cf.Enclose(f.FieldName()) + " AS " + cf.Enclose(f.Alias)));
            var sql = $@"CREATE VIEW {cf.Enclose(c.Entity.OutputViewName(c.Process.Name))} AS SELECT {columnNames} FROM {cf.Enclose(c.Entity.OutputTableName(c.Process.Name))}{cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlDropStarView(this OutputContext c, IConnectionFactory cf) {
            var sql = $"DROP {(cf.AdoProvider == AdoProvider.Access ? "TABLE" : "VIEW")} {cf.Enclose(c.Process.Star)}{cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlDropFlatTable(this OutputContext c, IConnectionFactory cf) {
            var sql = $"DROP TABLE {cf.Enclose(c.Process.Flat)}{cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static List<string> SqlStarFroms(this IContext c, IConnectionFactory cf) {
            var master = c.Process.Entities.First(e => e.IsMaster);
            var masterAlias = Utility.GetExcelName(master.Index);
            var builder = new StringBuilder();
            var leaves = c.Process.Entities.Where(e => !e.IsMaster).ToArray();

            var open = cf.AdoProvider != AdoProvider.Access ? string.Empty : new string('(', leaves.Length);
            var close = cf.AdoProvider != AdoProvider.Access ? string.Empty : ")";

            var froms = new List<string>(c.Process.Entities.Count){
                $"FROM {open}{cf.Enclose(master.OutputTableName(c.Process.Name))} {masterAlias}"
            };

            foreach (var entity in leaves) {
                builder.Clear();
                builder.AppendFormat("LEFT OUTER JOIN {0} {1} ON (", cf.Enclose(entity.OutputTableName(c.Process.Name)), Utility.GetExcelName(entity.Index));

                var relationship = entity.RelationshipToMaster.First();

                foreach (var join in relationship.Join.ToArray()) {
                    var leftField = c.Process.GetEntity(relationship.LeftEntity).GetField(join.LeftField);
                    var rightField = entity.GetField(join.RightField);
                    builder.AppendFormat("{0}.{1} = {2}.{3} AND ", masterAlias, cf.Enclose(leftField.FieldName()), Utility.GetExcelName(entity.Index), cf.Enclose(rightField.FieldName()));
                }

                if (entity.Delete) {
                    builder.Append($"{cf.Enclose(Utility.GetExcelName(entity.Index))}.{cf.Enclose(entity.TflDeleted().FieldName())} = 0");
                } else {
                    builder.Remove(builder.Length - 5, 5);
                }

                builder.Append($"){close}");
                froms.Add(builder.ToString());
            }

            return froms;
        }

        public static string SqlStarFields(this IContext c, IConnectionFactory cf) {
            var starFields = c.Process.GetStarFields().ToArray();
            var master = c.Process.Entities.First(e => e.IsMaster);
            var masterAlias = Utility.GetExcelName(master.Index);
            var masterNames = string.Join(",", starFields[0].Select(f => masterAlias + "." + cf.Enclose(f.FieldName()) + " AS " + cf.Enclose(f.Alias)));
            string slaveNames;
            if (cf.AdoProvider == AdoProvider.Access) {
                slaveNames = string.Join(",", starFields[1].Select(f => "IIF(ISNULL(" + Utility.GetExcelName(f.EntityIndex) + "." + cf.Enclose(f.FieldName()) + "), " + DefaultValue(f, cf) + "," + Utility.GetExcelName(f.EntityIndex) + "." + cf.Enclose(f.FieldName()) + ") AS " + cf.Enclose(f.Alias)));
            } else {
                slaveNames = string.Join(",", starFields[1].Select(f => "COALESCE(" + Utility.GetExcelName(f.EntityIndex) + "." + cf.Enclose(f.FieldName()) + ", " + DefaultValue(f, cf) + ") AS " + cf.Enclose(f.Alias)));
            }

            return $"{masterNames}{(slaveNames == string.Empty ? string.Empty : "," + slaveNames)}";
        }

        public static string SqlSelectStar(this IContext c, IConnectionFactory cf) {
            var builder = new StringBuilder();

            foreach (var from in SqlStarFroms(c, cf)) {
                builder.AppendLine(from);
            }

            return $"SELECT {SqlStarFields(c, cf)} {builder}";
        }

        public static string SqlCreateStarView(this IContext c, IConnectionFactory cf) {
            var select = SqlSelectStar(c, cf);
            var sql = $"CREATE VIEW {cf.Enclose(c.Process.Star)} AS {select}{cf.Terminator}";
            c.Debug(() => sql);
            return sql;
        }

        public static string SqlCreateFlatTable(this IContext c, IConnectionFactory cf) {
            var definitions = new List<string>();
            foreach (var entity in c.Process.GetStarFields()) {
                foreach (var field in entity) {
                    definitions.Add(cf.Enclose(field.Alias) + " " + cf.SqlDataType(field) + " NOT NULL");
                }
            }

            var sql = $"CREATE TABLE {cf.Enclose(c.Process.Flat)}({string.Join(",", definitions)}, ";
            if (cf.AdoProvider == AdoProvider.SqLite) {
                sql += $"PRIMARY KEY ({cf.Enclose(Constants.TflKey)} ASC));";
            } else {
                sql += $"CONSTRAINT {Utility.Identifier("pk_" + c.Process.Flat + "_tflkey")} PRIMARY KEY ({cf.Enclose(Constants.TflKey)})){cf.Terminator}";
            }
            c.Debug(() => sql);
            return sql;
        }

        private static string SqlKeyName(string[] pk) {
            return Utility.Identifier(string.Join("_", pk));
        }

        public static IDbDataParameter AddParameter(this IDbCommand cmd, string name, object value) {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            p.Direction = ParameterDirection.Input;
            return p;
        }

        private static string GetRequiredOrderBy(Field[] fields, IConnectionFactory cf) {
            var keys = string.Join(", ", fields.Where(f => f.PrimaryKey).Select(f => cf.Enclose(f.Name) + " ASC"));
            if (string.IsNullOrEmpty(keys)) {
                keys = fields.First(f => f.Input).Name + " ASC";
            }
            return $" ORDER BY {keys}";
        }
    }
}
