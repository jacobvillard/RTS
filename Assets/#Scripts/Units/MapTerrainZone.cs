using UnityEngine;

namespace _Scripts.Units {
    public enum MapTerrainType { Sand, Mud, Forest }

    [RequireComponent(typeof(Collider2D))]
    public class MapTerrainZone : MonoBehaviour {
        [SerializeField] private MapTerrainType terrainType;
        [SerializeField, Range(0.1f, 1f)] private float moveSpeedMultiplier = 0.5f;

        public float MoveSpeedMultiplier {
            get {
                switch (terrainType) {
                    case MapTerrainType.Sand:
                    case MapTerrainType.Mud:
                        return moveSpeedMultiplier;
                    default:
                        return 1f;
                }
            }
        }

        public bool ProvidesForestCover => terrainType == MapTerrainType.Forest;

        private void Reset() {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnValidate() {
            var zoneCollider = GetComponent<Collider2D>();
            if (zoneCollider != null) zoneCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (other.TryGetComponent(out Unit unit)) {
                unit.EnterTerrainZone(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other) {
            if (other.TryGetComponent(out Unit unit)) {
                unit.ExitTerrainZone(this);
            }
        }
    }
}
