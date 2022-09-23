﻿using System;
using System.Linq;

using GetText;

using Microsoft.Xna.Framework;

using Orts.Common;
using Orts.Common.DebugInfo;
using Orts.Common.Input;
using Orts.Formats.Msts;
using Orts.Graphics;
using Orts.Graphics.Window;
using Orts.Graphics.Window.Controls;
using Orts.Graphics.Window.Controls.Layout;
using Orts.Settings;
using Orts.Simulation;
using Orts.Simulation.MultiPlayer;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;

namespace Orts.ActivityRunner.Viewer3D.PopupWindows
{
    internal class DrivingTrainWindow : WindowBase
    {
        private const int monoColumnWidth = 40;
        private const int normalColumnWidth = 64;

        private enum WindowMode
        {
            Normal,
            NormalMono,
        }

        private enum DetailInfo
        {
            Time,
            Replay,
            Speed,
            Gradient,
            Direction,
            Throttle,
            CylinderCocks,
            Sander,
            TrainBrake,
            TrainBrakeEQStatus,
            TrainBrakeStatus,
            TrainBrakeFirstCar,
            TrainBrakeLastCar,
            Retainer,
            EngineBrake,
            EngineBC,
        }

        private readonly UserSettings settings;
        private readonly UserCommandController<UserCommand> userCommandController;
        private WindowMode windowMode;
        private Label labelExpandMono;
        private readonly EnumArray<ControlLayout, DetailInfo> groupDetails = new EnumArray<ControlLayout, DetailInfo>();
        private bool replaying;
        private bool eqAvailable;
        private bool retainerAvailable;
        private bool engineBcAvailable;

        private string directionKeyInput;
        private string throttleKeyInput;
        private string cylinderCocksInput;
        private string sanderInput;
        private string trainBrakeInput;
        private string engineBrakeInput;
        private bool throttleIncreaseDown;
        private bool throttleDecreaseDown;
        private bool dynamicBrakeIncreaseDown;
        private bool dynamicBrakeDecreaseDown;
        private string gearKeyInput;
        private bool pantographKeyDown;
        private bool autoPilotKeyDown;
        private bool firingKeyDown;
        private bool aiFireOnKeyDown;
        private bool aiFireOffKeyDown;
        private bool aiFireResetKeyDown;

        public DrivingTrainWindow(WindowManager owner, Point relativeLocation, UserSettings settings, Catalog catalog = null) :
            base(owner, (catalog ??= CatalogManager.Catalog).GetString("Train Driving Info"), relativeLocation, new Point(200, 300), catalog)
        {
            userCommandController = Owner.UserCommandController as UserCommandController<UserCommand>;
            this.settings = settings;
            _ = EnumExtension.GetValue(settings.PopupSettings[ViewerWindowType.DrivingTrainWindow], out windowMode);

            Resize();
        }

