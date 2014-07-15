﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MSTS.Parsers;
using ORTS.Scripting.Api;

namespace ORTS
{
    public class Pantographs
    {
        readonly MSTSWagon Wagon;

        public List<Pantograph> List = new List<Pantograph>();

        public Pantographs(MSTSWagon wagon)
        {
            Wagon = wagon;
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {

        }

        public void Copy(Pantographs pantographs)
        {
            List.Clear();

            foreach (Pantograph pantograph in pantographs.List)
            {
                List.Add(new Pantograph(Wagon));
                List.Last().Copy(pantograph);
            }
        }

        public void Restore(BinaryReader inf)
        {
            List.Clear();

            int n = inf.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                List.Add(new Pantograph(Wagon));
                List.Last().Restore(inf);
            }
        }

        public void Initialize()
        {
            foreach (Pantograph pantograph in List)
            {
                pantograph.Initialize();
            }
        }

        public void Update(float elapsedClockSeconds)
        {
            foreach (Pantograph pantograph in List)
            {
                pantograph.Update(elapsedClockSeconds);
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            foreach (Pantograph pantograph in List)
            {
                pantograph.HandleEvent(evt);
            }
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
            if (id <= List.Count)
            {
                List[id - 1].HandleEvent(evt);
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(List.Count());
            foreach (Pantograph pantograph in List)
            {
                pantograph.Save(outf);
            }
        }

        #region ListManipulation

        public void Add(Pantograph pantograph)
        {
            List.Add(pantograph);
        }

        public int Count { get { return List.Count; } }

        public Pantograph this[int i]
        {
            get { return List[i - 1]; }
            set { List[i - 1] = value; }
        }

        #endregion

        public PantographState State
        {
            get
            {
                PantographState state = PantographState.Down;

                foreach (Pantograph pantograph in List)
                {
                    if (pantograph.State > state)
                        state = pantograph.State;
                }

                return state;
            }
        }
    }

    public class Pantograph
    {
        readonly MSTSWagon Wagon;

        public PantographState State { get; private set; }
        public float DelayS { get; private set; }
        public float TimeS { get; private set; }
        public bool CommandUp {
            get
            {
                bool value;

                switch (State)
                {
                    default:
                    case PantographState.Down:
                    case PantographState.Lowering:
                        value = false;
                        break;

                    case PantographState.Up:
                    case PantographState.Raising:
                        value = true;
                        break;
                }

                return value;
            }
        }
        private int Id
        {
            get
            {
                return Wagon.Pantographs.List.IndexOf(this) + 1;
            }
        }

        public Pantograph(MSTSWagon wagon)
        {
            Wagon = wagon;

            State = PantographState.Down;
            DelayS = 0;
            TimeS = 0;
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {

        }

        public void Copy(Pantograph pantograph)
        {
            State = pantograph.State;
            DelayS = pantograph.DelayS;
            TimeS = pantograph.TimeS;
        }

        public void Restore(BinaryReader inf)
        {
            State = (PantographState) Enum.Parse(typeof(PantographState), inf.ReadString());
            DelayS = inf.ReadSingle();
            TimeS = inf.ReadSingle();
        }

        public void Initialize()
        {

        }

        public void Update(float elapsedClockSeconds)
        {
            switch (State)
            {
                case PantographState.Lowering:
                    TimeS -= elapsedClockSeconds;

                    if (TimeS < 0)
                    {
                        TimeS = 0;
                        State = PantographState.Down;
                    }
                    break;

                case PantographState.Raising:
                    TimeS += elapsedClockSeconds;

                    if (TimeS > DelayS)
                    {
                        TimeS = DelayS;
                        State = PantographState.Up;
                    }
                    break;
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            Event soundEvent = Event.None;

            switch (evt)
            {
                case PowerSupplyEvent.LowerPantograph:
                    if (State == PantographState.Up || State == PantographState.Raising)
                    {
                        State = PantographState.Lowering;

                        switch (Id)
                        {
                            default:
                            case 1:
                                soundEvent = Event.Pantograph1Down;
                                break;

                            case 2:
                                soundEvent = Event.Pantograph2Down;
                                break;
                        }
                    }

                    break;

                case PowerSupplyEvent.RaisePantograph:
                    if (State == PantographState.Down || State == PantographState.Lowering)
                    {
                        State = PantographState.Raising;

                        switch (Id)
                        {
                            default:
                            case 1:
                                soundEvent = Event.Pantograph1Up;
                                break;

                            case 2:
                                soundEvent = Event.Pantograph2Up;
                                break;
                        }
                    }
                    break;
            }

            if (soundEvent != Event.None)
            {
                foreach (var eventHandler in Wagon.EventHandlers)
                {
                    eventHandler.HandleEvent(soundEvent);
                }
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(State.ToString());
            outf.Write(DelayS);
            outf.Write(TimeS);
        }
    }
}
