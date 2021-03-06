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
using System;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Transformalize.Transforms {
    public class IsTransform : StringTransform {
        private readonly Field _input;
        private readonly Func<string, object> _canConvert;
        private readonly bool _isCompatible;

        public IsTransform(IContext context) : base(context, "bool") {
            if (IsMissing(context.Operation.Type)) {
                return;
            }
            _input = SingleInput();
            _isCompatible = Received() == context.Operation.Type || _input.IsNumeric() && context.Operation.Type == "double";
            _canConvert = v => Constants.CanConvert()[context.Operation.Type](v);
        }

        public override IRow Operate(IRow row) {
            row[Context.Field] = _isCompatible ? true : _canConvert(GetString(row, _input));
            Increment();
            return row;
        }

    }
}