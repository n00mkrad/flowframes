using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Flowframes.Ui
{
    internal class ControlTextResizer
    {
        // Dictionary to store the initial font size for every tracked Control
        private readonly Dictionary<Control, float> _initialFontSizes = new Dictionary<Control, float>();

        /// <summary>
        /// Registers a list of Control objects to enable mouse wheel resizing and middle-click reset.
        /// </summary>
        /// <param name="controls">List of Controls to enhance.</param>
        public void Register(IEnumerable<Control> controls)
        {
            if (controls == null) return;

            foreach (var ctrl in controls)
            {
                // Avoid registering the same Control twice
                if (_initialFontSizes.ContainsKey(ctrl)) continue;

                // 1. Store the initial size so we can reset to it later
                _initialFontSizes[ctrl] = ctrl.Font.Size;

                // 2. Subscribe to the necessary events
                ctrl.MouseWheel += Ctrl_MouseWheel;
                ctrl.MouseDown += Ctrl_MouseDown;
            }
        }

        /// <summary>
        /// Handles the MouseWheel event to increase or decrease font size.
        /// Note: In WinForms, the control usually needs focus to receive MouseWheel events.
        /// </summary>
        private void Ctrl_MouseWheel(object sender, MouseEventArgs e)
        {
            // Only resize if Ctrl is held down
            if ((Control.ModifierKeys & Keys.Control) != Keys.Control)
                return;

            if (sender is Control ctrl)
            {
                float currentSize = ctrl.Font.Size;
                float newSize = currentSize;

                if (e.Delta > 0)
                {
                    newSize *= 1.1f; // Increase size
                }
                else if (e.Delta < 0)
                {
                    newSize *= 0.9f; // Decrease size
                }

                // Retrieve initial size to calculate bounds
                if (_initialFontSizes.TryGetValue(ctrl, out float initialSize))
                {
                    float minSize = initialSize * 0.75f;
                    float maxSize = initialSize * 2.0f;

                    // Clamp the new size
                    if (newSize < minSize) newSize = minSize;
                    if (newSize > maxSize) newSize = maxSize;
                }
                else
                {
                    // Fallback if initial size isn't found (shouldn't happen if registered correctly)
                    if (newSize < 1.0f) newSize = 1.0f;
                }

                // Only apply if the size actually changed
                if (Math.Abs(newSize - currentSize) > 0.01f)
                {
                    ctrl.Font = new Font(ctrl.Font.FontFamily, newSize, ctrl.Font.Style, ctrl.Font.Unit);
                }
            }
        }

        /// <summary>
        /// Handles the MouseDown event to check for Middle Mouse Button clicks.
        /// </summary>
        private void Ctrl_MouseDown(object sender, MouseEventArgs e)
        {
            // Check if the Middle button (scroll wheel click) was pressed
            if (e.Button == MouseButtons.Middle && sender is Control ctrl)
            {
                // Retrieve the initial size if we have it stored
                if (_initialFontSizes.TryGetValue(ctrl, out float initialSize))
                {
                    ctrl.Font = new Font(ctrl.Font.FontFamily, initialSize, ctrl.Font.Style, ctrl.Font.Unit);
                }
            }
        }

        /// <summary>
        /// Optional: Call this to clean up events if you are disposing controls dynamically.
        /// </summary>
        public void Unregister(Control ctrl)
        {
            if (_initialFontSizes.ContainsKey(ctrl))
            {
                ctrl.MouseWheel -= Ctrl_MouseWheel;
                ctrl.MouseDown -= Ctrl_MouseDown;
                _initialFontSizes.Remove(ctrl);
            }
        }
    }
}
