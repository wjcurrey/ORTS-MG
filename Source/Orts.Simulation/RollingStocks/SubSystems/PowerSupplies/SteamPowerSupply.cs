﻿using System;
using System.Threading.Tasks;

using FreeTrainSimulator.Common;
using FreeTrainSimulator.Models.Imported.State;

using Orts.Formats.Msts.Parsers;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    /// <summary>
    /// Basic power supply class for steam locomotives
    /// For electrical systems powered by battery
    /// </summary>
    public class SteamPowerSupply : ILocomotivePowerSupply
    {
        public readonly MSTSSteamLocomotive Locomotive;
        public PowerSupplyType Type => PowerSupplyType.Steam;

        public BatterySwitch BatterySwitch { get; protected set; }
        public MasterKey MasterKey { get; protected set; }
        public ElectricTrainSupplySwitch ElectricTrainSupplySwitch => null;

        public PowerSupplyState MainPowerSupplyState => PowerSupplyState.PowerOn;
        public bool MainPowerSupplyOn => true;
        public bool DynamicBrakeAvailable => false;

        public PowerSupplyState AuxiliaryPowerSupplyState => PowerSupplyState.PowerOn;
        public bool AuxiliaryPowerSupplyOn => true;

        public PowerSupplyState LowVoltagePowerSupplyState => BatterySwitch.On ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff;
        public bool LowVoltagePowerSupplyOn => LowVoltagePowerSupplyState == PowerSupplyState.PowerOn;

        public PowerSupplyState BatteryState => BatterySwitch.On ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff;
        public bool BatteryOn => BatteryState == PowerSupplyState.PowerOn;

        public PowerSupplyState CabPowerSupplyState => MasterKey.On ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff;
        public bool CabPowerSupplyOn => CabPowerSupplyState == PowerSupplyState.PowerOn;

        public PowerSupplyState ElectricTrainSupplyState => PowerSupplyState.Unavailable;
        public bool ElectricTrainSupplyOn => false;
        public bool FrontElectricTrainSupplyCableConnected { get => false; set { } }
        public float ElectricTrainSupplyPowerW => 0f;

        public bool ServiceRetentionButton => false;
        public bool ServiceRetentionCancellationButton => false;

        public SteamPowerSupply(MSTSSteamLocomotive locomotive)
        {
            Locomotive = locomotive;

            BatterySwitch = new BatterySwitch(Locomotive);
            MasterKey = new MasterKey(Locomotive);
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortsbattery(mode":
                case "engine(ortsbattery(delay":
                case "engine(ortsbattery(defaulton":
                    BatterySwitch.Parse(lowercasetoken, stf);
                    break;
                case "engine(ortsmasterkey(mode":
                case "engine(ortsmasterkey(delayoff":
                case "engine(ortsmasterkey(headlightcontrol":
                    MasterKey.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public void Copy(IPowerSupply source)
        {
            if (source is SteamPowerSupply steamOther)
            {
                BatterySwitch.Copy(steamOther.BatterySwitch);
                MasterKey.Copy(steamOther.MasterKey);
            }
        }

        public void Initialize()
        {
            BatterySwitch.Initialize();
            MasterKey.Initialize();
        }

        public virtual void InitializeMoving()
        {
            BatterySwitch.InitializeMoving();
            MasterKey.InitializeMoving();
        }


        public async ValueTask<PowerSupplySaveState> Snapshot()
        {
            return new PowerSupplySaveState()
            {
                BatterySwitchState = await BatterySwitch.Snapshot().ConfigureAwait(false),
                MasterKeyState = await MasterKey.Snapshot().ConfigureAwait(false),
            };
        }

        public async ValueTask Restore(PowerSupplySaveState saveState)
        {
            ArgumentNullException.ThrowIfNull(saveState, nameof(saveState));

            await BatterySwitch.Restore(saveState.BatterySwitchState).ConfigureAwait(false);
            await MasterKey.Restore(saveState.MasterKeyState).ConfigureAwait(false);
        }

        public void Update(double elapsedClockSeconds)
        {
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            BatterySwitch.HandleEvent(evt);
            MasterKey.HandleEvent(evt);
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
        }

        public void HandleEventFromLeadLocomotive(PowerSupplyEvent evt)
        {
        }

        public void HandleEventFromLeadLocomotive(PowerSupplyEvent evt, int id)
        {
        }
    }
}
