using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using Jakaria.Utils;
using System.Collections;
using Sandbox.ModAPI;
using VRage.Game;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using Sandbox.Game.Entities;
using Jakaria.API;
using VRageRender;
using VRage.Game.ModAPI;
using Jakaria.Components;

namespace Jakaria
{
    public class WaterFace
    {
        internal Vector3D Up;
        internal Vector3D AxisA;
        internal Vector3D AxisB;

        internal Vector3D CornerPosition;
        internal Vector3D CenterPosition;
        internal double Radius;
        internal Chunk Tree;
        internal Water Water;

        public WaterFace(Water water, Vector3D upDirection)
        {
            this.Water = water;
            this.Radius = water.Radius;
            this.Up = upDirection;
            this.AxisA = new Vector3D(Up.Y, Up.Z, Up.X);
            this.AxisB = Up.Cross(AxisA);

            CenterPosition = (water.Radius * this.Up);
            CornerPosition = CenterPosition - (((this.AxisA * water.Radius) + (this.AxisB * water.Radius)));
        }

        public void ConstructTree()
        {
            Tree = new Chunk(CenterPosition, Radius, 0, this, -1);

            Tree.GenerateChildren();
        }

        public void Draw(bool closestToCamera)
        {
            if (Water.planet != null)
            {
                if (Tree == null)
                {
                    ConstructTree();
                }
                else
                {
                    Tree.Draw(closestToCamera);
                }
            }
        }
    }

    public class Chunk
    {
        public WaterFace Face;
        Chunk[] Children;
        Vector3D Position;
        double Radius;
        int DetailLevel;
        int TextureId; //The UV index for sea foam
        BoundingSphereD Sphere;
        float Altitude;
        int ParentIndex; //The index/corner of the subdivision this chunk is relative to the previous(parent) chunk

        MyBillboard[] surfaceBillboards;
        MyBillboard seaFoamBillboard;
        MyBillboard seaFoamLightBillboard;

        public Chunk(Vector3D position, double radius, int detailLevel, WaterFace face, int parentIndex)
        {
            Position = position;
            Radius = radius;
            Face = face;
            DetailLevel = detailLevel;
            Sphere = new BoundingSphereD(Face.Water.Position + (Vector3D.Normalize(position + ((-Face.AxisA + -Face.AxisB) * radius)) * Face.Water.Radius), radius * 3);
            ParentIndex = parentIndex;
        }

        public void GenerateChildren()
        {
            if (DetailLevel < WaterData.MinWaterSplitDepth || (Radius > WaterData.MinWaterSplitRadius && (Face.Water.Position + (Vector3D.Normalize(Position + (-Face.AxisA + -Face.AxisB) * Radius) * Face.Water.Radius) - WaterModComponent.Session.CameraPosition).AbsMax() < Radius * (3f + (WaterModComponent.Settings.Quality * 2))))
            {
                double halfRadius = Radius / 2.0;
                int detailPlusOne = DetailLevel + 1;

                Vector3D axisAPremultiplied = Face.AxisA * (0.5 * Radius);
                Vector3D axisBPremultiplied = Face.AxisB * (0.5 * Radius);
                Children = new Chunk[4];
                Children[0] = new Chunk(this.Position + (-axisAPremultiplied + -axisBPremultiplied), halfRadius, detailPlusOne, Face, 0);
                Children[1] = new Chunk(this.Position + (-axisAPremultiplied + axisBPremultiplied), halfRadius, detailPlusOne, Face, 1);
                Children[2] = new Chunk(this.Position + (axisAPremultiplied + -axisBPremultiplied), halfRadius, detailPlusOne, Face, 2);
                Children[3] = new Chunk(this.Position + (axisAPremultiplied + axisBPremultiplied), halfRadius, detailPlusOne, Face, 3);

                foreach (var child in Children)
                {
                    child.GenerateChildren();
                }
            }

            if (Children == null)
            {
                //Maximum depth
                TextureId = Math.Abs(((int)(Position.X + Position.Y + Position.Z) / 10) % 4);
                Altitude = Face.Water.Transparent ? (float)Math.Max(WaterUtils.GetAltitude(Face.Water.planet, Sphere.Center), 0) : float.MaxValue;
            }
        }

