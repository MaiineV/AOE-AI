using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovableWall : MonoBehaviour
{
    [SerializeField] private Transform[] _wayPoints;
    private int _index = 0;
    private int _actualDir = 1;
    [SerializeField] private bool _canGoBackwards;
    [SerializeField] private float _speed;

    private void Update()
    {
        var dir = _wayPoints[_index].position - transform.position;
        transform.position += dir.normalized * _speed * Time.deltaTime;

        if (dir.magnitude < 0.5f)
        {
            _index += _actualDir;

            if (_index >= _wayPoints.Length || _index < 0)
            {
                _index -= _actualDir;
                
                if (_canGoBackwards)
                {
                    _actualDir *= -1;
                }
                else
                {
                    _index = 0;
                }
            }
        }
    }
}