        protected override ControlLayout Layout(ControlLayout layout, float headerScaling = 1)
        {
            MSTSLocomotive playerLocomotive = Simulator.Instance.PlayerLocomotive;
            layout = base.Layout(layout, headerScaling).AddLayoutOffset(0);
            ControlLayout line = layout.AddLayoutHorizontal();
            line.HorizontalChildAlignment = HorizontalAlignment.Right;
            line.VerticalChildAlignment = VerticalAlignment.Top;
            line.Add(labelExpandMono = new Label(this, Owner.TextFontDefault.Height, Owner.TextFontDefault.Height, windowMode == WindowMode.NormalMono ? Markers.ArrowRight : Markers.ArrowLeft, HorizontalAlignment.Center, Color.Yellow));
            labelExpandMono.OnClick += LabelExpandMono_OnClick;
            layout = layout.AddLayoutVertical();

            void AddDetailLine(DetailInfo detail, int width, string caption, System.Drawing.Font font, HorizontalAlignment alignment = HorizontalAlignment.Left)
            {
                line = layout.AddLayoutHorizontalLineOfText();
                line.Add(new Label(this, (int)(Owner.DpiScaling * 12), font.Height, null, font) { TextColor = Color.Yellow });
                line.Add(new Label(this, 0, 0, width, font.Height, caption, alignment, font, Color.White));
                line.Add(new Label(this, (int)(Owner.DpiScaling * 12), font.Height, null, font) { TextColor = Color.Yellow });
                line.Add(new Label(this, width * 2, font.Height, null, font));

                groupDetails[detail] = line;
            }

            int columnWidth;
            System.Drawing.Font font;
            bool shortMode = false;
            if (windowMode == WindowMode.Normal)
            {
                columnWidth = (int)(Owner.DpiScaling * normalColumnWidth);
                font = Owner.TextFontDefault;
            }
            else
            {
                columnWidth = (int)(Owner.DpiScaling * monoColumnWidth);
                font = Owner.TextFontMonoDefault;
                shortMode = true;
            }

            AddDetailLine(DetailInfo.Time, columnWidth, shortMode ? FourCharAcronym.Time.GetLocalizedDescription() : Catalog.GetString("Time"), font);
            if (Simulator.Instance.IsReplaying)
            {
                AddDetailLine(DetailInfo.Replay, columnWidth, shortMode ? FourCharAcronym.Replay.GetLocalizedDescription() : Catalog.GetString("Replay"), font);
            }
            AddDetailLine(DetailInfo.Speed, columnWidth, shortMode ? FourCharAcronym.Speed.GetLocalizedDescription() : Catalog.GetString("Speed"), font);
            AddDetailLine(DetailInfo.Gradient, columnWidth, shortMode ? FourCharAcronym.Gradient.GetLocalizedDescription() : Catalog.GetString("Gradient"), font);
            layout.AddHorizontalSeparator(true);
            AddDetailLine(DetailInfo.Direction, columnWidth, playerLocomotive.EngineType == EngineType.Steam ?
                shortMode ? FourCharAcronym.Reverser.GetLocalizedDescription() : Catalog.GetString("Reverser") :
                shortMode ? FourCharAcronym.Direction.GetLocalizedDescription() : Catalog.GetString("Direction"), font);
            AddDetailLine(DetailInfo.Throttle, columnWidth, playerLocomotive.EngineType == EngineType.Steam ?
                shortMode ? FourCharAcronym.Regulator.GetLocalizedDescription() : Catalog.GetString("Regulator") :
                shortMode ? FourCharAcronym.Throttle.GetLocalizedDescription() : Catalog.GetString("Throttle"), font);
            if (playerLocomotive.EngineType == EngineType.Steam)
            {
                AddDetailLine(DetailInfo.CylinderCocks, columnWidth, shortMode ? FourCharAcronym.CylinderCocks.GetLocalizedDescription() : Catalog.GetString("Cyl Cocks"), font);
            }
            AddDetailLine(DetailInfo.Sander, columnWidth, shortMode ? FourCharAcronym.Sander.GetLocalizedDescription() : Catalog.GetString("Sander"), font);
            layout.AddHorizontalSeparator(true);
            AddDetailLine(DetailInfo.TrainBrake, columnWidth, shortMode ? FourCharAcronym.TrainBrake.GetLocalizedDescription() : Catalog.GetString("Train Brk"), font);
            (groupDetails[DetailInfo.TrainBrake].Controls[3] as Label).TextColor = Color.Cyan;
            if (eqAvailable)
            {
                AddDetailLine(DetailInfo.TrainBrakeEQStatus, columnWidth, shortMode ? FourCharAcronym.EQReservoir.GetLocalizedDescription() : "  " + Catalog.GetString("EQ Res"), font);
                AddDetailLine(DetailInfo.TrainBrakeFirstCar, columnWidth, shortMode ? FourCharAcronym.FirstTrainCar.GetLocalizedDescription() : "  " + Catalog.GetString("1st car"), font);
                AddDetailLine(DetailInfo.TrainBrakeLastCar, columnWidth, shortMode ? FourCharAcronym.EndOfTrainCar.GetLocalizedDescription() : "  " + Catalog.GetString("EOT car"), font);
            }
            else
            {
                groupDetails[DetailInfo.TrainBrakeEQStatus] = null;
                AddDetailLine(DetailInfo.TrainBrakeStatus, columnWidth, string.Empty, font);
            }
            if (retainerAvailable)
            {
                AddDetailLine(DetailInfo.Retainer, columnWidth, shortMode ? FourCharAcronym.Retainer.GetLocalizedDescription() : Catalog.GetString("Retainers"), font);
            }
            AddDetailLine(DetailInfo.EngineBrake, columnWidth, shortMode ? FourCharAcronym.EngineBrake.GetLocalizedDescription() : Catalog.GetString("Eng Brk"), font);
            (groupDetails[DetailInfo.EngineBrake].Controls[3] as Label).TextColor = Color.Cyan;
            if (engineBcAvailable)
            {
                AddDetailLine(DetailInfo.EngineBC, columnWidth, "  " + (shortMode ? FourCharAcronym.BrakeCylinder.GetLocalizedDescription() : Catalog.GetString("Brk Cyl")), font);
            }
            layout.AddHorizontalSeparator(true);
            return layout;
        }

