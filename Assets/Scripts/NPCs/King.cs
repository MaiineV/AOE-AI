using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum KingStates
{
    Idle,
    MovesTo,
    Chase,
    InputChase,
    Attack,
    Pathfinding
}

public class King : NPC, IController
{
    private EventFSM<KingStates> _fsm;
    private KingStates _previousState;

    private void Awake()
    {
        EventManager.Subscribe("ChangeTeam", ChangeSelectedTeam);
        _followTransform.parent = null;
        DoFsmSetup();
        _life = _maxLife;
    }

    private void DoFsmSetup()
    {
        #region SETUP

        var idle = new State<KingStates>("Idle");
        var movesTo = new State<KingStates>("MovesTo");
        var chase = new State<KingStates>("Chase");
        var inputChase = new State<KingStates>("InputChase");
        var attack = new State<KingStates>("Attack");
        var pathFinding = new State<KingStates>("Pathfinding");

        StateConfigurer.Create(idle)
            .SetTransition(KingStates.MovesTo, movesTo)
            .SetTransition(KingStates.Chase, chase)
            .SetTransition(KingStates.InputChase, inputChase)
            .SetTransition(KingStates.Attack, attack)
            .SetTransition(KingStates.Pathfinding, pathFinding)
            .Done();

        StateConfigurer.Create(movesTo)
            .SetTransition(KingStates.Idle, idle)
            .SetTransition(KingStates.Chase, chase)
            .SetTransition(KingStates.InputChase, inputChase)
            .SetTransition(KingStates.Attack, attack)
            .SetTransition(KingStates.Pathfinding, pathFinding)
            .Done();

        StateConfigurer.Create(chase)
            .SetTransition(KingStates.Idle, idle)
            .SetTransition(KingStates.MovesTo, movesTo)
            .SetTransition(KingStates.InputChase, inputChase)
            .SetTransition(KingStates.Attack, attack)
            .SetTransition(KingStates.Pathfinding, pathFinding)
            .Done();

        StateConfigurer.Create(inputChase)
            .SetTransition(KingStates.Idle, idle)
            .SetTransition(KingStates.MovesTo, movesTo)
            .SetTransition(KingStates.Chase, chase)
            .SetTransition(KingStates.Attack, attack)
            .SetTransition(KingStates.Pathfinding, pathFinding)
            .Done();

        StateConfigurer.Create(attack)
            .SetTransition(KingStates.Idle, idle)
            .SetTransition(KingStates.MovesTo, movesTo)
            .SetTransition(KingStates.Chase, chase)
            .SetTransition(KingStates.InputChase, inputChase)
            .SetTransition(KingStates.Pathfinding, pathFinding)
            .Done();

        StateConfigurer.Create(pathFinding)
            .SetTransition(KingStates.Idle, idle)
            .SetTransition(KingStates.MovesTo, movesTo)
            .SetTransition(KingStates.Chase, chase)
            .SetTransition(KingStates.InputChase, inputChase)
            .SetTransition(KingStates.Attack, attack)
            .Done();

        #endregion

        #region IDLE

        idle.OnExit += x => { _previousState = KingStates.Idle;};

        #endregion

        #region MOVES TO

        movesTo.OnUpdate += () =>
        {
            if (!MPathfinding.OnSight(transform.position, _followTransform.position))
            {
                SendInputToFSM(KingStates.Pathfinding);
                return;
            }

            var dir = _followTransform.position - transform.position;
            _baseDir = dir.normalized;

            if (dir.magnitude < 0.2f)
            {
                SendInputToFSM(KingStates.Idle);
            }
        };
        
        movesTo.OnExit += x => { _previousState = KingStates.MovesTo;};

        #endregion

        #region CHASE
        
        chase.OnUpdate += () =>
        {
            if (!MPathfinding.OnSight(transform.position, _followTransform.position))
            {
                SendInputToFSM(KingStates.Pathfinding);
                return;
            }

            var dir = _followTransform.position - transform.position;
            _baseDir = dir.normalized;

            if (dir.magnitude < _attackRange)
            {
                SendInputToFSM(KingStates.Attack);
            }
            else if (dir.magnitude > _detectionRange)
            {
                SendInputToFSM(KingStates.Idle);
            }
        };

        chase.OnExit += x => { _previousState = KingStates.Chase;};

        #endregion

        #region INPUT CHASE
        
        inputChase.OnUpdate += () =>
        {
            if (!MPathfinding.OnSight(transform.position, _followTransform.position))
            {
                SendInputToFSM(KingStates.Pathfinding);
                return;
            }

            var dir = _followTransform.position - transform.position;
            _baseDir = dir.normalized;

            if (dir.magnitude < _attackRange)
            {
                SendInputToFSM(KingStates.Attack);
            }
        };
        
        inputChase.OnExit += x => { _previousState = KingStates.InputChase;};

        #endregion

        #region ATTACK

        attack.OnUpdate += () =>
        {
            _timer += Time.deltaTime;

            if (_timer > _attackTime)
            {
                _timer = 0;
                _followTransform.parent?.GetComponent<ILife>()?.Damage(_dmg);
            }

            if (Vector3.Distance(transform.position, _followTransform.position) > _attackRange)
            {
                SendInputToFSM(KingStates.Chase);
            }
        };
        
        attack.OnExit += x => { _previousState = KingStates.Attack;};

        #endregion

        #region PATHFINDING

        pathFinding.OnEnter += x =>
        {
            _actualPath = MPathfinding.instance.GetPath(transform.position, _followTransform.position);

            if (_actualPath.PathCount() <= 0)
            {
                SendInputToFSM(KingStates.Idle);
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
                    SendInputToFSM(KingStates.Idle);
                    return;
                }

                _actualNode = _actualPath.GetNextNode().transform.position;
            }

            var dir = _actualNode - transform.position;

            _baseDir = dir.normalized;

            if (MPathfinding.OnSight(_followTransform.position, transform.position))
            {
                SendInputToFSM(_previousState);
                return;
            }

            if (!(dir.magnitude < 0.2f)) return;

            if (_actualPath.PathCount() > 0)
            {
                _actualNode = _actualPath.GetNextNode().transform.position;
            }
            else
            {
                SendInputToFSM(KingStates.Idle);
            }
        };
        
