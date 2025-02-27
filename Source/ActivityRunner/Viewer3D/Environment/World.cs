﻿// COPYRIGHT 2012, 2013 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

using FreeTrainSimulator.Common;
using FreeTrainSimulator.Common.Diagnostics;
using FreeTrainSimulator.Common.Position;

using Microsoft.Xna.Framework;

using Orts.ActivityRunner.Viewer3D.RollingStock.SubSystems;
using Orts.ActivityRunner.Viewer3D.Sound;
using Orts.Simulation;

namespace Orts.ActivityRunner.Viewer3D.Environment
{
    public class World
    {
        private readonly Viewer viewer;
        private readonly int initialViewingDistance;
        private readonly int initialDetailLevelBias;
        private Tile tile;
        private Tile visibleTile;
        private bool performanceTune;
        private bool markSweepError;

        public WeatherControl WeatherControl;
        public SkyViewer Sky { get; }
        public MSTSSkyDrawer MSTSSky { get; }
        public PrecipitationViewer Precipitation { get; }
        public TerrainViewer Terrain { get; }
        public SceneryDrawer Scenery { get; }
        public TrainDrawer Trains { get; }
        public RoadCarViewer RoadCars { get; }
        public ContainersViewer Containers { get; }
        public SoundSource GameSounds { get; }
        public WorldSounds Sounds { get; }

        public World(Viewer viewer, double gameTime)
        {
            this.viewer = viewer;
            initialViewingDistance = this.viewer.UserSettings.ViewingDistance;
            initialDetailLevelBias = this.viewer.UserSettings.DetailLevelBias;
            // Control stuff first.
            // check if weather file is defined
            WeatherControl = string.IsNullOrEmpty(viewer.Simulator.UserWeatherFile)
                ? new WeatherControl(viewer)
                : new AutomaticWeatherControl(viewer, viewer.Simulator.UserWeatherFile, gameTime);
            // Then drawers.
            if (viewer.UserSettings.MstsEnvironment)
                MSTSSky = new MSTSSkyDrawer(viewer);
            else
                Sky = new SkyViewer(viewer);
            Precipitation = new PrecipitationViewer(viewer, WeatherControl);
            Terrain = new TerrainViewer(viewer);
            Scenery = new SceneryDrawer(viewer);
            Trains = new TrainDrawer(viewer);
            RoadCars = new RoadCarViewer(viewer);
            Containers = new ContainersViewer(viewer);
            // Then sound.
            if (viewer.UserSettings.SoundDetailLevel > 0)
            {
                // Keep it silent while loading.
                ALSoundSource.MuteAll();
                // TODO: This looks kinda evil; do something about it.
                GameSounds = new SoundSource(SoundEventSource.InGame, Simulator.Instance.RouteFolder.SoundFile("ingame.sms"), true);
                this.viewer.SoundProcess.AddSoundSources(GameSounds.SMSFolder + "\\" + GameSounds.SMSFileName, new Collection<SoundSourceBase>() { GameSounds });
                Sounds = new WorldSounds(viewer);
            }
        }

        public void Load()
        {
            Terrain.Load();
            Scenery.Load();
            Trains.Load();
            RoadCars.Load();
            Containers.Load();
            if (tile != visibleTile)
            {
                tile = visibleTile;
                try
                {
                    viewer.ShapeManager.Mark();
                    viewer.MaterialManager.Mark();
                    viewer.TextureManager.Mark();
                    viewer.SignalTypeDataManager.Mark();
                    if (viewer.UserSettings.MstsEnvironment)
                        MSTSSky.Mark();
                    else
                        Sky.Mark();
                    Precipitation.Mark();
                    Terrain.Mark();
                    Scenery.Mark();
                    Trains.Mark();
                    RoadCars.Mark();
                    Containers.Mark();
                    viewer.ShapeManager.Sweep();
                    viewer.MaterialManager.Sweep();
                    viewer.TextureManager.Sweep();
                    viewer.SignalTypeDataManager.Sweep();
                }
                catch (Exception error) when (!markSweepError)
                {
                    Trace.WriteLine(error);
                    markSweepError = true;
                }
            }
        }

        public void Unload()
        {
            viewer.UserSettings.ViewingDistance = initialViewingDistance;
            viewer.UserSettings.DetailLevelBias = initialDetailLevelBias;
        }

