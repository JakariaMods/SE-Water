/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jakaria.API
{
    public class WaterAPICore
    {
        public Dictionary<string, Delegate> Methods = new Dictionary<string, Delegate>()
        {
            ["Foo"] = new Func<bool>(Foo),
            ["Bar"] = new Action(Bar),
        };

        static bool Foo()
        {
            return true;
        }

        static void Bar()
        {
            return;
        }

        public void AA()
        {
            bool thing = (bool)(Methods["Foo"] as Func<bool>).Invoke();

            (Methods["Bar"] as Action)?.Invoke();
        }
    }
}
*/