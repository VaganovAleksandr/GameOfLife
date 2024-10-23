using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game of life/Pattern")]
public class Pattern : ScriptableObject
{
    public Vector2Int[] current_cells;

    public Vector2Int get_center()
    {
        if (current_cells == null || current_cells.Length == 0)
        {
            return Vector2Int.zero;
        }
        Vector2Int min = Vector2Int.zero;
        Vector2Int max = Vector2Int.zero;
        for (uint i = 0; i < current_cells.Length; ++i) {
            Vector2Int cell = current_cells[i];
            min.x = Mathf.Min(cell.x, min.x);
            min.y = Mathf.Min(cell.y, min.y);
            max.x = Mathf.Max(cell.x, max.x);
            max.y = Mathf.Max(cell.y, max.y);
        }
        return (min + max) / 2;
    }
}
