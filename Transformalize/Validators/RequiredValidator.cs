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
using Transformalize.Configuration;
using Transformalize.Contracts;
using Transformalize.Transforms;

namespace Transformalize.Validators {
    public class RequiredValidator : StringValidate {

        private readonly Field _input;
        private readonly object _default;
        private readonly BetterFormat _betterFormat;

        public RequiredValidator(IContext context) : base(context) {
            if (!Run) {
                return;
            }
            var defaults = Constants.TypeDefaults();
            _input = SingleInput();
            _default = _input.Default == Constants.DefaultSetting ? defaults[_input.Type] : _input.Convert(_input.Default);

            var help = context.Field.Help;
            if (help == string.Empty) {
                help = $"{context.Field.Label} is required.";
            }
            _betterFormat = new BetterFormat(context, help, context.Entity.GetAllFields);
        }

        public override IRow Operate(IRow row) {
            var valid = !row[_input].Equals(_default);
            row[ValidField] = valid;
            if (!valid) {
                AppendMessage(row, _betterFormat.Format(row));
            }
            Increment();
            return row;
        }
    }
}