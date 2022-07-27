using Flowframes.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flowframes.Data
{
    public class ModelCollection
    {
        public AI Ai { get; set; } = null;
        public List<ModelInfo> Models { get; set; } = new List<ModelInfo>();

        public class ModelInfo
        {
            public AI Ai { get; set; } = null;
            public string Name { get; set; } = "";
            public string Desc { get; set; } = "";
            public string Dir { get; set; } = "";
            public bool SupportsAlpha { get; set; } = false;
            public bool IsDefault { get; set; } = false;
            private int[] _fixedFactors = null;
            public int[] FixedFactors { get { return _fixedFactors == null ? new int[0] : _fixedFactors; } set { _fixedFactors = value; } }

            public ModelInfo() { }

            public string GetUiString()
            {
                return $"{Name} - {Desc}{(SupportsAlpha ? " (Supports Transparency)" : "")}{(FixedFactors.Count() > 0 ? $" ({GetFactorsString()})" : "")}{(IsDefault ? " (Recommended)" : "")}";
            }

            public string GetFactorsString ()
            {
                return string.Join(", ", FixedFactors.Select(x => $"{x}x"));
            }
        }

        public ModelCollection(AI ai)
        {
            Ai = ai;
        }

        public ModelCollection(AI ai, string jsonContentOrPath)
        {
            Ai = ai;

            if (IoUtils.IsPathValid(jsonContentOrPath) && File.Exists(jsonContentOrPath))
                jsonContentOrPath = File.ReadAllText(jsonContentOrPath);

            Models = new List<ModelInfo>();
            dynamic data = JsonConvert.DeserializeObject(jsonContentOrPath);

            foreach (var item in data)
            {
                bool alpha = false;
                bool.TryParse((string)item.supportsAlpha, out alpha);

                bool def = false;
                bool.TryParse((string)item.isDefault, out def);

                ModelInfo modelInfo = new ModelInfo()
                {
                    Ai = ai,
                    Name = (string)item.name,
                    Desc = (string)item.desc,
                    Dir = (string)item.dir,
                    SupportsAlpha = alpha,
                    IsDefault = def,
                    FixedFactors = ((JArray)item.fixedFactors)?.Select(x => (int)x).ToArray(),
                };

                Models.Add(modelInfo);
            }
        }
    }
}
