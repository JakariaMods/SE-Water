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

namespace Jakaria
{
    public class WaterFace
    {
        public Vector3D up { private set; get; }
        public Vector3D axisA { private set; get; }
        public Vector3D axisB { private set; get; }

        public Vector3D position { private set; get; }
        public Vector3D cornerPosition { private set; get; }
        public Vector3D centerPosition { private set; get; }
        public double diameter { private set; get; }
        public double radius { private set; get; }
        public Chunk tree;
        public Water water;

        public WaterFace(Water water, Vector3D upDirection)
        {
            this.water = water;
            this.radius = water.radius;
            this.diameter = water.radius * 2;
            this.up = upDirection;
            this.axisA = new Vector3D(up.Y, up.Z, up.X);
            this.axisB = up.Cross(axisA);
            this.position = water.position;

            cornerPosition = (water.radius * this.up) - (((this.axisA * water.radius) + (this.axisB * water.radius)));
            centerPosition = (water.radius * this.up);
            ConstructTree();
        }

        public void ConstructTree()
        {
            tree = new Chunk(position + centerPosition, radius, 0, this);

            tree.GenerateChildren();
        }

        public void Draw(bool closestToCamera)
        {
            tree.Draw(closestToCamera);
        }
    }

    public class Chunk
    {
        Chunk[] children;
        Vector3D position;
        double radius;
        int detailLevel;
        public WaterFace face;
        public static Vector3D refZero = Vector3D.Zero;
        int textureId;
        Vector3I wakeKey;

        public Chunk(Vector3D position, double radius, int detailLevel, WaterFace face)
        {
            this.position = position;
            this.radius = radius;
            this.face = face;
            this.detailLevel = detailLevel;
            this.wakeKey = (Vector3I)(position / 10);
            this.textureId = Math.Abs(((int)(position.X + position.Y + position.Z) / 10) % 2);
            this.children = null;
        }

        public void GenerateChildren()
        {
            if (detailLevel < 2 || (radius > 4 && (face.position + (Vector3D.Normalize(position + ((-face.axisA + -face.axisB) * radius) - face.position) * face.water.currentRadius) - WaterMod.Session.CameraPosition).AbsMax() < radius * (1.5f + (WaterMod.Settings.Quality * 2))))
            //if (detailLevel < 2 || (radius > 4 && (face.position + (Vector3D.Normalize(position + ((-face.axisA + -face.axisB) * radius) - face.position) * face.water.currentRadius) - WaterMod.Session.CameraPosition).AbsMax() < radius * (4 - (WaterMod.Settings.Quality * 0.25f))))
            {
                double halfRadius = radius / 2.0;
                int detailPlusOne = detailLevel + 1;

                Vector3D axisAPremultiplied = radius * face.axisA * 0.5f;
                Vector3D axisBPremultiplied = radius * face.axisB * 0.5f;
                children = new Chunk[4];
                children[0] = new Chunk(this.position + (-axisAPremultiplied + -axisBPremultiplied), halfRadius, detailPlusOne, face);
                children[1] = new Chunk(this.position + (-axisAPremultiplied + axisBPremultiplied), halfRadius, detailPlusOne, face);
                children[2] = new Chunk(this.position + (axisAPremultiplied + -axisBPremultiplied), halfRadius, detailPlusOne, face);
                children[3] = new Chunk(this.position + (axisAPremultiplied + axisBPremultiplied), halfRadius, detailPlusOne, face);

                foreach (var child in children)
                {
                    child.GenerateChildren();
                }
            }
        }

