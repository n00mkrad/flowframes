using Flowframes.IO;
using System;
using System.Collections.Generic;

namespace Flowframes.Data
{
    class Implementations
    {
        public static AI rifeCuda = new AI("RIFE_CUDA", "RIFE", "CUDA/Pytorch Implementation of RIFE (Nvidia Only!)", "rife-cuda", new int[] { 2, 4, 8, 16 });
        public static AI rifeNcnn = new AI("RIFE_NCNN", "RIFE (NCNN)", "Vulkan/NCNN Implementation of RIFE", "rife-ncnn", new int[] { 2, 4, 8 }, true);
        public static AI flavrCuda = new AI("FLAVR_CUDA", "FLAVR", "Experimental Pytorch Implementation of FLAVR (Nvidia Only!)", "flavr-cuda", new int[] { 2, 4, 8 });
        public static AI dainNcnn = new AI("DAIN_NCNN", "DAIN (NCNN)", "Vulkan/NCNN Implementation of DAIN", "dain-ncnn", new int[] { 2, 3, 4, 5, 6, 7, 8 });
        public static AI xvfiCuda = new AI("XVFI_CUDA", "XVFI", "CUDA/Pytorch Implementation of XVFI (Nvidia Only!)", "xvfi-cuda", new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        public static List<AI> networks = new List<AI>();

        public static void Init ()
        {
            networks.Clear();
            networks.Add(rifeCuda);
            networks.Add(rifeNcnn);
            networks.Add(flavrCuda);
            networks.Add(dainNcnn);
            networks.Add(xvfiCuda);
        }

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