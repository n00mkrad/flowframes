using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.IO
{
    class Packages
    {
        public static FlowPackage dainNcnn = new FlowPackage("DAIN-NCNN (AMD/Nvidia)", "dain-ncnn.7z", 40, "NCNN/Vulkan implementation of DAIN. Very slow and VRAM-hungry.");
        public static FlowPackage cainNcnn = new FlowPackage("CAIN-NCNN (AMD/Nvidia)", "cain-ncnn.7z", 75, "NCNN/Vulkan implementation of CAIN. About 8x faster than DAIN and very lightweight on VRAM.");
        public static FlowPackage rifeCuda = new FlowPackage("RIFE (Nvidia)", "rife-cuda.7z", 50, "Pytorch implementation of RIFE. Very fast (~2x CAIN, >16x DAIN) and not too VRAM-heavy.");
        public static FlowPackage python = new FlowPackage("Python Runtime", "py.7z", 640, "Embedded Python runtime including Pytorch and all other dependencies. Install this if you don't have system Python.");
        public static FlowPackage audioVideo = new FlowPackage("Audio/Video Tools (Required)", "av.7z", 10, "Utilities for extracting frames, analysing videos, encoding videos and GIFs.");
        public static FlowPackage licenses = new FlowPackage("Licenses (Required)", "licenses.7z", 1, "License files for redistributed software.");
    }
}
