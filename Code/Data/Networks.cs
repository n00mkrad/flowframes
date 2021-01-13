using Flowframes.IO;
using System;
using System.Collections.Generic;

namespace Flowframes.Data
{
    class Networks
    {
        public static AI rifeCuda = new AI("RIFE_CUDA", "RIFE", "CUDA/Pytorch Implementation of RIFE", Packages.rifeCuda, 2, true);
        public static AI rifeNcnn = new AI("RIFE_NCNN", "RIFE (NCNN)", "Vulkan/NCNN Implementation of RIFE", Packages.rifeNcnn, 1, false);

        public static List<AI> networks = new List<AI>();

        public static void Init ()
        {
            networks.Clear();
            networks.Add(rifeCuda);
            networks.Add(rifeNcnn);
        }
    }
}
