using System.Collections.Generic;
using Transformalize.Libs.Rhino.Etl;

namespace Transformalize.Operations.Transform {
    public class CopyOperation : TflOperation {

        public CopyOperation(string inKey, string outKey) : base(inKey, outKey) { }

        public override IEnumerable<Row> Execute(IEnumerable<Row> rows) {
            foreach (var row in rows) {
                if (ShouldRun(row)) {
                    row[OutKey] = row[InKey];
                }
                yield return row;
            }
        }
    }
}