        public void Draw(bool closestToCamera)
        {
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.Draw(closestToCamera);
                }
            }
            else if (Radius < 8 || (WaterModComponent.Session.CameraAltitude > 10000 && MyAPIGateway.Session.Camera.WorldToScreen(ref Sphere.Center).AbsMax() > 1) || (closestToCamera && MyAPIGateway.Session.Camera.IsInFrustum(ref Sphere))) //Camera Frustum only works around 20km
            {
                Vector3D normal1 = Vector3D.Normalize(Position + ((-Face.AxisA + -Face.AxisB) * Radius));

                MyQuadD quad = new MyQuadD()
                {
                    Point0 = Face.Water.GetClosestSurfacePointFromNormal(ref normal1),
                };

                if (closestToCamera && Vector3D.DistanceSquared(quad.Point0, WaterModComponent.Session.CameraPosition) > WaterModComponent.Session.DistanceToHorizon * WaterModComponent.Session.DistanceToHorizon)
                    return;

                Vector3D normal2 = Vector3D.Normalize(Position + ((Face.AxisA + Face.AxisB) * Radius));
                Vector3D normal3 = Vector3D.Normalize(Position + ((-Face.AxisA + Face.AxisB) * Radius));
                Vector3D normal4 = Vector3D.Normalize(Position + ((Face.AxisA + -Face.AxisB) * Radius));

                quad.Point1 = Face.Water.GetClosestSurfacePointFromNormal(ref normal3);
                quad.Point2 = Face.Water.GetClosestSurfacePointFromNormal(ref normal2);
                quad.Point3 = Face.Water.GetClosestSurfacePointFromNormal(ref normal4);

                Vector3 quadNormal = Vector3.Normalize(Vector3.Cross(quad.Point2 - quad.Point0, quad.Point1 - quad.Point0));

                if (WaterModComponent.Settings.ShowDebug)
                {
                    float length = (float)Radius;
                    float radius = length * 0.01f;
                    //Currents
                    MyTransparentGeometry.AddLineBillboard(WaterData.BlankMaterial, WaterData.BlueColor, quad.Point0, Face.Water.GetCurrentVelocity(normal1), length, radius);

                    //Waves
                    MyTransparentGeometry.AddLineBillboard(WaterData.BlankMaterial, WaterData.GreenColor, quad.Point0, Face.Water.GetWaveVelocity(normal1), 1, 0.01f);

                    //Normals
                    MyTransparentGeometry.AddLineBillboard(WaterData.BlankMaterial, WaterData.RedColor, quad.Point0, quadNormal, length, radius);
                }

                double ScaleOffset = Math.Min(Math.Pow(WaterModComponent.Session.CameraDepth / 2000, Face.Water.PlanetConfig.DistantRadiusScaler), Face.Water.Radius * Face.Water.PlanetConfig.DistantRadiusOffset);
                float SoftnessOffset = Math.Min((float)Math.Pow(WaterModComponent.Session.CameraDepth / 2000, Face.Water.PlanetConfig.DistantSoftnessScaler), Face.Water.Radius * Face.Water.PlanetConfig.DistantSoftnessOffset) * Face.Water.PlanetConfig.DistantSoftnessMultiplier;

                quad.Point0 += normal1 * ScaleOffset;
                quad.Point1 += normal3 * ScaleOffset;
                quad.Point2 += normal2 * ScaleOffset;
                quad.Point3 += normal4 * ScaleOffset;

                Vector3 halfVector = Vector3.Normalize((WaterModComponent.Session.SunDirection + Vector3.Normalize(WaterModComponent.Session.CameraPosition - ((quad.Point0 + quad.Point1) / 2))) / 2);
                float dot = Face.Water.Lit ? Vector3.Dot(quadNormal, WaterModComponent.Session.SunDirection) : 1;
                float ColorIntensity = Face.Water.Lit ? Math.Max(dot, Face.Water.PlanetConfig.AmbientColorIntensity) * Face.Water.PlanetConfig.ColorIntensity : Face.Water.PlanetConfig.ColorIntensity;
                float Specularity = dot > 0 ? Math.Max((float)Math.Pow(Vector3.Dot(quadNormal, halfVector), Face.Water.PlanetConfig.Specularity), 0) * Face.Water.PlanetConfig.SpecularIntensity : 0;

                MyBillboard tempBillboard;

                //Sea foam
                if (!WaterModComponent.Session.CameraUnderwater)
                {
                    if (Face.Water.EnableFoam && Radius < 256 * WaterModComponent.Settings.Quality)
                    {
                        Vector3D noisePosition = (quad.Point0 + Face.Water.WaveTimer) * Face.Water.WaveScale;

                        float intensity = (float)Math.Max((Face.Water.noise.GetNoise(noisePosition.X, noisePosition.Y, noisePosition.Z) / 0.25), 0);

                        if (seaFoamLightBillboard == null)
                            seaFoamLightBillboard = new MyBillboard()
                            {
                                Material = WaterData.FoamMaterial,
                                CustomViewProjection = -1,
                                UVSize = WaterData.FoamUVSize,
                                UVOffset = new Vector2(TextureId / 4f, 0.5f),
                            };

                        if (Radius < 256 * WaterModComponent.Settings.Quality)
                        {
                            if (intensity > 0.2f)
                            {
                                if (seaFoamBillboard == null)
                                    seaFoamBillboard = new MyBillboard()
                                    {
                                        Material = WaterData.FoamMaterial,
                                        CustomViewProjection = -1,
                                        UVSize = WaterData.FoamUVSize,
                                        UVOffset = new Vector2(TextureId / 4f, 0),
                                    };

                                seaFoamBillboard.Position0 = quad.Point0;
                                seaFoamBillboard.Position1 = quad.Point1;
                                seaFoamBillboard.Position2 = quad.Point2;
                                seaFoamBillboard.Position3 = quad.Point3;

                                seaFoamBillboard.Color = WaterData.WhiteColor * intensity;
                                seaFoamBillboard.ColorIntensity = ColorIntensity + Specularity;
                                Face.Water.BillboardCache.Add(seaFoamBillboard);
                            }
                            else
                            {
                                seaFoamLightBillboard.Position0 = quad.Point0;
                                seaFoamLightBillboard.Position1 = quad.Point1;
                                seaFoamLightBillboard.Position2 = quad.Point2;
                                seaFoamLightBillboard.Position3 = quad.Point3;

                                seaFoamLightBillboard.Color = WaterData.WhiteColor * (1f - intensity);
                                seaFoamLightBillboard.ColorIntensity = ColorIntensity + Specularity;
                                Face.Water.BillboardCache.Add(seaFoamLightBillboard);
                            }
                        }
                    }
                }

                //Surface
                if (surfaceBillboards == null)
                {
                    surfaceBillboards = new MyBillboard[3];
                    for (int i = 0; i < 3; i++)
                    {
                        surfaceBillboards[i] = new MyBillboard()
                        {
                            Material = Face.Water.TextureID,
                            CustomViewProjection = -1,
                            UVOffset = Radius <= 4 ? WaterData.SurfaceUVOffsets[ParentIndex] : Vector2.Zero,
                            UVSize = Radius <= 4 ? new Vector2(0.5f, 0.5f) : Vector2.One,
                        };
                    }
                }

                int billboardCount = 0;

                //Top billboard
                tempBillboard = surfaceBillboards[billboardCount];

                if (Altitude < -Radius * 2) //I'm already calculating this so might as well use it
                    return;

                if (WaterModComponent.Session.CameraUnderwater)
                {
                    tempBillboard.Color = WaterData.WaterUnderwaterColor;
                }
                else if (Face.Water.Transparent)
                {
                    //tempBillboard.SoftParticleDistanceScale = (float)MathHelper.Lerp(2f, WaterData.WaterVisibility, Math.Min((WaterUtils.IsQuadAirtight(ref quad) / 4f) * (3f / Radius), 1));
                    if (Radius < 32)
                        tempBillboard.SoftParticleDistanceScale = (float)MathHelper.Lerp(2f, WaterData.WaterVisibility, Math.Min(8f / Radius, 1));
                    else
                        tempBillboard.SoftParticleDistanceScale = 0.5f + SoftnessOffset;

                    tempBillboard.Color = Vector4.Lerp(WaterData.WaterShallowColor, WaterData.WaterDeepColor, (float)(Math.Min((Altitude / WaterData.WaterVisibility) * Math.Min(Radius / 32f, 1), 1.0)));
                }
                else
                {
                    tempBillboard.Color = WaterData.WhiteColor;
                }

                if (WaterModComponent.Session.CameraUnderwater)
                    tempBillboard.ColorIntensity = ColorIntensity;
                else
                    tempBillboard.ColorIntensity = ColorIntensity + Specularity;

                tempBillboard.Position0 = quad.Point0;
                tempBillboard.Position1 = quad.Point1;
                tempBillboard.Position2 = quad.Point2;
                tempBillboard.Position3 = quad.Point3;
                billboardCount++;

                if (!WaterModComponent.Session.CameraUnderwater && closestToCamera)
                {
                    Vector3D Seperator = normal1 * WaterData.WaterVisibility;

                    tempBillboard = surfaceBillboards[billboardCount];

                    tempBillboard.Color = WaterData.WhiteColor;

                    tempBillboard.ColorIntensity = ColorIntensity + Specularity;
                    tempBillboard.Position0 = quad.Point0 - Seperator;
                    tempBillboard.Position1 = quad.Point1 - Seperator;
                    tempBillboard.Position2 = quad.Point2 - Seperator;
                    tempBillboard.Position3 = quad.Point3 - Seperator;
                    tempBillboard.SoftParticleDistanceScale = WaterData.WaterVisibility + SoftnessOffset;
                    billboardCount++;
                }

                for (int i = 0; i < billboardCount; i++)
                {
                    Face.Water.BillboardCache.Add(surfaceBillboards[i]);
                }
            }
        }
    }
}
