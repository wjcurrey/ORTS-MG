// COPYRIGHT 2020 by the Open Rails project.
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
using System.IO;
using System.Threading.Tasks;

using FreeTrainSimulator.Common;
using FreeTrainSimulator.Common.Api;
using FreeTrainSimulator.Models.Imported.State;

using Orts.Formats.Msts.Parsers;
using Orts.Scripting.Api;
using Orts.Scripting.Api.PowerSupply;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedCircuitBreaker : ISubSystem<ScriptedCircuitBreaker>, ISaveStateApi<CircuitBreakerSaveState>
    {
        public ScriptedLocomotivePowerSupply PowerSupply { get; protected set; }
        public MSTSLocomotive Locomotive => PowerSupply.Locomotive;

        private bool activated;
        private string ScriptName = "Automatic";
        private CircuitBreaker script;

        private float delayTimer;

        public CircuitBreakerState State { get; private set; } = CircuitBreakerState.Open;
        public bool DriverClosingOrder { get; private set; }
        public bool DriverOpeningOrder { get; private set; }
        public bool DriverClosingAuthorization { get; private set; }
        public bool TCSClosingOrder
        {
            get
            {
                if (Locomotive.Train.LeadLocomotive is MSTSLocomotive locomotive)
                    return locomotive.TrainControlSystem.CircuitBreakerClosingOrder;
                else
                    return false;
            }
        }
        public bool TCSOpeningOrder
        {
            get
            {
                if (Locomotive.Train.LeadLocomotive is MSTSLocomotive locomotive)
                    return locomotive.TrainControlSystem.CircuitBreakerOpeningOrder;
                else
                    return false;
            }
        }
        public bool TCSClosingAuthorization
        {
            get
            {
                if (Locomotive.Train.LeadLocomotive is MSTSLocomotive locomotive)
                    return locomotive.TrainControlSystem.PowerAuthorization;
                else
                    return false;
            }
        }
        public bool ClosingAuthorization { get; private set; }

        public ScriptedCircuitBreaker(ScriptedLocomotivePowerSupply powerSupply)
        {
            PowerSupply = powerSupply;
        }

        public void Copy(ScriptedCircuitBreaker source)
        {
            ScriptName = source.ScriptName;
            State = source.State;
            delayTimer = source.delayTimer;
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortscircuitbreaker":
                    ScriptName = stf.ReadStringBlock(null);
                    break;

                case "engine(ortscircuitbreakerclosingdelay":
                    delayTimer = stf.ReadFloatBlock(STFReader.Units.Time, null);
                    break;
            }
        }

        public void Initialize()
        {
            if (!activated)
            {
                if (ScriptName != null)
                {
                    switch(ScriptName)
                    {
                        case "Automatic":
                            script = new AutomaticCircuitBreaker() as CircuitBreaker;
                            break;

                        case "Manual":
                            script = new ManualCircuitBreaker() as CircuitBreaker;
                            break;

                        default:
                            script = Simulator.Instance.ScriptManager.Load(Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script"), ScriptName) as CircuitBreaker;
                            break;
                    }
                }
                // Fallback to automatic circuit breaker if the above failed.
                if (script == null)
                {
                    script = new AutomaticCircuitBreaker() as CircuitBreaker;
                }

                // AbstractScriptClass
                script.ClockTime = () => Simulator.Instance.ClockTime;
                script.GameTime = () => Simulator.Instance.GameTime;
                script.PreUpdate = () => Simulator.Instance.PreUpdate;
                script.DistanceM = () => Locomotive.DistanceTravelled;
                script.SpeedMpS = () => Math.Abs(Locomotive.SpeedMpS);
                script.Confirm = Simulator.Instance.Confirmer.Confirm;
                script.Message = Simulator.Instance.Confirmer.Message;
                script.SignalEvent = Locomotive.SignalEvent;
                script.SignalEventToTrain = (evt) =>
                {
                    if (Locomotive.Train != null)
                    {
                        Locomotive.Train.SignalEvent(evt);
                    }
                };

                // TractionCutOffSubsystem getters
                script.SupplyType = () => PowerSupply.Type;
                script.CurrentState = () => State;
                script.CurrentPantographState = () => Locomotive?.Pantographs.State ?? PantographState.Unavailable;
                script.CurrentDieselEngineState = () => (Locomotive as MSTSDieselLocomotive)?.DieselEngines.State ?? DieselEngineState.Unavailable;
                script.CurrentPowerSupplyState = () => PowerSupply.MainPowerSupplyState;
                script.DriverClosingOrder = () => DriverClosingOrder;
                script.DriverOpeningOrder = () => DriverOpeningOrder;
                script.DriverClosingAuthorization = () => DriverClosingAuthorization;
                script.TCSClosingAuthorization = () => TCSClosingAuthorization;
                script.ClosingAuthorization = () => ClosingAuthorization;
                script.IsLowVoltagePowerSupplyOn = () => PowerSupply.LowVoltagePowerSupplyOn;
                script.IsCabPowerSupplyOn = () => PowerSupply.CabPowerSupplyOn;
                script.ClosingDelayS = () => delayTimer;

                // TractionCutOffSubsystem setters
                script.SetDriverClosingOrder = (value) => DriverClosingOrder = value;
                script.SetDriverOpeningOrder = (value) => DriverOpeningOrder = value;
                script.SetDriverClosingAuthorization = (value) => DriverClosingAuthorization = value;
                script.SetClosingAuthorization = (value) => ClosingAuthorization = value;

                // CircuitBreaker getters
                script.CurrentState = () => State;
                script.TCSClosingOrder = () => TCSClosingOrder;
                script.TCSOpeningOrder = () => TCSOpeningOrder;

                // CircuitBreaker setters
                script.SetCurrentState = (value) =>
                {
                    State = value;
                    TCSEvent CircuitBreakerEvent = State == CircuitBreakerState.Closed ? TCSEvent.CircuitBreakerClosed : TCSEvent.CircuitBreakerOpen;
                    Locomotive.TrainControlSystem.HandleEvent(CircuitBreakerEvent);
                };

                script.Initialize();
                activated = true;
            }
        }

        public void InitializeMoving()
        {
            script?.InitializeMoving();

            State = CircuitBreakerState.Closed;
        }

        public void Update(double elapsedClockSeconds)
        {
            if (Locomotive.Train.TrainType == TrainType.Ai || Locomotive.Train.TrainType == TrainType.AiAutoGenerated
                || Locomotive.Train.TrainType == TrainType.AiPlayerHosting)
            {
                State = CircuitBreakerState.Closed;
            }
            else
            {
                if (script != null)
                {
                    script.Update(elapsedClockSeconds);
                }
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            if (script != null)
            {
                script.HandleEvent(evt);
            }
        }

        public ValueTask<CircuitBreakerSaveState> Snapshot()
        {
            return ValueTask.FromResult(new CircuitBreakerSaveState()
            {
                ScriptName = ScriptName,
                DelayTimer = delayTimer,
                CircuitBreakerState = State,
                DriverClosingOrder = DriverClosingOrder,
                DriverOpeningOrder = DriverOpeningOrder,
                DriverClosingAuthorization = DriverClosingAuthorization,
                ClosingAuthorization = ClosingAuthorization,
            });
        }

        public ValueTask Restore(CircuitBreakerSaveState saveState)
        {
            ArgumentNullException.ThrowIfNull(saveState, nameof(saveState));

            ScriptName = saveState.ScriptName;
            delayTimer = (float)saveState.DelayTimer;
            State = saveState.CircuitBreakerState;
            DriverClosingOrder = saveState.DriverClosingOrder;
            DriverOpeningOrder = saveState.DriverOpeningOrder;
            DriverClosingAuthorization = saveState.DriverClosingAuthorization;
            ClosingAuthorization = saveState.ClosingAuthorization;

            return ValueTask.CompletedTask;
        }
    }

    internal class AutomaticCircuitBreaker : CircuitBreaker
    {
        private Timer ClosingTimer;
        private CircuitBreakerState PreviousState;

        public override void Initialize()
        {
            ClosingTimer = new Timer(this);
            ClosingTimer.Setup(ClosingDelayS());

            SetDriverClosingOrder(false);
            SetDriverOpeningOrder(false);
            SetDriverClosingAuthorization(true);
        }

        public override void Update(double elapsedSeconds)
        {
            SetClosingAuthorization(TCSClosingAuthorization() && CurrentPantographState() == PantographState.Up);

            switch (CurrentState())
            {
                case CircuitBreakerState.Closed:
                    if (!ClosingAuthorization())
                    {
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Closing:
                    if (ClosingAuthorization())
                    {
                        if (!ClosingTimer.Started)
                        {
                            ClosingTimer.Start();
                        }

                        if (ClosingTimer.Triggered)
                        {
                            ClosingTimer.Stop();
                            SetCurrentState(CircuitBreakerState.Closed);
                        }
                    }
                    else
                    {
                        ClosingTimer.Stop();
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Open:
                    if (ClosingAuthorization())
                    {
                        SetCurrentState(CircuitBreakerState.Closing);
                    }
                    break;
            }

            if (PreviousState != CurrentState())
            {
                switch (CurrentState())
                {
                    case CircuitBreakerState.Open:
                        SignalEvent(TrainEvent.CircuitBreakerOpen);
                        break;

                    case CircuitBreakerState.Closing:
                        SignalEvent(TrainEvent.CircuitBreakerClosing);
                        break;

                    case CircuitBreakerState.Closed:
                        SignalEvent(TrainEvent.CircuitBreakerClosed);
                        break;
                }
            }

            PreviousState = CurrentState();
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            // Nothing to do since it is automatic
        }
    }

    internal class ManualCircuitBreaker : CircuitBreaker
    {
        private Timer ClosingTimer;
        private CircuitBreakerState PreviousState;

        public override void Initialize()
        {
            ClosingTimer = new Timer(this);
            ClosingTimer.Setup(ClosingDelayS());

            SetDriverClosingAuthorization(true);
        }

        public override void Update(double elapsedSeconds)
        {
            SetClosingAuthorization(TCSClosingAuthorization() && CurrentPantographState() == PantographState.Up);

            switch (CurrentState())
            {
                case CircuitBreakerState.Closed:
                    if (!ClosingAuthorization() || DriverOpeningOrder())
                    {
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Closing:
                    if (ClosingAuthorization() && DriverClosingOrder())
                    {
                        if (!ClosingTimer.Started)
                        {
                            ClosingTimer.Start();
                        }

                        if (ClosingTimer.Triggered)
                        {
                            ClosingTimer.Stop();
                            SetCurrentState(CircuitBreakerState.Closed);
                        }
                    }
                    else
                    {
                        ClosingTimer.Stop();
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Open:
                    if (ClosingAuthorization() && DriverClosingOrder())
                    {
                        SetCurrentState(CircuitBreakerState.Closing);
                    }
                    break;
            }

            if (PreviousState != CurrentState())
            {
                switch (CurrentState())
                {
                    case CircuitBreakerState.Open:
                        SignalEvent(TrainEvent.CircuitBreakerOpen);
                        break;

                    case CircuitBreakerState.Closing:
                        SignalEvent(TrainEvent.CircuitBreakerClosing);
                        break;

                    case CircuitBreakerState.Closed:
                        SignalEvent(TrainEvent.CircuitBreakerClosed);
                        break;
                }
            }

            PreviousState = CurrentState();
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.CloseCircuitBreaker:
                    SetDriverClosingOrder(true);
                    SetDriverOpeningOrder(false);
                    SignalEvent(TrainEvent.CircuitBreakerClosingOrderOn);

                    Confirm(CabControl.CircuitBreakerClosingOrder, CabSetting.On);
                    if (!ClosingAuthorization())
                    {
                        Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Circuit breaker closing not authorized"));
                    }
                    break;

                case PowerSupplyEvent.OpenCircuitBreaker:
                    SetDriverClosingOrder(false);
                    SetDriverOpeningOrder(true);
                    SignalEvent(TrainEvent.CircuitBreakerClosingOrderOff);

                    Confirm(CabControl.CircuitBreakerClosingOrder, CabSetting.Off);
                    break;
            }
        }
    }
}
