using System;
using System.Text;
using Jakaria.Components;
using Jakaria.Utils;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.Localization;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace Jakaria.TSS
{
    [MyTextSurfaceScript("Buoyancy", "Buoyant Force")]
    class TSSBuoyancy : MyTSSCommon
    {
        public static float ASPECT_RATIO = 3f;
        public static float DECORATION_RATIO = 0.25f;
        public static float TEXT_RATIO = 0.25f;

        private Vector2 m_innerSize;

        private StringBuilder m_sb = new StringBuilder();

        IMyCubeGrid grid;
        WaterPhysicsComponentGrid waterComponent;

        public override ScriptUpdate NeedsUpdate
        {
            get { return ScriptUpdate.Update10; }
        }

        public TSSBuoyancy(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            m_innerSize = new Vector2(ASPECT_RATIO, 1.1f);
            FitRect(surface.SurfaceSize, ref m_innerSize);

            grid = block.CubeGrid;
            Block.CubeGrid.Components.TryGet<WaterPhysicsComponentGrid>(out waterComponent);
        }

        public override void Run()
        {
            base.Run();

            using (var frame = m_surface.DrawFrame())
            {
                AddBackground(frame, new Color(m_backgroundColor, .66f));

                if (m_block == null)
                    return;

                m_sb.Clear();
                m_sb.Append("Buoyancy:");

                var Text = new MySprite()
                {
                    Position = new Vector2(m_halfSize.X, m_halfSize.Y - 16),
                    Size = new Vector2(m_innerSize.X, m_innerSize.Y),
                    Type = SpriteType.TEXT,
                    FontId = m_fontId,
                    Alignment = TextAlignment.CENTER,
                    Color = m_foregroundColor,
                    RotationOrScale = m_fontScale,
                    Data = m_sb.ToString(),
                };

                frame.Add(Text);
                m_sb.Clear();

                if (waterComponent == null)
                    Block.CubeGrid.Components.TryGet<WaterPhysicsComponentGrid>(out waterComponent);

                if (grid.Physics != null && !grid.Physics.IsStatic && grid.Physics.Mass != 0)
                    m_sb.Append(waterComponent != null ? waterComponent.BuoyancyForce.Length().ToString("0") + "N" : "0N");

                var Text2 = new MySprite()
                {
                    Position = new Vector2(m_halfSize.X, m_halfSize.Y + 16),
                    Size = new Vector2(m_innerSize.X, m_innerSize.Y),
                    Type = SpriteType.TEXT,
                    FontId = m_fontId,
                    Alignment = TextAlignment.CENTER,
                    Color = m_foregroundColor,
                    RotationOrScale = m_fontScale,
                    Data = m_sb.ToString(),

                };
                frame.Add(Text2);

                AddBrackets(frame, new Vector2(64, 256), m_innerSize.Y / 256 * 0.9f, (m_size.X - m_innerSize.X) / 2);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
