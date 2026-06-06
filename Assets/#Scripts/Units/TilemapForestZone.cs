using System.Collections.Generic;
using _Scripts.GameManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.Units {
    /// <summary>
    /// Applies forest terrain effects from occupied tiles on a Tilemap.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class TilemapForestZone : MonoBehaviour, IMapTerrainEffect {

        #region Variables

        [Header("Forest")]
        [SerializeField, Range(0.1f, 1f)] private float moveSpeedMultiplier = 1f; // Optional forest movement multiplier.
        [SerializeField] private float refreshInterval = 0.15f;                  // Seconds between unit/tile checks.

        [Header("Player Visibility")]
        [SerializeField, Range(0f, 1f)] private float occupiedPlayerTileAlpha = 0.45f; // Alpha used while player units stand in forest.

        private readonly Dictionary<Unit, Vector3Int> _unitForestCells = new(); // Units currently inside forest tiles.
        private readonly Dictionary<Vector3Int, Color> _originalTileColours = new(); // Original tile colours before fading.
        private readonly HashSet<Vector3Int> _fadedCells = new();              // Cells currently rendered transparent.
        private readonly HashSet<Vector3Int> _nextFadedCells = new();          // Rebuilt faded-cell set each refresh.
        private readonly List<Unit> _missingUnits = new();                     // Reused list of units to remove from tracking.
        private readonly Dictionary<Vector3Int, int> _forestAreaIds = new();   // Connected forest area lookup by tile cell.

        private Tilemap _tilemap;       // Tilemap containing forest tiles.
        private float _nextRefreshTime; // Next allowed refresh time.

        public bool ProvidesForestCover => true;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;

        #endregion
        #region Unity Methods

        private void Awake() {
            _tilemap = GetComponent<Tilemap>();
            RebuildForestAreas();
        }

        private void Update() {
            if (Time.time < _nextRefreshTime) return;

            _nextRefreshTime = Time.time + refreshInterval;
            RefreshUnits();
            RefreshPlayerTileTransparency();
        }

        private void OnDisable() {
            ClearAllUnits();
            ClearFadedTiles();
        }

        #endregion
        #region Forest Areas

        /// <summary>
        /// Gets the connected forest area id occupied by a unit.
        /// </summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <returns>The connected area id, or -1 when not inside this forest tilemap.</returns>
        public int GetForestAreaId(Unit unit) {
            if (unit == null || _tilemap == null) return -1;
            if (_forestAreaIds.Count == 0) {
                RebuildForestAreas();
            }

            var cell = _tilemap.WorldToCell(unit.transform.position);
            return _forestAreaIds.TryGetValue(cell, out var areaId)
                ? areaId
                : -1;
        }

        /// <summary>
        /// Rebuilds connected forest areas so separated forests stay separate for visibility.
        /// </summary>
        private void RebuildForestAreas() {
            if (_tilemap == null) return;

            _forestAreaIds.Clear();
            var nextAreaId = 0;
            foreach (var position in _tilemap.cellBounds.allPositionsWithin) {
                if (!_tilemap.HasTile(position) || _forestAreaIds.ContainsKey(position)) continue;

                FloodFillForestArea(position, nextAreaId);
                nextAreaId++;
            }
        }

        /// <summary>
        /// Marks one connected group of forest tiles with a shared id.
        /// </summary>
        /// <param name="startCell">First tile in the connected area.</param>
        /// <param name="areaId">Id assigned to the connected area.</param>
        private void FloodFillForestArea(Vector3Int startCell, int areaId) {
            var cellsToVisit = new Queue<Vector3Int>();
            cellsToVisit.Enqueue(startCell);
            _forestAreaIds[startCell] = areaId;

            while (cellsToVisit.Count > 0) {
                var cell = cellsToVisit.Dequeue();
                TryQueueForestNeighbour(cell + Vector3Int.up, areaId, cellsToVisit);
                TryQueueForestNeighbour(cell + Vector3Int.down, areaId, cellsToVisit);
                TryQueueForestNeighbour(cell + Vector3Int.left, areaId, cellsToVisit);
                TryQueueForestNeighbour(cell + Vector3Int.right, areaId, cellsToVisit);
            }
        }

        /// <summary>
        /// Queues an unvisited neighbouring forest tile.
        /// </summary>
        /// <param name="cell">Neighbouring tile cell.</param>
        /// <param name="areaId">Id to assign when this is a forest tile.</param>
        /// <param name="cellsToVisit">Queue used by the flood fill.</param>
        private void TryQueueForestNeighbour(Vector3Int cell, int areaId, Queue<Vector3Int> cellsToVisit) {
            if (_forestAreaIds.ContainsKey(cell) || !_tilemap.HasTile(cell)) return;

            _forestAreaIds[cell] = areaId;
            cellsToVisit.Enqueue(cell);
        }

        #endregion
        #region Unit Terrain

        /// <summary>
        /// Applies forest enter/exit state to every registered unit.
        /// </summary>
        private void RefreshUnits() {
            if (_tilemap == null || BattleController.Instance == null) return;

            RefreshTeamUnits(BattleController.Instance.GetFriendlyUnits(Team.Player));
            RefreshTeamUnits(BattleController.Instance.GetFriendlyUnits(Team.AI));
            RemoveMissingUnits();
        }

        /// <summary>
        /// Applies forest state to one team's units.
        /// </summary>
        /// <param name="units">Units to inspect.</param>
        private void RefreshTeamUnits(List<Unit> units) {
            foreach (var unit in units) {
                if (unit == null || !unit.IsAlive) continue;

                var cell = _tilemap.WorldToCell(unit.transform.position);
                var isInForestTile = _tilemap.HasTile(cell);
                var wasInForestTile = _unitForestCells.ContainsKey(unit);

                if (isInForestTile) {
                    _unitForestCells[unit] = cell;
                    if (!wasInForestTile) {
                        unit.EnterTerrainZone(this);
                    }
                }
                else if (wasInForestTile) {
                    unit.ExitTerrainZone(this);
                    _unitForestCells.Remove(unit);
                }
            }
        }

        /// <summary>
        /// Removes destroyed or dead units from forest tracking.
        /// </summary>
        private void RemoveMissingUnits() {
            _missingUnits.Clear();
            foreach (var entry in _unitForestCells) {
                if (entry.Key == null || !entry.Key.IsAlive) {
                    _missingUnits.Add(entry.Key);
                }
            }

            foreach (var unit in _missingUnits) {
                if (unit != null) {
                    unit.ExitTerrainZone(this);
                }

                _unitForestCells.Remove(unit);
            }

            _missingUnits.Clear();
        }

        /// <summary>
        /// Clears this terrain effect from all tracked units.
        /// </summary>
        private void ClearAllUnits() {
            foreach (var unit in _unitForestCells.Keys) {
                if (unit != null) {
                    unit.ExitTerrainZone(this);
                }
            }

            _unitForestCells.Clear();
        }

        #endregion
        #region Tile Transparency

        /// <summary>
        /// Fades forest tiles occupied by player units and restores old cells.
        /// </summary>
        private void RefreshPlayerTileTransparency() {
            if (_tilemap == null || BattleController.Instance == null) return;

            _nextFadedCells.Clear();
            foreach (var unit in BattleController.Instance.GetFriendlyUnits(Team.Player)) {
                if (unit == null || !unit.IsAlive) continue;

                var cell = _tilemap.WorldToCell(unit.transform.position);
                if (_tilemap.HasTile(cell)) {
                    _nextFadedCells.Add(cell);
                }
            }

            foreach (var cell in _fadedCells) {
                if (!_nextFadedCells.Contains(cell)) {
                    RestoreTileColour(cell);
                }
            }

            foreach (var cell in _nextFadedCells) {
                SetTileAlpha(cell, occupiedPlayerTileAlpha);
            }

            _fadedCells.Clear();
            foreach (var cell in _nextFadedCells) {
                _fadedCells.Add(cell);
            }
        }

        /// <summary>
        /// Restores every faded tile to full opacity.
        /// </summary>
        private void ClearFadedTiles() {
            if (_tilemap == null) return;

            foreach (var cell in _fadedCells) {
                RestoreTileColour(cell);
            }

            _fadedCells.Clear();
            _nextFadedCells.Clear();
            _originalTileColours.Clear();
        }

        /// <summary>
        /// Sets tile alpha while leaving its RGB colour untouched.
        /// </summary>
        /// <param name="cell">Tile cell to update.</param>
        /// <param name="alpha">Alpha to apply.</param>
        private void SetTileAlpha(Vector3Int cell, float alpha) {
            if (!_originalTileColours.ContainsKey(cell)) {
                _originalTileColours.Add(cell, _tilemap.GetColor(cell));
            }

            _tilemap.SetTileFlags(cell, TileFlags.None);
            var colour = _originalTileColours[cell];
            colour.a = alpha;
            _tilemap.SetColor(cell, colour);
        }

        /// <summary>
        /// Restores a faded tile to its original colour.
        /// </summary>
        /// <param name="cell">Tile cell to restore.</param>
        private void RestoreTileColour(Vector3Int cell) {
            if (_originalTileColours.TryGetValue(cell, out var originalColour)) {
                _tilemap.SetTileFlags(cell, TileFlags.None);
                _tilemap.SetColor(cell, originalColour);
                _originalTileColours.Remove(cell);
            }
        }

        #endregion
    }
}
