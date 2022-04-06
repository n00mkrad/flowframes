using System.Collections.Generic;

namespace Flowframes.Data
{
    class Implementations
    {
        public static AI rifeCuda = new AI(AI.Backend.Pytorch, "RIFE_CUDA", "RIFE", 
            "CUDA/Pytorch Implementation of RIFE (Nvidia Only!)", "rife-cuda", AI.FactorSupport.AnyInteger, new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        public static AI rifeNcnn = new AI(AI.Backend.Ncnn, "RIFE_NCNN", "RIFE (NCNN)", 
            "Vulkan/NCNN Implementation of RIFE", "rife-ncnn", AI.FactorSupport.AnyFloat, new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        public static AI flavrCuda = new AI(AI.Backend.Pytorch, "FLAVR_CUDA", "FLAVR", 
            "Experimental Pytorch Implementation of FLAVR (Nvidia Only!)", "flavr-cuda", AI.FactorSupport.Fixed, new int[] { 2, 4, 8 });

        public static AI dainNcnn = new AI(AI.Backend.Ncnn, "DAIN_NCNN", "DAIN (NCNN)", 
            "Vulkan/NCNN Implementation of DAIN", "dain-ncnn", AI.FactorSupport.AnyInteger, new int[] { 2, 3, 4, 5, 6, 7, 8 });

        public static AI xvfiCuda = new AI(AI.Backend.Pytorch, "XVFI_CUDA", "XVFI", 
            "CUDA/Pytorch Implementation of XVFI (Nvidia Only!)", "xvfi-cuda", AI.FactorSupport.AnyInteger, new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        public static List<AI> networks = new List<AI> { rifeCuda, rifeNcnn, flavrCuda, dainNcnn, xvfiCuda };

        public static AI GetAi (string aiName)
        {
            foreach(AI ai in networks)
            {
                if (ai.aiName == aiName)
                    return ai;
            }

            return networks[0];
        }
    }
}