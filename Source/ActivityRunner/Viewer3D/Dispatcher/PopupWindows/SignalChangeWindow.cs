﻿using System;

using FreeTrainSimulator.Common;
using FreeTrainSimulator.Graphics.Window;
using FreeTrainSimulator.Graphics.Window.Controls;
using FreeTrainSimulator.Graphics.Window.Controls.Layout;

using GetText;

using Microsoft.Xna.Framework;

using Orts.Simulation.Multiplayer;
using Orts.Simulation.Multiplayer.Messaging;

namespace Orts.ActivityRunner.Viewer3D.Dispatcher.PopupWindows
{
    public class SignalChangeWindow : WindowBase
    {
        private readonly Point offset;
        private ISignal signal;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private RadioButton rbtnSystem;
        private ControlLayout callonLine;
#pragma warning restore CA2213 // Disposable fields should be disposed

        public SignalChangeWindow(WindowManager owner, Point relativeLocation, Catalog catalog = null) :
            base(owner ?? throw new ArgumentNullException(nameof(owner)), (catalog ??= CatalogManager.Catalog).GetString("Change Signal"), relativeLocation, new Point(140, 105), catalog)
        {
            Modal = true;
            offset = new Point((int)((Borders.Width / -3) * owner.DpiScaling), (int)(10 * owner.DpiScaling));
        }

        public void OpenAt(Point point, ISignal signal)
        {
            this.signal = signal;
            rbtnSystem.State = true;
            callonLine.Visible = signal?.CallOnEnabled ?? false;
            Relocate(point + offset);
            Open();
        }

        protected override ControlLayout Layout(ControlLayout layout, float headerScaling = 1)
        {
            Label label;
            RadioButton radioButton;
            layout = base.Layout(layout, headerScaling);
            ControlLayout rbLayout = layout.AddLayoutVertical();
            RadioButtonGroup radioButtonGroup = new RadioButtonGroup();
            callonLine = rbLayout.AddLayoutHorizontalLineOfText();
            callonLine.Add(rbtnSystem = new RadioButton(this, radioButtonGroup) { TextColor = Color.White, State = true, Tag = SignalState.Clear });
            rbtnSystem.OnClick += Button_OnClick;
            callonLine.Add(label = new Label(this, callonLine.RemainingWidth, Owner.TextFontDefault.Height, Catalog.GetString("System Controlled")) { Tag = SignalState.Clear });
            label.OnClick += Button_OnClick;

            callonLine = rbLayout.AddLayoutHorizontalLineOfText();
            callonLine.Add(radioButton = new RadioButton(this, radioButtonGroup) { TextColor = Color.Red, Tag = SignalState.Lock });
            radioButton.OnClick += Button_OnClick;
            callonLine.Add(label = new Label(this, callonLine.RemainingWidth, Owner.TextFontDefault.Height, Catalog.GetString("Stop")) { Tag = SignalState.Lock });
            label.OnClick += Button_OnClick;

            callonLine = rbLayout.AddLayoutHorizontalLineOfText();
            callonLine.Add(radioButton = new RadioButton(this, radioButtonGroup) { TextColor = Color.Yellow, Tag = SignalState.Approach });
            radioButton.OnClick += Button_OnClick;
            callonLine.Add(label = new Label(this, callonLine.RemainingWidth, Owner.TextFontDefault.Height, Catalog.GetString("Approach")) { Tag = SignalState.Approach });
            label.OnClick += Button_OnClick;

            callonLine = rbLayout.AddLayoutHorizontalLineOfText();
            callonLine.Add(radioButton = new RadioButton(this, radioButtonGroup) { TextColor = Color.LimeGreen, Tag = SignalState.Manual });
            radioButton.OnClick += Button_OnClick;
            callonLine.Add(label = new Label(this, callonLine.RemainingWidth, Owner.TextFontDefault.Height, Catalog.GetString("Proceed")) { Tag = SignalState.Manual });
            label.OnClick += Button_OnClick;

            callonLine = rbLayout.AddLayoutHorizontalLineOfText();
            callonLine.Add(radioButton = new RadioButton(this, radioButtonGroup) { TextColor = Color.White, Tag = SignalState.CallOn });
            radioButton.OnClick += Button_OnClick;
            callonLine.Add(label = new Label(this, callonLine.RemainingWidth, Owner.TextFontDefault.Height, Catalog.GetString("Call On")) { Tag = SignalState.CallOn });
            label.OnClick += Button_OnClick;
            return layout;
        }

        private void Button_OnClick(object sender, MouseClickEventArgs e)
        {
            if (sender is WindowControl control && control.Tag != null)
            {
                if (MultiPlayerManager.Instance().AmAider)
                {
                    MultiPlayerManager.Broadcast(new SignalChangeMessage(signal, (SignalState)control.Tag));
                    return;
                }
                signal.State = (SignalState)control.Tag;
            }
            Close();
        }

        protected override void FocusLost()
        {
            Close();
            base.FocusLost();
        }
    }
}
