using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    public GridManager gridManager;
    public Material lineMaterial;

    void Start()
    {
        DrawGrid();
    }

    void DrawGrid()
    {
        float width = gridManager.width;
        float height = gridManager.height;
        float size = gridManager.tileSize;

        GameObject gridObj = new GameObject("GridLines");

        for (int x = 0; x <= width; x++)
        {
            CreateLine(
                new Vector3(x * size - width * size / 2f, 0.1f, -height * size / 2f),
                new Vector3(x * size - width * size / 2f, 0.1f, height * size / 2f),
                gridObj.transform
            );
        }

        for (int z = 0; z <= height; z++)
        {
            CreateLine(
                new Vector3(-width * size / 2f, 0.1f, z * size - height * size / 2f),
                new Vector3(width * size / 2f, 0.1f, z * size - height * size / 2f),
                gridObj.transform
            );
        }
    }

    void CreateLine(Vector3 start, Vector3 end, Transform parent)
    {
        GameObject line = new GameObject("Line");
        line.transform.parent = parent;

        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.material = lineMaterial;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
    }
}