using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILife
{
    public abstract void Damage(float dmg);
    public abstract void Health(float health);
}
