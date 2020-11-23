using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Data
{
    class Networks
    {
        public static AI rifeCuda = new AI("RIFE_CUDA", "RIFE", "Pytorch Implementation of RIFE", Packages.rifeCuda);
        public static AI dainNcnn = new AI("DAIN_NCNN", "DAIN (NCNN)", "Vulkan/NCNN Implementation of DAIN", Packages.dainNcnn);
        public static AI cainNcnn = new AI("CAIN_NCNN", "CAIN (NCNN)", "Vulkan/NCNN Implementation of CAIN", Packages.cainNcnn);

        public static List<AI> networks = new List<AI>();

        public static void Init ()
        {
            networks.Clear();
            networks.Add(rifeCuda);
            networks.Add(dainNcnn);
            networks.Add(cainNcnn);
        }
    }
}
