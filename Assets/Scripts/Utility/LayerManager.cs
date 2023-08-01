using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LayerManager
{
    public static int L_NODE = 4;
    public static int L_WALL = 5;
    public static int L_FLOOR = 5;
    public static int L_OBSTACLE = 6;

    public static LayerMask LM_NODE = 1 << L_NODE;
    public static LayerMask LM_WALL = 1 << L_WALL;
    public static LayerMask LM_FLOOR = 1 << L_FLOOR;
    public static LayerMask LM_OBSTACLE = 1 << L_OBSTACLE;
    
    public static LayerMask LM_ALLOBSTACLE = LM_OBSTACLE |LM_WALL| LM_FLOOR;
    public static LayerMask LM_NODEOBSTACLE = LM_OBSTACLE |LM_WALL;
}
