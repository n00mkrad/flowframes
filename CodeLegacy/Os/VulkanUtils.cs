using Flowframes.Forms.Main;
using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.Ui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using Vulkan;

namespace Flowframes.Os
{
    public class VulkanUtils
    {
        public class VkDevice
        {
            public int Id { get; set; } = -1;
            public string Name { get; set; } = "";
            public int ComputeQueueCount { get; set; } = 0;

            public override string ToString()
            {
                return $"[{Id}] {Name} [{ComputeQueueCount} Compute Queues]";
            }
        }

        public static List<VkDevice> VkDevices { get; private set; } = null;

        [HandleProcessCorruptedStateExceptions] [SecurityCritical] // To catch AccessViolationException. Thanks EA Javelin Anticheat for causing this error.
        public static void Init()
        {
            var sw = new NmkdStopwatch();
            VkDevices = new List<VkDevice>();

            try
            {
                Instance vkInstance = new Instance(new InstanceCreateInfo());
                PhysicalDevice[] physicalDevices = vkInstance.EnumeratePhysicalDevices();

                for (int idx = 0; idx < physicalDevices.Length; idx++)
                {
                    PhysicalDevice device = physicalDevices[idx];

                    // Get queue families and find the one with Compute support but no Graphics support. This is the one that gives us the correct thread count to use for NCNN etc.
                    QueueFamilyProperties[] queueFamilies = device.GetQueueFamilyProperties();
                    var validQueueFamilies = queueFamilies.Where(q => q.QueueFlags.HasFlag(QueueFlags.Compute) && !q.QueueFlags.HasFlag(QueueFlags.Graphics));
                    int compQueues = validQueueFamilies.Any() ? (int)validQueueFamilies.First().QueueCount : 0;

                    if(compQueues <= 0)
                        continue;

                    string name = device.GetProperties().DeviceName;
                    VkDevices.Add(new VkDevice { Id = idx, Name = name, ComputeQueueCount = compQueues });
                    Logger.Log($"[VK] Found Vulkan device: {VkDevices.Last()}", true);
                }

                // Clean up Vulkan resources
                vkInstance.Destroy();
                Logger.Log($"[VK] Vulkan device check completed in {sw.ElapsedMs} ms", true);
            }
            catch (AccessViolationException ave)
            {
                Form1.CloseAllSplashForms();
                UiUtils.ShowMessageBox($"Failed to initialize Vulkan (Access Violation).\n\nThis may be caused by driver issues or Anti-Cheat software (e.g. EA Javelin).\n" +
                    $"Ensure that your drivers are up to date and that no games are running.\n\nError Message:\n{ave.Message}", type: UiUtils.MessageType.Error, monospace: true);

                if(!System.Diagnostics.Debugger.IsAttached)
                    Environment.Exit(-1);
            }
            catch(Exception ex)
            {
                Logger.Log($"Vulkan Error: {ex.Message}", true);
            }

            if (VkDevices.Count == 0)
            {
                Logger.Log($"No Vulkan-capable GPUs found. NCNN implementations will run on the CPU instead and may be unstable.");
                Config.Set(Config.Key.ncnnGpus, "-1"); // -1 = CPU
                return;
            }

            // Set the device that has the most compute queues as default GPU
            var maxQueuesDevice = VkDevices.OrderByDescending(d => d.ComputeQueueCount).First();
            Config.Set(Config.Key.ncnnGpus, $"{maxQueuesDevice.Id}");
        }

        public static int GetMaxNcnnThreads(int deviceId)
        {
            var matchingDevices = VkDevices.Where(d => d.Id == deviceId);

            if (matchingDevices.Any())
                return matchingDevices.First().ComputeQueueCount;

            return 0;
        }
    }
}