using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Flowframes.Ui
{
    public static class ControlExtensions
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool LockWindowUpdate(IntPtr hWndLock);

        public static void Suspend(this Control control)
        {
            LockWindowUpdate(control.Handle);
        }

        public static void Resume(this Control control)
        {
            LockWindowUpdate(IntPtr.Zero);
        }

        public static List<Control> GetControls(this Control control)
        {
            List<Control> list = new List<Control>();
            var controls = control.Controls.Cast<Control>().ToList();
            list.AddRange(controls);
            controls.ForEach(c => list.AddRange(c.GetControls()));
            return list;
        }

        public static void SetEnabled(this Control control, bool enabled, bool includeChildren = false)
        {
            // Set Enabled property of the control only if it's different from the desired value to avoid event firing etc.
            if (enabled && !control.Enabled)
            {
                control.Enabled = true;
            }
            else if (!enabled && control.Enabled)
            {
                control.Enabled = false;
            }

            if (includeChildren)
            {
                control.GetControls().ForEach(c => c.SetEnabled(enabled, includeChildren));
            }
        }

        public static void SetVisible(this Control control, bool visible)
        {
            // Set Visible property of the control only if it's different from the desired value to avoid event firing etc.
            if (visible && !control.Visible)
            {
                control.Visible = true;
            }
            else if (!visible && control.Visible)
            {
                control.Visible = false;
            }
        }

        public static void Invoke(this Control control, MethodInvoker action)
        {
            control.Invoke(action);
        }
    }
}