        private void LabelExpandMono_OnClick(object sender, MouseClickEventArgs e)
        {
            windowMode = windowMode.Next();
            Resize();
        }

        public override bool Open()
        {
            userCommandController.AddEvent(UserCommand.ControlReverserForward, KeyEventType.KeyDown, DirectionCommandForward, true);
            userCommandController.AddEvent(UserCommand.ControlReverserBackward, KeyEventType.KeyDown, DirectionCommandBackward, true);
            userCommandController.AddEvent(UserCommand.ControlThrottleIncrease, KeyEventType.KeyDown, ThrottleCommandIncrease, true);
            userCommandController.AddEvent(UserCommand.ControlThrottleDecrease, KeyEventType.KeyDown, ThrottleCommandDecrease, true);
            userCommandController.AddEvent(UserCommand.ControlCylinderCocks, KeyEventType.KeyDown, CylinderCocksCommand, true);
            userCommandController.AddEvent(UserCommand.ControlSander, KeyEventType.KeyDown, SanderCommand, true);
            userCommandController.AddEvent(UserCommand.ControlSanderToggle, KeyEventType.KeyDown, SanderCommand, true);
            userCommandController.AddEvent(UserCommand.ControlTrainBrakeIncrease, KeyEventType.KeyDown, TrainBrakeCommandIncrease, true);
            userCommandController.AddEvent(UserCommand.ControlTrainBrakeDecrease, KeyEventType.KeyDown, TrainBrakeCommandDecrease, true);
            userCommandController.AddEvent(UserCommand.ControlEngineBrakeIncrease, KeyEventType.KeyDown, EngineBrakeCommandIncrease, true);
            userCommandController.AddEvent(UserCommand.ControlEngineBrakeDecrease, KeyEventType.KeyDown, EngineBrakeCommandDecrease, true);
            userCommandController.AddEvent(UserCommand.ControlDynamicBrakeIncrease, KeyEventType.KeyDown, DynamicBrakeCommandIncrease, true);
            userCommandController.AddEvent(UserCommand.ControlDynamicBrakeDecrease, KeyEventType.KeyDown, DynamicBrakeCommandDecrease, true);
            userCommandController.AddEvent(UserCommand.ControlGearUp, KeyEventType.KeyDown, GearCommandUp, true);
            userCommandController.AddEvent(UserCommand.ControlGearDown, KeyEventType.KeyDown, GearCommandDown, true);
            userCommandController.AddEvent(UserCommand.ControlGearDown, KeyEventType.KeyDown, GearCommandDown, true);
            userCommandController.AddEvent(UserCommand.ControlPantograph1, KeyEventType.KeyDown, PantographCommand, true);
            userCommandController.AddEvent(UserCommand.ControlPantograph2, KeyEventType.KeyDown, PantographCommand, true);
            userCommandController.AddEvent(UserCommand.ControlPantograph3, KeyEventType.KeyDown, PantographCommand, true);
            userCommandController.AddEvent(UserCommand.ControlPantograph4, KeyEventType.KeyDown, PantographCommand, true);
            userCommandController.AddEvent(UserCommand.GameAutopilotMode, KeyEventType.KeyDown, AutoPilotCommand, true);
            userCommandController.AddEvent(UserCommand.ControlFiring, KeyEventType.KeyDown, FiringCommand, true);
            userCommandController.AddEvent(UserCommand.ControlAIFireOn, KeyEventType.KeyDown, AIFiringOnCommand, true);
            userCommandController.AddEvent(UserCommand.ControlAIFireOff, KeyEventType.KeyDown, AIFiringOffCommand, true);
            userCommandController.AddEvent(UserCommand.ControlAIFireReset, KeyEventType.KeyDown, AIFiringResetCommand, true);

            userCommandController.AddEvent(UserCommand.DisplayTrainDrivingWindow, KeyEventType.KeyPressed, TabAction, true);

            return base.Open();
        }

