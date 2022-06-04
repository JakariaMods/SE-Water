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
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component that simulates effects like birds, splashes, fish, etc.
    /// </summary>
    public class WaterEffectsComponent : SessionComponentBase
    {
        public List<Splash> SurfaceSplashes = new List<Splash>(128);
        public Seagull[] Seagulls = new Seagull[8];
        public Fish[] Fishes = new Fish[24];
        public List<AnimatedPointBillboard> Bubbles = new List<AnimatedPointBillboard>();
        public AnimatedPointBillboard[] AmbientBubbles = new AnimatedPointBillboard[128];
        public List<SimulatedSplash> SimulatedSplashes = new List<SimulatedSplash>();
        public Object EffectLock = new Object();

        private WaterRenderComponent _renderComponent;
        private WaterModComponent _modComponent;
        private WaterSettingsComponent _settingsComponent;

        public static WaterEffectsComponent Static;

        public WaterEffectsComponent()
        {
            Static = this;
            UpdateOrder = MyUpdateOrder.AfterSimulation;
        }

        public override void LoadDependencies()
        {
            _renderComponent = WaterRenderComponent.Static;
            _modComponent = WaterModComponent.Static;
            _settingsComponent = WaterSettingsComponent.Static;
        }

        public override void UnloadDependencies()
        {
            _modComponent = null;
            _renderComponent = null;
            _settingsComponent = null;

            Static = null;
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null && MyAPIGateway.Session.Camera != null)
            {
                _renderComponent.ClosestPlanet = MyGamePruningStructure.GetClosestPlanet(_renderComponent.CameraPosition);
                _renderComponent.ClosestWater = _modComponent.GetClosestWater(_renderComponent.CameraPosition);

                if (_renderComponent.ClosestPlanet != null)
                {
                    _renderComponent.CameraGravityDirection = Vector3.Normalize(_renderComponent.ClosestPlanet.PositionComp.GetPosition() - _renderComponent.CameraPosition);
                }

                if (_renderComponent.ClosestWater != null)
                    MyAPIGateway.Parallel.StartBackground(SimulateEffects);
            }
        }

        public void SimulateEffects()
        {
            if (_renderComponent.ClosestWater == null || _renderComponent.ClosestPlanet == null)
                return;

            lock (EffectLock)
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
                if (_renderComponent.ClosestWater.Material.DrawBubbles)
                {
                    if (Bubbles != null && _renderComponent.ClosestWater.Material.DrawBubbles && Bubbles.Count > 0)
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
                    if (AmbientBubbles != null && _renderComponent.CameraUnderwater && _renderComponent.ClosestWater.Material.DrawBubbles)
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
                                            AmbientBubbles[i] = new AnimatedPointBillboard(_renderComponent.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 40)), -_renderComponent.CameraGravityDirection, 0.05f, MyUtils.GetRandomInt(150, 250), 0, WaterData.BubbleMaterial);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    AmbientBubbles[i] = new AnimatedPointBillboard(_renderComponent.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 40)), -_renderComponent.CameraGravityDirection, 0.05f, MyUtils.GetRandomInt(150, 250), 0, WaterData.BubbleMaterial);
                                }

                            }
                            else
                            {
                                Bubble.Simulate();
                            }
                        }
                }

                if (_renderComponent.ClosestWater.EnableFish)
                    for (int i = 0; i < Fishes.Length; i++)
                    {
                        Fish Fish = Fishes[i];
                        if (Fish == null || Fish.MarkedForClose)
                        {
                            Vector3D NewPosition = _renderComponent.CameraClosestWaterPosition + (MyUtils.GetRandomPerpendicularVector(_renderComponent.CameraGravityDirection) * MyUtils.GetRandomFloat(_renderComponent.ClosestWater.WaveHeight, 100)) + (_renderComponent.CameraGravityDirection * (_renderComponent.ClosestWater.WaveHeight + _renderComponent.ClosestWater.TideHeight + MyUtils.GetRandomFloat(1, (float)(_renderComponent.CameraAltitude - _renderComponent.CameraDepth))));

                            Fishes[i] = new Fish(NewPosition, MyUtils.GetRandomPerpendicularVector(_renderComponent.CameraGravityDirection), _renderComponent.CameraGravityDirection, MyUtils.GetRandomInt(1000, 2000), 1);
                        }
                        else
                        {
                            Fish.Simulate();
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

                if (_renderComponent.ClosestWater.EnableSeagulls)
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

        /// <summary>
        /// Creates a bubble (for underwater only)
        /// </summary>
        public void CreateBubble(ref Vector3D position, float radius)
        {
            if (MyAPIGateway.Utilities.IsDedicated || _renderComponent.ClosestWater?.Material?.DrawBubbles != true)
                return;

            lock (EffectLock)
                Bubbles.Add(new AnimatedPointBillboard(position, -_renderComponent.CameraGravityDirection, radius, MyUtils.GetRandomInt(250, 500), MyUtils.GetRandomFloat(0, 360), WaterData.BubblesMaterial));
        }

        public void CreateSplash(Vector3D Position, float Radius, bool Audible)
        {
            if (MyAPIGateway.Utilities.IsDedicated || _renderComponent.ClosestWater?.Material?.DrawSplashes != true)
                return;

            lock (EffectLock)
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
            if (!MyAPIGateway.Utilities.IsDedicated && _renderComponent.ClosestWater != null && explosionInfo.ExplosionType != MyExplosionTypeEnum.GRID_DEFORMATION && explosionInfo.ExplosionType != MyExplosionTypeEnum.GRID_DESTRUCTION)
            {
                float explosionRadius = (float)explosionInfo.ExplosionSphere.Radius;
                Vector3D explosionPosition = explosionInfo.ExplosionSphere.Center;

                float explosionDepth = (float)_renderComponent.ClosestWater.GetDepth(ref explosionPosition);

                if (explosionDepth - explosionRadius <= 0)
                {
                    float SurfaceRatio = Math.Max(1f - (Math.Abs(explosionDepth) / explosionRadius), 0f);

                    explosionInfo.Direction = -_renderComponent.CameraGravityDirection;
                    explosionInfo.PlaySound = false;

                    if (explosionDepth <= 0 && _renderComponent.CameraUnderwater)
                    {
                        explosionInfo.CreateParticleEffect = false;

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
                            explosionInfo.PlaySound = true;
                        }

                        if (explosionDepth + explosionRadius <= 0 && _renderComponent.ClosestWater.Material.DrawBubbles)
                            CreateBubble(ref explosionPosition, explosionRadius / 4 * SurfaceRatio);
                    }
                    else
                    {
                        explosionInfo.ExplosionType = MyExplosionTypeEnum.CUSTOM;
                        explosionInfo.CustomSound = WaterData.SurfaceExplosionSound;
                        explosionInfo.PlaySound = true;
                    }

                    Vector3D surfacePoint = _renderComponent.ClosestWater.GetClosestSurfacePointGlobal(explosionInfo.ExplosionSphere.Center);

                    //CreatePhysicsSplash(surfacePoint, -Session.GravityDirection * 120 * SurfaceRatio, Math.Max(ExplosionRadius / 4, 2f), SurfaceRatio, (int)(explosionInfo.ExplosionSphere.Radius * 2 * SurfaceRatio));
                    CreateSplash(surfacePoint, explosionRadius * SurfaceRatio, true);
                }
            }
        }

        public override void LoadData()
        {
            MyExplosions.OnExplosion += MyExplosions_OnExplosion;
        }

        public override void UnloadData()
        {
            MyExplosions.OnExplosion -= MyExplosions_OnExplosion;
        }
    }
}
