using Flowframes.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Flowframes.Data
{
    public class ModelCollection
    {
        public AI ai;
        public List<ModelInfo> models = new List<ModelInfo>();

        public class ModelInfo
        {
            public AI ai;
            public string name;
            public string desc;
            public string dir;
            public bool supportsAlpha;
            public bool isDefault;

            public ModelInfo(AI ai, string name, string desc, string dir, bool supportsAlpha, bool isDefault)
            {
                this.ai = ai;
                this.name = name;
                this.desc = desc;
                this.dir = dir;
                this.supportsAlpha = supportsAlpha;
                this.isDefault = isDefault;
            }

            public string GetUiString()
            {
                return $"{name} - {desc}{(supportsAlpha ? " (Supports Transparency)" : "")}{(isDefault ? " (Recommended)" : "")}";
            }

            public override string ToString()
            {
                return $"{name} - {desc} ({dir}){(supportsAlpha ? " (Supports Transparency)" : "")}{(isDefault ? " (Recommended)" : "")}";
            }
        }

        public ModelCollection(AI ai)
        {
            this.ai = ai;
        }

        public ModelCollection(AI ai, string jsonContentOrPath)
        {
            this.ai = ai;

            if (IoUtils.IsPathValid(jsonContentOrPath) && File.Exists(jsonContentOrPath))
                jsonContentOrPath = File.ReadAllText(jsonContentOrPath);

            models = new List<ModelInfo>();
            dynamic data = JsonConvert.DeserializeObject(jsonContentOrPath);

            foreach (var item in data)
            {
                bool alpha = false;
                bool.TryParse((string)item.supportsAlpha, out alpha);

                bool def = false;
                bool.TryParse((string)item.isDefault, out def);

                models.Add(new ModelInfo(ai, (string)item.name, (string)item.desc, (string)item.dir, alpha, def));
            }
        }
    }
}
