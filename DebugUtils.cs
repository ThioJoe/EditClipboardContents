﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EditClipboardContents;

namespace EditClipboardContents
{
    public static class DebugUtils
    {
        private static ToolTip toolTip = new ToolTip();
        private static Dictionary<Control, string> tooltipOriginalState = new Dictionary<Control, string>();

        public static void SetDebugTooltips(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is ToolStrip toolStrip)
                {
                    HandleToolStrip(toolStrip, true);
                }
                else
                {
                    if (!tooltipOriginalState.ContainsKey(control))
                    {
                        tooltipOriginalState[control] = toolTip.GetToolTip(control);
                    }
                    string dimensions = $"{control.Width}x{control.Height}";
                    toolTip.SetToolTip(control, dimensions);
                }

                // Handle nested controls
                if (control.Controls.Count > 0)
                {
                    SetDebugTooltips(control);
                }
            }
        }

        public static void RestoreOriginalTooltips(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is ToolStrip toolStrip)
                {
                    HandleToolStrip(toolStrip, false);
                }
                else
                {
                    if (tooltipOriginalState.ContainsKey(control))
                    {
                        toolTip.SetToolTip(control, tooltipOriginalState[control]);
                    }
                }

                // Handle nested controls
                if (control.Controls.Count > 0)
                {
                    RestoreOriginalTooltips(control);
                }
            }
        }

        private static void HandleToolStrip(ToolStrip toolStrip, bool setDebug)
        {
            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (setDebug)
                {
                    if (!tooltipOriginalState.ContainsKey(toolStrip))
                    {
                        tooltipOriginalState[toolStrip] = item.ToolTipText;
                    }
                    item.ToolTipText = $"{item.Width}x{item.Height}";
                }
                else
                {
                    if (tooltipOriginalState.ContainsKey(toolStrip))
                    {
                        item.ToolTipText = tooltipOriginalState[toolStrip];
                    }
                }
            }
        }

        
    }

    public partial class MainForm : Form
    {
        private void TestCountAdd()
        {
            #if DEBUG
            string currentTest = "Calls to grid update: ";
            testCounter++;
            labelTestCount.Text = currentTest + testCounter.ToString();
            #endif
        }
    }
}
