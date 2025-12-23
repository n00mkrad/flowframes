using Flowframes.Data;
using Flowframes.IO;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Flowframes.Main
{
    class AiModels
    {
        public static ModelCollection GetModels (AiInfo ai)
        {
            string pkgPath = Path.Combine(Paths.GetPkgPath(), ai.PkgDir);
            string modelsFile = Path.Combine(pkgPath, "models.json");

            if (!File.Exists(modelsFile))
            {
                Logger.Log($"Error: '{modelsFile}' is missing for {ai.NameInternal}, can't load AI models for this implementation!", true);
                return new ModelCollection(ai);
            }

            ModelCollection modelCollection = new ModelCollection(ai, modelsFile);

            foreach (string customModel in GetCustomModels(ai))
            {
                string name = customModel.Remove("_alpha").Remove("_custom");
                bool alpha = customModel.Contains("_alpha");
                modelCollection.Models.Add(new ModelCollection.ModelInfo() { Ai = ai, Name = name, Desc = "Custom Model", Dir = customModel, SupportsAlpha = alpha, IsDefault = false });
            }

            return modelCollection;
        }

        public static List<string> GetCustomModels(AiInfo ai)
        {
            string pkgPath = Path.Combine(Paths.GetPkgPath(), ai.PkgDir);
            List<string> custModels = new List<string>();

            foreach (DirectoryInfo dir in new DirectoryInfo(pkgPath).GetDirectories())
            {
                if (dir.Name.EndsWith("_custom") && Regex.IsMatch(dir.Name, @"^[a-zA-Z0-9_]+$"))
                    custModels.Add(dir.Name);
            }

            return custModels;
        }

        public static ModelCollection.ModelInfo GetModelByName(AiInfo ai, string modelName)
        {
            ModelCollection modelCollection = GetModels(ai);

            foreach(ModelCollection.ModelInfo model in modelCollection.Models)
            {
                if (model.Name == modelName)
                    return model;
            }

            return null;
        }

        public static ModelCollection.ModelInfo GetModelByDir(AiInfo ai, string dirName)
        {
            ModelCollection modelCollection = GetModels(ai);

            foreach (ModelCollection.ModelInfo model in modelCollection.Models)
            {
                if (model.Dir == dirName)
                    return model;
            }

            return null;
        }
    }
}
