using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Applies a close-range damage boost to friendly units around a banner unit.
    /// </summary>
    public class MoraleAura : MonoBehaviour {

        #region Variables

        [Header("Morale")]
        [SerializeField] private float radius = 2.5f;                // Range of the morale aura.
        [SerializeField] private float damageMultiplier = 1.05f;     // Damage multiplier applied to nearby allies.
        [SerializeField] private float refreshInterval = 0.25f;      // Seconds between aura refreshes.

        private readonly Collider2D[] _nearbyColliders = new Collider2D[64]; // Reused aura query buffer.
        private readonly List<Unit> _boostedUnits = new();                   // Units boosted during the previous refresh.
        private Unit _banner;                                                // Unit providing the aura.
        private float _nextRefreshTime;                                       // Next allowed refresh time.

        #endregion
        #region Unity Methods

        private void Update() {
            if (Time.time < _nextRefreshTime) return;

            _nextRefreshTime = Time.time + refreshInterval;
            RefreshAura();
        }

        private void OnDisable() {
            ClearPreviousBoosts();
        }

        #endregion
        #region Aura

        /// <summary>
        /// Rebuilds the list of nearby boosted friendly units.
        /// </summary>
        private void RefreshAura() {
            ClearPreviousBoosts();
            ResolveBanner();
            if (_banner == null || !_banner.IsAlive) return;

            var count = Physics2D.OverlapCircleNonAlloc(transform.position, radius, _nearbyColliders);
            for (var i = 0; i < count; i++) {
                var unit = _nearbyColliders[i] != null
                    ? _nearbyColliders[i].GetComponentInParent<Unit>()
                    : null;

                if (unit == null || unit == _banner || !unit.IsAlive || unit.team != _banner.team) continue;
                if (_boostedUnits.Contains(unit)) continue;

                unit.SetDamageBoostMultiplier(damageMultiplier);
                _boostedUnits.Add(unit);
            }
        }

        /// <summary>
        /// Resets units boosted by the previous aura tick.
        /// </summary>
        private void ClearPreviousBoosts() {
            foreach (var unit in _boostedUnits) {
                if (unit != null && unit.IsAlive) {
                    unit.SetDamageBoostMultiplier(1f);
                }
            }

            _boostedUnits.Clear();
        }

        /// <summary>
        /// Finds the runtime Unit component after UnitInit has created it.
        /// </summary>
        private void ResolveBanner() {
            if (_banner == null) {
                _banner = GetComponent<Unit>();
            }
        }

        #endregion
    }
}
