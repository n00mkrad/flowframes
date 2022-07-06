using Flowframes.Os;
using System.Collections.Generic;
using System.Linq;

namespace Flowframes.Data
{
    class Implementations
    {
        public static AI rifeCuda = new AI(AI.AiBackend.Pytorch, "RIFE_CUDA", "RIFE",
            "CUDA/Pytorch Implementation of RIFE", "rife-cuda", AI.InterpFactorSupport.AnyInteger, new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        public static AI rifeNcnnVs = new AI(AI.AiBackend.Ncnn, "RIFE_NCNN_VS", "RIFE (NCNN/VS)",
            "Vulkan/NCNN/VapourSynth Implementation of RIFE", "rife-ncnn-vs", AI.InterpFactorSupport.AnyFloat, new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 })
        { Piped = true };

        public static AI rifeNcnn = new AI(AI.AiBackend.Ncnn, "RIFE_NCNN", "RIFE (NCNN)",
            "Vulkan/NCNN Implementation of RIFE", "rife-ncnn", AI.InterpFactorSupport.AnyFloat, new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        public static AI flavrCuda = new AI(AI.AiBackend.Pytorch, "FLAVR_CUDA", "FLAVR",
            "Experimental Pytorch Implementation of FLAVR", "flavr-cuda", AI.InterpFactorSupport.Fixed, new int[] { 2, 4, 8 });

        public static AI dainNcnn = new AI(AI.AiBackend.Ncnn, "DAIN_NCNN", "DAIN (NCNN)",
            "Vulkan/NCNN Implementation of DAIN", "dain-ncnn", AI.InterpFactorSupport.AnyFloat, new int[] { 2, 3, 4, 5, 6, 7, 8 });

        public static AI xvfiCuda = new AI(AI.AiBackend.Pytorch, "XVFI_CUDA", "XVFI",
            "CUDA/Pytorch Implementation of XVFI", "xvfi-cuda", AI.InterpFactorSupport.AnyInteger, new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        public static List<AI> NetworksAll
        {
            get
            {
                return new List<AI> { rifeCuda, rifeNcnnVs, rifeNcnn, flavrCuda, dainNcnn, xvfiCuda };
            }
        }

        public static List<AI> NetworksAvailable
        {
            get
            {
                bool pytorchAvailable = Python.IsPytorchReady();
                return NetworksAll.Where(x => x.Backend != AI.AiBackend.Pytorch).ToList();
            }
        }

        public static AI GetAi(string aiName)
        {
            foreach (AI ai in NetworksAll)
            {
                if (ai.AiName == aiName)
                    return ai;
            }

            Logger.Log($"AI implementation lookup failed! This should not happen! Please tell the developer! (Implementations.cs)");
            return NetworksAll[0];
        }
    }
}