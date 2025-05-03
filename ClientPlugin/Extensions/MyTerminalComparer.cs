using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using VRage.Scripting.MemorySafeTypes;

namespace ClientPlugin.Extensions
{
    // Verbatim copy of internal class MyTerminalComparer from Sandbox.Game.Screens.Helpers
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    internal class MyTerminalComparer : IComparer<MyTerminalBlock>, IComparer<MyBlockGroup>
    {
        public static readonly MyTerminalComparer Static = new MyTerminalComparer();

        public int Compare(MyTerminalBlock lhs, MyTerminalBlock rhs)
        {
            // ReSharper disable once StringCompareToIsCultureSpecific
            var num = (
                lhs.CustomName != null
                    ? lhs.CustomName.ToString()
                    : lhs.DefinitionDisplayNameText).CompareTo(rhs.CustomName != null
                ? rhs.CustomName.ToString()
                : rhs.DefinitionDisplayNameText);
            if (num != 0)
                return num;
            return lhs.NumberInGrid != rhs.NumberInGrid ? lhs.NumberInGrid.CompareTo(rhs.NumberInGrid) : 0;
        }

        public int Compare(MyBlockGroup x, MyBlockGroup y) => x.Name.CompareTo(y.Name);
    }
}