        public override bool Close()
        {
            userCommandController.RemoveEvent(UserCommand.ControlReverserForward, KeyEventType.KeyDown, DirectionCommandForward);
            userCommandController.RemoveEvent(UserCommand.ControlReverserBackward, KeyEventType.KeyDown, DirectionCommandBackward);
            userCommandController.RemoveEvent(UserCommand.ControlThrottleIncrease, KeyEventType.KeyDown, ThrottleCommandIncrease);
            userCommandController.RemoveEvent(UserCommand.ControlThrottleDecrease, KeyEventType.KeyDown, ThrottleCommandDecrease);
            userCommandController.RemoveEvent(UserCommand.ControlCylinderCocks, KeyEventType.KeyDown, CylinderCocksCommand);
            userCommandController.RemoveEvent(UserCommand.ControlSander, KeyEventType.KeyDown, SanderCommand);
            userCommandController.RemoveEvent(UserCommand.ControlSanderToggle, KeyEventType.KeyDown, SanderCommand);
            userCommandController.RemoveEvent(UserCommand.ControlTrainBrakeIncrease, KeyEventType.KeyDown, TrainBrakeCommandIncrease);
            userCommandController.RemoveEvent(UserCommand.ControlTrainBrakeDecrease, KeyEventType.KeyDown, TrainBrakeCommandDecrease);
            userCommandController.RemoveEvent(UserCommand.ControlEngineBrakeIncrease, KeyEventType.KeyDown, EngineBrakeCommandIncrease);
            userCommandController.RemoveEvent(UserCommand.ControlEngineBrakeDecrease, KeyEventType.KeyDown, EngineBrakeCommandDecrease);
            userCommandController.RemoveEvent(UserCommand.ControlDynamicBrakeIncrease, KeyEventType.KeyDown, DynamicBrakeCommandIncrease);
            userCommandController.RemoveEvent(UserCommand.ControlDynamicBrakeDecrease, KeyEventType.KeyDown, DynamicBrakeCommandDecrease);
            userCommandController.RemoveEvent(UserCommand.ControlGearUp, KeyEventType.KeyDown, GearCommandUp);
            userCommandController.RemoveEvent(UserCommand.ControlGearDown, KeyEventType.KeyDown, GearCommandDown);
            userCommandController.RemoveEvent(UserCommand.ControlPantograph1, KeyEventType.KeyDown, PantographCommand);
            userCommandController.RemoveEvent(UserCommand.ControlPantograph2, KeyEventType.KeyDown, PantographCommand);
            userCommandController.RemoveEvent(UserCommand.ControlPantograph3, KeyEventType.KeyDown, PantographCommand);
            userCommandController.RemoveEvent(UserCommand.ControlPantograph4, KeyEventType.KeyDown, PantographCommand);
            userCommandController.RemoveEvent(UserCommand.GameAutopilotMode, KeyEventType.KeyDown, AutoPilotCommand);
            userCommandController.RemoveEvent(UserCommand.ControlFiring, KeyEventType.KeyDown, FiringCommand);
            userCommandController.RemoveEvent(UserCommand.ControlAIFireOn, KeyEventType.KeyDown, AIFiringOnCommand);
            userCommandController.RemoveEvent(UserCommand.ControlAIFireOff, KeyEventType.KeyDown, AIFiringOffCommand);
            userCommandController.RemoveEvent(UserCommand.ControlAIFireReset, KeyEventType.KeyDown, AIFiringResetCommand);

            userCommandController.RemoveEvent(UserCommand.DisplayTrainDrivingWindow, KeyEventType.KeyPressed, TabAction);
            return base.Close();
        }

