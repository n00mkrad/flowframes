using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.Main;
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

		public static async Task DownloadModels (bool rifeC, bool rifeN, bool dainN, bool flavrC, bool xvfiC)
        {
			form.SetDownloadBtnEnabled(true);
			canceled = false;
			List<AiInfo> ais = new List<AiInfo>();

			if (rifeC) ais.Add(Implementations.rifeCuda);
			if (rifeN) ais.Add(Implementations.rifeNcnn);
			if (dainN) ais.Add(Implementations.dainNcnn);
			if (flavrC) ais.Add(Implementations.flavrCuda);
			if (xvfiC) ais.Add(Implementations.xvfiCuda);

			if (ais.Count < 1)
				return;

			taskCounter = 1;
			tasksToDo = GetTaskCount(ais) + 1;
			form.SetWorking(true);
			await Task.Delay(10);
			UpdateProgressBar();

			foreach (AiInfo ai in ais)
				await DownloadForAi(ai);

			form.SetWorking(false);
			form.SetStatus($"Done.");
			form.SetDownloadBtnEnabled(false);

			if (Cli.CloseMdlDownloaderWhenDone)
			{
				form.Close();
            }
		}

        public static async Task DownloadForAi(AiInfo ai)
        {
			ModelCollection modelCollection = AiModels.GetModels(ai);

			for (int i = 0; i < modelCollection.Models.Count; i++)
			{
				if (canceled)
					return;

				ModelCollection.ModelInfo modelInfo = modelCollection.Models[i];
				form.SetStatus($"Downloading files for {modelInfo.Ai.NameInternal.Replace("_", "-")}...");
				await ModelDownloader.DownloadModelFiles(ai, modelInfo.Dir, false);
				taskCounter++;
				UpdateProgressBar();
			}
		}

		static void UpdateProgressBar ()
        {
			form.SetProgress((((float)taskCounter / tasksToDo) * 100f).RoundToInt());
		}

		public static void Cancel ()
		{
			canceled = true;
			ModelDownloader.canceled = true;
		}

		public static int GetTaskCount (List<AiInfo> ais)
        {
			int count = 0;

			foreach(AiInfo ai in ais)
            {
				ModelCollection modelCollection = AiModels.GetModels(ai);
				count += modelCollection.Models.Count;
			}

			return count;
        }
    }
}
