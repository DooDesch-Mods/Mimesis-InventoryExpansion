using System;

namespace InventoryExpansion.Patches
{
    internal static class BackpackLayout
    {
        internal static int[] Rows(int slots)
        {
            if (slots < 0) throw new ArgumentOutOfRangeException(nameof(slots));
            if (slots == 0) return Array.Empty<int>();
            int rows = (slots + 7) / 8;
            var result = new int[rows];
            for (int i = 0; i < rows; i++) result[i] = slots / rows + (i < slots % rows ? 1 : 0);
            return result;
        }
    }
}
