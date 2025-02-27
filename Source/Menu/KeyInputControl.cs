﻿// COPYRIGHT 2012, 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Windows.Forms;

using FreeTrainSimulator.Common.Input;

namespace FreeTrainSimulator.Menu
{
    /// <summary>
    /// A control for viewing and altering keyboard input settings, in combination with <see cref="KeyInputEditControl"/>.
    /// </summary>
    /// <remarks>
    /// <para>This control will modify the <see cref="UserCommandInput"/> it is given (but not the default input).</para>
    /// <para>The control displays the currently assigned keyboard shortcut and highlights the text if it is not the default. Clicking on the text invokes the editing behaviour via <see cref="KeyInputEditControl"/>.</para>
    /// </remarks>
    public partial class KeyInputControl : TextBox
    {
        public UserCommandInput UserInput { get; private set; }
        public UserCommandInput DefaultInput { get; private set; }

        public KeyInputControl(UserCommandInput userInput, UserCommandInput defaultInput)
        {
            InitializeComponent();

            UserInput = userInput;
            DefaultInput = defaultInput;
            UpdateText();
        }

        private void UpdateText()
        {
            Text = UserInput.ToString();
            if (Text == DefaultInput.ToString())
            {
                BackColor = SystemColors.Window;
                ForeColor = SystemColors.WindowText;
            }
            else
            {
                BackColor = SystemColors.Highlight;
                ForeColor = SystemColors.HighlightText;
            }
        }

        private void KeyInputControl_Click(object sender, EventArgs e)
        {
            using (KeyInputEditControl editKey = new KeyInputEditControl(this))
            {
                int originalUniqueDescriptor = UserInput.UniqueDescriptor;
                DialogResult result = editKey.ShowDialog(this);
                GC.KeepAlive(editKey); // Required to ensure keyboard hook is not collected too early.

                if (result == DialogResult.Cancel)
                    UserInput.UniqueDescriptor = originalUniqueDescriptor;
                else if (result == DialogResult.Ignore)
                    UserInput.UniqueDescriptor = DefaultInput.UniqueDescriptor;

            }

            // Ensure the modifiable inputs are kept in sync.
            if (UserInput is UserCommandModifiableKeyInput)
                (UserInput as UserCommandModifiableKeyInput).SynchronizeCombine();

            UpdateText();
            Parent.Focus();
        }
    }
}
