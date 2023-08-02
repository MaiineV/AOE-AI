using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class NPC : MonoBehaviour, ILife
{
    [SerializeField] protected float _maxLife;
    protected float _life;
    [SerializeField] protected float _normalSpeed;
    [SerializeField] protected float _fleeSpeed;
    protected float _actualSpeed;
    [SerializeField] protected float _rotationSpeed;
    [SerializeField] protected float _attackRange;
    [SerializeField] protected float _dmg;
    [SerializeField] protected float _attackTime;
    [SerializeField] protected float _detectionRange;

    protected bool _isDead = false;

    protected float _timer;

    [SerializeField] protected bool _isBlueTeam;

    [SerializeField] protected Transform _followTransform;

    protected Coroutine _dmgCoroutine;
    [SerializeField] protected Renderer _renderer;
    [SerializeField] protected Material _baseMat;
    [SerializeField] protected Material _dmgMat;

    protected Path _actualPath;
    protected Vector3 _actualNode;

    protected Vector3 _baseDir;
    protected Vector3 _obstacleDir;

    private IEnumerable<Vector3> _dirsToCheck => new Vector3[]
    {
        Vector3.forward,
        Vector3.back,
        Vector3.right,
        Vector3.left,
        Vector3.forward + Vector3.right,
        Vector3.forward + Vector3.left,
        Vector3.back + Vector3.right,
        Vector3.back + Vector3.left,
    };

    private Ray _actualRay;
    [SerializeField] private float _obstacleDist;
    [SerializeField] [Range(0,1)] protected float _obstacleWeight;

    protected void ObstacleAvoidance()
    {
        _obstacleDir = Vector3.zero;
        foreach (var dir in _dirsToCheck)
        {
            _actualRay = new Ray(transform.position, dir);
            if (Physics.Raycast(_actualRay, _obstacleDist, LayerManager.LM_ALLOBSTACLE))
            {
                _obstacleDir += dir.normalized * -1;
            }
        }
    }

    public abstract void Damage(float dmg);

    public abstract void Health(float health);

    protected IEnumerator ResetMat()
    {
        _renderer.material = _dmgMat;
        yield return new WaitForSeconds(0.5f);
        _renderer.material = _baseMat;
    }

    protected void Death()
    {
        if (_dmgCoroutine != null)
        {
            StopCoroutine(_dmgCoroutine);
        }

        _isDead = true;

        transform.DetachChildren();
        _followTransform.parent = transform;

        Destroy(gameObject);
    }
}