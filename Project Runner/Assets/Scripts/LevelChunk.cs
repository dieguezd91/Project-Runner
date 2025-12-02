using UnityEngine;

public class LevelChunk : MonoBehaviour
{
    // Identificador único para el Pooling (útil si tienes biomas distintos luego)
    public Vector2Int Coordinate { get; private set; }

    public void Setup(Vector2Int coord)
    {
        Coordinate = coord;
        // Aquí podrías inicializar obstáculos aleatorios dentro del chunk
        // GenerateObstacles(); 
    }
}