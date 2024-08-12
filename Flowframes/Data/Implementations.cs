using Flowframes.Os;
using System.Collections.Generic;
using System.Linq;

namespace Flowframes.Data
{
    class Implementations
    {
        public static bool DisablePython = false;

        public static AI rifeCuda = new AI()
        {
            Backend = AI.AiBackend.Pytorch,
            NameInternal = "RIFE_CUDA",
            NameLong = "Real-Time Intermediate Flow Estimation",
            FactorSupport = AI.InterpFactorSupport.AnyInteger,
            SupportedFactors = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 }
        };

        public static AI rifeNcnn = new AI()
        {
            Backend = AI.AiBackend.Ncnn,
            NameInternal = "RIFE_NCNN",
            NameLong = "Real-Time Intermediate Flow Estimation",
            FactorSupport = AI.InterpFactorSupport.AnyFloat,
            SupportedFactors = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 },
        };

        public static AI rifeNcnnVs = new AI()
        {
            Backend = AI.AiBackend.Ncnn,
            NameInternal = "RIFE_NCNN_VS",
            NameLong = "Real-Time Intermediate Flow Estimation",
            FactorSupport = AI.InterpFactorSupport.AnyFloat,
            SupportedFactors = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 },
            Piped = true
        };

        public static AI flavrCuda = new AI()
        {
            Backend = AI.AiBackend.Pytorch,
            NameInternal = "FLAVR_CUDA",
            NameLong = "Flow-Agnostic Video Representations",
            FactorSupport = AI.InterpFactorSupport.Fixed,
            SupportedFactors = new int[] { 2, 4, 8 },
        };

        public static AI dainNcnn = new AI()
        {
            Backend = AI.AiBackend.Ncnn,
            NameInternal = "DAIN_NCNN",
            NameLong = "Depth-Aware Video Frame Interpolation",
            FactorSupport = AI.InterpFactorSupport.AnyFloat,
            SupportedFactors = new int[] { 2, 3, 4, 5, 6, 7, 8 },
        };

        public static AI xvfiCuda = new AI()
        {
            Backend = AI.AiBackend.Pytorch,
            NameInternal = "XVFI_CUDA",
            NameLong = "eXtreme Video Frame Interpolation",
            FactorSupport = AI.InterpFactorSupport.AnyInteger,
            SupportedFactors = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 },
        };

        public static AI ifrnetNcnn = new AI()
        {
            Backend = AI.AiBackend.Ncnn,
            NameInternal = "IFRNet_NCNN",
            NameLong = "Intermediate Feature Refine Network",
            FactorSupport = AI.InterpFactorSupport.Fixed,
            SupportedFactors = new int[] { 2 },
        };

        public static List<AI> NetworksAll
        {
            get
            {
                return new List<AI> { rifeNcnnVs, rifeNcnn, rifeCuda, flavrCuda, dainNcnn, xvfiCuda, /* ifrnetNcnn */ };
            }
        }

        public static List<AI> NetworksAvailable
        {
            get
            {
                bool pytorchAvailable = !DisablePython && Python.IsPytorchReady();

                if (pytorchAvailable)
                    return NetworksAll;
                
                return NetworksAll.Where(x => x.Backend != AI.AiBackend.Pytorch).ToList();
            }
        }

        public static AI GetAi(string aiName)
        {
            foreach (AI ai in NetworksAll)
            {
                if (ai.NameInternal == aiName)
                    return ai;
            }

            Logger.Log($"AI implementation lookup failed! This should not happen! Please tell the developer! (Implementations.cs)");
            return NetworksAll[0];
        }
    }
}