﻿using System;
using System.ComponentModel;

namespace ORTS.Common
{
    /// <summary>
    /// Specifies game commands.
    /// </summary>
    /// <remarks>
    /// <para>The ordering and naming of these commands is important. They are listed in the UI in the order they are defined in the code, and the first word of each command is the "group" to which it belongs.</para>
    /// </remarks>
    public enum UserCommand
    {
        [Description("Game Pause Menu")] GamePauseMenu,
        [Description("Game Save")] GameSave,
        [Description("Game Quit")] GameQuit,
        [Description("Game Pause")] GamePause,
        [Description("Game Screenshot")] GameScreenshot,
        [Description("Game Fullscreen")] GameFullscreen,
        [Description("Game External Controller (RailDriver)")] GameExternalCabController,
        [Description("Game Switch Ahead")] GameSwitchAhead,
        [Description("Game Switch Behind")] GameSwitchBehind,
        [Description("Game Switch Picked")] GameSwitchPicked,
        [Description("Game Signal Picked")] GameSignalPicked,
        [Description("Game Switch With Mouse")] GameSwitchWithMouse,
        [Description("Game Uncouple With Mouse")] GameUncoupleWithMouse,
        [Description("Game Change Cab")] GameChangeCab,
        [Description("Game Request Control")] GameRequestControl,
        [Description("Game Multi Player Dispatcher")] GameMultiPlayerDispatcher,
        [Description("Game Multi Player Texting")] GameMultiPlayerTexting,
        [Description("Game Switch Manual Mode")] GameSwitchManualMode,
        [Description("Game Clear Signal Forward")] GameClearSignalForward,
        [Description("Game Clear Signal Backward")] GameClearSignalBackward,
        [Description("Game Reset Signal Forward")] GameResetSignalForward,
        [Description("Game Reset Signal Backward")] GameResetSignalBackward,
        [Description("Game Autopilot Mode")] GameAutopilotMode,
        [Description("Game Suspend Old Player")] GameSuspendOldPlayer,

        [Description("Display Next Window Tab")] DisplayNextWindowTab,
        [Description("Display Help Window")] DisplayHelpWindow,
        [Description("Display Track Monitor Window")] DisplayTrackMonitorWindow,
        [Description("Display HUD")] DisplayHUD,
        [Description("Display Car Labels")] DisplayCarLabels,
        [Description("Display Station Labels")] DisplayStationLabels,
        [Description("Display Switch Window")] DisplaySwitchWindow,
        [Description("Display Train Operations Window")] DisplayTrainOperationsWindow,
        [Description("Display Next Station Window")] DisplayNextStationWindow,
        [Description("Display Compass Window")] DisplayCompassWindow,
        [Description("Display Basic HUD Toggle")] DisplayBasicHUDToggle,
        [Description("Display Train List Window")] DisplayTrainListWindow,

        [Description("Debug Speed Up")] DebugSpeedUp,
        [Description("Debug Speed Down")] DebugSpeedDown,
        [Description("Debug Speed Reset")] DebugSpeedReset,
        [Description("Debug Overcast Increase")] DebugOvercastIncrease,
        [Description("Debug Overcast Decrease")] DebugOvercastDecrease,
        [Description("Debug Fog Increase")] DebugFogIncrease,
        [Description("Debug Fog Decrease")] DebugFogDecrease,
        [Description("Debug Precipitation Increase")] DebugPrecipitationIncrease,
        [Description("Debug Precipitation Decrease")] DebugPrecipitationDecrease,
        [Description("Debug Precipitation Liquidity Increase")] DebugPrecipitationLiquidityIncrease,
        [Description("Debug Precipitation Liquidity Decrease")] DebugPrecipitationLiquidityDecrease,
        [Description("Debug Weather Change")] DebugWeatherChange,
        [Description("Debug Clock Forwards")] DebugClockForwards,
        [Description("Debug Clock Backwards")] DebugClockBackwards,
        [Description("Debug Logger")] DebugLogger,
        [Description("Debug Lock Shadows")] DebugLockShadows,
        [Description("Debug Dump Keyboard Map")] DebugDumpKeymap,
        [Description("Debug Log Render Frame")] DebugLogRenderFrame,
        [Description("Debug Tracks")] DebugTracks,
        [Description("Debug Signalling")] DebugSignalling,
        [Description("Debug Reset Wheel Slip")] DebugResetWheelSlip,
        [Description("Debug Toggle Advanced Adhesion")] DebugToggleAdvancedAdhesion,
        [Description("Debug Sound Form")] DebugSoundForm,
        [Description("Debug Physics Form")] DebugPhysicsForm,

