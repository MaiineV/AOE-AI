using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = System.Random;

public enum KnightState
{
    Idle,
    Follow,
    Attack,
    Chase,
    Runaway,
    Pathfinding
}

public class Knight : NPC
{
    private EventFSM<KnightState> _fsm;
    private KnightState _previousState;

    [SerializeField] private float _followDistance;
    [SerializeField] private float _escapeLife;


    [SerializeField] [Range(0, 1)] private float _flockingWeight;
    [SerializeField] private float _flockingRange;
    private Vector3 _flockingVector;

    private void Awake()
    {
        _life = _maxLife;
        _actualSpeed = _normalSpeed;
        DoFsmSetup();
    }

    private void DoFsmSetup()
    {
        #region SETUP

        var idle = new State<KnightState>("Idle");
        var follow = new State<KnightState>("Follow");
        var attack = new State<KnightState>("Attack");
        var chase = new State<KnightState>("Chase");
        var runaway = new State<KnightState>("Runaway");
        var pathFinding = new State<KnightState>("Pathfinding");

        StateConfigurer.Create(idle)
            .SetTransition(KnightState.Follow, follow)
            .SetTransition(KnightState.Chase, chase)
            .SetTransition(KnightState.Runaway, runaway)
            .SetTransition(KnightState.Attack, attack)
            .SetTransition(KnightState.Pathfinding, pathFinding)
            .Done();

        StateConfigurer.Create(follow)
            .SetTransition(KnightState.Idle, idle)
            .SetTransition(KnightState.Chase, chase)
            .SetTransition(KnightState.Runaway, runaway)
            .SetTransition(KnightState.Attack, attack)
            .SetTransition(KnightState.Pathfinding, pathFinding)
            .Done();

        StateConfigurer.Create(attack)
            .SetTransition(KnightState.Idle, idle)
            .SetTransition(KnightState.Follow, follow)
            .SetTransition(KnightState.Chase, chase)
            .SetTransition(KnightState.Runaway, runaway)
            .SetTransition(KnightState.Pathfinding, pathFinding)
            .Done();

        StateConfigurer.Create(chase)
            .SetTransition(KnightState.Idle, idle)
            .SetTransition(KnightState.Follow, follow)
            .SetTransition(KnightState.Runaway, runaway)
            .SetTransition(KnightState.Attack, attack)
            .SetTransition(KnightState.Pathfinding, pathFinding)
            .Done();

        StateConfigurer.Create(runaway)
            .SetTransition(KnightState.Idle, idle)
            .SetTransition(KnightState.Follow, follow)
            .SetTransition(KnightState.Chase, chase)
            .SetTransition(KnightState.Attack, attack)
            .SetTransition(KnightState.Pathfinding, pathFinding)
            .Done();

        StateConfigurer.Create(pathFinding)
            .SetTransition(KnightState.Idle, idle)
            .SetTransition(KnightState.Follow, follow)
            .SetTransition(KnightState.Chase, chase)
            .SetTransition(KnightState.Runaway, runaway)
            .SetTransition(KnightState.Attack, attack)
            .Done();

        #endregion

        #region IDLE

        idle.OnEnter += x =>
        {
            _followTransform.parent = null;
            _followTransform.position = transform.position;
        };

        idle.OnUpdate += () =>
        {
            Flocking();
            var colliders = Physics.OverlapSphere(transform.position, _detectionRange,
                _isBlueTeam ? LayerManager.LM_REDTEAM : LayerManager.LM_BLUETEAM);

            if (colliders.Any())
            {
                if (_life > _escapeLife)
                {
                    _followTransform.parent = colliders[0].transform;
                    _followTransform.localPosition = Vector3.zero;
                    SendInputToFSM(KnightState.Chase);
                }
                else
                {
                    SendInputToFSM(KnightState.Runaway);
                }

                return;
            }

            colliders = Physics.OverlapSphere(transform.position, _detectionRange,
                _isBlueTeam ? LayerManager.LM_BLUEKING : LayerManager.LM_REDKING);

            if (!colliders.Any()) return;

            var king = colliders.Select(x => x.GetComponent<King>());

            if (!king.Any()) return;

            _followTransform.parent = king.First().transform;
            _followTransform.localPosition = Vector3.zero;
            SendInputToFSM(KnightState.Follow);
        };

        idle.OnExit += x => { _previousState = KnightState.Idle; };

        #endregion

        #region FOLLOW

        follow.OnUpdate += () =>
        {
            var colliders = Physics.OverlapSphere(transform.position, _detectionRange,
                _isBlueTeam ? LayerManager.LM_REDTEAM : LayerManager.LM_BLUETEAM);

            if (colliders.Any())
            {
                if (_life > _escapeLife)
                {
                    _followTransform.parent = colliders[0].transform;
                    _followTransform.localPosition = Vector3.zero;
                    SendInputToFSM(KnightState.Chase);
                }
                else
                {
                    SendInputToFSM(KnightState.Runaway);
                }

                return;
            }

            if (!MPathfinding.OnSight(transform.position, _followTransform.position))
            {
                SendInputToFSM(KnightState.Pathfinding);
                return;
            }

            Flocking();
            var dir = _followTransform.position - transform.position;

            if (dir.magnitude < _followDistance) return;

            _baseDir = dir.normalized;
        };

        follow.OnExit += x => { _previousState = KnightState.Follow; };

        #endregion

        #region CHASE

        chase.OnUpdate += () =>
        {
            if (!MPathfinding.OnSight(transform.position, _followTransform.position))
            {
                SendInputToFSM(KnightState.Pathfinding);
                return;
            }
            
            Flocking();
            var dir = _followTransform.position - transform.position;
            _baseDir = dir.normalized;

            if (dir.magnitude < _attackRange)
            {
                SendInputToFSM(KnightState.Attack);
            }
            else if (dir.magnitude > _detectionRange * 2)
            {
                SendInputToFSM(KnightState.Idle);
            }
        };

        chase.OnExit += x => { _previousState = KnightState.Chase; };

        #endregion

        #region RUNAWAY

        runaway.OnEnter += x =>
        {
            _actualSpeed = _fleeSpeed;

            var actualRange = 200;
            var watchdog = 5;
            var colliders = Physics.OverlapSphere(transform.position, actualRange, LayerManager.LM_HIDINGSPOT);

            while (!colliders.Any())
            {
                if (watchdog < 0)
                {
                    SendInputToFSM(KnightState.Idle);
                    return;
                }

                actualRange *= 2;
                colliders = Physics.OverlapSphere(transform.position, actualRange, LayerManager.LM_HIDINGSPOT);

                watchdog--;
            }

            var closestHidingSpot =
                colliders.OrderBy(h => Vector3.Distance(transform.position, h.transform.position)).ToArray();


            _followTransform.parent =
                closestHidingSpot[UnityEngine.Random.Range(0, closestHidingSpot.Length)].transform;
            _followTransform.localPosition = Vector3.zero;
        };

        runaway.OnUpdate += () =>
        {
            if (!MPathfinding.OnSight(transform.position, _followTransform.position))
            {
                SendInputToFSM(KnightState.Pathfinding);
                return;
            }

            var dir = _followTransform.position - transform.position;

            if (dir.magnitude < 0.2f)
            {
                SendInputToFSM(KnightState.Idle);
                return;
            }

            _baseDir = dir.normalized;
        };

        runaway.OnExit += x =>
        {
            _actualSpeed = _normalSpeed;
            _previousState = KnightState.Runaway;
        };

        #endregion

        #region ATTACK

        attack.OnUpdate += () =>
        {
            if (!_followTransform.parent)
            {
                SendInputToFSM(KnightState.Idle);
                return;
            }

            _timer += Time.deltaTime;

            if (_timer > _attackTime)
            {
                _timer = 0;
                _followTransform.parent?.GetComponent<ILife>()?.Damage(_dmg);
            }

            if (Vector3.Distance(transform.position, _followTransform.position) > _attackRange)
            {
                SendInputToFSM(KnightState.Chase);
            }
        };

        attack.OnExit += x => { _previousState = KnightState.Attack; };

        #endregion

        #region PATHFINDING

        pathFinding.OnEnter += x =>
        {
            _actualPath = MPathfinding.instance.GetPath(transform.position, _followTransform.position);

            if (_actualPath.PathCount() <= 0)
            {
                SendInputToFSM(KnightState.Idle);
                return;
            }

            _actualNode = _actualPath.GetNextNode().transform.position;
        };

        pathFinding.OnUpdate += () =>
        {
            if (!MPathfinding.OnSight(transform.position, _actualNode))
            {
                _actualPath = MPathfinding.instance.GetPath(transform.position, _followTransform.position);

                if (_actualPath.PathCount() <= 0)
                {
                    SendInputToFSM(KnightState.Idle);
                    return;
                }

                _actualNode = _actualPath.GetNextNode().transform.position;
            }

            Flocking();
            var dir = _actualNode - transform.position;

            _baseDir = dir.normalized;

            if (MPathfinding.OnSight(_followTransform.position, transform.position))
            {
                SendInputToFSM(_previousState);
                return;
            }

            if (!(dir.magnitude < 0.5f)) return;

            if (_actualPath.PathCount() > 0)
            {
                _actualNode = _actualPath.GetNextNode().transform.position;
            }
            else
            {
                SendInputToFSM(KnightState.Idle);
            }
        };

        pathFinding.OnExit += x => { _previousState = KnightState.Idle; };

        #endregion

        _fsm = new EventFSM<KnightState>(idle);
    }