        pathFinding.OnExit += x => { _previousState = KingStates.Idle;};

        #endregion

        _fsm = new EventFSM<KingStates>(idle);
    }

    private void SendInputToFSM(KingStates state)
    {
        _fsm.SendInput(state);
    }

    private void Update()
    {
        _baseDir = Vector3.zero;
        _fsm.Update();
        ObstacleAvoidance();

        var dir = (_baseDir + (_obstacleDir.normalized * 0.5f));

        if (dir.magnitude == 0)return;

        transform.forward = dir;
        transform.position +=  transform.forward * (_normalSpeed * Time.deltaTime);
    }

    private void ChangeSelectedTeam(params object[] parameters)
    {
        if ((bool)parameters[0] != _isBlueTeam) return;

        EventManager.Trigger("SetKing", this);
    }

    #region ICONTROLLER

    public void SetPoint(Vector3 point)
    {
        _followTransform.parent = null;
        _followTransform.position = point;

        SendInputToFSM(MPathfinding.OnSight(transform.position, _followTransform.position)
            ? KingStates.MovesTo
            : KingStates.Pathfinding);
        
        _previousState = KingStates.MovesTo;
    }

    public void SetEnemy(Transform enemy)
    {
        _followTransform.parent = enemy;
        _followTransform.localPosition = Vector3.zero;

        SendInputToFSM(MPathfinding.OnSight(transform.position, _followTransform.position)
            ? KingStates.InputChase
            : KingStates.Pathfinding);
        
        _previousState = KingStates.InputChase;
    }

    #endregion

    #region ILife

    public override void Damage(float dmg)
    {
        if (_isDead)return;
        
        if (_dmgCoroutine != null)
        {
            StopCoroutine(_dmgCoroutine);
        }
        _dmgCoroutine = StartCoroutine(ResetMat());
        
        _life -= dmg;

        if (_life <= 0)
        {
            Death();
        }
    }

    public override void Health(float health)
    {
        _life += health;

        _life = Mathf.Clamp(_life, 0f, _maxLife);
    }

    #endregion
}