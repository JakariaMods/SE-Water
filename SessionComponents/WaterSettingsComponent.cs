using Jakaria.Utils;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace Jakaria.SessionComponents
{
    public class WaterSettingsComponent : SessionComponentBase
    {
        public const string FILE_NAME = "WaterClientSettings.xml";

        public static WaterSettingsComponent Static;

        public WaterClientSettings Settings = WaterClientSettings.Default;

        public WaterSettingsComponent()
        {
            Static = this;
        }

        public override void LoadDependencies()
        {
            
        }

        public override void UnloadDependencies()
        {
            Static = null;
        }

        public override void LoadData()
        {
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInGlobalStorage(FILE_NAME))
                {
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInGlobalStorage(FILE_NAME);
                    if (reader != null)
                    {
                        string xml = reader.ReadToEnd();

                        WaterClientSettings settings = MyAPIGateway.Utilities.SerializeFromXML<WaterClientSettings>(xml);

                        Settings = settings;

                        reader.Close();
                    }
                }
            }
            catch(Exception e)
            {
                WaterUtils.ShowMessage(e.ToString());
                WaterUtils.WriteLog(e.ToString());

                Settings = new WaterClientSettings();
            }

            ClampValues(ref Settings);
        }

        private void ClampValues(ref WaterClientSettings settings)
        {
            settings.Volume = MathHelper.Clamp(settings.Volume, 0, 1);
            settings.Quality = MathHelper.Clamp(settings.Quality, 0.4f, 3f);
        }

        public override void SaveData()
        {
            ClampValues(ref Settings);

            try
            {
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(FILE_NAME);
                if (writer != null)
                {
                    string xml = MyAPIGateway.Utilities.SerializeToXML<WaterClientSettings>(Settings);

                    writer.Write(xml);

                    writer.Close();
                }
            }
            catch (Exception e)
            {
                WaterUtils.ShowMessage(e.ToString());
                WaterUtils.WriteLog(e.ToString());
            }
        }
    }
}
