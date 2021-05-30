using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            return modelCollection;
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
