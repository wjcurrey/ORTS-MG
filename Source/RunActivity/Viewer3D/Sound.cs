﻿/// ORTS SOUND SYSTEM
/// 
/// Sounds are generated by SoundSource objects.   All sound-making items, ie scenery, railcars, etc 
/// create a SoundSource object, passing it the MSTS SMS file that specifies the sound.
/// SoundSource objects
///  - have a physical location in the world, 
///  - assume the listener is located at the same location as the 3D viewer
///  - may be attached to a railcar in which case it moves with the car
///  - railcar-attached sounds can poll control variables in the simulator
///  - have one or more SoundStreams
///  SoundStreams
///  - can play only one sound at a time
///  - the sound played is controlled by the various triggers
///  SoundTriggers
///  - defined in the SMS file
///  - triggered by various events
///  - when triggered, executes a SoundCommand
///  SoundCommands
///  - used by triggers to control the SoundStream
///  - ie play a sound, stop a sound etc
///  
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using IrrKlang;
using System.IO;


namespace ORTS
{

/////////////////////////////////////////////////////////
/// SOUND SOURCE
/////////////////////////////////////////////////////////

    
    public class SoundSource 
    {
        /// <summary>
        /// Construct a SoundSource attached to a train car.
        /// </summary>
        /// <param name="viewer"></param>
        /// <param name="car"></param>
        /// <param name="smsFilePath"></param>
        public SoundSource(Viewer3D viewer, MSTSWagon car, string smsFilePath)
        {
            Car = car;
            Initialize(viewer, car.WorldPosition.WorldLocation, smsFilePath);
        }

        /// <summary>
        /// Construct a SoundSource stationary at the specified worldLocation
        /// </summary>
        /// <param name="viewer"></param>
        /// <param name="worldLocation"></param>
        /// <param name="smsFilePath"></param>
        public SoundSource(Viewer3D viewer, WorldLocation worldLocation, string smsFilePath)
        {
            Initialize(viewer, worldLocation, smsFilePath);
        }


        
        public WorldLocation WorldLocation;   // current location for the sound source
        public Viewer3D Viewer;                 // the listener is connected to this viewer
        public MSTSWagon Car = null;          // the sound may be from a train car

        public string SMSFolder;              // the wave files will be relative to this folder
        public bool Active = false;
        private MSTS.Activation ActivationConditions;
        private MSTS.Deactivation DeactivationConditions;

        List<SoundStream> SoundStreams = new List<SoundStream>();

        public void Initialize(Viewer3D viewer, WorldLocation worldLocation, string smsFilePath)
        {
            Viewer = viewer;
            WorldLocation = worldLocation;
            SMSFolder = Path.GetDirectoryName(smsFilePath);
            MSTS.SMSFile smsFile = MSTS.SharedSMSFileManager.Get(smsFilePath);


            // find correct ScalabiltyGroup
            int iSG = 0;
            while ( iSG < smsFile.Tr_SMS.ScalabiltyGroups.Count)
                {
            
                if (smsFile.Tr_SMS.ScalabiltyGroups[iSG].DetailLevel <= Viewer.SoundDetailLevel)
                {
                    break;
                }

                ++iSG;
            }
            if (iSG < smsFile.Tr_SMS.ScalabiltyGroups.Count)  // else we want less sound so don't provide any
            {
                MSTS.ScalabiltyGroup mstsScalabiltyGroup = smsFile.Tr_SMS.ScalabiltyGroups[iSG];

                ActivationConditions = mstsScalabiltyGroup.Activation;
                DeactivationConditions = mstsScalabiltyGroup.Deactivation;

                foreach (MSTS.SMSStream mstsStream in mstsScalabiltyGroup.Streams)
                    SoundStreams.Add(new SoundStream(mstsStream, this));
            }
        }

