using Flowframes.Data;
using Flowframes.Main;
using Flowframes.MiscUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Forms.Main
{
    partial class Form1
    {
        Enums.Output.Format OutputFormat { get { return ParseUtils.GetEnum<Enums.Output.Format>(comboxOutputFormat.Text, true, Strings.OutputFormat); } }

        Enums.Encoding.Encoder Encoder
        {
            get
            {
                if (comboxOutputEncoder.Visible)
                    return ParseUtils.GetEnum<Enums.Encoding.Encoder>(comboxOutputEncoder.Text, true, Strings.Encoder);
                else
                    return (Enums.Encoding.Encoder)(-1);
            }
        }

        Enums.Encoding.PixelFormat PixelFormat
        {
            get
            {
                if (comboxOutputColors.Visible)
                    return ParseUtils.GetEnum<Enums.Encoding.PixelFormat>(comboxOutputColors.Text, true, Strings.PixelFormat);
                else
                    return (Enums.Encoding.PixelFormat)(-1);
            }
        }

        public ModelCollection.ModelInfo GetModel(AI currentAi)
        {
            try
            {
                return AiModels.GetModels(currentAi).Models[aiModel.SelectedIndex];
            }
            catch
            {
                return null;
            }
        }

        public AI GetAi()
        {
            try
            {
                foreach (AI ai in Implementations.NetworksAll)
                {
                    if (GetAiComboboxName(ai) == aiCombox.Text)
                        return ai;
                }

                return Implementations.NetworksAvailable[0];
            }
            catch
            {
                return null;
            }
        }

        public OutputSettings GetOutputSettings()
        {
            string custQ = textboxOutputQualityCust.Visible ? textboxOutputQualityCust.Text.Trim() : "";
            return new OutputSettings() { Encoder = Encoder, Format = OutputFormat, PixelFormat = PixelFormat, Quality = comboxOutputQuality.Text, CustomQuality = custQ };
        }
    }
}
