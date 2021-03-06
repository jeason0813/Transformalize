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
using System.Collections.Generic;
using Quartz;
using Transformalize.Contracts;

namespace Transformalize.Command {
    [DisallowConcurrentExecution]
    public class NowExecutor : BaseExecutor, IJob, IDisposable {

        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public NowExecutor(IPipelineLogger logger, Options options) : base(logger, options, false) {
        }

        /// <summary>
        /// This is the method Quartz.NET will use
        /// </summary>
        /// <param name="context"></param>
        public void Execute(IJobExecutionContext context) {
            Execute(Cfg, Parameters);
        }

        public void Dispose() {
            // shouldn't be anything to dispose
        }
    }
}