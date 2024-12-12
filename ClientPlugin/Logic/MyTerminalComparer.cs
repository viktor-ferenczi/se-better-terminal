using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;

namespace ClientPlugin.Logic
{
    // Copied directly
    internal class MyTerminalComparer : IComparer<MyTerminalBlock>, IComparer<MyBlockGroup>
    {
        public static MyTerminalComparer Static = new MyTerminalComparer();

        public int Compare(MyTerminalBlock lhs, MyTerminalBlock rhs)
        {
            int num = ((lhs.CustomName != null) ? lhs.CustomName.ToString() : lhs.DefinitionDisplayNameText).CompareTo((rhs.CustomName != null) ? rhs.CustomName.ToString() : rhs.DefinitionDisplayNameText);
            if (num != 0)
            {
                return num;
            }

            if (lhs.NumberInGrid != rhs.NumberInGrid)
            {
                return lhs.NumberInGrid.CompareTo(rhs.NumberInGrid);
            }

            return 0;
        }

        public int Compare(MyBlockGroup x, MyBlockGroup y)
        {
            return x.Name.CompareTo(y.Name);
        }
    }
}