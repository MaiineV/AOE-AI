using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MPathfinding : MonoBehaviour
{
    public static MPathfinding _instance;
    private Path _actualPath;
    private MNode _origenNode;
    private MNode _targetNode;
    private MNode _actualnode;
    public float searchingRange;
    
    private HashSet<MNode> _closeNodes = new HashSet<MNode>();
    private PriorityQueue<MNode> _openNodes = new PriorityQueue<MNode>();

    public List<MNode> checker = new List<MNode>();

    private void Awake()
    {
        _instance = this;
    }

    public Path GetPath(Vector3 origen, Vector3 target)
    {
        checker = new List<MNode>();
        
        _actualPath = new Path();
        _closeNodes = new HashSet<MNode>();
        _openNodes = new PriorityQueue<MNode>();

        _origenNode = GetClosestNode(origen);
        _targetNode = GetClosestNode(target);

        _actualnode = _origenNode;

        AStar();

        return _actualPath;
    }

    private void AStar()
    {
        if (_actualnode == null)
        {
            Debug.Log("ACA DEBERIA CRASHEAR!");
            return;
        }

        _closeNodes.Add(_actualnode);
        _actualnode.nodeColor = Color.green;

        var watchdog = 10000;
        var checkingNodes = new Queue<MNode>();

        while (_actualnode != _targetNode && watchdog > 0)
        {
            watchdog--;

            for (var i = 0; i < _actualnode.NeighboursCount(); i++)
            {
                var node = _actualnode.GetNeighbor(i);
                node.nodeColor = Color.magenta;
                if (_closeNodes.Contains(node)) continue;

                node.previousNode = _actualnode;
                node.SetWeight(_actualnode.GetWeight() + 1 +
                               Vector3.Distance(node.transform.position, _targetNode.transform.position));

                _openNodes.Enqueue(node);
                checkingNodes.Enqueue(node);
            }

            if (checkingNodes.Count > 0)
            {
                var cheaperNode = checkingNodes.Dequeue();
                while (checkingNodes.Count > 0)
                {
                    if (cheaperNode == _targetNode) break;

                    var actualNode = checkingNodes.Dequeue();

                    if (actualNode.GetWeight() < cheaperNode.GetWeight())
                        cheaperNode = actualNode;
                }

                _actualnode = cheaperNode;
            }
            else
            {
                _actualnode = _openNodes.Dequeue();
            }


            _closeNodes.Add(_actualnode);
        }

        ThetaStar();
    }

    private void ThetaStar()
    {
        var stack = new Stack();
        _actualnode = _targetNode;
        stack.Push(_actualnode);
        var previousNode = _actualnode.previousNode;

        if (previousNode == null) Debug.Log("no existe");
        var watchdog = 10000;
        while (_actualnode != _origenNode && watchdog > 0)
        {
            watchdog--;

            if (previousNode.previousNode && OnSight(_actualnode.transform.position,
                    previousNode.previousNode.transform.position))
            {
                previousNode = previousNode.previousNode;
            }
            else
            {
                _actualnode.previousNode = previousNode;
                _actualnode = previousNode;
                stack.Push(_actualnode);
            }
        }

        watchdog = 10000;
        while (stack.Count > 0 && watchdog > 0)
        {
            watchdog--;

            var nextNode = stack.Pop() as MNode;
            checker.Add(nextNode);
            _actualPath.AddNode(nextNode);
        }
    }

    public MNode GetClosestNode(Vector3 t, bool isForAssistant = false)
    {
        var actualSearchingRange = searchingRange;
        var closestNodes = Physics.OverlapSphere(t, actualSearchingRange, LayerManager.LM_NODE)
            .Where(x =>
            {
                var dir = x.transform.position - t;
                return !Physics.Raycast(t, dir, dir.magnitude * 1, LayerManager.LM_OBSTACLE);
            }).ToArray();

        var watchdog = 10000;
        while (closestNodes.Length <= 0)
        {
            watchdog--;
            if (watchdog <= 0)
            {
                return null;
            }

            actualSearchingRange += searchingRange;
            closestNodes = Physics.OverlapSphere(t, actualSearchingRange, LayerManager.LM_NODE)
                .Where(x =>
                {
                    var dir = x.transform.position - t;
                    return !Physics.Raycast(t, dir, dir.magnitude * 1, LayerManager.LM_OBSTACLE);
                }).ToArray();
        }

        MNode mNode = null;

        var minDistance = Mathf.Infinity;
        foreach (var node in closestNodes)
        {
            var distance = Vector3.Distance(t, node.transform.position);
            if (distance > minDistance) continue;

            var tempNode = node.gameObject.GetComponent<MNode>();

            if (tempNode == null) continue;

            mNode = tempNode;
            minDistance = distance;
        }

        return mNode;
    }

    private bool OnSight(Vector3 from, Vector3 to)
    {
        var dir = to - from;
        var ray = new Ray(from, dir);

        return !Physics.Raycast(ray, dir.magnitude, LayerManager.LM_ALLOBSTACLE);
    }
}