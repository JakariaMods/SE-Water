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

        private Vector2 _innerSize;
        private StringBuilder _stringBuilder = new StringBuilder();

        private WaterPhysicsComponentGrid _waterComponent;

        public override ScriptUpdate NeedsUpdate
        {
            get { return ScriptUpdate.Update10; }
        }

        public TSSPressure(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            _innerSize = new Vector2(ASPECT_RATIO, 1.1f);
            FitRect(surface.SurfaceSize, ref _innerSize);

            Block.CubeGrid.Components.TryGet<WaterPhysicsComponentGrid>(out _waterComponent);
        }

        public override void Run()
        {
            base.Run();

            using (var frame = m_surface.DrawFrame())
            {
                AddBackground(frame, new Color(m_backgroundColor, .66f));

                if (m_block == null)
                    return;

                _stringBuilder.Clear();
                _stringBuilder.Append("Pressure:");

                var Text = new MySprite()
                {
                    Position = new Vector2(m_halfSize.X, m_halfSize.Y - 16),
                    Size = new Vector2(_innerSize.X, _innerSize.Y),
                    Type = SpriteType.TEXT,
                    FontId = m_fontId,
                    Alignment = TextAlignment.CENTER,
                    Color = m_foregroundColor,
                    RotationOrScale = m_fontScale,
                    Data = _stringBuilder.ToString(),
                };

                frame.Add(Text);
                _stringBuilder.Clear();
                _stringBuilder.Append(_waterComponent != null ? _waterComponent.FluidPressure.ToString("0") + "kPa" : "0kPa");

                var Text2 = new MySprite()
                {
                    Position = new Vector2(m_halfSize.X, m_halfSize.Y + 16),
                    Size = new Vector2(_innerSize.X, _innerSize.Y),
                    Type = SpriteType.TEXT,
                    FontId = m_fontId,
                    Alignment = TextAlignment.CENTER,
                    Color = m_foregroundColor,
                    RotationOrScale = m_fontScale,
                    Data = _stringBuilder.ToString(),

                };
                frame.Add(Text2);

                AddBrackets(frame, new Vector2(64, 256), _innerSize.Y / 256 * 0.9f, (m_size.X - _innerSize.X) / 2);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