        public void Update(ElapsedTime elapsedTime)
        {
            if (!Active)
            {
                if (Activate())
                {
                    Active = true;

                    // run the initial triggers
                    foreach( SoundStream stream in SoundStreams )
                        foreach (ORTSTrigger trigger in stream.Triggers)
                            trigger.Initialize();

                    // restore any looping sounds
                    foreach( SoundStream stream in SoundStreams )
                        stream.Activate();
                }
            }
            else
            {
                if (DeActivate())
                {
                    foreach (SoundStream stream in SoundStreams)
                        stream.Deactivate();

                    Active = false;
                }
            }

            if (Car != null)
            {
                WorldLocation = Car.WorldPosition.WorldLocation;
            }

            if (Active)
            {
                // update the sound position relative to the listener
                Vector3 RelativePosition = WorldLocation.Location;
                RelativePosition.X += 2048 * (WorldLocation.TileX - Viewer.Camera.TileX);
                RelativePosition.Z += 2048 * (WorldLocation.TileZ - Viewer.Camera.TileZ);

                Vector3 XNARelativePosition = new Vector3(RelativePosition.X, RelativePosition.Y, -RelativePosition.Z);
                XNARelativePosition = Vector3.Transform(XNARelativePosition, Viewer.Camera.XNAView);

                foreach (SoundStream stream in SoundStreams)
                    stream.Update( new Vector3D(XNARelativePosition.X / 10, XNARelativePosition.Y / 10, XNARelativePosition.Z / 10));
            }
        } // Update