        private void TabAction(UserCommandArgs args)
        {
            if (args is ModifiableKeyCommandArgs keyCommandArgs && (keyCommandArgs.AdditionalModifiers & settings.Input.WindowTabCommandModifier) == settings.Input.WindowTabCommandModifier)
            {
                windowMode = windowMode.Next();
                Resize();
            }
        }

        private void Resize()
        {
            Point size = windowMode switch
            {
                WindowMode.Normal => new Point(3 * normalColumnWidth + 36, 300),
                WindowMode.NormalMono => new Point(3 * monoColumnWidth + 36, 300),
                _ => throw new InvalidOperationException(),
            };

            Resize(size);

            settings.PopupSettings[ViewerWindowType.DrivingTrainWindow] = windowMode.ToString();
        }

        //we need to keep a delegate reference to be able to unsubscribe, so those are just forwarders
#pragma warning disable IDE0022 // Use block body for methods
        private void DirectionCommandForward() => DirectionCommand(Direction.Forward);
        private void DirectionCommandBackward() => DirectionCommand(Direction.Backward);
        private void ThrottleCommandIncrease() => ThrottleCommand(true);
        private void ThrottleCommandDecrease() => ThrottleCommand(false);
        private void TrainBrakeCommandIncrease(UserCommandArgs userCommandArgs) => TrainBrakeCommand(true);
        private void TrainBrakeCommandDecrease(UserCommandArgs userCommandArgs) => TrainBrakeCommand(false);
        private void EngineBrakeCommandIncrease(UserCommandArgs userCommandArgs) => EngineBrakeCommand(true);
        private void EngineBrakeCommandDecrease(UserCommandArgs userCommandArgs) => EngineBrakeCommand(false);
        private void DynamicBrakeCommandIncrease(UserCommandArgs userCommandArgs) => DynamicBrakeCommand(true);
        private void DynamicBrakeCommandDecrease(UserCommandArgs userCommandArgs) => DynamicBrakeCommand(false);
        private void GearCommandDown(UserCommandArgs userCommandArgs) => GearCommand(true);
        private void GearCommandUp(UserCommandArgs userCommandArgs) => GearCommand(false);
#pragma warning restore IDE0022 // Use block body for methods

        private void DirectionCommand(Direction direction)
        {
            TrainCar locomotive = Simulator.Instance.PlayerLocomotive;
            directionKeyInput = (locomotive.EngineType != EngineType.Steam && (locomotive.ThrottlePercent >= 1 || locomotive.AbsSpeedMpS > 1))
                || (locomotive.EngineType == EngineType.Steam && locomotive is MSTSSteamLocomotive mstsSteamLocomotive && mstsSteamLocomotive.CutoffController.MaximumValue == Math.Abs(locomotive.Train.MUReverserPercent / 100))
                ? Markers.Block
                : direction == Direction.Forward ? Markers.ArrowUp : Markers.ArrowDown;
        }

