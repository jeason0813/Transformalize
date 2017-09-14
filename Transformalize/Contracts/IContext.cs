﻿#region license
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
using Transformalize.Configuration;

namespace Transformalize.Contracts {
    public interface IContext {
        Process Process { get; }
        Entity Entity { get; }
        Field Field { get; }
        Operation Operation { get; }
        void Debug(Func<string> lamda);
        void Error(string message, params object[] args);
        void Error(Exception exception, string message, params object[] args);
        void Info(string message, params object[] args);
        void Warn(string message, params object[] args);
        IEnumerable<Field> GetAllEntityFields();
        IEnumerable<Field> GetAllEntityOutputFields();
        LogLevel LogLevel { get; }
        string Key { get; }
        IPipelineLogger Logger { get; }
        object[] ForLog { get; }
    }
}