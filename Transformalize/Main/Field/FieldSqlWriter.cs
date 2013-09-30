#region License

// /*
// Transformalize - Replicate, Transform, and Denormalize Your Data...
// Copyright (C) 2013 Dale Newman
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// */

#endregion

using System.Collections.Generic;
using System.Linq;
using Transformalize.Main.Providers;

namespace Transformalize.Main {
    public class FieldSqlWriter {
        private const string BATCH_ID = "TflBatchId";
        private const string SURROGATE_KEY = "TflKey";
        private Dictionary<string, Field> _original;
        private SortedDictionary<string, string> _output;

        public FieldSqlWriter() {
            StartEmpty();
        }

        public FieldSqlWriter(Field field) {
            StartWithField(field);
        }

        public FieldSqlWriter(IEnumerable<Field> fields) {
            StartWithFields(fields);
        }

        public FieldSqlWriter(IDictionary<string, Field> fields) {
            StartWithDictionary(fields);
        }

        public FieldSqlWriter(params IDictionary<string, Field>[] fields) {
            StartWithDictionaries(fields);
        }

        public FieldSqlWriter(params IEnumerable<Field>[] fields) {
            StartWithEnumerable(fields);
        }

        public FieldSqlWriter(params Fields[] fields) {
            StartWithIFields(fields);
        }

        public FieldSqlWriter Reload(Field field) {
            StartWithField(field);
            return this;
        }

        public FieldSqlWriter Reload(IEnumerable<Field> fields) {
            StartWithFields(fields);
            return this;
        }

        public FieldSqlWriter Reload(IDictionary<string, Field> fields) {
            StartWithDictionary(fields);
            return this;
        }

        public FieldSqlWriter Reload(params IDictionary<string, Field>[] fields) {
            if (fields.Length == 0)
                StartWithDictionary(_original);
            else
                StartWithDictionaries(fields);
            return this;
        }

        private void StartWithField(Field field) {
            _original = new Dictionary<string, Field>
                            {
                                {field.Alias, field}
                            };
            _output = new SortedDictionary<string, string>
                          {
                              {field.Alias, string.Empty}
                          };
        }

        private void StartEmpty() {
            _original = new Dictionary<string, Field>();
            _output = new SortedDictionary<string, string>();
        }

        private void StartWithFields(IEnumerable<Field> fields) {
            var expanded = fields.ToArray();
            _original = expanded.ToDictionary(f => f.Alias, f => f);
            _output = new SortedDictionary<string, string>(expanded.ToDictionary(f => f.Alias, f => string.Empty));
        }

        private void StartWithDictionary(IDictionary<string, Field> fields) {
            _original = fields.ToDictionary(f => f.Key, f => f.Value);
            _output = new SortedDictionary<string, string>(fields.ToDictionary(f => f.Key, f => string.Empty));
        }

        private void StartWithDictionaries(params IDictionary<string, Field>[] fields) {
            _original = new Dictionary<string, Field>();
            foreach (var dict in fields) {
                foreach (var pair in dict) {
                    _original[pair.Key] = pair.Value;
                }
            }
            _output = new SortedDictionary<string, string>(_original.ToDictionary(f => f.Key, f => string.Empty));
        }

        private void StartWithIFields(params Fields[] fields) {
            _original = new Dictionary<string, Field>();
            foreach (var dict in fields) {
                if (dict != null)
                    foreach (var pair in dict)
                        _original[pair.Key] = pair.Value;
            }
            _output = new SortedDictionary<string, string>(_original.ToDictionary(f => f.Key, f => string.Empty));
        }

        private void StartWithEnumerable(params IEnumerable<Field>[] fields) {
            _original = new Dictionary<string, Field>();
            foreach (var field in fields) {
                foreach (var f in field) {
                    _original[f.Alias] = f;
                }
            }
            _output = new SortedDictionary<string, string>(_original.ToDictionary(f => f.Key, f => string.Empty));
        }

