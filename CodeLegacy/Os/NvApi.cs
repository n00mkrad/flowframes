using Flowframes.MiscUtils;
using NvAPIWrapper;
using NvAPIWrapper.GPU;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Flowframes.Os
{
    class NvApi
    {
        public enum Architecture { Undetected, Fermi, Kepler, Maxwell, Pascal, Turing, Ampere, Ada, Blackwell };
        public static List<PhysicalGPU> NvGpus = new List<PhysicalGPU>();
        public static Dictionary<PhysicalGPU, float> GpuVram = new Dictionary<PhysicalGPU, float>();
        public static PhysicalGPU GpuWithMostVram => GpuVram.OrderByDescending(kv => kv.Value).First().Key;

        public static void Init()
        {
            try
            {
                var sw = new NmkdStopwatch();
                NVIDIA.Initialize();
                PhysicalGPU[] gpus = PhysicalGPU.GetPhysicalGPUs();

                if (gpus.Length == 0)
                    return;

                foreach (PhysicalGPU gpu in gpus)
                {
                    float vramGb = gpu.GetVramGb();
                    vramGb = (float)(Math.Round(vramGb * 2, MidpointRounding.AwayFromZero) / 2); // Round to nearest 0.5 GB
                    Logger.Log($"[NvAPI] Nvidia GPU: {gpu.FullName}, {vramGb.ToString("0.#")} GB, {GetArch(gpu)} Architecture", true);
                    GpuVram[gpu] = vramGb;
                }

                NvGpus = gpus.OrderByDescending(g => GpuVram[g]).ThenBy(g => g.FullName).ToList();
                string mostVramGpu = gpus.Length > 1 ? $" Most VRAM: {GpuWithMostVram.FullName} ({GpuVram[GpuWithMostVram].ToString("0.#")} GB)" : "";
                Logger.Log($"[NvAPI] Initialized Nvidia API in {sw.ElapsedMs} ms - GPU{(gpus.Length > 1 ? "s" : "")}: {string.Join(", ", gpus.Select(g => g.FullName))}{mostVramGpu}", true);
            }
            catch (Exception e)
            {
                Logger.Log("No Nvidia GPU(s) detected. You will not be able to use CUDA implementations on GPU.");
                Logger.Log($"Failed to initialize NvApi: {e.Message}\nIgnore this if you don't have an Nvidia GPU.", true);
            }
        }

        public static float GetVramGb (int gpu = 0)
        {
            try
            {
                return (NvGpus[gpu].MemoryInformation.AvailableDedicatedVideoMemoryInkB / 1000f / 1024f);
            }
            catch
            {
                return 0f;
            }
        }

        public static float GetFreeVramGb(int gpu = 0)
        {
            try
            {
                return (NvGpus[gpu].MemoryInformation.CurrentAvailableDedicatedVideoMemoryInkB / 1000f / 1024f);
            }
            catch
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

        public static bool HasAmpereOrNewer()
        {
            foreach (PhysicalGPU gpu in NvGpus)
            {
                Architecture arch = GetArch(gpu);

                if (arch == Architecture.Ampere || arch == Architecture.Undetected)
                    return true;
            }

            return false;
        }

        public static Architecture GetArch(PhysicalGPU gpu)
        {
            string arch = gpu.ArchitectInformation.ShortName.Trim();

            if (arch.StartsWith("GF")) return Architecture.Fermi;
            if (arch.StartsWith("GK")) return Architecture.Kepler;
            if (arch.StartsWith("GM")) return Architecture.Maxwell;
            if (arch.StartsWith("GP")) return Architecture.Pascal;
            if (arch.StartsWith("TU")) return Architecture.Turing;
            if (arch.StartsWith("GA")) return Architecture.Ampere;
            if (arch.StartsWith("AD")) return Architecture.Ada;
            if (arch.StartsWith("GB")) return Architecture.Blackwell;

            return Architecture.Undetected;
        }

        public static bool HasTensorCores (int gpu = 0)
        {
            try
            {
                if (NvGpus == null)
                    Init();

                if (NvGpus == null)
                    return false;

                Architecture arch = GetArch(NvGpus[gpu]);
                return arch >= Architecture.Turing;
            }
            catch (Exception e)
            {
                Logger.Log($"HasTensorCores({gpu}) Error: {e.Message}", true);
                return false;
            }
        }
    }
}
