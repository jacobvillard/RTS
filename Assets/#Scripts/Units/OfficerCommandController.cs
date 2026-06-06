using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Lets an officer issue simple tactical commands to nearby friendly units.
    /// </summary>
    public class OfficerCommandController : MonoBehaviour {

        #region Variables

        [Header("Command Range")]
        [SerializeField] private float commandRadius = 15f; // Friendly units inside this radius receive officer commands.

        [Header("Range Visual")]
        [SerializeField] private Color rangeIndicatorColor = new(1f, 0.9f, 0.15f, 0.75f); // Colour used for the command range circle.
        [SerializeField] private float rangeIndicatorDuration = 0.65f;                    // Seconds the range circle remains visible.
        [SerializeField] private float rangeIndicatorWidth = 0.06f;                       // Line width for the range circle.
        [SerializeField] private int rangeIndicatorSegments = 96;                         // Number of points used to draw the circle.

        private readonly Collider2D[] _nearbyColliders = new Collider2D[64]; // Reused command query buffer.
        private Unit _officer;                                               // Runtime unit attached to this officer.

        #endregion
        #region Commands

        /// <summary>
        /// Orders nearby friendly units to hold their current positions.
        /// </summary>
        public void Hold() {
            ResolveOfficer();
            foreach (var unit in GetCommandableUnits()) {
                unit.CommandHold();
            }
        }

        /// <summary>
        /// Orders nearby friendly units to follow this officer.
        /// </summary>
        public void FollowMe() {
            ResolveOfficer();
            foreach (var unit in GetCommandableUnits()) {
                unit.CommandFollow(_officer);
            }
        }

        /// <summary>
        /// Orders nearby friendly units to attack their closest visible enemy.
        /// </summary>
        public void Attack() {
            ResolveOfficer();
            foreach (var unit in GetCommandableUnits()) {
                var target = unit.FindClosestVisibleEnemy();
                if (target == null) continue;

                unit.CommandAttack(target);
            }
        }

        #endregion
        #region Visuals

        /// <summary>
        /// Shows the command radius briefly around this officer.
        /// </summary>
        public void ShowCommandRangeIndicator() {
            StartCoroutine(ShowCommandRangeIndicatorCoroutine());
        }

        /// <summary>
        /// Creates and fades a line-rendered command radius circle.
        /// </summary>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator ShowCommandRangeIndicatorCoroutine() {
            var indicator = new GameObject("Officer Command Range");
            indicator.transform.position = new Vector3(transform.position.x, transform.position.y, -0.1f);

            var lineRenderer = indicator.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = Mathf.Max(8, rangeIndicatorSegments);
            lineRenderer.startWidth = rangeIndicatorWidth;
            lineRenderer.endWidth = rangeIndicatorWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = rangeIndicatorColor;
            lineRenderer.endColor = rangeIndicatorColor;
            lineRenderer.sortingLayerName = "Default";
            lineRenderer.sortingOrder = 150;

            DrawRangeCircle(lineRenderer);

            var elapsed = 0f;
            while (elapsed < rangeIndicatorDuration) {
                elapsed += Time.deltaTime;
                var alpha = Mathf.Lerp(rangeIndicatorColor.a, 0f, elapsed / rangeIndicatorDuration);
                var fadedColor = new Color(
                    rangeIndicatorColor.r,
                    rangeIndicatorColor.g,
                    rangeIndicatorColor.b,
                    alpha);

                lineRenderer.startColor = fadedColor;
                lineRenderer.endColor = fadedColor;
                yield return null;
            }

            Destroy(indicator);
        }

        /// <summary>
        /// Draws a circle around the officer using the configured command radius.
        /// </summary>
        /// <param name="lineRenderer">Line renderer to populate.</param>
        private void DrawRangeCircle(LineRenderer lineRenderer) {
            var segmentCount = lineRenderer.positionCount;
            for (var i = 0; i < segmentCount; i++) {
                var progress = i / (float)segmentCount;
                var angle = progress * Mathf.PI * 2f;
                var point = new Vector3(
                    Mathf.Cos(angle) * commandRadius,
                    Mathf.Sin(angle) * commandRadius,
                    0f);

                lineRenderer.SetPosition(i, point);
            }
        }

        #endregion
        #region Queries

        /// <summary>
        /// Finds living friendly units inside the command radius.
        /// </summary>
        /// <returns>Reusable list of commandable units.</returns>
        private List<Unit> GetCommandableUnits() {
            var units = new List<Unit>();
            if (_officer == null || !_officer.IsAlive) return units;

            var count = Physics2D.OverlapCircleNonAlloc(transform.position, commandRadius, _nearbyColliders);
            for (var i = 0; i < count; i++) {
                var unit = _nearbyColliders[i] != null
                    ? _nearbyColliders[i].GetComponentInParent<Unit>()
                    : null;

                if (unit == null || unit == _officer || !unit.IsAlive || unit.team != _officer.team) continue;
                if (units.Contains(unit)) continue;

                units.Add(unit);
            }

            return units;
        }

        /// <summary>
        /// Finds the runtime Unit component after UnitInit has created it.
        /// </summary>
        private void ResolveOfficer() {
            if (_officer == null) {
                _officer = GetComponent<Unit>();
            }
        }

        #endregion
    }
}