        public string Write(string separator = ", ", bool flush = true) {
            var result = string.Join(separator, _output.Select(kv => kv.Value));
            if (flush)
                Flush();
            return result;
        }

        public IEnumerable<string> Values(bool flush = true) {
            return _output.Select(kv => kv.Value);
        }

        public IEnumerable<string> Keys(bool flush = true) {
            return _output.Select(kv => kv.Key);
        }

        private void Flush() {
            _output = new SortedDictionary<string, string>(_original.ToDictionary(f => f.Key, f => string.Empty));
        }

        private IEnumerable<string> CopyOutputKeys() {
            var keys = new string[_output.Keys.Count];
            _output.Keys.CopyTo(keys, 0);
            return keys;
        }

        private static string SafeColumn(string name, AbstractProvider provider) {
            return string.Concat(provider.L, name, provider.R);
        }

        public FieldSqlWriter Name(AbstractProvider provider) {
            foreach (var key in CopyOutputKeys()) {
                _output[key] = SafeColumn(_original[key].Name, provider);
            }
            return this;
        }

        public FieldSqlWriter Alias(AbstractProvider provider) {
            foreach (var key in CopyOutputKeys()) {
                _output[key] = SafeColumn(_original[key].Alias, provider);
            }
            return this;
        }

        public FieldSqlWriter IsNull() {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                var d = field.Default ?? new ConversionFactory().Convert(string.Empty, field.SimpleType);

                if (field.SimpleType == "byte[]" || field.SimpleType == "rowversion")
                    d = "0x";

                if (field.SimpleType.StartsWith("bool"))
                    d = (bool)d ? 1 : 0;

                _output[key] = string.Concat("ISNULL(", _output[key], ", ", field.Quote, d, field.Quote, ")");
            }
            return this;
        }

