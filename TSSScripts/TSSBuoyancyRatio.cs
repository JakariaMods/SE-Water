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
    [MyTextSurfaceScript("BuoyancyRatio", "Buoyancy Ratio")]
    class TSSBuoyancyRatio : MyTSSCommon
    {
        public static float ASPECT_RATIO = 3f;
        public static float DECORATION_RATIO = 0.25f;
        public static float TEXT_RATIO = 0.25f;

        private Vector2 _innerSize;
        private StringBuilder _stringBuilder = new StringBuilder();

        private IMyCubeGrid _grid;
        private WaterPhysicsComponentGrid _waterComponent;

        public override ScriptUpdate NeedsUpdate
        {
            get { return ScriptUpdate.Update10; }
        }

        public TSSBuoyancyRatio(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            _innerSize = new Vector2(ASPECT_RATIO, 1.1f);
            FitRect(surface.SurfaceSize, ref _innerSize);

            _grid = block.CubeGrid;
            Block.CubeGrid.Components.TryGet(out _waterComponent);
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
                _stringBuilder.Append("Buoyancy Ratio:");

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

                if (_waterComponent == null)
                    Block.CubeGrid.Components.TryGet(out _waterComponent);

                if (_grid.Physics != null && !_grid.Physics.IsStatic && _grid.Physics.Mass != 0)
                    _stringBuilder.Append(_waterComponent != null ? (_waterComponent.BuoyancyForce.Length() / (_grid.Physics.Mass * _grid.Physics.Gravity.Length()) * 100).ToString("0.0") + "%" : "0%");

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
