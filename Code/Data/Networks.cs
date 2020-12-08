using Flowframes.IO;
using System;
using System.Collections.Generic;

namespace Flowframes.Data
{
    class Networks
    {
        public static AI rifeCuda = new AI("RIFE_CUDA", "RIFE", "CUDA/Pytorch Implementation of RIFE", Packages.rifeCuda, 2, false, true);
        public static AI rifeNcnn = new AI("RIFE_NCNN", "RIFE (NCNN)", "Vulkan/NCNN Implementation of RIFE", Packages.rifeNcnn, 1, false, false);
        public static AI dainNcnn = new AI("DAIN_NCNN", "DAIN (NCNN)", "Vulkan/NCNN Implementation of DAIN", Packages.dainNcnn, 0, true, true);
        public static AI cainNcnn = new AI("CAIN_NCNN", "CAIN (NCNN)", "Vulkan/NCNN Implementation of CAIN", Packages.cainNcnn, 0, true, false);

        public static List<AI> networks = new List<AI>();

        public static void Init ()
        {
            networks.Clear();
            networks.Add(rifeCuda);
            networks.Add(rifeNcnn);
            networks.Add(dainNcnn);
            //networks.Add(cainNcnn);
        }
    }
}
