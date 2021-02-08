using Flowframes.IO;
using System;
using System.Collections.Generic;

namespace Flowframes.Data
{
    class Networks
    {
        public static AI rifeCuda = new AI("RIFE_CUDA", "RIFE", "CUDA/Pytorch Implementation of RIFE", Packages.rifeCuda, true);
        public static AI rifeNcnn = new AI("RIFE_NCNN", "RIFE (NCNN)", "Vulkan/NCNN Implementation of RIFE", Packages.rifeNcnn, false);
        public static AI dainNcnn = new AI("DAIN_NCNN", "DAIN (NCNN)", "Vulkan/NCNN Implementation of DAIN", Packages.dainNcnn, true);

        public static List<AI> networks = new List<AI>();

        public static void Init ()
        {
            networks.Clear();
            networks.Add(rifeCuda);
            networks.Add(rifeNcnn);
            networks.Add(dainNcnn);
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