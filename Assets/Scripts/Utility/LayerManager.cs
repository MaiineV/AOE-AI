using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LayerManager
{
    private static int L_BLUETEAM = 6;
    private static int L_REDTEAM = 7;
    private static int L_FLOOR = 8;
    private static int L_OBSTACLE = 9;
    private static int L_WALL = 10;
    private static int L_NODE = 11;
    private static int L_HIDINGSPOT = 12;
    private static int L_BLUEKING = 13;
    private static int L_REDKING = 14;

    public static LayerMask LM_BLUETEAM = 1 << L_BLUETEAM;
    public static LayerMask LM_REDTEAM = 1 << L_REDTEAM;
    public static LayerMask LM_FLOOR = 1 << L_FLOOR;
    public static LayerMask LM_OBSTACLE = 1 << L_OBSTACLE;
    public static LayerMask LM_WALL = 1 << L_WALL;
    public static LayerMask LM_NODE = 1 << L_NODE;
    public static LayerMask LM_HIDINGSPOT = 1 << L_HIDINGSPOT;
    public static LayerMask LM_BLUEKING = 1 << L_BLUEKING;
    public static LayerMask LM_REDKING = 1 << L_REDKING;
    
    public static LayerMask LM_ALLOBSTACLE = LM_OBSTACLE |LM_WALL| LM_FLOOR;
    public static LayerMask LM_ENEMIES = LM_BLUETEAM | LM_REDTEAM;
    public static LayerMask LM_OBSTACLESANDENEMIES = LM_OBSTACLE |LM_WALL| LM_FLOOR | LM_ENEMIES;
    public static LayerMask LM_NODEOBSTACLE = LM_OBSTACLE |LM_WALL;
}