        private void ThrottleCommand(bool increase)
        {
            throttleIncreaseDown = increase;
            throttleDecreaseDown = !increase;
            TrainCar locomotive = Simulator.Instance.PlayerLocomotive;
            throttleKeyInput = locomotive.DynamicBrakePercent < 1 && (increase && (locomotive as MSTSLocomotive).ThrottleController.MaximumValue == locomotive.ThrottlePercent / 100)
                || (!increase && locomotive.ThrottlePercent == 0)
                ? Markers.Block
                : locomotive.DynamicBrakePercent > -1
                    ? Markers.BlockLowerHalf
                    : increase ? Markers.ArrowUp : Markers.ArrowDown;
        }

        private void CylinderCocksCommand()
        {
            if (Simulator.Instance.PlayerLocomotive is MSTSSteamLocomotive)
                cylinderCocksInput = Markers.ArrowRight;
        }

        private void SanderCommand()
        {
            sanderInput = Markers.ArrowDown;
        }

        private void TrainBrakeCommand(bool increase)
        {
            trainBrakeInput = increase ? Markers.ArrowUp : Markers.ArrowDown;
        }

        private void EngineBrakeCommand(bool increase)
        {
            engineBrakeInput = increase ? Markers.ArrowDown : Markers.ArrowUp;
        }

        private void DynamicBrakeCommand(bool increase)
        {
            dynamicBrakeIncreaseDown = increase;
            dynamicBrakeDecreaseDown = !increase;
        }

        private void GearCommand(bool down)
        {
            gearKeyInput = down ? Markers.ArrowDown : Markers.ArrowUp;
        }

        private void PantographCommand()
        {
            pantographKeyDown = true;
        }

        private void AutoPilotCommand()
        {
            autoPilotKeyDown = true;
        }

        private void FiringCommand()
        {
            firingKeyDown = true;
        }

        private void AIFiringOnCommand()
        {
            aiFireOnKeyDown = true;
        }

        private void AIFiringOffCommand()
        {
            aiFireOffKeyDown = true;
        }

        private void AIFiringResetCommand()
        {
            aiFireResetKeyDown = true;
        }

        protected override void Update(GameTime gameTime, bool shouldUpdate)
        {
            base.Update(gameTime, shouldUpdate);
            if (shouldUpdate)
                if (UpdateDrivingInformation())
                    Resize();
        }

