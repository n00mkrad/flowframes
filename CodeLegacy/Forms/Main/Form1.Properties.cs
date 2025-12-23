using Flowframes.Data;
using Flowframes.Main;
using Flowframes.MiscUtils;

namespace Flowframes.Forms.Main
{
    partial class Form1
    {
        Enums.Output.Format OutputFormat { get { return ParseUtils.GetEnum<Enums.Output.Format>(comboxOutputFormat.Text, true, Strings.OutputFormat); } }

        Enums.Encoding.Encoder CurrentEncoder
        {
            get
            {
                if (comboxOutputEncoder.Visible)
                    return ParseUtils.GetEnum<Enums.Encoding.Encoder>(comboxOutputEncoder.Text, true, Strings.Encoder);
                else
                    return (Enums.Encoding.Encoder)(-1);
            }
        }

        Enums.Encoding.PixelFormat CurrentPixFmt
        {
            get
            {
                if (comboxOutputColors.Visible)
                    return ParseUtils.GetEnum<Enums.Encoding.PixelFormat>(comboxOutputColors.Text, true, Strings.PixelFormat);
                else
                    return (Enums.Encoding.PixelFormat)(-1);
            }
        }

        public ModelCollection.ModelInfo GetModel(AiInfo currentAi)
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

        public AiInfo GetAi()
        {
            try
            {
                foreach (AiInfo ai in Implementations.NetworksAll)
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
            return new OutputSettings() { Encoder = CurrentEncoder, Format = OutputFormat, PixelFormat = CurrentPixFmt, Quality = comboxOutputQuality.Text, CustomQuality = custQ };
        }
    }
}