        [Description("Camera Cab")] CameraCab,
        [Description("Camera Change Passenger Viewpoint")] CameraChangePassengerViewPoint,
        [Description("Camera 3D Cab")] CameraThreeDimensionalCab,
        [Description("Camera Toggle Show Cab")] CameraToggleShowCab,
        [Description("Camera Head Out Forward")] CameraHeadOutForward,
        [Description("Camera Head Out Backward")] CameraHeadOutBackward,
        [Description("Camera Outside Front")] CameraOutsideFront,
        [Description("Camera Outside Rear")] CameraOutsideRear,
        [Description("Camera Trackside")] CameraTrackside,
        [Description("Camera SpecialTracksidePoint")] CameraSpecialTracksidePoint,
        [Description("Camera Passenger")] CameraPassenger,
        [Description("Camera Brakeman")] CameraBrakeman,
        [Description("Camera Free")] CameraFree,
        [Description("Camera Previous Free")] CameraPreviousFree,
        [Description("Camera Reset")] CameraReset,
        [Description("Camera Move Fast")] CameraMoveFast,
        [Description("Camera Move Slow")] CameraMoveSlow,
        [Description("Camera Pan (Rotate) Left")] CameraPanLeft,
        [Description("Camera Pan (Rotate) Right")] CameraPanRight,
        [Description("Camera Pan (Rotate) Up")] CameraPanUp,
        [Description("Camera Pan (Rotate) Down")] CameraPanDown,
        [Description("Camera Zoom In (Move Z)")] CameraZoomIn,
        [Description("Camera Zoom Out (Move Z)")] CameraZoomOut,
        [Description("Camera Rotate (Pan) Left")] CameraRotateLeft,
        [Description("Camera Rotate (Pan) Right")] CameraRotateRight,
        [Description("Camera Rotate (Pan) Up")] CameraRotateUp,
        [Description("Camera Rotate (Pan) Down")] CameraRotateDown,
        [Description("Camera Car Next")] CameraCarNext,
        [Description("Camera Car Previous")] CameraCarPrevious,
        [Description("Camera Car First")] CameraCarFirst,
        [Description("Camera Car Last")] CameraCarLast,
        [Description("Camera Jumping Trains")] CameraJumpingTrains,
        [Description("Camera Jump Back Player")] CameraJumpBackPlayer,
        [Description("Camera Jump See Switch")] CameraJumpSeeSwitch,
        [Description("Camera Vibrate")] CameraVibrate,
        [Description("Camera Scroll Right")] CameraScrollRight,
        [Description("Camera Scroll Left")] CameraScrollLeft,
        [Description("Camera Browse Backwards")] CameraBrowseBackwards,
        [Description("Camera Browse Forwards")] CameraBrowseForwards,

