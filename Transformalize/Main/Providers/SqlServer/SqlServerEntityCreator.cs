using System.Collections.Generic;
using Transformalize.Libs.Dapper;
using Transformalize.Logging;

namespace Transformalize.Main.Providers.SqlServer {

    public class SqlServerEntityCreator : DatabaseEntityCreator {

        public override void Create(AbstractConnection connection, Process process, Entity entity) {

            if (EntityExists != null && EntityExists.Exists(connection, entity)) {
                TflLogger.Warn(process.Name, entity.Name, "Trying to create entity that already exists! {0}", entity.Name);
                return;
            }

            var keyType = entity.IsMaster() ? FieldType.MasterKey : FieldType.PrimaryKey;

            var writer = process.StarEnabled && keyType == FieldType.MasterKey ?
                new FieldSqlWriter(entity.Fields, entity.CalculatedFields, process.CalculatedFields, GetRelationshipFields(process.Relationships, entity)) :
                new FieldSqlWriter(entity.Fields, entity.CalculatedFields);

            var primaryKey = writer.FieldType(keyType).Alias(connection.L, connection.R).Asc().Values();
            var defs = new List<string>();
            defs.AddRange(writer
                .Reload()
                .AddBatchId(entity.Index)
                .AddDeleted(entity)
                .AddSurrogateKey(entity.Index)
                .Output()
                .Alias(connection.L, connection.R)
                .DataType(new SqlServerDataTypeService())
                .AppendIf(" NOT NULL", keyType)
                .Values());

            var rowVersion = entity.Fields.WithSimpleType("rowversion").WithoutInput().WithoutOutput();
            if (rowVersion.Any()) {
                var alias = rowVersion.First().Alias;
                defs.Add(connection.Enclose(alias) + " [ROWVERSION] NOT NULL");
            }

            var createSql = connection.TableQueryWriter.CreateTable(entity.OutputName(), defs);
            TflLogger.Debug(process.Name, entity.Name, createSql);

            var indexSql = connection.TableQueryWriter.AddUniqueClusteredIndex(entity.OutputName());
            TflLogger.Debug(process.Name, entity.Name, indexSql);

            var keySql = connection.TableQueryWriter.AddPrimaryKey(entity.OutputName(), primaryKey);
            TflLogger.Debug(process.Name, entity.Name, keySql);

            using (var cn = connection.GetConnection()) {
                cn.Open();
                cn.Execute(createSql);
                cn.Execute(indexSql);
                cn.Execute(keySql);
                TflLogger.Info(process.Name, entity.Name, "Initialized {0} in {1} on {2}.", entity.OutputName(), connection.Database, connection.Server);
            }
        }

    }
}