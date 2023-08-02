using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IController
{
    public abstract void SetPoint(Vector3 point);
    public abstract void SetEnemy(Transform enemy);
}
