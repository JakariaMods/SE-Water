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
        int detailLevel = 0;
        public WaterFace face;
        public static Vector3D refZero = Vector3D.Zero;

        public Chunk(Vector3D position, double radius, int detailLevel, WaterFace face)
        {
            this.position = position;
            this.radius = radius;
            this.face = face;
            this.detailLevel = detailLevel;
        }

        public void GenerateChildren()
        {
            if (detailLevel < 2 || (radius > 4 && (face.position + (Vector3D.Normalize(position + ((-face.axisA + -face.axisB) * radius) - face.position) * face.water.currentRadius) - WaterMod.Session.CameraPosition).AbsMax() < radius * (1.5f + (WaterMod.Settings.Quality * 2))))
            {
                double halfRadius = radius / 2.0;
                children = new Chunk[4];
                children[0] = new Chunk(this.position + (((-radius * face.axisA) + (-radius * face.axisB)) / 2.0), halfRadius, detailLevel + 1, face);
                children[1] = new Chunk(this.position + (((-radius * face.axisA) + (radius * face.axisB)) / 2.0), halfRadius, detailLevel + 1, face);
                children[2] = new Chunk(this.position + (((radius * face.axisA) + (-radius * face.axisB)) / 2.0), halfRadius, detailLevel + 1, face);
                children[3] = new Chunk(this.position + (((radius * face.axisA) + (radius * face.axisB)) / 2.0), halfRadius, detailLevel + 1, face);

                foreach (var child in children)
                {
                    child.GenerateChildren();
                }
            }
        }

        public void Draw(bool closestToCamera)
        {

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
                Vector3D corner2 = face.water.GetClosestSurfacePoint(face.position + (normal2 * face.water.currentRadius));
                Vector3D corner3 = face.water.GetClosestSurfacePoint(face.position + (normal3 * face.water.currentRadius));
                Vector3D corner4 = face.water.GetClosestSurfacePoint(face.position + (normal4 * face.water.currentRadius));

                Vector3D average = ((corner1 + corner2 + corner3 + corner4) / 4);
                float distToCamera = Vector3.RectangularDistance(average, WaterMod.Session.CameraPosition);

                if (distToCamera > 100)
                {
                    if (Vector3.Dot(Vector3.Normalize(average - WaterMod.Session.CameraPosition), WaterMod.Session.CameraRotation) < WaterData.DotMaxFOV)
                        return;

                    if (closestToCamera && (distToCamera > WaterMod.Session.DistanceToHorizon + (radius * 4)))
                        return;

                    if (Vector3.Dot(Vector3.Normalize(average - WaterMod.Session.CameraPosition), WaterMod.Session.CameraRotation) < 0)
                        return;
                }

                if (WaterUtils.IsUnderGround(face.water.planet, average, radius * 2) && (face.water.planet.GetMaterialAt(ref average) != null && face.water.planet.GetMaterialAt(ref corner1) != null && face.water.planet.GetMaterialAt(ref corner2) != null && face.water.planet.GetMaterialAt(ref corner3) != null && face.water.planet.GetMaterialAt(ref corner4) != null))
                    return;

                Vector4 WaterColor = WaterData.WaterColor;
                Vector4 WaterFadeColor = WaterData.WaterFadeColor;
                Vector4 WhiteColor = Vector4.One;

                if (face.water.lit)
                {
                    float dot = MyMath.Clamp(Vector3.Dot(normal1, WaterMod.Session.SunDirection) + 0.05f, 0.05f, 1f);

                    WaterColor *= dot;
                    WaterColor.W = WaterData.WaterColor.W;
                    WaterFadeColor *= dot;
                    WaterFadeColor.W = WaterData.WaterFadeColor.W;
                    WhiteColor = new Vector4(dot, dot, dot, 1);
                }

                MyQuadD quad = new MyQuadD();
                quad.Point0 = corner1;
                quad.Point1 = corner3;
                quad.Point2 = corner2;
                quad.Point3 = corner4;

                if (face.water.transparent)
                {
                    if (WaterMod.Session.CameraUnderwater || !closestToCamera)
                    {
                        MyTransparentGeometry.AddQuad(face.water.textureId, ref quad, WaterColor * 0.5f, ref refZero);
                    }
                    else
                    {
                        if (detailLevel > 4)
                        {
                            int count = (int)Math.Min(Math.Ceiling(detailLevel - 2f) * 1.25f, 8);

                            for (int i = 0; i < count; i++)
                            {
                                Vector3D layerSeperation = WaterMod.Session.GravityDirection * ((float)i / count) * 20;

                                if (i == count - 1)
                                {
                                    MyQuadD quad2 = quad;
                                    quad2.Point0 += layerSeperation;
                                    quad2.Point1 += layerSeperation;
                                    quad2.Point2 += layerSeperation;
                                    quad2.Point3 += layerSeperation;
                                    MyTransparentGeometry.AddQuad(face.water.textureId, ref quad2, WhiteColor, ref refZero);
                                }
                                else
                                {
                                    if (i == 0)
                                    {
                                        MyTransparentGeometry.AddQuad(face.water.textureId, ref quad, WaterColor, ref refZero);
                                    }
                                    else
                                    {
                                        MyQuadD quad2 = quad;
                                        quad2.Point0 += layerSeperation;
                                        quad2.Point1 += layerSeperation;
                                        quad2.Point2 += layerSeperation;
                                        quad2.Point3 += layerSeperation;
                                        MyTransparentGeometry.AddQuad(face.water.textureId, ref quad2, WaterFadeColor, ref refZero);
                                    }
                                }
                            }
                        }
                        else
                        {
                            MyTransparentGeometry.AddQuad(face.water.textureId, ref quad, WhiteColor, ref refZero);
                        }

                    }
                }
                else
                {
                    Vector3D Seperator = WaterMod.Session.GravityDirection * face.water.waveHeight;
                    MyTransparentGeometry.AddQuad(face.water.textureId, ref quad, WhiteColor, ref refZero);
                    MyQuadD quad2 = quad;
                    quad2.Point0 += Seperator;
                    quad2.Point1 += Seperator;
                    quad2.Point2 += Seperator;
                    quad2.Point3 += Seperator;
                    MyTransparentGeometry.AddQuad(face.water.textureId, ref quad2, WhiteColor, ref refZero);
                }
            }
        }
    }
}