    private void SendInputToFSM(KnightState state)
    {
        _fsm.SendInput(state);
    }

    private void Update()
    {
        _baseDir = Vector3.zero;
        _flockingVector = Vector3.zero;
        _fsm.Update();
        ObstacleAvoidance();

        var finalDir = (_baseDir + (_obstacleDir.normalized * _obstacleWeight) +
                        _flockingVector.normalized * _flockingWeight);
        finalDir.y = 0;

        if (finalDir.magnitude == 0) return;

        var targetRotation = Quaternion.LookRotation(finalDir);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

        //transform.forward = finalDir;
        transform.position += transform.forward * (_actualSpeed * Time.deltaTime);
    }

    public override void Damage(float dmg)
    {
        if (_isDead) return;

        if (_dmgCoroutine != null)
        {
            StopCoroutine(_dmgCoroutine);
        }

        _dmgCoroutine = StartCoroutine(ResetMat());

        _life -= dmg;

        if (_life <= 0)
        {
            Death();
            return;
        }

        if (_life < _escapeLife)
        {
            SendInputToFSM(KnightState.Runaway);
        }
    }

    public override void Health(float health)
    {
        _life += health;

        _life = Mathf.Clamp(_life, 0f, _maxLife);
    }

    private void Flocking()
    {
        var nearAllies = Physics.OverlapSphere(transform.position, _flockingRange,
            _isBlueTeam ? LayerManager.LM_BLUETEAM : LayerManager.LM_REDTEAM).Select(x => x.gameObject).ToList();

        nearAllies.Remove(gameObject);

        var nearEnemies = Physics.OverlapSphere(transform.position, _flockingRange,
            _isBlueTeam ? LayerManager.LM_REDTEAM : LayerManager.LM_BLUETEAM).Select(x => x.gameObject).ToList();

        var cohesion = Vector3.zero;
        var separationVec = Vector3.zero;

        if (nearAllies.Any())
        {
            foreach (var ally in nearAllies)
            {
                cohesion += (ally.transform.forward);
                separationVec += (transform.position - ally.transform.position).normalized;
            }
        }

        if (nearEnemies.Any())
        {
            foreach (var enemy in nearEnemies)
            {
                separationVec += (transform.position - enemy.transform.position).normalized * 0.5f;
            }
        }


        var nearKing = Physics.OverlapSphere(transform.position, _flockingRange,
            _isBlueTeam ? LayerManager.LM_BLUEKING : LayerManager.LM_REDKING).Select(x => x.gameObject).ToList();

        var align = Vector3.zero;
        if (nearKing.Any())
        {
            align = nearKing.First().transform.forward;
        }

        _flockingVector += cohesion + separationVec + align;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
    }
}