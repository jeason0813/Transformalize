#region license
// Transformalize
// Configurable Extract, Transform, and Load
// Copyright 2013-2016 Dale Newman
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
using Autofac;
using Pipeline.Configuration;
using Pipeline.Contracts;

namespace Pipeline.Ioc.Autofac {
    public class PipelineAction : IAction {
        private readonly IContainer _container;

        public Process Process { get; }

        public PipelineAction(IContainer container, Process process) {
            _container = container;
            Process = process;
        }

        public ActionResponse Execute() {
            var response = new ActionResponse();
            if (!Process.Enabled)
                return response;

            using (var scope = _container.BeginLifetimeScope()) {
                scope.Resolve<IProcessController>().Execute();
            }

            return response;
        }
    }
}