using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Controller : MonoBehaviour
{
    private IController _player;

    private Camera _camera;
    private bool _isBlueTeam = true;

    [SerializeField] private Transform _followTransform; 

    private void Awake()
    {
        _camera = Camera.main;
        
        EventManager.Subscribe("SetKing", SetKing);
    }

    private void Start()
    {
        EventManager.Trigger("ChangeTeam", _isBlueTeam);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit, Mathf.Infinity,
                    _isBlueTeam ? LayerManager.LM_REDTEAM : LayerManager.LM_BLUETEAM))
            {
                _player.SetEnemy(hit.transform);
            }
            else if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerManager.LM_FLOOR))
            {
                var actualSearchingRange = 50;
                var nodesColliders =
                    Physics.OverlapSphere(transform.position, actualSearchingRange, LayerManager.LM_NODE);
                
                if (nodesColliders.Any())
                {
                    nodesColliders = nodesColliders.Where(x =>
                    {
                        var walls = Physics.OverlapSphere(x.transform.position, 1, LayerManager.LM_ALLOBSTACLE);
                        return walls.Length <= 0;
                    }).ToArray();
                }

                var watchdog = 5;

                while (!nodesColliders.Any())
                {
                    if (watchdog < 0)
                    {
                        Debug.Log("a");
                        return;
                    }
                    
                    actualSearchingRange *= 2;
                    nodesColliders =
                        Physics.OverlapSphere(transform.position, actualSearchingRange, LayerManager.LM_NODE);

                    if (nodesColliders.Any())
                    {
                        nodesColliders = nodesColliders.Where(x =>
                        {
                            var walls = Physics.OverlapSphere(x.transform.position, 0.1f, LayerManager.LM_ALLOBSTACLE);
                            return !walls.Any();
                        }).ToArray();
                    }

                    watchdog--;
                }

                var closeNode = nodesColliders.OrderBy(x => Vector3.Distance(x.transform.position, hit.point)).First();
                
                _player.SetPoint(closeNode.transform.position);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            _isBlueTeam = !_isBlueTeam;
            EventManager.Trigger("ChangeTeam", _isBlueTeam, _followTransform);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(0);
        }
    }

    private void SetKing(params object[] parameters)
    {
        _player = (IController)parameters[0];
    }
}