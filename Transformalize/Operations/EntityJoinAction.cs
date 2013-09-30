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

using System.Linq;
using Transformalize.Libs.Rhino.Etl;
using Transformalize.Libs.Rhino.Etl.Operations;
using Transformalize.Main;

namespace Transformalize.Operations
{
    public class EntityJoinAction : JoinOperation
    {
        private readonly Entity _entity;
        private readonly string _firstKey;
        private readonly string[] _keys;
        private readonly int _tflBatchId;
        private readonly string _rowVersion;

        public EntityJoinAction(Entity entity)
        {
            _entity = entity;
            _keys = entity.PrimaryKey.Keys.ToArray();
            _firstKey = _keys[0];
            _tflBatchId = entity.TflBatchId;
            _rowVersion = entity.CanDetectChanges() ? entity.Version.Alias : null;
        }

        protected override Row MergeRows(Row leftRow, Row rightRow)
        {
            if (rightRow.ContainsKey(_firstKey))
            {
                if (_rowVersion == null || UpdateIsNecessary(ref leftRow, ref rightRow))
                {
                    leftRow["a"] = EntityAction.Update;
                    leftRow["TflKey"] = rightRow["TflKey"];
                }
                else
                {
                    leftRow["a"] = EntityAction.None;
                }

            }
            else
            {
                leftRow["a"] = EntityAction.Insert;
                leftRow["TflBatchId"] = _tflBatchId;
            }

            return leftRow;
        }

        protected override void SetupJoinConditions()
        {
            LeftJoin.Left(_keys).Right(_keys);
        }
        
        private bool UpdateIsNecessary(ref Row leftRow, ref Row rightRow)
        {
            var bytes = new[] { "byte[]", "rowversion" };
            if (bytes.Any(t => t == _entity.Version.SimpleType)) {
                var beginBytes = (byte[]) leftRow[_entity.Version.Alias];
                var endBytes = (byte[]) rightRow[_entity.Version.Alias];
                return !beginBytes.SequenceEqual(endBytes);
            }
            return !leftRow[_entity.Version.Alias].Equals(rightRow[_entity.Version.Alias]);

        }
    }
}