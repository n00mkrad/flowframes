using Microsoft.Toolkit.Uwp.Notifications;

namespace Flowframes.MiscUtils
{
    public class ToastUtils
    {
        public static void ShowToast(string title, string content)
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(content)
                .Show();
        }
    }
}