        public FieldSqlWriter ToAlias(AbstractProvider provider, bool ifNecessary = false) {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                if (ifNecessary) {
                    if (field.Alias != field.Name) {
                        //_output[key] = string.Concat(provider.L, field.Alias, provider.R, " = ", _output[key]);
                        _output[key] = string.Concat(_output[key], " AS ", provider.L, field.Alias, provider.R);
                    }
                } else {
                    //_output[key] = string.Concat(provider.L, field.Alias, provider.R, " = ", _output[key]);
                    _output[key] = string.Concat(_output[key], " AS ", provider.L, field.Alias, provider.R);
                }
            }
            return this;
        }

        public FieldSqlWriter AsAlias(AbstractProvider provider) {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                if (field.Alias != field.Name) {
                    _output[key] = string.Concat(_output[key], " AS ", provider.L, _original[key].Alias, provider.R);
                }
            }
            return this;
        }

        public FieldSqlWriter DataType() {
            foreach (var key in CopyOutputKeys()) {
                _output[key] = string.Concat(_output[key], " ", _original[key].SqlDataType);
            }
            return this;
        }

        public FieldSqlWriter Prepend(string prefix) {
            foreach (var key in CopyOutputKeys()) {
                _output[key] = string.Concat(prefix, _output[key]);
            }
            return this;
        }

        public FieldSqlWriter PrependEntityOutput(AbstractProvider provider, string entityName = null) {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                var entity = Common.EntityOutputName(field.Entity, field.Process);
                var table = SafeColumn(entityName ?? entity, provider);
                _output[key] = string.Concat(table, ".", _output[key]);
            }
            return this;
        }

        public FieldSqlWriter Append(string suffix) {
            foreach (var key in CopyOutputKeys()) {
                _output[key] = string.Concat(_output[key], suffix);
            }
            return this;
        }

        public FieldSqlWriter Asc() {
            Append(" ASC");
            return this;
        }

        public FieldSqlWriter Desc() {
            Append(" DESC");
            return this;
        }

        public FieldSqlWriter AppendIf(string suffix, params FieldType[] fieldTypes) {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                if (fieldTypes.Any(ft => ft.HasFlag(field.FieldType)))
                    _output[key] = string.Concat(_output[key], suffix);
            }
            return this;
        }

        public FieldSqlWriter AppendIfNot(string suffix, FieldType fieldType) {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                if (!field.FieldType.HasFlag(fieldType))
                    _output[key] = string.Concat(_output[key], suffix);
            }
            return this;
        }

        /// <summary>
        ///     Presents the field for a select.
        /// </summary>
        /// <returns>field's name</returns>
        public FieldSqlWriter Select(AbstractProvider provider) {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                _output[key] = SafeColumn(field.Name, provider);
            }
            return this;
        }

        public FieldSqlWriter Output(bool answer = true) {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                if (field.Output != answer)
                    _output.Remove(key);
            }
            return this;
        }

        public FieldSqlWriter Input(bool answer = true) {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                if (field.Input != answer)
                    _output.Remove(key);
            }
            return this;
        }

        public FieldSqlWriter Aggregate() {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                if (field.Aggregate == string.Empty || field.FieldType.HasFlag(Main.FieldType.PrimaryKey) ||
                    field.FieldType.HasFlag(Main.FieldType.MasterKey))
                    _output.Remove(key);
            }
            return this;
        }

        public FieldSqlWriter HasTransform() {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                if (!field.HasTransforms)
                    _output.Remove(key);
            }
            return this;
        }

        public FieldSqlWriter HasDefault() {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                if (field.Default == null)
                    _output.Remove(key);
            }
            return this;
        }

        public FieldSqlWriter FieldType(params FieldType[] answers) {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                if (!answers.Any(a => field.FieldType.HasFlag(a)))
                    _output.Remove(key);
            }
            return this;
        }

        public FieldSqlWriter Set(string left, string right) {
            foreach (var key in CopyOutputKeys()) {
                var name = _output[key];
                _output[key] = string.Concat(left, ".", name, " = ", right, ".", name);
            }
            return this;
        }

        public FieldSqlWriter SetParam() {
            foreach (var key in CopyOutputKeys()) {
                var name = _output[key];
                _output[key] = string.Concat(name, " = @", name.Trim("[]".ToCharArray()));
            }
            return this;
        }

        public FieldSqlWriter AddBatchId(bool forCreate = true) {
            _original[BATCH_ID] = new Field("System.Int32", "8", Main.FieldType.Field, true, "0") {
                Alias = BATCH_ID,
                NotNull = forCreate
            };

            _output[BATCH_ID] = string.Empty;
            return this;
        }

        public FieldSqlWriter AddSurrogateKey(bool forCreate = true) {
            if (forCreate)
                _original[SURROGATE_KEY] = new Field("System.Int32", "8", Main.FieldType.Field, true, "0") {
                    Alias =
                        SURROGATE_KEY,
                    NotNull
                        =
                        true,
                    Clustered
                        =
                        true,
                    Identity
                        =
                        true
                };
            else
                _original[SURROGATE_KEY] = new Field("System.Int32", "8", Main.FieldType.Field, true, "0") {
                    Alias =
                        SURROGATE_KEY
                };

            _output[SURROGATE_KEY] = string.Empty;
            return this;
        }

        public override string ToString() {
            return Write();
        }

        public Fields Context() {
            var results = new Fields();
            foreach (var pair in _output) {
                results[pair.Key] = _original[pair.Key];
            }
            return results;
        }

        public Field[] ToArray() {
            var results = new Fields();
            foreach (var pair in _output) {
                results[pair.Key] = _original[pair.Key];
            }
            return results.ToEnumerable().ToArray();
        }

        public FieldSqlWriter Remove(string @alias) {
            foreach (var key in CopyOutputKeys()) {
                var field = _original[key];
                if (field.Alias == @alias)
                    _output.Remove(key);
            }
            return this;
        }
    }
}