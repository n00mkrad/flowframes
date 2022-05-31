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
        public string AiName { get; set; } = "";
        public string AiNameShort { get; set; } = "";
        public string FriendlyName { get; set; } = "";
        public string Description { get; set; } = "";
        public string PkgDir { get; set; } = "";
        public enum InterpFactorSupport { Fixed, AnyPowerOfTwo, AnyInteger, AnyFloat }
        public InterpFactorSupport FactorSupport { get; set; } = InterpFactorSupport.Fixed;
        public int[] SupportedFactors { get; set; } = new int[0];
        public bool Piped { get; set; } = false;

        public AI(AiBackend backend, string aiName, string friendlyName, string desc, string pkgDir, InterpFactorSupport factorSupport = InterpFactorSupport.Fixed, int[] supportedFactors = null)
        {
            Backend = backend;
            AiName = aiName;
            AiNameShort = aiName.Split(' ')[0].Split('_')[0];
            FriendlyName = friendlyName;
            Description = desc;
            PkgDir = pkgDir;
            SupportedFactors = supportedFactors;
            FactorSupport = factorSupport;
        }
    }
}
