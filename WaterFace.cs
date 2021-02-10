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
                //MyTransparentGeometry.AddPointBillboard(WaterData.IconMaterial, WaterData.WhiteColor, face.position + (Vector3D.Normalize(position - face.position) * face.radius), (float)radius / 2, 0);

                double tempTime = WaterMod.Session.SessionTimer * 0.0001;

                Vector3D normal1 = Vector3D.Normalize(position + ((-face.axisA + -face.axisB) * radius) - face.position);

                Vector3D corner1 = face.position + (normal1 * face.water.currentRadius);

                float distToCamera = Vector3.RectangularDistance(corner1, WaterMod.Session.CameraPosition);
                //float dotToCamera = Vector3.Dot(Vector3.Normalize(corner1 - WaterMod.Session.CameraPosition), WaterMod.Session.CameraRotation);

                if (closestToCamera && (distToCamera > WaterMod.Session.DistanceToHorizon + (radius * 5) || (distToCamera > 100 && Vector3.Dot(Vector3.Normalize(corner1 - WaterMod.Session.CameraPosition), WaterMod.Session.CameraRotation) < WaterData.DotMaxFOV)) || (!WaterMod.Session.CameraUnderwater && Vector3.Dot(normal1, WaterMod.Session.CameraRotation) > 0.8f))
                    return;

                float offset = (float)((tempTime - (int)tempTime) * 0.5);
                float offset2 = (float)((tempTime - (int)tempTime + 1) * 0.5);

                Vector2 uv1 = new Vector2(offset, 0);
                Vector2 uv2 = new Vector2(offset2, 1);
                Vector2 uv3 = new Vector2(offset, 1);
                Vector2 uv4 = new Vector2(offset2, 0);

                Vector3D normal2 = Vector3D.Normalize(position + ((face.axisA + face.axisB) * radius) - face.position);
                Vector3D normal3 = Vector3D.Normalize(position + ((-face.axisA + face.axisB) * radius) - face.position);
                Vector3D normal4 = Vector3D.Normalize(position + ((face.axisA + -face.axisB) * radius) - face.position);

                Vector3D corner2 = face.position + (normal2 * face.water.currentRadius);

                if ((distToCamera > 100 && Vector3.Dot(Vector3.Normalize(corner2 - WaterMod.Session.CameraPosition), WaterMod.Session.CameraRotation) < WaterData.DotMaxFOV))
                    return;

                Vector3D corner3;
                Vector3D corner4;

                if (radius < 100)
                {
                    corner1 = face.water.GetClosestSurfacePoint(corner1);
                    corner2 = face.water.GetClosestSurfacePoint(corner2);
                    corner3 = face.water.GetClosestSurfacePoint(face.position + (normal3 * face.water.currentRadius));
                    corner4 = face.water.GetClosestSurfacePoint(face.position + (normal4 * face.water.currentRadius));

                    if (WaterUtils.GetAltitude(face.water.planet, corner1) < radius + radius && WaterUtils.IsUnderGround(face.water.planet, corner3, radius + radius))
                        return;

                    if (WaterMod.Session.CameraAirtight && WaterUtils.IsPositionAirtight(corner1))
                        return;
                }
                else
                {
                    corner3 = face.position + (normal3 * face.water.currentRadius);
                    corner4 = face.position + (normal4 * face.water.currentRadius);
                }

                Vector4 WaterColor = WaterData.WaterColor;
                Vector4 WaterFadeColor = WaterData.WaterFadeColor;
                Vector4 WhiteColor = Vector4.One;

                if (face.water.lit)
                {
                    float dot = MyMath.Clamp(Vector3.Dot(normal1, WaterMod.Session.SunDirection) + 0.05f, 0.01f, 1f);

                    WaterColor *= dot;
                    WaterColor.W = WaterData.WaterColor.W;
                    WaterFadeColor *= dot;
                    WaterFadeColor.W = WaterData.WaterFadeColor.W;
                    WhiteColor = new Vector4(dot, dot, dot, 1);
                }

                if (face.water.transparent)
                {
                    if (WaterMod.Session.CameraUnderwater || !closestToCamera)
                    {
                        MyTransparentGeometry.AddTriangleBillboard(corner1, corner2, corner3, normal1, normal2, normal3, uv2, uv1, uv3, face.water.textureId, 0, Vector3D.Zero, WaterColor * 0.5f);
                        MyTransparentGeometry.AddTriangleBillboard(corner1, corner4, corner2, normal1, normal4, normal2, uv2, uv4, uv1, face.water.textureId, 0, Vector3D.Zero, WaterColor * 0.5f);
                    }
                    else
                    {
                        if (detailLevel > 4)
                        {
                            int count = (int)(Math.Ceiling(detailLevel - 2f) * 1.25f);

                            for (int i = 0; i < count; i++)
                            {
                                Vector3D layerSeperation = WaterMod.Session.GravityDirection * ((float)i / count) * 10;

                                if (i == count - 1)
                                {
                                    MyTransparentGeometry.AddTriangleBillboard(corner1 + layerSeperation, corner2 + layerSeperation, corner3 + layerSeperation, normal1, normal2, normal3, uv2, uv1, uv3, face.water.textureId, 0, Vector3D.Zero, WhiteColor);
                                    MyTransparentGeometry.AddTriangleBillboard(corner1 + layerSeperation, corner4 + layerSeperation, corner2 + layerSeperation, normal1, normal4, normal2, uv2, uv4, uv1, face.water.textureId, 0, Vector3D.Zero, WhiteColor);
                                }
                                else
                                {
                                    if (i == 0)
                                    {
                                        MyTransparentGeometry.AddTriangleBillboard(corner1, corner2, corner3, normal1, normal2, normal3, uv2, uv1, uv3, face.water.textureId, 0, Vector3D.Zero, WaterColor);
                                        MyTransparentGeometry.AddTriangleBillboard(corner1, corner4, corner2, normal1, normal4, normal2, uv2, uv4, uv1, face.water.textureId, 0, Vector3D.Zero, WaterColor);
                                    }
                                    else
                                    {
                                        MyTransparentGeometry.AddTriangleBillboard(corner1 + layerSeperation, corner2 + layerSeperation, corner3 + layerSeperation, normal1, normal2, normal3, uv1, uv2, uv3, face.water.textureId, 0, Vector3D.Zero, WaterFadeColor);
                                        MyTransparentGeometry.AddTriangleBillboard(corner1 + layerSeperation, corner4 + layerSeperation, corner2 + layerSeperation, normal1, normal4, normal2, uv1, uv4, uv2, face.water.textureId, 0, Vector3D.Zero, WaterFadeColor);
                                    }
                                }
                            }
                        }
                        else
                        {
                            MyTransparentGeometry.AddTriangleBillboard(corner1, corner2, corner3, normal1, normal2, normal3, uv2, uv1, uv3, face.water.textureId, 0, Vector3D.Zero, WhiteColor);
                            MyTransparentGeometry.AddTriangleBillboard(corner1, corner4, corner2, normal1, normal4, normal2, uv2, uv4, uv1, face.water.textureId, 0, Vector3D.Zero, WhiteColor);
                        }

                    }
                }
                else
                {
                    Vector3 Seperator = WaterMod.Session.GravityDirection * face.water.waveHeight;
                    MyTransparentGeometry.AddTriangleBillboard(corner1, corner2, corner3, normal1, normal2, normal3, uv2, uv1, uv3, face.water.textureId, 0, Vector3D.Zero, WhiteColor);
                    MyTransparentGeometry.AddTriangleBillboard(corner1, corner4, corner2, normal1, normal4, normal2, uv2, uv4, uv1, face.water.textureId, 0, Vector3D.Zero, WhiteColor);
                    MyTransparentGeometry.AddTriangleBillboard(corner1 + Seperator, corner2 + Seperator, corner3 + Seperator, normal1, normal2, normal3, uv1, uv2, uv3, face.water.textureId, 0, Vector3D.Zero, WhiteColor);
                    MyTransparentGeometry.AddTriangleBillboard(corner1 + Seperator, corner4 + Seperator, corner2 + Seperator, normal1, normal4, normal2, uv1, uv4, uv2, face.water.textureId, 0, Vector3D.Zero, WhiteColor);
                }
            }
        }
    }
}