        public void Draw(bool closestToCamera)
        {
            if (face.water == null || face.water.planet == null)
                return;

            //if (WaterUtils.IsUnderGround(face.water.planet, face.position + (Vector3D.Normalize(position + ((-face.axisA + -face.axisB) * radius) - face.position) * face.water.currentRadius), radius))
            //return;

            if (children != null)
            {
                foreach (var child in children)
                {
                    child.Draw(closestToCamera);
                }
            }
            else
            {
                Vector3D normal1 = Vector3D.Normalize(position + ((-face.axisA + -face.axisB) * radius) - face.position);
                Vector3D normal2 = Vector3D.Normalize(position + ((face.axisA + face.axisB) * radius) - face.position);
                Vector3D normal3 = Vector3D.Normalize(position + ((-face.axisA + face.axisB) * radius) - face.position);
                Vector3D normal4 = Vector3D.Normalize(position + ((face.axisA + -face.axisB) * radius) - face.position);

                Vector3D corner1 = face.water.GetClosestSurfacePoint(face.position + (normal1 * face.water.currentRadius));

                float distToCamera = Vector3.RectangularDistance(corner1, WaterMod.Session.CameraPosition);

                if (distToCamera > 100)
                {
                    if (closestToCamera && (distToCamera > WaterMod.Session.DistanceToHorizon + (radius * 6)))
                        return;

                    if (Vector3.Dot((corner1 - WaterMod.Session.CameraPosition), WaterMod.Session.CameraRotation) < WaterData.DotMaxFOV)
                        return;
                }

                Vector3D corner2 = face.water.GetClosestSurfacePoint(face.position + (normal2 * face.water.currentRadius));
                Vector3D corner3 = face.water.GetClosestSurfacePoint(face.position + (normal3 * face.water.currentRadius));
                Vector3D corner4 = face.water.GetClosestSurfacePoint(face.position + (normal4 * face.water.currentRadius));

                Vector3D average = ((corner1 + corner2 + corner3 + corner4) / 4.0);

                if (face.water.planet != null)
                {
                    MyPlanet planet = face.water.planet;

                    if (WaterUtils.GetAltitude(planet, average) < -radius * 2 && (planet.GetMaterialAt(ref average) != null && planet.GetMaterialAt(ref corner1) != null && planet.GetMaterialAt(ref corner2) != null && planet.GetMaterialAt(ref corner3) != null && planet.GetMaterialAt(ref corner4) != null))
                        return;
                }

                Vector4 WaterColor = WaterData.WaterColor;
                Vector4 WaterFadeColor = WaterData.WaterFadeColor;
                Vector4 WhiteColor = Vector4.One;

                float dot = face.water.lit ? MyMath.Clamp(Vector3.Dot(normal1, WaterMod.Session.SunDirection) + 0.22f, 0.22f, 1f) : 1;

                if (face.water.lit)
                {
                    WaterColor *= dot;
                    WaterColor.W = WaterData.WaterColor.W;
                    WaterFadeColor *= dot;
                    WaterFadeColor.W = WaterData.WaterFadeColor.W;
                    WhiteColor = new Vector4(dot, dot, dot, 1);
                }

                MyQuadD quad = new MyQuadD()
                {
                    Point0 = corner1,
                    Point1 = corner3,
                    Point2 = corner2,
                    Point3 = corner4
                };

                if (!WaterMod.Session.CameraUnderwater && closestToCamera)
                {
                    if (face.water.enableFoam && radius < 128 * WaterMod.Settings.Quality)
                    {
                        Vector3D noisePosition = (average + (Vector3D.One * face.water.waveTimer)) * face.water.waveScale;

                        float intensity = (float)MyMath.Clamp((face.water.noise.GetNoise(noisePosition.X, noisePosition.Y, noisePosition.Z) / 0.25f), 0, 1);

                        if (intensity > 0.1f)
                            WaterMod.Static.QuadBillboards.Push(new WaterMod.QuadBillboard(ref WaterData.FoamMaterials[textureId], ref quad, WhiteColor * intensity));

                        if (intensity < 0.9f)
                            WaterMod.Static.QuadBillboards.Push(new WaterMod.QuadBillboard(ref WaterData.FoamLightMaterials[textureId], ref quad, WhiteColor * (1f - (intensity * intensity))));
                    }
                }

                if (face.water.transparent)
                {
                    if (closestToCamera)
                    {
                        if (WaterMod.Session.CameraUnderwater)
                        {
                            WaterMod.Static.QuadBillboards.Push(new WaterMod.QuadBillboard(ref face.water.textureId, ref quad, WaterColor * 0.5f));
                        }
                        else
                        {
                            if (detailLevel > 4)
                            {
                                int count = (int)Math.Max(Math.Min(Math.Ceiling((detailLevel - 2) * WaterMod.Settings.Quality), 8), 3);

                                Vector3D layerSeperation = -normal1 * (1.0f / count) * 20f;

                                for (int i = 0; i < count; i++)
                                {
                                    if (i == count - 1)
                                    {
                                        quad.Point0 += layerSeperation;
                                        quad.Point1 += layerSeperation;
                                        quad.Point2 += layerSeperation;
                                        quad.Point3 += layerSeperation;

                                        WaterMod.Static.QuadBillboards.Push(new WaterMod.QuadBillboard(ref face.water.textureId, ref quad, ref WhiteColor));
                                    }
                                    else
                                    {
                                        if (i == 0)
                                        {
                                            WaterMod.Static.QuadBillboards.Push(new WaterMod.QuadBillboard(ref face.water.textureId, ref quad, ref WaterColor));
                                        }
                                        else
                                        {
                                            quad.Point0 += layerSeperation;
                                            quad.Point1 += layerSeperation;
                                            quad.Point2 += layerSeperation;
                                            quad.Point3 += layerSeperation;
                                            WaterMod.Static.QuadBillboards.Push(new WaterMod.QuadBillboard(ref face.water.textureId, ref quad, ref WaterFadeColor));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                WaterMod.Static.QuadBillboards.Push(new WaterMod.QuadBillboard(ref face.water.textureId, ref quad, ref WhiteColor));
                            }
                        }
                    }
                    else
                    {
                        if (WaterMod.Session.CameraUnderwater)
                        {
                            return;
                        }
                        else
                        {
                            WaterMod.Static.QuadBillboards.Push(new WaterMod.QuadBillboard(ref face.water.textureId, ref quad, ref WaterColor));
                            Vector3D offset = normal1 * 100;
                            quad.Point0 += offset;
                            quad.Point1 += offset;
                            quad.Point2 += offset;
                            quad.Point3 += offset;
                            WaterMod.Static.QuadBillboards.Push(new WaterMod.QuadBillboard(ref face.water.textureId, ref quad, ref WhiteColor));
                        }
                    }
                }
                else
                {
                    WaterMod.Static.QuadBillboards.Push(new WaterMod.QuadBillboard(ref face.water.textureId, ref quad, ref WhiteColor));

                    if (!WaterMod.Session.CameraUnderwater)
                    {
                        Vector3D Seperator = normal1 * face.water.waveHeight;

                        quad.Point0 -= Seperator;
                        quad.Point1 -= Seperator;
                        quad.Point2 -= Seperator;
                        quad.Point3 -= Seperator;
                        WaterMod.Static.QuadBillboards.Push(new WaterMod.QuadBillboard(ref face.water.textureId, ref quad, ref WhiteColor));
                    }
                }
            }
        }
    }
}
