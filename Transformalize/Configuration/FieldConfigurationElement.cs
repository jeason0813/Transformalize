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

using System.Configuration;

namespace Transformalize.Configuration
{
    public class FieldConfigurationElement : ConfigurationElement {

        private const string AGGREGATE = "aggregate";
        private const string NAME = "name";
        private const string ALIAS = "alias";
        private const string TYPE = "type";
        private const string XML = "xml";
        private const string LENGTH = "length";
        private const string PRECISION = "precision";
        private const string SCALE = "scale";
        private const string INPUT = "input";
        private const string OUTPUT = "output";
        private const string UNICODE = "unicode";
        private const string VARIABLE_LENGTH = "variable-length";
        private const string DEFAULT = "default";
        private const string TRANSFORMS = "transforms";
        private const string SEARCH_TYPE = "search-type";
        private const string SEARCH_TYPES = "search-types";

        public override bool IsReadOnly()
        {
            return false;
        }

        [ConfigurationProperty(NAME, IsRequired = true)]
        public string Name {
            get {
                return this[NAME] as string;
            }
            set { this[NAME] = value; }
        }

        [ConfigurationProperty(ALIAS, IsRequired = false, DefaultValue = "")]
        public string Alias {
            get {
                var alias = this[ALIAS] as string;
                return alias == null || alias.Equals(string.Empty) ? Name : alias;
            }
            set { this[ALIAS] = value; }
        }

        [ConfigurationProperty(TYPE, IsRequired = false, DefaultValue = "string")]
        public string Type {
            get {
                return this[TYPE] as string;
            }
            set { this[TYPE] = value; }
        }


        [ConfigurationProperty(SEARCH_TYPE, IsRequired = false, DefaultValue = "default")]
        public string SearchType
        {
            get
            {
                return this[SEARCH_TYPE] as string;
            }
            set { this[SEARCH_TYPE] = value; }
        }

        [ConfigurationProperty(XML)]
        public XmlElementCollection Xml {
            get {
                return this[XML] as XmlElementCollection;
            }
        }

        [ConfigurationProperty(SEARCH_TYPES)]
        public FieldSearchTypeElementCollection SearchTypes
        {
            get
            {
                return this[SEARCH_TYPES] as FieldSearchTypeElementCollection;
            }
        }

        [ConfigurationProperty(LENGTH, IsRequired = false, DefaultValue = "64")]
        public string Length {
            get {
                return this[LENGTH] as string;
            }
            set { this[LENGTH] = value; }
        }

        [ConfigurationProperty(PRECISION, IsRequired = false, DefaultValue = 18)]
        public int Precision {
            get {
                return (int)this[PRECISION];
            }
            set { this[PRECISION] = value; }
        }

        [ConfigurationProperty(SCALE, IsRequired = false, DefaultValue = 9)]
        public int Scale {
            get {
                return (int)this[SCALE];
            }
            set { this[SCALE] = value; }
        }

        [ConfigurationProperty(INPUT, IsRequired = false, DefaultValue = true)]
        public bool Input {
            get {
                return (bool)this[INPUT];
            }
            set { this[INPUT] = value; }
        }

        [ConfigurationProperty(OUTPUT, IsRequired = false, DefaultValue = true)]
        public bool Output {
            get {
                return (bool) this[OUTPUT];
            }
            set { this[OUTPUT] = value; }
        }

        [ConfigurationProperty(UNICODE, IsRequired = false, DefaultValue = true)]
        public bool Unicode {
            get {
                return (bool)this[UNICODE];
            }
            set { this[UNICODE] = value; }
        }

        [ConfigurationProperty(VARIABLE_LENGTH, IsRequired = false, DefaultValue = true)]
        public bool VariableLength {
            get {
                return (bool)this[VARIABLE_LENGTH];
            }
            set { this[VARIABLE_LENGTH] = value; }
        }

        [ConfigurationProperty(DEFAULT, IsRequired = false, DefaultValue = "")]
        public string Default {
            get {
                return (string) this[DEFAULT];
            }
            set { this[DEFAULT] = value; }
        }

        [ConfigurationProperty(TRANSFORMS)]
        public TransformElementCollection Transforms {
            get {
                return this[TRANSFORMS] as TransformElementCollection;
            }
        }

        [ConfigurationProperty(AGGREGATE, IsRequired = false, DefaultValue = "")]
        public string Aggregate {
            get {
                return (string)this[AGGREGATE];
            }
            set { this[AGGREGATE] = value; }
        }

    }
}