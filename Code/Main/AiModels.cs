using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Flowframes.Main
{
    class AiModels
    {
        public static ModelCollection GetModels (AI ai)
        {
            string pkgPath = Path.Combine(Paths.GetPkgPath(), ai.pkgDir);
            string modelsFile = Path.Combine(pkgPath, "models.json");
            ModelCollection modelCollection = new ModelCollection(ai, modelsFile);

            foreach (string customModel in GetCustomModels(ai))
            {
                string name = customModel.Remove("_alpha").Remove("_custom");
                bool alpha = customModel.Contains("_alpha");
                modelCollection.models.Add(new ModelCollection.ModelInfo(ai, name, "Custom Model", customModel, alpha, false));
            }

            return modelCollection;
        }

        public static List<string> GetCustomModels(AI ai)
        {
            string pkgPath = Path.Combine(Paths.GetPkgPath(), ai.pkgDir);
            List<string> custModels = new List<string>();

            foreach (DirectoryInfo dir in new DirectoryInfo(pkgPath).GetDirectories())
            {
                if (dir.Name.EndsWith("_custom") && Regex.IsMatch(dir.Name, @"^[a-zA-Z0-9_]+$"))
                    custModels.Add(dir.Name);
            }

            return custModels;
        }

        public static ModelCollection.ModelInfo GetModelByName(AI ai, string modelName)
        {
            ModelCollection modelCollection = GetModels(ai);

            foreach(ModelCollection.ModelInfo model in modelCollection.models)
            {
                if (model.name == modelName)
                    return model;
            }

            return null;
        }

        public static ModelCollection.ModelInfo GetModelByDir(AI ai, string dirName)
        {
            ModelCollection modelCollection = GetModels(ai);

            foreach (ModelCollection.ModelInfo model in modelCollection.models)
            {
                if (model.dir == dirName)
                    return model;
            }

            return null;
        }
    }
}
