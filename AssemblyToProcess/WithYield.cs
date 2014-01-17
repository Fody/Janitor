using System;
using System.Collections.Generic;

public class WithYield
{
    public IEnumerable<int> Numbers()
    {
        // foreach creates an iterator state machine
        // with code in the Dispose method to dispose
        // of the enumerable.
        foreach (var n in new int[] { 0, 1 })
        {
            yield return n;
        }
    }
}
