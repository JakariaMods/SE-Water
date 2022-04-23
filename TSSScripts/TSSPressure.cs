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
    [MyTextSurfaceScript("Pressure", "Pressure Meter")]
    class TSSPressure : MyTSSCommon
    {
        public static float ASPECT_RATIO = 3f;
        public static float DECORATION_RATIO = 0.25f;
        public static float TEXT_RATIO = 0.25f;

        private Vector2 m_innerSize;

        private StringBuilder m_sb = new StringBuilder();

        WaterPhysicsComponentGrid waterComponent;

        public override ScriptUpdate NeedsUpdate
        {
            get { return ScriptUpdate.Update10; }
        }

        public TSSPressure(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            m_innerSize = new Vector2(ASPECT_RATIO, 1.1f);
            FitRect(surface.SurfaceSize, ref m_innerSize);

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
                m_sb.Append("Pressure:");

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
                m_sb.Append(waterComponent != null ? waterComponent.FluidPressure.ToString("0") + "kPa" : "0kPa");

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
