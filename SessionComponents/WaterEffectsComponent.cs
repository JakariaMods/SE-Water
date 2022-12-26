using Jakaria.Components;
using Jakaria.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component that simulates effects like birds, splashes, etc.
    /// </summary>
    public class WaterEffectsComponent : SessionComponentBase
    {
        public List<Splash> SurfaceSplashes = new List<Splash>(128);
        public Seagull[] Seagulls = new Seagull[8];
        public List<AnimatedPointBillboard> Bubbles = new List<AnimatedPointBillboard>();
        public AnimatedPointBillboard[] AmbientBubbles = new AnimatedPointBillboard[128];
        public List<SimulatedSplash> SimulatedSplashes = new List<SimulatedSplash>();

        private WaterRenderSessionComponent _renderComponent;

        private void OnWaterChanged(WaterComponent water)
        {
            if (water == null || water.Settings.Material.DrawBubbles)
            {
                foreach (var bubble in Bubbles)
                {
                    if (bubble != null && bubble.InScene)
                        MyTransparentGeometry.RemovePersistentBillboard(bubble.Billboard);
                }

                Bubbles.Clear();
            }

            if(water == null || water.Settings.Material.DrawSplashes)
            {
                foreach (var splash in SurfaceSplashes)
                {
                    if(splash != null)
                    MyTransparentGeometry.RemovePersistentBillboard(splash.Billboard);
                }

                foreach (var splash in SimulatedSplashes)
                {
                    if (splash != null && splash.InScene)
                        MyTransparentGeometry.RemovePersistentBillboard(splash.Billboard);
                }

                SurfaceSplashes.Clear();
                SimulatedSplashes.Clear();
            }

            if (water == null || !water.Settings.EnableSeagulls)
            {
                foreach (var seagull in Seagulls)
                {
                    if (seagull != null && seagull.InScene)
                        MyTransparentGeometry.RemovePersistentBillboard(seagull.Billboard);
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null && MyAPIGateway.Session.Camera != null)
            {
                if (_renderComponent.ClosestPlanet != null)
                {
                    _renderComponent.CameraGravityDirection = Vector3.Normalize(_renderComponent.ClosestPlanet.PositionComp.GetPosition() - _renderComponent.CameraPosition);

                    if (_renderComponent.ClosestWater != null)
                    {
                        lock (SurfaceSplashes)
                        {
                            if (SurfaceSplashes != null && SurfaceSplashes.Count > 0)
                                for (int i = SurfaceSplashes.Count - 1; i >= 0; i--)
                                {
                                    Splash splash = SurfaceSplashes[i];
                                    if (splash == null || splash.Life > splash.MaxLife)
                                    {
                                        if (splash != null)
                                            MyTransparentGeometry.RemovePersistentBillboard(splash.Billboard);

                                        SurfaceSplashes.RemoveAtFast(i);
                                        continue;
                                    }
                                    splash.Life++; //todo redo
                                }

                        }

                        lock (SimulatedSplashes)
                        {
                            if (SimulatedSplashes != null && SimulatedSplashes.Count > 0)
                                for (int i = SimulatedSplashes.Count - 1; i >= 0; i--)
                                {
                                    SimulatedSplash Splash = SimulatedSplashes[i];
                                    if (Splash == null || Splash.MarkedForClose)
                                    {
                                        if (Splash != null && Splash.InScene)
                                            MyTransparentGeometry.RemovePersistentBillboard(Splash.Billboard);

                                        SimulatedSplashes.RemoveAtFast(i);
                                        continue;
                                    }
                                    else
                                    {
                                        Splash.Simulate();
                                    }
                                }
                        }

                        if (_renderComponent.ClosestWater.Settings.Material.DrawBubbles)
                        {
                            lock (Bubbles)
                            {
                                if (Bubbles != null && _renderComponent.ClosestWater.Settings.Material.DrawBubbles && Bubbles.Count > 0)
                                    for (int i = Bubbles.Count - 1; i >= 0; i--)
                                    {
                                        AnimatedPointBillboard Bubble = Bubbles[i];
                                        if (Bubble == null || Bubble.MarkedForClose)
                                        {
                                            if (Bubble != null && Bubble.InScene)
                                                MyTransparentGeometry.RemovePersistentBillboard(Bubble.Billboard);

                                            Bubbles.RemoveAtFast(i);
                                            continue;
                                        }
                                        else
                                        {
                                            Bubble.Simulate();
                                        }
                                    }
                            }

                            lock (AmbientBubbles)
                            {
                                if (AmbientBubbles != null && _renderComponent.CameraUnderwater && _renderComponent.ClosestWater.Settings.Material.DrawBubbles)
                                    for (int i = 0; i < AmbientBubbles.Length; i++)
                                    {
                                        AnimatedPointBillboard Bubble = AmbientBubbles[i];
                                        if (Bubble == null || Bubble.MarkedForClose)
                                        {
                                            if (Bubble != null && Bubble.InScene)
                                                MyTransparentGeometry.RemovePersistentBillboard(Bubble.Billboard);

                                            Vector3D randomPosition = _renderComponent.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 40));
                                            IMyCubeGrid grid = WaterUtils.GetApproximateGrid(_renderComponent.CameraPosition);

                                            if (grid != null)
                                            {
                                                for (int j = 0; j < 10; j++)
                                                {
                                                    randomPosition = _renderComponent.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 40));
                                                    Vector3I localPosition = grid.WorldToGridInteger(randomPosition);
                                                    if (grid.GetCubeBlock(localPosition) == null && !grid.IsRoomAtPositionAirtight(localPosition))
                                                    {
                                                        var bubble = AmbientBubbles[i] = new AnimatedPointBillboard(_renderComponent.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 40)), -_renderComponent.CameraGravityDirection, 0.05f, MyUtils.GetRandomInt(150, 250), 0, WaterData.BubbleMaterial);
                                                        bubble.Billboard.Color *= _renderComponent.WhiteColor;
                                                        break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var bubble = AmbientBubbles[i] = new AnimatedPointBillboard(_renderComponent.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 40)), -_renderComponent.CameraGravityDirection, 0.05f, MyUtils.GetRandomInt(150, 250), 0, WaterData.BubbleMaterial);
                                                bubble.Billboard.Color *= _renderComponent.WhiteColor;
                                            }
                                        }
                                        else
                                        {
                                            Bubble.Simulate();
                                        }
                                    }
                            }
                        }

                        lock (Seagulls)
                        {
                            if (_renderComponent.ClosestWater.Settings.EnableSeagulls)
                                for (int i = 0; i < Seagulls.Length; i++)
                                {
                                    Seagull Seagull = Seagulls[i];
                                    if (Seagull == null || Seagull.MarkedForClose)
                                    {
                                        Vector3D NewPosition = _renderComponent.CameraClosestSurfacePosition - (_renderComponent.CameraGravityDirection * (_renderComponent.CameraAltitude + _renderComponent.CameraDepth + MyUtils.GetRandomDouble(10, 50))) + (MyUtils.GetRandomPerpendicularVector(_renderComponent.CameraGravityDirection) * MyUtils.GetRandomFloat(0, 100));

                                        Seagulls[i] = new Seagull(NewPosition, MyUtils.GetRandomPerpendicularVector(_renderComponent.CameraGravityDirection), _renderComponent.CameraGravityDirection, MyUtils.GetRandomInt(2000, 3000), 1);
                                    }
                                    else
                                    {
                                        Seagull.Simulate();
                                    }
                                }
                            else
                            {
                                foreach (var seagull in Seagulls)
                                {
                                    if (seagull != null && seagull.InScene)
                                    {
                                        MyTransparentGeometry.RemovePersistentBillboard(seagull.Billboard);
                                        seagull.InScene = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a bubble (for underwater only)
        /// </summary>
        public void CreateBubble(ref Vector3D position, float radius)
        {
            if (_renderComponent.ClosestWater == null || !_renderComponent.ClosestWater.Settings.Material.DrawSplashes)
                return;

            lock (Bubbles)
                Bubbles.Add(new AnimatedPointBillboard(position, -_renderComponent.CameraGravityDirection, radius, MyUtils.GetRandomInt(250, 500), MyUtils.GetRandomFloat(0, 360), WaterData.BubblesMaterial));
        }

        public void CreateSplash(Vector3D Position, float Radius, bool Audible)
        {
            if (_renderComponent.ClosestWater == null || !_renderComponent.ClosestWater.Settings.Material.DrawSplashes)
                return;

            lock (SurfaceSplashes)
                SurfaceSplashes.Add(new Splash(Position, Radius, Audible ? 1 : 0));
        }

        public void CreatePhysicsSplash(Vector3D Position, Vector3D Velocity, float Radius, float Variation, int Count = 1)
        {
            for (int i = 0; i < Count; i++)
            {
                Vector3 randomHemisphere = MyUtils.GetRandomVector3HemisphereNormalized(Vector3.Normalize(Velocity)) * 20;
                float rand = MyUtils.GetRandomFloat(-1, 1);
                SimulatedSplashes.Add(new SimulatedSplash(Position + (randomHemisphere / 2), (Velocity + randomHemisphere * (rand * Variation)) / 3, Radius + rand + 1, _renderComponent.ClosestWater));
            }
        }

        private void MyExplosions_OnExplosion(ref MyExplosionInfo explosionInfo)
        {
            if (!MyAPIGateway.Utilities.IsDedicated && _renderComponent.ClosestWater != null)
            {
                float explosionRadius = (float)explosionInfo.ExplosionSphere.Radius;
                Vector3D explosionPosition = explosionInfo.ExplosionSphere.Center;

                float explosionDepth = (float)_renderComponent.ClosestWater.GetDepthGlobal(ref explosionPosition);

                if (explosionDepth - explosionRadius <= 0)
                {
                    explosionInfo.Direction = -_renderComponent.CameraGravityDirection;
                    explosionInfo.CreateParticleEffect = false;

                    if (explosionDepth <= 0 && _renderComponent.CameraUnderwater)
                    {
                        if (explosionInfo.ExplosionType == MyExplosionTypeEnum.GRID_DEFORMATION || 
                            explosionInfo.ExplosionType == MyExplosionTypeEnum.GRID_DESTRUCTION || 
                            explosionInfo.ExplosionType == MyExplosionTypeEnum.MISSILE_EXPLOSION || 
                            explosionInfo.ExplosionType == MyExplosionTypeEnum.CUSTOM)
                        {
                            MatrixD matrix = MatrixD.CreateWorld(explosionPosition, _renderComponent.GravityAxisA, _renderComponent.CameraGravityDirection);
                            MyParticleEffect effect;
                            if (MyParticlesManager.TryCreateParticleEffect("UnderwaterBreak", ref matrix, ref explosionPosition, 0, out effect))
                            {
                                effect.Autodelete = true;
                            }
                        }

                        if (explosionInfo.ExplosionType == MyExplosionTypeEnum.CUSTOM)
                        {
                            if (explosionInfo.CustomSound != null)
                            {
                                string SoundName = explosionInfo.CustomSound.ToString();
                                if (SoundName == "RealPoofExplosionCat1" || SoundName == "ArcPoofExplosionCat1")
                                    explosionInfo.CustomSound = WaterData.UnderwaterPoofSound;
                            }
                        }
                        else
                        {
                            explosionInfo.CustomSound = WaterData.UnderwaterExplosionSound;
                            explosionInfo.ExplosionType = MyExplosionTypeEnum.CUSTOM;
                        }
                    }
                    else
                    {
                        Vector3D surfacePosition = _renderComponent.ClosestWater.GetClosestSurfacePointGlobal(ref explosionPosition);
                        Assert.False(surfacePosition == explosionPosition);
                        explosionInfo.ExplosionType = MyExplosionTypeEnum.CUSTOM;
                        MatrixD matrix = MatrixD.CreateWorld(surfacePosition, _renderComponent.GravityAxisA, _renderComponent.CameraGravityDirection);
                        MyParticleEffect effect;
                        if (MyParticlesManager.TryCreateParticleEffect("WaterBlast", ref matrix, ref surfacePosition, 0, out effect))
                        {
                            effect.UserScale = explosionRadius / 23f;
                            effect.UserColorIntensityMultiplier = _renderComponent.ClosestWater.PlanetConfig.ColorIntensity;
                        }

                        explosionInfo.CustomSound = WaterData.SurfaceExplosionSound;
                    }
                }
            }
        }

        public override void LoadData()
        {
            _renderComponent = Session.Instance.Get<WaterRenderSessionComponent>();

            MyExplosions.OnExplosion += MyExplosions_OnExplosion;
            _renderComponent.OnWaterChanged += OnWaterChanged;
        }

        public override void UnloadData()
        {
            MyExplosions.OnExplosion -= MyExplosions_OnExplosion;
            _renderComponent.OnWaterChanged -= OnWaterChanged;
        }
    }
}
