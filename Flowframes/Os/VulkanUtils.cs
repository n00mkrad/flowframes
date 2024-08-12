using Flowframes.MiscUtils;
using System;
using System.Collections.Generic;
using System.Linq;
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

                    string name = device.GetProperties().DeviceName;
                    VkDevices.Add(new VkDevice { Id = idx, Name = name, ComputeQueueCount = compQueues });
                    Logger.Log($"[VK] Found Vulkan device: {VkDevices.Last()}", true);
                }

                // Clean up Vulkan resources
                vkInstance.Destroy();
                Logger.Log($"[VK] Vulkan device check took {sw.ElapsedMs} ms", true);
            }
            catch(Exception ex)
            {
                Logger.Log($"Vulkan Error: {ex.Message}", true);
                Logger.Log($"Vulkan initialization failed. NCNN implementations might not work, or run on the CPU.");
            }
        }

        public static int GetMaxNcnnThreads(int deviceId)
        {
            var matchingDevices = VkDevices.Where(d => d.Id == deviceId);

            if (matchingDevices.Any())
                return matchingDevices.First().ComputeQueueCount;

            return 0;
        }

        public static int GetMaxNcnnThreads(VkDevice device)
        {
            return device.ComputeQueueCount;
        }
    }
}