using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.Main;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flowframes.MiscUtils
{
    class ModelDownloadFormUtils
    {
		public static ModelDownloadForm form;
		static int taskCounter = 0;
		static int tasksToDo = 0;
		static bool canceled = false;

		public static async Task DownloadAll ()
        {
			canceled = false;
			AI[] ais = new AI[] { Networks.rifeCuda, Networks.rifeNcnn, Networks.dainNcnn, Networks.flavrCuda };
			taskCounter = 0;
			tasksToDo = GetTaskCount(ais);
			form.SetWorking(true);
			await Task.Delay(10);

			foreach(AI ai in ais)
				await DownloadForAi(ai);

			form.SetWorking(false);
			form.SetStatus($"");
		}

        public static async Task DownloadForAi(AI ai)
        {
			ModelCollection modelCollection = AiModels.GetModels(ai);

			for (int i = 0; i < modelCollection.models.Count; i++)
			{
				if (canceled)
					return;

				ModelCollection.ModelInfo modelInfo = modelCollection.models[i];
				form.SetStatus($"Downloading files for {modelInfo.ai.aiName.Replace("_", "-")}...");
				await ModelDownloader.DownloadModelFiles(ai, modelInfo.dir, false);
				taskCounter++;
				form.SetProgress((((float)taskCounter / tasksToDo) * 100f).RoundToInt());
			}
		}

		public static void Cancel ()
		{
			canceled = true;
			ModelDownloader.canceled = true;
		}

		public static int GetTaskCount (AI[] ais)
        {
			int count = 0;

			foreach(AI ai in ais)
            {
				ModelCollection modelCollection = AiModels.GetModels(ai);
				count += modelCollection.models.Count;
			}

			return count;
        }
    }
}
