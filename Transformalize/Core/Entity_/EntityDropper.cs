/*
Transformalize - Replicate, Transform, and Denormalize Your Data...
Copyright (C) 2013 Dale Newman

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Transformalize.Core.Process_;
using Transformalize.Providers.SqlServer;

namespace Transformalize.Core.Entity_ {
    public class EntityDropper {

        private readonly IEntityDropper _dropper;

        public EntityDropper(IEntityDropper dropper = null) {
            _dropper = dropper ?? new SqlServerEntityDropper();
        }

        public void Drop() {
            foreach (var entity in Process.Entities) {
                _dropper.DropOutput(entity);
            }
        }
    }
}