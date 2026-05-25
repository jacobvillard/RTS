using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Lightweight visual projectile that travels to a target and applies musket damage on impact.
    /// </summary>
    public class MusketProjectile : MonoBehaviour {

        #region Variables

        private const float HitDistance = 0.08f; // Distance from target considered an impact.
        private const float LifeSeconds = 3f;    // Safety timeout before destroying the projectile.

        private Unit _shooter;              // Unit that fired the projectile.
        private Unit _target;               // Unit being shot.
        private float _damage;              // Damage applied on impact.
        private float _speed;               // Projectile movement speed.
        private float _lifeTimer;           // Time since spawn.
        private LineRenderer _lineRenderer; // Trail/shot visual.

        #endregion
        #region Factory

        /// <summary>
        /// Spawns a musket projectile aimed at a target unit.
        /// </summary>
        /// <param name="shooter">The firing unit.</param>
        /// <param name="target">The target unit.</param>
        /// <param name="damage">Damage to apply on hit.</param>
        /// <param name="speed">Projectile travel speed.</param>
        public static void Spawn(Unit shooter, Unit target, float damage, float speed) {
            if (shooter == null || target == null) return;

            var projectileObject = new GameObject("Musket Projectile");
            projectileObject.transform.position = shooter.transform.position;

            var projectile = projectileObject.AddComponent<MusketProjectile>();
            projectile.Initialize(shooter, target, damage, speed);
        }

        #endregion
        #region Unity Methods

        private void Update() {
            _lifeTimer += Time.deltaTime;
            if (ShouldExpire()) {
                Destroy(gameObject);
                return;
            }

            MoveTowardsTarget();
            UpdateShotTrail();
            TryHitTarget();
        }

        #endregion
        #region Initialization

        /// <summary>
        /// Sets projectile state after the runtime GameObject is created.
        /// </summary>
        /// <param name="shooter">The firing unit.</param>
        /// <param name="target">The target unit.</param>
        /// <param name="damage">Damage to apply on hit.</param>
        /// <param name="speed">Projectile travel speed.</param>
        private void Initialize(Unit shooter, Unit target, float damage, float speed) {
            _shooter = shooter;
            _target = target;
            _damage = damage;
            _speed = speed;

            CreateLineRenderer();
        }

        /// <summary>
        /// Creates the visual line used for the projectile trail.
        /// </summary>
        private void CreateLineRenderer() {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = 0.035f;
            _lineRenderer.endWidth = 0.01f;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = Color.yellow;
            _lineRenderer.endColor = Color.white;
            _lineRenderer.sortingLayerName = "Default";
            _lineRenderer.sortingOrder = 100;
        }

        #endregion
        #region Movement

        /// <summary>
        /// Checks if the projectile should be destroyed before impact.
        /// </summary>
        /// <returns>True when the projectile should expire.</returns>
        private bool ShouldExpire() {
            return _lifeTimer >= LifeSeconds || _target == null || !_target.IsAlive;
        }

        /// <summary>
        /// Moves the projectile towards the target.
        /// </summary>
        private void MoveTowardsTarget() {
            var targetPosition = _target.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _speed * Time.deltaTime);
        }

        /// <summary>
        /// Updates the two-point line renderer trail.
        /// </summary>
        private void UpdateShotTrail() {
            if (_lineRenderer == null || _target == null) return;

            var targetPosition = _target.transform.position;
            var direction = (targetPosition - transform.position).normalized;
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, transform.position - direction * 0.25f);
        }

        /// <summary>
        /// Applies damage and destroys the projectile when it reaches the target.
        /// </summary>
        private void TryHitTarget() {
            if (_target == null) return;
            if (Vector2.Distance(transform.position, _target.transform.position) > HitDistance) return;

            _target.ApplyProjectileDamage(_shooter, _damage);
            Destroy(gameObject);
        }

        #endregion
    }
}