        /// <summary>
        /// Return true if activation conditions are met,
        /// ie PassengerCam, CabCam, Distance etc
        /// </summary>
        /// <returns></returns>
        public bool Activate()
        {
            if (ConditionsMet(ActivationConditions))
            {
                float distanceSquared = WorldLocation.DistanceSquared(WorldLocation, Viewer.Camera.WorldLocation);
                if (distanceSquared < ActivationConditions.Distance * ActivationConditions.Distance)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Return true if deactivation conditions are met
        /// ie PassengerCam, CabCam, Distance etc
        /// </summary>
        /// <returns></returns>
        public bool DeActivate()
        {
            if (ConditionsMet(DeactivationConditions))
                return true;

            float distanceSquared = WorldLocation.DistanceSquared(WorldLocation, Viewer.Camera.WorldLocation);
            if (distanceSquared > DeactivationConditions.Distance * DeactivationConditions.Distance)
                return true;

            return false;
        }

        /// <summary>
        /// Return true of the ViewPoint matches any of the ones specified in the conditions
        /// for activation or deactivation.
        /// </summary>
        /// <param name="conditions"></param>
        /// <returns></returns>
        private bool ConditionsMet(MSTS.Activation conditions)
        {
            if (conditions.CabCam && Viewer.Camera.ViewPoint == Camera.ViewPoints.Cab)
                return true;
            if (conditions.PassengerCam && Viewer.Camera.ViewPoint == Camera.ViewPoints.Passenger)
                return true;
            if (conditions.ExternalCam && Viewer.Camera.ViewPoint == Camera.ViewPoints.External)
                return true;

            return false;
        }

    }

/////////////////////////////////////////////////////////
/// SOUND STREAM
/////////////////////////////////////////////////////////
        
    public class SoundStream
    {
        public SoundSource SoundSource;
        public float Volume
        {
            get { return volume / MSTSStream.Volume; }
            set { volume = value * MSTSStream.Volume;  if (ISound != null) ISound.Volume = volume; }
        }
        private float volume = 1;

        public List<ORTSTrigger> Triggers = new List<ORTSTrigger>();

        protected MSTS.SMSStream MSTSStream;

        private ISound ISound = null;
        private float SampleRate; // ie 11025 - set by play command
        private  ISoundSource RepeatingSound = null; // allows us to reactivate
        
        public SoundStream( MSTS.SMSStream mstsStream, SoundSource soundSource )
        {
            SoundSource = soundSource;
            MSTSStream = mstsStream;

            if (mstsStream.Triggers != null) 
                foreach( MSTS.Trigger trigger in mstsStream.Triggers )
                {
                    if (trigger.SoundCommand == null) // ignore improperly formed SMS files
                    {
                        Triggers.Add(new ORTSTrigger()); // null trigger
                    }
                    else if (trigger.GetType() == typeof(MSTS.Dist_Travelled_Trigger) && soundSource.Car != null )
                    {
                        Triggers.Add(new ORTSDistanceTravelledTrigger(this, (MSTS.Dist_Travelled_Trigger)trigger));
                    }
                    else if (trigger.GetType() == typeof(MSTS.Initial_Trigger))
                    {
                        Triggers.Add(new ORTSInitialTrigger(this, (MSTS.Initial_Trigger)trigger));
                    }
                    else if (trigger.GetType() == typeof(MSTS.Random_Trigger))
                    {
                        Triggers.Add(new ORTSRandomTrigger(this, (MSTS.Random_Trigger)trigger));
                    }
                    else if (trigger.GetType() == typeof(MSTS.Variable_Trigger) && soundSource.Car != null )
                    {
                        Triggers.Add(new ORTSVariableTrigger(this, (MSTS.Variable_Trigger)trigger));
                    }
                    else if (trigger.GetType() == typeof(MSTS.Discrete_Trigger) && soundSource.Car != null )
                    {
                        ORTSDiscreteTrigger ortsTrigger = new ORTSDiscreteTrigger(this, (MSTS.Discrete_Trigger)trigger);
                        Triggers.Add( ortsTrigger );  // list them here so we can enable and disable 
                        SoundSource.Car.EventHandlers.Add(ortsTrigger);  // tell the simulator to call us when the event occurs
                    }
                }  // for each mstsStream.Trigger
        }

        /// <summary>
        /// Update frequency and volume relative to curves
        /// Position is in IRRKLANG space relative to listener
        /// </summary>
        public void Update( IrrKlang.Vector3D IRRposition )
        {
            foreach (ORTSTrigger trigger in Triggers)
                trigger.TryTrigger();


            if (ISound != null)
            {
                ISound.Position = IRRposition;
            }

            MSTSWagon car = SoundSource.Car;
            if (car != null && ISound != null)
            {
                if (MSTSStream.FrequencyCurve != null)
                {
                    float x = ReadValue(MSTSStream.FrequencyCurve.Control, car);
                    float y = Interpolate(x, MSTSStream.FrequencyCurve.CurvePoints);
                    ISound.PlaybackSpeed = y / SampleRate;
                }
                if (MSTSStream.VolumeCurve != null)
                {
                    float x = ReadValue(MSTSStream.VolumeCurve.Control, car);
                    float y = Interpolate(x, MSTSStream.VolumeCurve.CurvePoints);
                    Volume = y;
                }
            }
        }

        /// <summary>
        /// There must be at least two points in the curve
        /// // TODO do we need to implement support for Granularity()
        /// </summary>
        /// <param name="x"></param>
        /// <param name="curvePoints"></param>
        /// <returns></returns>
        private float Interpolate(float x, MSTS.CurvePoint[] curvePoints)
        {
            int i = 1;
            while (i < curvePoints.Length - 1
                && curvePoints[i].X < x) ++i;
            // i points to the point equal to or above x, or to the last point in the table

            x -= curvePoints[i - 1].X;
            float rx = x / (curvePoints[i].X - curvePoints[i - 1].X);

            float dy = curvePoints[i].Y - curvePoints[i - 1].Y;

            float y = curvePoints[i - 1].Y + rx * dy;

            return y;
        }

        /// <summary>
        /// Read a variable from the car data in the simulator.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="car"></param>
        /// <returns></returns>
        private float ReadValue(MSTS.VolumeCurve.Controls control, MSTSWagon car)
        {
            switch (control)
            {
                case MSTS.VolumeCurve.Controls.DistanceControlled: return car.DistanceM;
                case MSTS.VolumeCurve.Controls.SpeedControlled: return Math.Abs(car.SpeedMpS);
                case MSTS.VolumeCurve.Controls.Variable1Controlled: return car.Variable1;
                case MSTS.VolumeCurve.Controls.Variable2Controlled: return car.Variable2;
                case MSTS.VolumeCurve.Controls.Variable3Controlled: return car.Variable3;
                default: return 0;
            }
        }

        public void Stop()
        {
            if (ISound != null)
            {
                ISound.Stop();
                ISound = null;
                RepeatingSound = null;
            }
        }

        /// <summary>
        /// Restore any previously playing sounds
        /// </summary>
        public void Activate()
        {
            float v = volume;
            if (RepeatingSound != null)
            {
                Play3D(true, RepeatingSound);
                volume = v;
                ISound.Volume = v;
            }
        }

        public void Deactivate()
        {
            if (ISound != null)
            {
                ISound.Stop();
                ISound = null;
            }
        }

        /// <summary>
        /// Play the specified sound 
        /// at the default volume.
        /// </summary>
        /// <param name="repeat"></param>
        /// <param name="iSoundSource"></param>
        public void Play3D( bool repeat, IrrKlang.ISoundSource iSoundSource )
        {

            if (ISound != null)
                Stop();

            Viewer3D viewer = SoundSource.Viewer;

            // position relative to camera
            WorldLocation worldLocation = SoundSource.WorldLocation;
            Vector3 location = worldLocation.Location;
            location.X += 2048 * (worldLocation.TileX - viewer.Camera.TileX);
            location.Z += 2048 * (worldLocation.TileZ - viewer.Camera.TileZ);
            location.Z *= -1;
            location = Vector3.Transform(location, viewer.Camera.XNAView);

            SampleRate = iSoundSource.AudioFormat.SampleRate;  // ie 11025
            if( viewer.SoundEngine != null )
                ISound = viewer.SoundEngine.Play3D(iSoundSource, location.X / 10, location.Y / 10, location.Z / 10, repeat, false, false);
            Volume = 1.0f;

            if (repeat)
                RepeatingSound = iSoundSource;  // remember this so we can reactivate if needed
            else
                RepeatingSound = null;
        }


    } // class ORTSStream



/////////////////////////////////////////////////////////
/// SOUND TRIGGERS
/////////////////////////////////////////////////////////

    public class ORTSTrigger
    {
        public bool Enabled = true;  // set by the DisableTrigger, EnableTrigger sound commands

        public virtual void TryTrigger() { }

        public virtual void Initialize() { }
    }


    /// <summary>
    /// Play this sound when a discrete TrainCar event occurs in the simulator
    /// </summary>
    public class ORTSDiscreteTrigger: ORTSTrigger, CarEventHandler
    {
        public EventID TriggerID;
        ORTSSoundCommand SoundCommand;

        public ORTSDiscreteTrigger(SoundStream soundStream, MSTS.Discrete_Trigger smsData)
        {
            TriggerID = (EventID)smsData.TriggerID;
            SoundCommand = ORTSSoundCommand.FromMSTS(smsData.SoundCommand, soundStream);
        }

        public void HandleCarEvent(EventID eventID)
        {
            if( Enabled && eventID == TriggerID)
                 SoundCommand.Run();
        }

    } // class ORTSDiscreteTrigger

    /// <summary>
    /// Play this sound controlled by the distance a TrainCar has travelled
    /// </summary>
    public class ORTSDistanceTravelledTrigger: ORTSTrigger
    {
        MSTS.Dist_Travelled_Trigger SMS;
        ORTSSoundCommand SoundCommand;
        float triggerDistance;
        TrainCar car;
        SoundStream SoundStream;

        public ORTSDistanceTravelledTrigger(SoundStream soundStream, MSTS.Dist_Travelled_Trigger smsData)
        {
            SoundStream = soundStream;
            car = soundStream.SoundSource.Car;
            SMS = smsData;
            SoundCommand = ORTSSoundCommand.FromMSTS(SMS.SoundCommand, soundStream );
            Initialize();
        }

        public override void Initialize()
        {
            UpdateTriggerDistance();
        }

        public override void TryTrigger()
        {
            if( car.DistanceM > triggerDistance )
            {
                if (Enabled)
                {
                    SoundCommand.Run();
                    float volume = (float)Program.Random.NextDouble() * (SMS.Volume_Max - SMS.Volume_Min) + SMS.Volume_Min;
                    SoundStream.Volume = volume;
                }
                UpdateTriggerDistance();
            }
        }

        private void UpdateTriggerDistance()
        {
                triggerDistance = car.DistanceM + ( (float)Program.Random.NextDouble() * (SMS.Dist_Max - SMS.Dist_Min) + SMS.Dist_Min );
        }

    } // class ORTSDistanceTravelledTrigger

    /// <summary>
    /// Play this sound immediately when this SoundSource becomes active
    /// </summary>
    public class ORTSInitialTrigger: ORTSTrigger
    {
        ORTSSoundCommand SoundCommand;

        public ORTSInitialTrigger(SoundStream soundStream, MSTS.Initial_Trigger smsData)
        {
            SoundCommand = ORTSSoundCommand.FromMSTS(smsData.SoundCommand, soundStream);
        }

        public override void Initialize()
        {
            if( Enabled )
                SoundCommand.Run();
        }

    }

    /// <summary>
    /// Play the sound at random times
    /// </summary>
    public class ORTSRandomTrigger: ORTSTrigger
    {
        ORTSSoundCommand SoundCommand;
        Simulator Simulator;
        MSTS.Random_Trigger SMS;
        double StartSeconds = 0.0;
        double triggerAtSeconds;
        SoundStream SoundStream;

        public ORTSRandomTrigger(SoundStream soundStream, MSTS.Random_Trigger smsData)
        {
            SoundStream = soundStream;
            SMS = smsData;
            Simulator = soundStream.SoundSource.Viewer.Simulator;
            SoundCommand = ORTSSoundCommand.FromMSTS(smsData.SoundCommand, soundStream);
            Initialize();
        }

        public override void  Initialize()
        {
            StartSeconds = Simulator.ClockTime;
            UpdateTriggerAtSeconds();
        }

        public override void TryTrigger()
        {
            if (Simulator.ClockTime > triggerAtSeconds)
            {
                if (Enabled)
                {
                    SoundCommand.Run();
                    float volume = (float)Program.Random.NextDouble() * (SMS.Volume_Max - SMS.Volume_Min) + SMS.Volume_Min;
                    SoundStream.Volume = volume;
                }
                UpdateTriggerAtSeconds();
            }
        }

        private void UpdateTriggerAtSeconds()
        {
            double interval = Program.Random.NextDouble() * (SMS.Delay_Max - SMS.Delay_Min) + SMS.Delay_Min;
            triggerAtSeconds = Simulator.ClockTime + interval;
        }

    }  // class RandomTrigger

    /// <summary>
    /// Control sounds based on TrainCar variables in the simulator 
    /// </summary>
    public class ORTSVariableTrigger: ORTSTrigger
    {
        MSTS.Variable_Trigger SMS;
        MSTSWagon car;
        ORTSSoundCommand SoundCommand;

        float StartValue;

        public ORTSVariableTrigger(SoundStream soundStream, MSTS.Variable_Trigger smsData)
        {
            SMS = smsData;
            car = soundStream.SoundSource.Car;
            SoundCommand = ORTSSoundCommand.FromMSTS(smsData.SoundCommand, soundStream);
            Initialize();
        }

        public override void  Initialize()
        {
 	        StartValue = 0;
        }

        public override void TryTrigger( )
        {
            float newValue = ReadValue();
            bool triggered = false;

            switch (SMS.Event)
            {
                case MSTS.Variable_Trigger.Events.Distance_Dec_Past:
                case MSTS.Variable_Trigger.Events.Speed_Dec_Past:
                case MSTS.Variable_Trigger.Events.Variable1_Dec_Past:
                case MSTS.Variable_Trigger.Events.Variable2_Dec_Past:
                case MSTS.Variable_Trigger.Events.Variable3_Dec_Past:
                    if (newValue < SMS.Threshold
                        && StartValue >= SMS.Threshold)
                        triggered = true;
                    break;
                case MSTS.Variable_Trigger.Events.Distance_Inc_Past:
                case MSTS.Variable_Trigger.Events.Speed_Inc_Past:
                case MSTS.Variable_Trigger.Events.Variable1_Inc_Past:
                case MSTS.Variable_Trigger.Events.Variable2_Inc_Past:
                case MSTS.Variable_Trigger.Events.Variable3_Inc_Past:
                    if (newValue > SMS.Threshold
                        && StartValue <= SMS.Threshold)
                        triggered = true;
                    break;
            }

            StartValue = newValue;
            if (triggered && Enabled )
            {
                SoundCommand.Run();
            }
        } // TryTrigger

        private float ReadValue()
        {
            switch (SMS.Event)
            {
                case MSTS.Variable_Trigger.Events.Distance_Dec_Past:
                case MSTS.Variable_Trigger.Events.Distance_Inc_Past:
                    return car.DistanceM;
                case MSTS.Variable_Trigger.Events.Speed_Dec_Past:
                case MSTS.Variable_Trigger.Events.Speed_Inc_Past:
                    return Math.Abs(car.SpeedMpS);
                case MSTS.Variable_Trigger.Events.Variable1_Dec_Past:
                case MSTS.Variable_Trigger.Events.Variable1_Inc_Past:
                    return car.Variable1;
                case MSTS.Variable_Trigger.Events.Variable2_Dec_Past:
                case MSTS.Variable_Trigger.Events.Variable2_Inc_Past:
                    return car.Variable2;
                case MSTS.Variable_Trigger.Events.Variable3_Dec_Past:
                case MSTS.Variable_Trigger.Events.Variable3_Inc_Past:
                    return car.Variable3;
                default:
                    return 0;
            }
        }

    }  // class VariableTrigger


/////////////////////////////////////////////////////////
/// SOUND COMMANDS
/////////////////////////////////////////////////////////
    

    /// <summary>
    /// Play a sound file once.
    /// </summary>
    public class ORTSPlayOneShot : ORTSSoundPlayCommand
    {
        public ORTSPlayOneShot(SoundStream ortsStream, MSTS.SoundPlayCommand mstsSoundPlayCommand)
            : base(ortsStream, mstsSoundPlayCommand)
        {
        }
        public override void Run()
        {
            Play3D(false);
        }
    } 

    /// <summary>
    /// Start a repeating sound
    /// </summary>
    public class ORTSStartLoop : ORTSSoundPlayCommand
    {
        public ORTSStartLoop( SoundStream ortsStream, MSTS.SoundPlayCommand mstsSoundPlayCommand )
            : base( ortsStream, mstsSoundPlayCommand )
        {
        }
        public override void  Run( )
        {
            Play3D( true);
        }
    } 

    /// <summary>
    /// Stop a repeating sound.
    /// </summary>
    public class ORTSReleaseLoopRelease : ORTSSoundCommand
    {
        public ORTSReleaseLoopRelease(SoundStream ortsStream)
            : base(ortsStream)
        {
        }
        public override void Run()
        {
            ORTSStream.Stop();
        }
    }

    /// <summary>
    /// Start a looping sound that uses repeat markers
    /// TODO - until we implement markers, this will start the sound as a simple one shot
    /// </summary>
    public class ORTSStartLoopRelease : ORTSPlayOneShot  
    {
        public ORTSStartLoopRelease(SoundStream ortsStream, MSTS.PlayOneShot mstsStartLoopRelease)
            : base(ortsStream, mstsStartLoopRelease)
        {
        }
    }

    /// <summary>
    /// Jump to the exit portion of a looping sound with repeat markers   
    /// </summary>
    public class ORTSReleaseLoopReleaseWithJump : ORTSSoundCommand
    {
        public ORTSReleaseLoopReleaseWithJump(SoundStream ortsStream)
            : base(ortsStream)
        {
        }

        public override void Run()
        {
            // TODO until we implement markers
            // we just ignore this command since we started as a PlayOneShot type sound it will end on its own
        }
    }

    /// <summary>
    /// Shut down this stream trigger 
    /// </summary>
    public class ORTSDisableTrigger : ORTSSoundCommand
    {
        int TriggerIndex;  // index into the stream's trigger list 

        public ORTSDisableTrigger(SoundStream ortsStream, MSTS.DisableTrigger smsData )
            : base(ortsStream)
        {
            TriggerIndex = smsData.TriggerID - 1;
        }

        public override void Run()
        {
            if (TriggerIndex >= 0 && TriggerIndex < ORTSStream.Triggers.Count)
                ORTSStream.Triggers[TriggerIndex].Enabled = false;
        }
    }

    /// <summary>
    /// Re-enable this stream trigger
    /// </summary>
    public class ORTSEnableTrigger : ORTSSoundCommand
    {
        int TriggerIndex;

        public ORTSEnableTrigger(SoundStream ortsStream, MSTS.DisableTrigger smsData)
            : base(ortsStream)
        {
            TriggerIndex = smsData.TriggerID - 1;
        }

        public override void Run()
        {
            if ( TriggerIndex >= 0 && TriggerIndex < ORTSStream.Triggers.Count)
                ORTSStream.Triggers[TriggerIndex].Enabled = true;
        }
    }

    /// <summary>
    /// Set Volume Command
    /// </summary>
    public class ORTSSetStreamVolume : ORTSSoundCommand
    {
        float Volume;

        public ORTSSetStreamVolume(SoundStream ortsStream, MSTS.SetStreamVolume smsData)
            : base(ortsStream)
        {
            Volume = smsData.Volume;
        }

        public override void Run()
        {
            ORTSStream.Volume = Volume;
        }
    }

    /// <summary>
    /// Used when the SMS file sound command is missing or malformed
    /// </summary>
    public class ORTSNoOp : ORTSSoundCommand
    {
        public ORTSNoOp()
            : base(null)
        {
        }
        public override void Run()
        {
        }
    }

    /// <summary>
    /// A base class for all sound commands
    /// Defines that they all have a stream and a 'run()' function
    /// </summary>
    public abstract class ORTSSoundCommand
    {
        protected SoundStream ORTSStream;

        public ORTSSoundCommand(SoundStream ortsStream)
        {
            ORTSStream = ortsStream;
        }

        public abstract void Run();


        /// <summary>
        /// Create a sound command based on the sound command variable in an SMS file.
        /// </summary>
        /// <param name="mstsSoundCommand"></param>
        /// <param name="soundStream"></param>
        /// <returns></returns>
        public static ORTSSoundCommand FromMSTS(MSTS.SoundCommand mstsSoundCommand, SoundStream soundStream)
        {
            if (mstsSoundCommand == null)
            {
                return new ORTSNoOp();
            }
            else if (mstsSoundCommand.GetType() == typeof(MSTS.PlayOneShot))
            {
                return new ORTSPlayOneShot(soundStream, (MSTS.PlayOneShot)mstsSoundCommand);
            }
            else if (mstsSoundCommand.GetType() == typeof(MSTS.StartLoop))
            {
                return new ORTSStartLoop(soundStream, (MSTS.StartLoop)mstsSoundCommand);
            }
            else if (mstsSoundCommand.GetType() == typeof(MSTS.StartLoopRelease))
            {
                return new ORTSStartLoopRelease(soundStream, (MSTS.StartLoopRelease)mstsSoundCommand);
            }
            else if (mstsSoundCommand.GetType() == typeof(MSTS.ReleaseLoopRelease))
            {
                return new ORTSReleaseLoopRelease(soundStream);
            }
            else if (mstsSoundCommand.GetType() == typeof(MSTS.ReleaseLoopReleaseWithJump))
            {
                return new ORTSReleaseLoopReleaseWithJump(soundStream);
            }
            else if (mstsSoundCommand.GetType() == typeof(MSTS.SetStreamVolume))
            {
                return new ORTSSetStreamVolume(soundStream, (MSTS.SetStreamVolume) mstsSoundCommand);
            }
            else if (mstsSoundCommand.GetType() == typeof(MSTS.DisableTrigger))
            {
                return new ORTSDisableTrigger(soundStream, (MSTS.DisableTrigger)mstsSoundCommand);
            }
            else if (mstsSoundCommand.GetType() == typeof(MSTS.EnableTrigger))
            {
                return new ORTSEnableTrigger(soundStream, (MSTS.EnableTrigger)mstsSoundCommand);
            }
            throw new System.Exception("Unexpected soundCommand type " + mstsSoundCommand.GetType().ToString() + " in " + soundStream.SoundSource.SMSFolder );
        }

    }// ORTSSoundCommand

    /// <summary>
    /// A base class for commands that play a sound.
    /// Provides for selecting the sound from multiple files
    /// using a random or sequential selection strategy.
    /// </summary>
    public abstract class ORTSSoundPlayCommand : ORTSSoundCommand
    {
        protected String[] Files;
        protected MSTS.SoundCommand.SelectionMethods SelectionMethod;
        protected int iFile = 0;

        public ORTSSoundPlayCommand(SoundStream ortsStream, MSTS.SoundPlayCommand mstsSoundPlayCommand)
            : base(ortsStream)
        {
            Files = mstsSoundPlayCommand.Files;
            SelectionMethod = mstsSoundPlayCommand.SelectionMethod;
        }

        protected void Play3D( bool repeat)
        {
            if (SelectionMethod == MSTS.SoundCommand.SelectionMethods.SequentialSelection)
            {
                ++iFile;
                if (iFile >= Files.Length)
                    iFile = 0;
            }
            else if (SelectionMethod == MSTS.SoundCommand.SelectionMethods.RandomSelection)
            {
                iFile = Program.Random.Next(Files.Length);
            }

            string filePath = ORTSStream.SoundSource.SMSFolder + @"\" + Files[iFile];
            if (File.Exists(filePath) && ORTSStream.SoundSource.Viewer.SoundEngine != null )
            {
                IrrKlang.ISoundSource iSoundSource = ORTSStream.SoundSource.Viewer.SoundEngine.GetSoundSource(ORTSStream.SoundSource.SMSFolder + @"\" + Files[iFile], true);
                ORTSStream.Play3D(repeat, iSoundSource);
            }
        }
    } // ORTSSoundPlayCommand 

}

