using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour {
    public static SpawnManager Instance {get; private set;}

    [System.Serializable] 
    public struct SpawnPoint {
        public Transform transform;
        public bool isOccupied;

        public SpawnPoint(Transform transform, bool isOccupied) {
            this.transform = transform;
            this.isOccupied = isOccupied;
        }
    }

    [SerializeField] private List<SpawnPoint> playerSpawnPoints;

    private void Awake() {
        Instance = this;
    }

    public Transform GetSpawnPoint(int playerIndex) {
        if (playerSpawnPoints[playerIndex].isOccupied) {
            for (int i = 0; i < playerSpawnPoints.Count; i++) {
                if (!playerSpawnPoints[i].isOccupied) {
                    playerIndex = i;
                    break;
                }
            }
        }
        return playerSpawnPoints[playerIndex].transform;
    }

    public void SetSpawnPointOccupancy(int playerIndex, bool isOccupied) {
        playerSpawnPoints[playerIndex] = new SpawnPoint(
            playerSpawnPoints[playerIndex].transform,
            isOccupied
        );
    }
}
