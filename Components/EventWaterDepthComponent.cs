/*using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;
using SpaceEngineers.Game.EntityComponents.Blocks;
using VRage.Sync;
using Sandbox.Definitions;
using VRage.Game.ModAPI.Network;
using Sandbox.Game.Gui;
using VRage.Game.ModAPI;
using Sandbox.Game.Multiplayer;
using Jakaria.Utils;

namespace Jakaria.Components
{
    [MyComponentType(typeof(EventWaterDepthComponent))]
    [MyEntityDependencyType(typeof(IMyEventControllerBlock))]
    [MyComponentBuilder(typeof(MyObjectBuilder_EventWaterDepthComponent))]
    public class EventWaterDepthComponent : MyEventProxyEntityComponent, IMyEventComponentWithGui
    {
        private MySync<float, SyncDirection.BothWays> _depthTrigger;
        private IMyTerminalControlSlider _depthSlider;
        //private MyEventControllerGenericEvent<IMyCubeGrid> _eventGeneric;

        private MyObjectBuilder_ModCustomComponent _deserializeBuilder;

        private const float MIN_DEPTH = -1000;
        private const float MAX_DEPTH = 1000;

        private float _previousDepth;
        private float _depth;

        public override string ComponentTypeDebugString => nameof(EventWaterDepthComponent);

        public bool IsThresholdUsed => false;

        public bool IsBlocksListUsed => false;

        public bool IsConditionSelectionUsed => true;

        public long UniqueSelectionId { get { return 1000; } }

        public bool IsSelected { get; set; }

        public MyStringId EventDisplayName => MyStringId.GetOrCompute("Depth");

        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);
        }

        private void UpdateDetailedInfo(int slot, long entityId, float value)
        {
            IMyTerminalBlock myTerminalBlock;
            IMyEventControllerBlock myEventControllerBlock;
            if (!MyAPIGateway.Utilities.IsDedicated && (myTerminalBlock = base.Entity as IMyTerminalBlock) != null && (myEventControllerBlock = base.Entity as IMyEventControllerBlock) != null)
            {
                myTerminalBlock.ClearDetailedInfo();
                //_eventGeneric.UpdateDetailedInfo(myTerminalBlock.GetDetailedInfo(), _depthTrigger, slot, entityId, value, myEventControllerBlock.IsLowerOrEqualCondition);
                myTerminalBlock.SetDetailedInfoDirty();
            }
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            _depthTrigger.ValueChanged += _depthTrigger_ValueChanged;
            GetGrid().PositionComp.OnPositionChanged += PositionComp_OnPositionChanged;
        }

        private void _depthTrigger_ValueChanged(MySync<float, SyncDirection.BothWays> obj)
        {
            UpdateTerminal();
        }

        private void UpdateTerminal()
        {

        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            _depthTrigger.ValueChanged -= _depthTrigger_ValueChanged;
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var customData = new MyObjectBuilder_EventWaterDepthComponent();
            customData.DepthTrigger = _depthTrigger;
            var data = MyAPIGateway.Utilities.SerializeToBinary<MyObjectBuilder_EventWaterDepthComponent>(customData);

            var builder = new MyObjectBuilder_ModCustomComponent();
            builder.ComponentType = GetType().Name;
            builder.CustomModData = Convert.ToBase64String(data);
            return builder;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);
            _deserializeBuilder = builder as MyObjectBuilder_ModCustomComponent;

            if (_deserializeBuilder != null && !string.IsNullOrEmpty(_deserializeBuilder.CustomModData))
            {
                var customData = MyAPIGateway.Utilities.SerializeFromBinary<MyObjectBuilder_EventWaterDepthComponent>(Convert.FromBase64String(_deserializeBuilder.CustomModData));

                _depthTrigger.ValidateRange(MIN_DEPTH, MAX_DEPTH);
                if (_deserializeBuilder != null)
                {
                    _depthTrigger.SetLocalValue(customData.DepthTrigger);
                    _deserializeBuilder = null;
                }
                else
                {
                    _depthTrigger.SetLocalValue(MIN_DEPTH);
                }
            }
        }

        private IMyCubeGrid GetGrid()
        {
            IMyCubeBlock myCubeBlock;
            if ((myCubeBlock = base.Container?.Entity as IMyCubeBlock) != null)
            {
                return myCubeBlock.CubeGrid;
            }

            return null;
        }

        public override void SetContainer(MyComponentContainer container)
        {
            base.SetContainer(container);
            IMyCubeGrid grid;
            if (IsSelected && (grid = GetGrid()) != null)
            {
                grid.PositionComp.OnPositionChanged += PositionComp_OnPositionChanged;
            }
        }

        private void PositionComp_OnPositionChanged(MyPositionComponentBase obj)
        {
            MyAPIGateway.Utilities.ShowNotification($"{_depth} < {_depthTrigger.Value}", 16);

            IMyEventControllerBlock block;
            if (IsSelected && (block = base.Entity as IMyEventControllerBlock) != null)
            {
                _previousDepth = _depth;
                _depth = GetDepth();

                if(_depth < _depthTrigger.Value && _previousDepth > _depthTrigger.Value)
                {
                    MyAPIGateway.Utilities.ShowNotification("Activate");
                    block.TriggerAction(0);
                }
                else if (_depth > _depthTrigger.Value && _previousDepth < _depthTrigger.Value)
                {
                    MyAPIGateway.Utilities.ShowNotification("Deactivate");
                    block.TriggerAction(1);
                }
            }
        }

        private float GetDepth()
        {
            IMyCubeGrid grid;
            WaterPhysicsComponentGrid gridComponent;
            if ((grid = GetGrid()) != null && grid.Components.TryGet<WaterPhysicsComponentGrid>(out gridComponent))
            {
                if (gridComponent.ClosestWater != null)
                    return (float)gridComponent.FluidDepth;
            }

            return float.MaxValue;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            IMyCubeGrid grid;
            if ((grid = GetGrid()) != null)
            {
                grid.PositionComp.OnPositionChanged -= PositionComp_OnPositionChanged;
            }

            base.OnBeforeRemovedFromContainer();
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public bool IsBlockValidForList(IMyTerminalBlock block)
        {
            return false;
        }

        public void AddBlocks(List<IMyTerminalBlock> blocks)
        {
        }

        public void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks)
        {
        }

        public void NotifyValuesChanged()
        {
            *//*IMyEventControllerBlock eventBlock;
            if ((eventBlock = base.Entity as IMyEventControllerBlock) != null)
            {
                m_eventGeneric.NotifyValuesChanged(eventBlock, m_heightTrigger);
            }*//*
        }

        public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock
        {
            _depthSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>("EventWaterDepthComponentSlider");
            _depthSlider.Title = MyStringId.GetOrCompute("Depth");
            _depthSlider.Tooltip = MyStringId.GetOrCompute("Negative values are below the fluid, Positive values are above the fluid");
            _depthSlider.Getter = GetValue;
            _depthSlider.Setter = SetValue;
            _depthSlider.Visible = IsVisible;
            _depthSlider.SetLimits(MIN_DEPTH, MAX_DEPTH);
            _depthSlider.Writer = (block, builder) =>
            {
                builder.Append(MyValueFormatter.GetFormatedFloat(GetValue(block), 1));
            };

            MyAPIGateway.TerminalControls.AddControl<T>(_depthSlider);
        }

        private static float GetValue(IMyTerminalBlock block)
        {
            EventWaterDepthComponent component = null;
            if (block.Components.TryGet<EventWaterDepthComponent>(out component))
            {
                return component._depthTrigger;
            }

            return 0f;
        }

        private static void SetValue(IMyTerminalBlock block, float newValue)
        {
            EventWaterDepthComponent component = null;
            if (block.Components.TryGet<EventWaterDepthComponent>(out component))
            {
                component._depthTrigger.Value = newValue;
                component.NotifyValuesChanged();
            }
        }

        private static bool IsVisible(IMyTerminalBlock block)
        {
            EventWaterDepthComponent component = null;
            if (block.Components.TryGet<EventWaterDepthComponent>(out component))
            {
                return component.IsSelected;
            }

            return false;
        }
    }

    [ProtoBuf.ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EventWaterDepthComponent : MyObjectBuilder_ComponentBase
    {
        [ProtoMember(1)]
        public float DepthTrigger;
    }
}
*/