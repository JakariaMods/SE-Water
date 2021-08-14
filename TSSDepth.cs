using System;
using System.Text;
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

namespace Jakaria
{
    [MyTextSurfaceScript("Depth", "Depth Meter")]
    class TSSDepth : MyTSSCommon
    {
        public static float ASPECT_RATIO = 3f;
        public static float DECORATION_RATIO = 0.25f;
        public static float TEXT_RATIO = 0.25f;

        private Vector2 m_innerSize;
        private Vector2 m_decorationSize;

        private float m_firstLine;
        private float m_secondLine;

        private StringBuilder m_sb = new StringBuilder();

        public override ScriptUpdate NeedsUpdate
        {
            get { return ScriptUpdate.Update10; }
        }

        public TSSDepth(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            m_innerSize = new Vector2(ASPECT_RATIO, 1.1f);
            FitRect(surface.SurfaceSize, ref m_innerSize);

            m_decorationSize = new Vector2(0.012f * m_innerSize.X, DECORATION_RATIO * m_innerSize.Y);

            m_firstLine = m_halfSize.Y - m_decorationSize.Y * 0.55f;
            m_secondLine = m_halfSize.Y + m_decorationSize.Y * 0.55f;
        }

        public override void Run()
        {
            base.Run();

            using (var frame = m_surface.DrawFrame())
            {
                AddBackground(frame, new Color(m_backgroundColor, .66f));

                if (m_block == null)
                    return;

                Water water = WaterMod.Static.GetClosestWater(Block.GetPosition());

                m_sb.Clear();
                m_sb.Append("Depth:");

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
                Vector3D blockPosition = Block.GetPosition();
                m_sb.Append(water != null ? Math.Round(-water.GetDepth(ref blockPosition), 2).ToString() + "m" : "0m");
                
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
