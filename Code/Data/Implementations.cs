using Flowframes.Os;
using System.Collections.Generic;
using System.Linq;

namespace Flowframes.Data
{
    class Implementations
    {
        public static AI rifeCuda = new AI(AI.AiBackend.Pytorch, "RIFE_CUDA", AI.InterpFactorSupport.AnyInteger, new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        public static AI rifeNcnnVs = new AI(AI.AiBackend.Ncnn, "RIFE_NCNN_VS", AI.InterpFactorSupport.AnyFloat, new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 })
        { Piped = true };

        public static AI rifeNcnn = new AI(AI.AiBackend.Ncnn, "RIFE_NCNN", AI.InterpFactorSupport.AnyFloat, new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        public static AI flavrCuda = new AI(AI.AiBackend.Pytorch, "FLAVR_CUDA", AI.InterpFactorSupport.Fixed, new int[] { 2, 4, 8 });

        public static AI dainNcnn = new AI(AI.AiBackend.Ncnn, "DAIN_NCNN", AI.InterpFactorSupport.AnyFloat, new int[] { 2, 3, 4, 5, 6, 7, 8 });

        public static AI xvfiCuda = new AI(AI.AiBackend.Pytorch, "XVFI_CUDA", AI.InterpFactorSupport.AnyInteger, new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        public static AI ifrnetNcnn = new AI(AI.AiBackend.Ncnn, "IFRNet_NCNN", AI.InterpFactorSupport.Fixed, new int[] { 2 });

        public static List<AI> NetworksAll
        {
            get
            {
                return new List<AI> { rifeCuda, rifeNcnnVs, rifeNcnn, flavrCuda, dainNcnn, xvfiCuda, ifrnetNcnn };
            }
        }

        public static List<AI> NetworksAvailable
        {
            get
            {
                bool pytorchAvailable = Python.IsPytorchReady();

                if (pytorchAvailable)
                    return NetworksAll;
                else
                    return NetworksAll.Where(x => x.Backend != AI.AiBackend.Pytorch).ToList();
            }
        }

        public static AI GetAi(string aiName)
        {
            foreach (AI ai in NetworksAll)
            {
                if (ai.NameInternal == aiName)
                    return ai;
            }

            Logger.Log($"AI implementation lookup failed! This should not happen! Please tell the developer! (Implementations.cs)");
            return NetworksAll[0];
        }
    }
}