using System.Collections.Generic;
using Transformalize.Configuration;
using Transformalize.Main;

namespace Transformalize.Orchard.Models {
    public class TransformalizeRequest {

        public Options Options { get; set; }
        public ConfigurationPart Part { get; set; }
        public string Configuration { get; set; }
        public Dictionary<string, string> Query { get; set; }
        public TflRoot Root { get; set; }

        public TransformalizeRequest(
            ConfigurationPart part,
            Dictionary<string, string> query,
            string modifiedConfiguration,
            TflRoot root = null) {
                Part = part;
                Configuration = modifiedConfiguration ?? part.Configuration;
                Query = query ?? new Dictionary<string, string>();
                Options = Query.ContainsKey("Mode") ? new Options { Mode = Query["Mode"] } : new Options();
                Root = root ?? new TflRoot(modifiedConfiguration ?? part.Configuration, query);
        }
    }


}