        private bool UpdateDrivingInformation()
        {
            bool result = false;
            // Client and server may have a time difference.
            MSTSLocomotive playerLocomotive = Simulator.Instance.PlayerLocomotive;
            if (groupDetails[DetailInfo.Time]?.Controls[3] is Label timeLabel)
            {
                timeLabel.Text = MultiPlayerManager.MultiplayerState == MultiplayerState.Client ? $"{FormatStrings.FormatTime(Simulator.Instance.ClockTime + MultiPlayerManager.Instance().ServerTimeDifference)}" : $"{FormatStrings.FormatTime(Simulator.Instance.ClockTime)}";
            }
            // Replay info
            result |= replaying != (replaying = Simulator.Instance.IsReplaying);
            if (replaying && groupDetails[DetailInfo.Replay]?.Controls[3] is Label replayLabel)
                replayLabel.Text = $"{FormatStrings.FormatTime(Simulator.Instance.Log.ReplayEndsAt - Simulator.Instance.ClockTime)}";
            // Speed info
            if (groupDetails[DetailInfo.Speed]?.Controls[3] is Label speedLabel)
            {
                speedLabel.Text = $"{FormatStrings.FormatSpeedDisplay(playerLocomotive.SpeedMpS, Simulator.Instance.MetricUnits)}";
                speedLabel.TextColor = ColorCoding.SpeedingColor(playerLocomotive.AbsSpeedMpS, playerLocomotive.Train.MaxTrainSpeedAllowed);
            }
            // Gradient info
            if (groupDetails[DetailInfo.Gradient]?.Controls[3] is Label gradientLabel)
            {
                double gradient = Math.Round(playerLocomotive.CurrentElevationPercent, 1);
                if (gradient == 0) // to avoid negative zero string output if gradient after rounding is -0.0
                    gradient = 0;
                gradientLabel.Text = $"{gradient:F1}% {(gradient > 0 ? Markers.Ascent : gradient < 0 ? Markers.Descent : string.Empty)}";
                gradientLabel.TextColor = (gradient > 0 ? Color.Yellow : gradient < 0 ? Color.LightSkyBlue : Color.White);
            }
            // Direction
            if (groupDetails[DetailInfo.Direction]?.Controls[3] is Label directionLabel)
            {
                float reverserPercent = playerLocomotive.Train.MUReverserPercent;
                directionLabel.Text = $"{(Math.Abs(reverserPercent) != 100 ? $"{reverserPercent}% " : string.Empty)}{playerLocomotive.Direction.GetLocalizedDescription()}";
                (groupDetails[DetailInfo.Direction].Controls[0] as Label).Text = directionKeyInput;
                (groupDetails[DetailInfo.Direction].Controls[2] as Label).Text = directionKeyInput;
                directionKeyInput = string.Empty;
            }
            // Throttle
            if (groupDetails[DetailInfo.Throttle]?.Controls[3] is Label throttleLabel)
            {
                throttleLabel.Text = $"{Math.Round(playerLocomotive.ThrottlePercent):F0}% {(playerLocomotive is MSTSDieselLocomotive && playerLocomotive.Train.DistributedPowerMode == DistributedPowerMode.Traction ? $"({Math.Round(playerLocomotive.Train.DPThrottlePercent):F0}%)" : string.Empty)}";
                (groupDetails[DetailInfo.Throttle].Controls[0] as Label).Text = throttleKeyInput;
                (groupDetails[DetailInfo.Throttle].Controls[2] as Label).Text = throttleKeyInput;
                throttleKeyInput = string.Empty;
            }
            // Cylinder Cocks
            if (groupDetails[DetailInfo.CylinderCocks]?.Controls[3] is Label cocksLabel && playerLocomotive is MSTSSteamLocomotive mstsSteamLocomotive)
            {
                cocksLabel.Text = $"{(mstsSteamLocomotive.CylinderCocksAreOpen ? Catalog.GetString("Open") : Catalog.GetString("Closed"))}";
                cocksLabel.TextColor = mstsSteamLocomotive.CylinderCocksAreOpen ? Color.Orange : Color.White;
            }
            // Sander
            if (groupDetails[DetailInfo.Sander]?.Controls[3] is Label sanderLabel)
            {
                bool sanderBlocked = playerLocomotive.AbsSpeedMpS > playerLocomotive.SanderSpeedOfMpS;
                sanderLabel.Text = $"{(playerLocomotive.Sander ? sanderBlocked ? Catalog.GetString("Blocked") : Catalog.GetString("On") : Catalog.GetString("Off"))}";
                sanderLabel.TextColor = playerLocomotive.Sander ? sanderBlocked ? Color.OrangeRed : Color.Orange : Color.White;
                (groupDetails[DetailInfo.Sander].Controls[0] as Label).Text = sanderInput;
                (groupDetails[DetailInfo.Sander].Controls[2] as Label).Text = sanderInput;
                sanderInput = string.Empty;
            }
            // Train Brake
            if (groupDetails[DetailInfo.TrainBrake]?.Controls[3] is Label trainBrakeStatusLabel)
            {
                trainBrakeStatusLabel.Text = (playerLocomotive.TrainBrakeController as INameValueInformationProvider).DebugInfo[windowMode != WindowMode.Normal ? "StatusShort" : "Status"];
                (groupDetails[DetailInfo.TrainBrake].Controls[0] as Label).Text = trainBrakeInput;
                (groupDetails[DetailInfo.TrainBrake].Controls[2] as Label).Text = trainBrakeInput;
                trainBrakeInput = string.Empty;
            }
            // Train Brake Equalizer Reservoir
            result |= eqAvailable != (eqAvailable = !string.IsNullOrEmpty((playerLocomotive.BrakeSystem as INameValueInformationProvider).DebugInfo["EQ"]));
            if (eqAvailable && groupDetails[DetailInfo.TrainBrakeEQStatus]?.Controls[3] is Label trainBrakeEQLabel)
            {
                string eqReservoir = (playerLocomotive.BrakeSystem as INameValueInformationProvider).DebugInfo["EQ"];
                if (windowMode != WindowMode.Normal)
                    eqReservoir = eqReservoir?.Split(' ')[0];
                trainBrakeEQLabel.Text = eqReservoir;
                (groupDetails[DetailInfo.TrainBrakeFirstCar]?.Controls[3] as Label).Text = (windowMode != WindowMode.Normal) ?
                    (playerLocomotive.Train.FirstWagonCar?.BrakeSystem as INameValueInformationProvider)?.DebugInfo["StatusShort"] :
                    (playerLocomotive.Train.FirstWagonCar?.BrakeSystem as INameValueInformationProvider)?.DebugInfo["Status"];
                (groupDetails[DetailInfo.TrainBrakeLastCar]?.Controls[3] as Label).Text = (windowMode != WindowMode.Normal) ?
                    (playerLocomotive.Train.EndOfTrainCar?.BrakeSystem as INameValueInformationProvider)?.DebugInfo["StatusShort"] :
                    (playerLocomotive.Train.EndOfTrainCar?.BrakeSystem as INameValueInformationProvider)?.DebugInfo["Status"];
            }
            else if (groupDetails[DetailInfo.TrainBrakeStatus]?.Controls[3] is Label trainBrakeLabel)
            {
                trainBrakeLabel.Text = (windowMode != WindowMode.Normal) ?
                    (playerLocomotive.BrakeSystem as INameValueInformationProvider).DebugInfo["StatusShort"] :
                    (playerLocomotive.BrakeSystem as INameValueInformationProvider).DebugInfo["Status"];
            }
            result |= retainerAvailable != (retainerAvailable = playerLocomotive.Train.BrakeSystem.RetainerSetting != RetainerSetting.Exhaust);
            if (retainerAvailable && groupDetails[DetailInfo.Retainer]?.Controls[3] is Label retainerLabel)
            {
                retainerLabel.Text = $"{playerLocomotive.Train.BrakeSystem.RetainerPercent}% {playerLocomotive.Train.BrakeSystem.RetainerSetting.GetLocalizedDescription()}";
            }
            if (groupDetails[DetailInfo.EngineBrake]?.Controls[3] is Label engineBrakeLabel)
            {
                engineBrakeLabel.Text = (playerLocomotive.EngineBrakeController as INameValueInformationProvider).DebugInfo["Status"];
                (groupDetails[DetailInfo.EngineBrake].Controls[0] as Label).Text = engineBrakeInput ?? (((playerLocomotive.EngineBrakeController as INameValueInformationProvider).DebugInfo["BailOff"] != null) ? Markers.Block : null);
                (groupDetails[DetailInfo.EngineBrake].Controls[2] as Label).Text = engineBrakeInput ?? (((playerLocomotive.EngineBrakeController as INameValueInformationProvider).DebugInfo["BailOff"] != null) ? Markers.Block : null);
                result |= engineBcAvailable != (engineBcAvailable = !string.IsNullOrEmpty((playerLocomotive.EngineBrakeController as INameValueInformationProvider).DebugInfo["BC"]));
                if (engineBcAvailable && groupDetails[DetailInfo.EngineBC]?.Controls[3] is Label engineBCLabel)
                {
                    engineBCLabel.Text = (playerLocomotive.EngineBrakeController as INameValueInformationProvider).DebugInfo["BC"];
                }
                engineBrakeInput = null;
            }
            return result;
        }
    }
}
