using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Terrain behaviours that can be applied to units inside a zone.
    /// </summary>
    public enum MapTerrainType { Sand, Mud, Forest }

    /// <summary>
    /// Applies map terrain effects to units that enter this trigger area.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MapTerrainZone : MonoBehaviour {

        #region Variables

        [Header("Terrain")]
        [SerializeField] private MapTerrainType terrainType; // Terrain behaviour applied by this zone.
        [SerializeField, Range(0.1f, 1f)] private float moveSpeedMultiplier = 0.5f; // Sand/mud speed multiplier.

        public bool ProvidesForestCover => terrainType == MapTerrainType.Forest;

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

        #endregion
        #region Unity Methods

        private void Reset() {
            MakeColliderTrigger();
        }

        private void OnValidate() {
            MakeColliderTrigger();
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

        #endregion
        #region Setup

        /// <summary>
        /// Ensures this terrain area detects units without physically blocking them.
        /// </summary>
        private void MakeColliderTrigger() {
            var zoneCollider = GetComponent<Collider2D>();
            if (zoneCollider != null) {
                zoneCollider.isTrigger = true;
            }
        }

        #endregion
    }
}
