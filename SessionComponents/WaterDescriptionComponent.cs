using Jakaria.Configs;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component used to extend block descriptions with info like underwater functionality.
    /// </summary>
    public class WaterDescriptionComponent : SessionComponentBase
    {
        private WaterModComponent _modComponent;

        public static WaterDescriptionComponent Static;

        public WaterDescriptionComponent()
        {
            Static = this;
        }

        public override void LoadDependencies()
        {
            _modComponent = WaterModComponent.Static;
        }

        public override void UnloadDependencies()
        {
            _modComponent = null;

            Static = null;
        }

        public override void LoadData()
        {
            var definitions = MyDefinitionManager.Static.GetAllDefinitions();
            foreach (var definition in definitions)
            {
                if (definition is MyCubeBlockDefinition)
                {
                    MyCubeBlockDefinition blockDefinition = (MyCubeBlockDefinition)definition;

                    if (blockDefinition.Enabled && blockDefinition.Id != null)
                    {
                        BlockConfig blockConfig;
                        if (WaterData.BlockConfigs.TryGetValue(definition.Id, out blockConfig))
                        {
                            if (blockConfig.Volume > (blockDefinition.CubeSize == MyCubeSize.Large ? WaterData.MinVolumeLarge : WaterData.MinVolumeSmall))
                            {
                                blockDefinition.DescriptionString = blockDefinition.DescriptionText + "\n" +
                                    "\nBlock Volume: " + blockConfig.Volume.ToString("0.00") + "m³";
                                blockDefinition.DescriptionEnum = null;
                            }
                            else
                            {
                                blockDefinition.DescriptionString = blockDefinition.DescriptionText + "\n\nBlock Volume: Not Simulated";
                                blockDefinition.DescriptionEnum = null;
                            }

                            if (blockDefinition is MyFunctionalBlockDefinition && !(blockDefinition is MyShipControllerDefinition) && !(blockDefinition is MyPlanterDefinition) && !(blockDefinition is MyKitchenDefinition))
                            {
                                blockDefinition.DescriptionString += "\nFunctional Underwater: " + blockConfig.FunctionalUnderWater;
                                blockDefinition.DescriptionString += "\nFunctional Abovewater: " + blockConfig.FunctionalAboveWater;
                            }
                        }
                    }
                }
            }
        }
    }
}
