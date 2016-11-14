﻿#region license
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
using System.Linq;
using NUnit.Framework;

namespace Pipeline.Test {

    [TestFixture]
    public class CopyTransform {

        [Test(Description = "Copy Transform")]
        public void CopyTransform1() {

            const string xml = @"
<add name='TestProcess'>
    <entities>
        <add name='TestData'>
            <rows>
                <add Field1='1' Field2='2' />
            </rows>
            <fields>
                <add name='Field1' type='int'/>
                <add name='Field2' type='int'/>
            </fields>
            <calculated-fields>
                <add name='Field3' type='int' t='copy(Field2)' />
            </calculated-fields>
        </add>
    </entities>
</add>";


            var composer = new CompositionRoot();
            var controller = composer.Compose(xml);

            var output = controller.Read().ToArray();

            Assert.AreEqual(2, output[0][composer.Process.Entities.First().CalculatedFields.First()]);

        }

    }
}
