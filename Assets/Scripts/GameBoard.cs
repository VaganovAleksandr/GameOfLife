using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
public class GameBoard : MonoBehaviour
{
    [SerializeField] private Tilemap currentState;
    [SerializeField] private Tilemap nextState;
    [SerializeField] private Tile alive;
    [SerializeField] private Tile dead;
    [SerializeField] private Tile firstPlayer;
    [SerializeField] private Tile secondPlayer;
    [SerializeField] private float updateInterval = 0.05f;
    private Tile _currentTile;
    [SerializeField] private Pattern pattern;
    [SerializeField] public int width = 50;
    [SerializeField] public int height = 50;
    private bool _freezed = true;
    private bool _multiplayer = false;
    private bool _firstPlayerMove = true;
    [SerializeField] private uint firstPlayerScore = 0;
    [SerializeField] private uint secondPlayerScore = 0;
    
    public Camera mOrthographicCamera;

    public Slider slider;

    void change_current_cell_state(bool state)
    {
        if (!_freezed)
        {
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }


        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int currentCell = currentState.WorldToCell(mouseWorldPos);
        if (state)
        {
            currentState.SetTile(currentCell, _currentTile);
            _aliveCells.Add(currentCell);
        }
        else
        {
            currentState.SetTile(currentCell, dead);
            _aliveCells.Remove(currentCell);
        }
    }

    void OnGUI()
    {
        if (Event.current.Equals(Event.KeyboardEvent(KeyCode.Escape.ToString())))
        {
            if (!_freezed)
            {
                Time.timeScale = 0;
                _freezed = true;
            }
            else
            {
                Time.timeScale = 1;
                _freezed = false;
            }
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            start_random();
        }
        else if (Input.GetMouseButton(0))
        {
            change_current_cell_state(true);
        }
        else if (Input.GetMouseButton(1))
        {
            change_current_cell_state(false);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            mOrthographicCamera.orthographicSize += 0.1f;
        }

        if (Input.GetKey(KeyCode.E))
        {
            mOrthographicCamera.orthographicSize -= 0.1f;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Clear();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            _multiplayer = true;
            _currentTile = _multiplayer ? firstPlayer : alive;
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            if (!_multiplayer) return;
            _currentTile = secondPlayer;
            _firstPlayerMove = false;
        }

        if (!_multiplayer) return;
        GUI.Label(new Rect(10, 10, 200, 20), $"First Player Score: {firstPlayerScore.ToString()}");
        GUI.Label(new Rect(200, 10, 200, 20), $"Second Player Score: {secondPlayerScore.ToString()}");
        if (Input.GetKeyDown(KeyCode.S) || _aliveCells.Count == 0)
        {
            StopGame();
        }
    }

