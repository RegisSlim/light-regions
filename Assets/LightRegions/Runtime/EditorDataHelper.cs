using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EditorDataHelper
{
    public static int RegionLayer;
    public static int InfluenceLayer;
    public static LayerMask RegionInclude;
    public static LayerMask RegionExclude;
    public static LayerMask InfluenceInclude;
    public static LayerMask InfluenceExclude;

    public static void BuildMasks()
    {

        RegionInclude = 1 << RegionLayer;
        RegionExclude = ~(1 << RegionLayer);
        InfluenceInclude = 1 << InfluenceLayer;
        InfluenceExclude = ~(1 << InfluenceLayer);
    }
}
