using System.Linq;
using Transformalize.Libs.Rhino.Etl;
using Transformalize.Main;
using Transformalize.Operations;
using Transformalize.Runner;

namespace Transformalize.Processes {
    public class MasterJoinProcess : EtlProcess {
        private readonly Process _process;
        private readonly CollectorOperation _collector;

        public MasterJoinProcess(Process process, ref CollectorOperation collector) {
            _process = process;
            _collector = collector;
        }


        protected override void Initialize() {
            Register(new RowsOperation(_process.Relationships.First().LeftEntity.Rows));
            foreach (var rel in _process.Relationships) {
                Register(new EntityJoinOperation(rel).Right(new RowsOperation(rel.RightEntity.Rows)));
            }
            Register(_collector);
        }
    }
}