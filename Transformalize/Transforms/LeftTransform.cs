#region license
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
using Transformalize.Contracts;
using Transformalize.Extensions;

namespace Transformalize.Transforms {
    public class LeftTransform : BaseTransform {
        private readonly int _length;
        private readonly IField _input;

        public LeftTransform(IContext context): base(context, "string") {

            if (IsNotReceiving("string")) {
                return;
            }

            if (context.Operation.Length == 0) {
                Error("The left transform requires a length parameter.");
                Run = false;
                return;
            }

            _length = context.Operation.Length;
            _input = SingleInput();
        }

        public override IRow Operate(IRow row) {
            row[Context.Field] = row[_input].ToString().Left(_length);
            Increment();
            return row;
        }

    }
}