    private void StopGame()
    {
        _freezed = true;
        if (!_multiplayer) return;
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0,0,Color.gray);
        texture.Apply();
        GUI.skin.box.normal.background = texture;
        GUI.Box(new Rect(0, 0, 1920, 1080), GUIContent.none);
        if (firstPlayerScore > secondPlayerScore)
        {
            GUI.Label(new Rect(0, 0, 1920, 1080), $"First player wins!");
        }
        else if (firstPlayerScore < secondPlayerScore)
        {
            GUI.Label(new Rect(0, 0, 1920, 1080), $"Second player wins!");
        }
        else
        {
            GUI.Label(new Rect(0, 0, 1920, 1080), $"Draw!");
        }
        StopAllCoroutines();
    }
    
    private void start_random()
    {
        if (!_multiplayer)
        {
            Clear();
            for (var x = -width / 2; x < width / 2; ++x)
            {
                for (var y = -height / 2; y < height / 2; ++y)
                {
                    var r = Random.Range(0, 2);
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (r != 1) continue;
                    currentState.SetTile(cell, _currentTile);
                    _aliveCells.Add(cell);
                }
            }

            return;
        }

        for (var x = -width / 2; x < width / 2; ++x)
        {
            for (var y = -height / 2; y < height / 2; ++y)
            {
                var r = Random.Range(0, 2);
                var offset = new Vector3Int(_firstPlayerMove ? -2 * width : 2 * width, 0, 0);
                var cell = new Vector3Int(x, y, 0) + offset;
                if (r != 1) continue;
                currentState.SetTile(cell, _currentTile);
                _aliveCells.Add(cell);
            }
        }
    }

    private HashSet<Vector3Int> _aliveCells;
    private HashSet<Vector3Int> _cellsToCheck;

    private void Awake()
    {
        _aliveCells = new HashSet<Vector3Int>();
        _cellsToCheck = new HashSet<Vector3Int>();
    }

    private void set_pattern(Pattern givenPattern)
    {
        Clear();

        Vector2Int center = givenPattern.get_center();

        for (uint i = 0; i < givenPattern.current_cells.Length; ++i)
        {
            var cell = (Vector3Int)(givenPattern.current_cells[i] - center);
            currentState.SetTile(cell, _currentTile);
            _aliveCells.Add(cell);
        }
    }

    private void Clear()
    {
        currentState.ClearAllTiles();
        nextState.ClearAllTiles();
    }

    private void OnEnable()
    {
        StartCoroutine(Simulate());
    }

    private IEnumerator Simulate()
    {
        while (enabled)
        {
            updateInterval = slider.value;
            update_state();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private bool is_alive(Vector3Int cell)
    {
        return (currentState.GetTile(cell) == firstPlayer || currentState.GetTile(cell) == secondPlayer ||
                currentState.GetTile(cell) == alive);
    }

    private uint count_neighbours(Vector3Int cell)
    {
        uint counter = 0;
        for (var x = -1; x < 2; ++x)
        {
            for (var y = -1; y < 2; ++y)
            {
                if (x == 0 && y == 0) continue;

                var neighbour = cell + new Vector3Int(x, y, 0);
                if (is_alive(neighbour)) ++counter;
            }
        }

        return counter;
    }

    private (uint, uint) count_neighbours_multiplayer(Vector3Int cell)
    {
        uint counterFirst = 0;
        uint counterSecond = 0;
        for (var x = -1; x < 2; ++x)
        {
            for (var y = -1; y < 2; ++y)
            {
                if (x == 0 && y == 0) continue;
                
                var neighbour = cell + new Vector3Int(x, y, 0);
                if (!is_alive(neighbour)) continue;
                if (currentState.GetTile(neighbour) == firstPlayer)
                {
                    counterFirst++;
                }
                else
                {
                    counterSecond++;
                }
            }
        }
        return (counterFirst, counterSecond);
    }
    
    private void update_state()
    {
        _cellsToCheck.Clear();
        foreach (var cell in _aliveCells)
        {
            for (int x = -1; x < 2; ++x)
            {
                for (int y = -1; y < 2; ++y)
                {
                    _cellsToCheck.Add(cell + new Vector3Int(x, y, 0));
                }
            }
        }

        foreach (var cell in _cellsToCheck)
        {
            var neighbours = count_neighbours(cell);
            var cellIsAlive = is_alive(cell);
            switch (cellIsAlive)
            {
                case false when neighbours == 3:
                    if (_multiplayer)
                    {
                        uint counterFirst, counterSecond;
                        (counterFirst, counterSecond) = count_neighbours_multiplayer(cell);
                        if (counterFirst > counterSecond)
                        {
                            nextState.SetTile(cell, firstPlayer);
                            ++firstPlayerScore;
                        }
                        else if (counterSecond > counterFirst)
                        {
                            nextState.SetTile(cell, secondPlayer);
                            ++secondPlayerScore;
                        }
                        else
                        {
                            nextState.SetTile(cell, currentState.GetTile(cell));
                        }
                    }
                    else
                    {
                        nextState.SetTile(cell, alive);
                    }
                    
                    _aliveCells.Add(cell);
                    break;
                case true when neighbours is < 2 or > 3:
                    _aliveCells.Remove(cell);
                    break;
                default:
                    nextState.SetTile(cell, currentState.GetTile(cell));
                    break;
            }
        }

        (currentState, nextState) = (nextState, currentState);
        nextState.ClearAllTiles();
    }

    // Start is called before the first frame update
    void Start()
    {
        _currentTile = alive;
        slider.minValue = 0.001f;
        slider.maxValue = 1;
        Time.timeScale = 0;
        mOrthographicCamera.orthographic = true;
    }
}