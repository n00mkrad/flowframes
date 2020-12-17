using NvAPIWrapper;
using NvAPIWrapper.GPU;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.OS
{
    class NvApi
    {
        static PhysicalGPU gpu;

        public static async void Init()
        {
            try
            {
                NVIDIA.Initialize();
                PhysicalGPU[] gpus = PhysicalGPU.GetPhysicalGPUs();
                if (gpus.Length == 0)
                    return;
                gpu = gpus[0];

                Logger.Log($"Initialized NvApi. GPU: {gpu.FullName}");
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to initialize NvApi: {e.Message}\nIgnore this if you don't have an Nvidia GPU.");
            }
        }

        public static float GetVramGb ()
        {
            try
            {
                return (gpu.MemoryInformation.AvailableDedicatedVideoMemoryInkB / 1000f / 1024f);
            }
            catch (Exception e)
            {
                return 0f;
            }
        }

        public static float GetFreeVramGb()
        {
            try
            {
                return (gpu.MemoryInformation.CurrentAvailableDedicatedVideoMemoryInkB / 1000f / 1024f);
            }
            catch (Exception e)
            {
                return 0f;
            }
        }

        public static string GetGpuName()
        {
            try
            {
                NVIDIA.Initialize();
                PhysicalGPU[] gpus = PhysicalGPU.GetPhysicalGPUs();
                if (gpus.Length == 0)
                    return "";

                return gpus[0].FullName;
            }
            catch
            {
                return "";
            }
        }
    }
}