        [Description("Control Forwards")] ControlForwards,
        [Description("Control Backwards")] ControlBackwards,
        [Description("Control Throttle Increase")] ControlThrottleIncrease,
        [Description("Control Throttle Decrease")] ControlThrottleDecrease,
        [Description("Control Throttle Zero")] ControlThrottleZero,
        [Description("Control Gear Up")] ControlGearUp,
        [Description("Control Gear Down")] ControlGearDown,
        [Description("Control Train Brake Increase")] ControlTrainBrakeIncrease,
        [Description("Control Train Brake Decrease")] ControlTrainBrakeDecrease,
        [Description("Control Train Brake Zero")] ControlTrainBrakeZero,
        [Description("Control Engine Brake Increase")] ControlEngineBrakeIncrease,
        [Description("Control Engine Brake Decrease")] ControlEngineBrakeDecrease,
        [Description("Control Dynamic Brake Increase")] ControlDynamicBrakeIncrease,
        [Description("Control Dynamic Brake Decrease")] ControlDynamicBrakeDecrease,
        [Description("Control Bail Off")] ControlBailOff,
        [Description("Control Initialize Brakes")] ControlInitializeBrakes,
        [Description("Control Handbrake Full")] ControlHandbrakeFull,
        [Description("Control Handbrake None")] ControlHandbrakeNone,
        [Description("Control Odometer Show/Hide")] ControlOdoMeterShowHide,
        [Description("Control Odometer Reset")] ControlOdoMeterReset,
        [Description("Control Odometer Direction")] ControlOdoMeterDirection,
        [Description("Control Retainers On")] ControlRetainersOn,
        [Description("Control Retainers Off")] ControlRetainersOff,
        [Description("Control Brake Hose Connect")] ControlBrakeHoseConnect,
        [Description("Control Brake Hose Disconnect")] ControlBrakeHoseDisconnect,
        [Description("Control Alerter")] ControlAlerter,
        [Description("Control Emergency Push Button")] ControlEmergencyPushButton,
        [Description("Control Sander")] ControlSander,
        [Description("Control Sander Toggle")] ControlSanderToggle,
        [Description("Control Wiper")] ControlWiper,
        [Description("Control Horn")] ControlHorn,
        [Description("Control Bell")] ControlBell,
        [Description("Control Bell Toggle")] ControlBellToggle,
        [Description("Control Door Left")] ControlDoorLeft,
        [Description("Control Door Right")] ControlDoorRight,
        [Description("Control Mirror")] ControlMirror,
        [Description("Control Light")] ControlLight,
        [Description("Control Pantograph 1")] ControlPantograph1,
        [Description("Control Pantograph 2")] ControlPantograph2,
        [Description("Control Pantograph 3")] ControlPantograph3,
        [Description("Control Pantograph 4")] ControlPantograph4,
        [Description("Control Circuit Breaker Closing Order")] ControlCircuitBreakerClosingOrder,
        [Description("Control Circuit Breaker Opening Order")] ControlCircuitBreakerOpeningOrder,
        [Description("Control Circuit Breaker Closing Authorization")] ControlCircuitBreakerClosingAuthorization,
        [Description("Control Diesel Player")] ControlDieselPlayer,
        [Description("Control Diesel Helper")] ControlDieselHelper,
        [Description("Control Headlight Increase")] ControlHeadlightIncrease,
        [Description("Control Headlight Decrease")] ControlHeadlightDecrease,
        [Description("Control Injector 1 Increase")] ControlInjector1Increase,
        [Description("Control Injector 1 Decrease")] ControlInjector1Decrease,
        [Description("Control Injector 1")] ControlInjector1,
        [Description("Control Injector 2 Increase")] ControlInjector2Increase,
        [Description("Control Injector 2 Decrease")] ControlInjector2Decrease,
        [Description("Control Injector 2")] ControlInjector2,
        [Description("Control Blower Increase")] ControlBlowerIncrease,
        [Description("Control Blower Decrease")] ControlBlowerDecrease,
        [Description("Control Steam Heat Increase")] ControlSteamHeatIncrease,
        [Description("Control Steam Heat Decrease")] ControlSteamHeatDecrease,
        [Description("Control Damper Increase")] ControlDamperIncrease,
        [Description("Control Damper Decrease")] ControlDamperDecrease,
        [Description("Control Firebox Open")] ControlFireboxOpen,
        [Description("Control Firebox Close")] ControlFireboxClose,
        [Description("Control Firing Rate Increase")] ControlFiringRateIncrease,
        [Description("Control Firing Rate Decrease")] ControlFiringRateDecrease,
        [Description("Control Fire Shovel Full")] ControlFireShovelFull,
        [Description("Control Cylinder Cocks")] ControlCylinderCocks,
        [Description("Control Small Ejector Increase")] ControlSmallEjectorIncrease,
        [Description("Control Small Ejector Decrease")] ControlSmallEjectorDecrease,
        [Description("Control Cylinder Compound")] ControlCylinderCompound,
        [Description("Control Firing")] ControlFiring,
        [Description("Control Refill")] ControlRefill,
        [Description("Control TroughRefill")] ControlTroughRefill,
        [Description("Control ImmediateRefill")] ControlImmediateRefill,
        [Description("Control Turntable Clockwise")] ControlTurntableClockwise,
        [Description("Control Turntable Counterclockwise")] ControlTurntableCounterclockwise,
        [Description("Control Cab Radio")] ControlCabRadio,
        [Description("Control AI Fire On")] ControlAIFireOn,
        [Description("Control AI Fire Off")] ControlAIFireOff,
        [Description("Control AI Fire Reset")] ControlAIFireReset,
    }

    /// <summary>
    /// Specifies the keyboard modifiers for <see cref="UserCommands"/>.
    /// </summary>
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4
    }

}
