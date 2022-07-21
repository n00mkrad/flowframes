using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Data
{
    public class AI
    {
        public enum AiBackend { Pytorch, Ncnn, Tensorflow, Other }
        public AiBackend Backend { get; set; } = AiBackend.Pytorch;
        public string NameInternal { get; set; } = "";
        public string NameShort { get { return NameInternal.Split(' ')[0].Split('_')[0]; } }
        public string FriendlyName { get { return $"{NameShort} ({GetFrameworkString()})"; } }
        public string Description { get { return $"{GetImplemString()} of {NameShort}{(Backend == AiBackend.Pytorch ? " (Nvidia Only!)" : "")}"; } }
        public string PkgDir { get { return NameInternal.Replace("_", "-").ToLower(); } }
        public enum InterpFactorSupport { Fixed, AnyPowerOfTwo, AnyInteger, AnyFloat }
        public InterpFactorSupport FactorSupport { get; set; } = InterpFactorSupport.Fixed;
        public int[] SupportedFactors { get; set; } = new int[0];
        public bool Piped { get; set; } = false;

        public string LogFilename { get { return PkgDir + "-log"; } }

        public AI(AiBackend backend, string aiName, InterpFactorSupport factorSupport = InterpFactorSupport.Fixed, int[] supportedFactors = null)
        {
            Backend = backend;
            NameInternal = aiName;
            SupportedFactors = supportedFactors;
            FactorSupport = factorSupport;
        }

        private string GetImplemString ()
        {
            if (Backend == AiBackend.Pytorch)
                return $"CUDA/Pytorch Implementation";

            if(Backend == AiBackend.Ncnn)
                return $"Vulkan/NCNN{(Piped ? "/VapourSynth" : "")} Implementation";

            if (Backend == AiBackend.Tensorflow)
                return $"Tensorflow Implementation";

            return "";
        }

        private string GetFrameworkString()
        {
            if (Backend == AiBackend.Pytorch)
                return $"CUDA";

            if (Backend == AiBackend.Ncnn)
                return $"NCNN{(Piped ? "/VS" : "")}";

            if (Backend == AiBackend.Tensorflow)
                return $"TF";

            return "Custom";
        }
    }
}