        public void Update(in ElapsedTime elapsedTime)
        {
            if (performanceTune && viewer.Game.IsActive)
            {
                // Work out how far we need to change the actual FPS to get to the target.
                //   +ve = under-performing/too much detail
                //   -ve = over-performing/not enough detail
                var fpsTarget = viewer.UserSettings.PerformanceTunerTarget - MetricCollector.Instance.Metrics[SlidingMetric.FrameRate].SmoothedValue;

                // If vertical sync is on, we're capped to 60 FPS. This means we need to shift a target of 60FPS down to 57FPS.
                if (viewer.UserSettings.VerticalSync && viewer.UserSettings.PerformanceTunerTarget > 55)
                    fpsTarget -= 3;

                // Summarise the FPS adjustment to: +1 (add detail), 0 (keep), -1 (remove detail).
                var fpsChange = fpsTarget < -2.5 ? +1 : fpsTarget > 2.5 ? -1 : 0;

                // If we're not vertical sync-limited, there's no point calculating the CPU change, just assume adding detail is okay.
                double cpuTarget = 0;
                var cpuChange = 1;
                if (viewer.UserSettings.VerticalSync)
                {
                    // Work out how much spare CPU we have; the target is 90%.
                    //   +ve = under-performing/too much detail
                    //   -ve = over-performing/not enough detail
                    var cpuTargetRender = Profiler.ProfilingData[ProcessType.Render].Wall.SmoothedValue - 90;
                    var cpuTargetUpdater = Profiler.ProfilingData[ProcessType.Updater].Wall.SmoothedValue - 90;
                    cpuTarget = cpuTargetRender > cpuTargetUpdater ? cpuTargetRender : cpuTargetUpdater;

                    // Summarise the CPS adjustment to: +1 (add detail), 0 (keep), -1 (remove detail).
                    cpuChange = cpuTarget < -2.5 ? +1 : cpuTarget > 2.5 ? -1 : 0;
                }

                // Now we adjust the viewing distance to try and balance out the FPS.
                var oldViewingDistance = viewer.UserSettings.ViewingDistance;
                if (fpsChange < 0)
                    viewer.UserSettings.ViewingDistance -= (int)(fpsTarget - 1.5);
                else if (cpuChange < 0)
                    viewer.UserSettings.ViewingDistance -= (int)(cpuTarget - 1.5);
                else if (fpsChange > 0 && cpuChange > 0)
                    viewer.UserSettings.ViewingDistance += (int)(-fpsTarget - 1.5);
                viewer.UserSettings.ViewingDistance = MathHelper.Clamp(viewer.UserSettings.ViewingDistance, 500, 10000);
                viewer.UserSettings.DetailLevelBias = (int)MathHelper.Clamp(initialDetailLevelBias + 100 * ((float)viewer.UserSettings.ViewingDistance / initialViewingDistance - 1), -100, 100);

                // If we've changed the viewing distance, we need to update the camera matricies.
                if (oldViewingDistance != viewer.UserSettings.ViewingDistance)
                    viewer.Camera.ScreenChanged();

                // Flag as done, so the next load prep (every 250ms) can trigger us again.
                performanceTune = false;
            }
            WeatherControl.Update(elapsedTime);
            Scenery.Update(elapsedTime);
        }

        public void LoadPrep()
        {
            Terrain.LoadPrep();
            Scenery.LoadPrep();
            Trains.LoadPrep();
            RoadCars.LoadPrep();
            Containers.LoadPrep();
            visibleTile = viewer.Camera.Tile;
            performanceTune = viewer.UserSettings.PerformanceTuner;
        }

        public void PrepareFrame(RenderFrame frame, in ElapsedTime elapsedTime)
        {
            if (viewer.UserSettings.MstsEnvironment)
                MSTSSky.PrepareFrame(frame, elapsedTime);
            else
                Sky.PrepareFrame(frame, elapsedTime);
            Precipitation.PrepareFrame(frame, elapsedTime);
            Terrain.PrepareFrame(frame, elapsedTime);
            Scenery.PrepareFrame(frame, elapsedTime);
            Trains.PrepareFrame(frame, elapsedTime);
            Containers.PrepareFrame(frame, elapsedTime);
            RoadCars.PrepareFrame(frame, elapsedTime);
        }
    }
}
