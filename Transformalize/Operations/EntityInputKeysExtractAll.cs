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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Transformalize.Libs.Rhino.Etl;
using Transformalize.Libs.Rhino.Etl.Operations;
using Transformalize.Main;

namespace Transformalize.Operations {

    public class EntityInputKeysExtractAll : InputCommandOperation {

        private readonly Dictionary<string, Func<IDataReader, int, object, object>> _map = Common.GetReaderMap();
        private readonly Entity _entity;
        private readonly FieldTypeDefault[] _fields;
        private readonly int _length;

        public EntityInputKeysExtractAll(Entity entity)
            : base(entity.InputConnection) {

            _entity = entity;
            _fields = _entity.PrimaryKey.Select(f => new FieldTypeDefault(f.Value.Alias, _map.ContainsKey(f.Value.SimpleType) ? f.Value.SimpleType : string.Empty, f.Value.Default)).ToArray();
            _length = _fields.Length;

            var connection = _entity.InputConnection;

            if (entity.CanDetectChanges()) {
                connection.LoadEndVersion(_entity);
                if (!_entity.HasRows) {
                    Debug("No data detected in {0}.", _entity.Alias);
                }
            }

        }

        protected override Row CreateRowFromReader(IDataReader reader) {
            var row = new Row();
            for (var i = 0; i < _length; i++) {
                row[_fields[i].Alias] = _map[_fields[i].Type](reader, i, _fields[i].Default);
            }
            return row;
        }

        protected override void PrepareCommand(IDbCommand cmd) {
            cmd.CommandTimeout = 0;
            cmd.CommandText = _entity.KeysQuery();
            AddParameter(cmd, "@End", _entity.End);
        }
    }
}