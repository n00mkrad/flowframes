using Flowframes.Os;
using System.Collections.Generic;
using System.Linq;

namespace Flowframes.Data
{
    public class Implementations
    {
        public enum Ai
        {
            RifeCuda,
            RifeNcnnVs,
            RifeNcnn,
            FlavrCuda,
            DainNcnn,
            XvfiCuda,
            IfrnetNcnn,
        }

        public static AiInfo rifeCuda = new AiInfo()
        {
            Backend = AiInfo.AiBackend.Pytorch,
            NameInternal = "RIFE_CUDA",
            NameLong = "Real-Time Intermediate Flow Estimation",
            FactorSupport = AiInfo.InterpFactorSupport.AnyInteger,
            SupportedFactors = Enumerable.Range(2, 15).ToArray(),  // 2 to 16
        };

        public static AiInfo rifeNcnn = new AiInfo()
        {
            Backend = AiInfo.AiBackend.Ncnn,
            NameInternal = "RIFE_NCNN",
            NameLong = "Real-Time Intermediate Flow Estimation",
            FactorSupport = AiInfo.InterpFactorSupport.AnyFloat,
            SupportedFactors = Enumerable.Range(2, 15).ToArray(),  // 2 to 16
        };

        public static AiInfo rifeNcnnVs = new AiInfo()
        {
            Backend = AiInfo.AiBackend.Ncnn,
            NameInternal = "RIFE_NCNN_VS",
            NameLong = "Real-Time Intermediate Flow Estimation",
            FactorSupport = AiInfo.InterpFactorSupport.AnyFloat,
            SupportedFactors = Enumerable.Range(2, 15).ToArray(),  // 2 to 16
            Piped = true
        };

        public static AiInfo flavrCuda = new AiInfo()
        {
            Backend = AiInfo.AiBackend.Pytorch,
            NameInternal = "FLAVR_CUDA",
            NameLong = "Flow-Agnostic Video Representations",
            FactorSupport = AiInfo.InterpFactorSupport.Fixed,
            SupportedFactors = new int[] { 2, 4, 8 },
        };

        public static AiInfo dainNcnn = new AiInfo()
        {
            Backend = AiInfo.AiBackend.Ncnn,
            NameInternal = "DAIN_NCNN",
            NameLong = "Depth-Aware Video Frame Interpolation",
            FactorSupport = AiInfo.InterpFactorSupport.AnyFloat,
            SupportedFactors = Enumerable.Range(2, 7).ToArray(),  // 2 to 8
        };

        public static AiInfo xvfiCuda = new AiInfo()
        {
            Backend = AiInfo.AiBackend.Pytorch,
            NameInternal = "XVFI_CUDA",
            NameLong = "eXtreme Video Frame Interpolation",
            FactorSupport = AiInfo.InterpFactorSupport.AnyInteger,
            SupportedFactors = Enumerable.Range(2, 9).ToArray(),  // 2 to 10
        };

        public static AiInfo ifrnetNcnn = new AiInfo()
        {
            Backend = AiInfo.AiBackend.Ncnn,
            NameInternal = "IFRNet_NCNN",
            NameLong = "Intermediate Feature Refine Network",
            FactorSupport = AiInfo.InterpFactorSupport.Fixed,
            SupportedFactors = new int[] { 2 },
        };

        // Lookup table
        private static readonly Dictionary<Ai, AiInfo> AiLookup = new Dictionary<Ai, AiInfo>
        {
            { Ai.RifeCuda, rifeCuda },
            { Ai.RifeNcnnVs, rifeNcnnVs },
            { Ai.RifeNcnn, rifeNcnn },
            { Ai.FlavrCuda, flavrCuda },
            { Ai.DainNcnn, dainNcnn },
            { Ai.XvfiCuda, xvfiCuda },
            /* { Ai.IfrnetNcnn, ifrnetNcnn }, */
        };

        public static List<AiInfo> NetworksAll => AiLookup.Values.ToList();

        public static List<AiInfo> NetworksAvailable
        {
            get
            {
                bool pytorchAvailable = !Python.DisablePython && Python.IsPytorchReady();

                if (pytorchAvailable)
                    return NetworksAll;

                return NetworksAll.Where(x => x.Backend != AiInfo.AiBackend.Pytorch).ToList();
            }
        }

        public static AiInfo GetAi(Ai ai)
        {
            if (AiLookup.TryGetValue(ai, out AiInfo aiObj))
                return aiObj;

            Logger.Log($"AI implementation lookup failed for '{ai}'! This should not happen! Please tell the developer!");
            return NetworksAll[0];
        }
    }
}
