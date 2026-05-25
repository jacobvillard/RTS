using UnityEngine;

namespace _Scripts.Units {
    public class MusketProjectile : MonoBehaviour {
        private const float HitDistance = 0.08f;
        private const float LifeSeconds = 3f;

        private Unit _shooter;
        private Unit _target;
        private float _damage;
        private float _speed;
        private float _lifeTimer;
        private LineRenderer _lineRenderer;

        public static void Spawn(Unit shooter, Unit target, float damage, float speed) {
            if (shooter == null || target == null) return;

            var projectileObject = new GameObject("Musket Projectile");
            projectileObject.transform.position = shooter.transform.position;

            var projectile = projectileObject.AddComponent<MusketProjectile>();
            projectile.Initialize(shooter, target, damage, speed);
        }

        private void Initialize(Unit shooter, Unit target, float damage, float speed) {
            _shooter = shooter;
            _target = target;
            _damage = damage;
            _speed = speed;

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

        private void Update() {
            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= LifeSeconds || _target == null || !_target.IsAlive) {
                Destroy(gameObject);
                return;
            }

            var targetPosition = _target.transform.position;
            var nextPosition = Vector3.MoveTowards(transform.position, targetPosition, _speed * Time.deltaTime);
            transform.position = nextPosition;

            var direction = (targetPosition - transform.position).normalized;
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, transform.position - direction * 0.25f);

            if (Vector2.Distance(transform.position, targetPosition) <= HitDistance) {
                _target.ApplyProjectileDamage(_shooter, _damage);
                Destroy(gameObject);
            }
        }
    }
}
