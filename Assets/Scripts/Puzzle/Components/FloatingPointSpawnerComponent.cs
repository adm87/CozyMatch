using System.Collections;
using System.Collections.Generic;
using Cozy.Match.UI.Components;
using UnityEngine;

namespace Cozy.Match.Puzzle.Components
{
    public class FloatingPointSpawnerComponent : MonoBehaviour
    {
        [SerializeField]
        private GameObject floatingPointPrefab;

        private List<GameObject> pooledFloatingPoints;

        private GameObject container;

        private void Awake()
        {
            pooledFloatingPoints = new List<GameObject>();

            container = new GameObject("FloatingPointsContainer");
            container.transform.SetParent(transform);
        }

        public void Spawn(Vector3 position, int points)
        {
            GameObject floatingPointObject = GetFloatingPoint();
            floatingPointObject.transform.position = position;

            PointsFloatingComponent pointsFloating = floatingPointObject.GetComponent<PointsFloatingComponent>();
            pointsFloating.SetPoints(points);

            StartCoroutine(Animate(pointsFloating));
        }

        private IEnumerator Animate(PointsFloatingComponent points)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            float distance = 0.5f;

            Vector3 startPosition = points.transform.position;
            Vector3 targetPosition = startPosition + Vector3.up * distance;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = t * t * (3f - 2f * t); // Smoothstep ease-in-out

                points.transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
                points.Alpha = Mathf.Lerp(1f, 0f, easedT);

                yield return null;
            }

            ReturnFloatingPoint(points.gameObject);
        }

        private GameObject GetFloatingPoint()
        {
            GameObject floatingPoint;
            if (pooledFloatingPoints.Count > 0)
            {
                floatingPoint = pooledFloatingPoints[pooledFloatingPoints.Count - 1];
                pooledFloatingPoints.RemoveAt(pooledFloatingPoints.Count - 1);
                floatingPoint.SetActive(true);
            }
            else
            {
                floatingPoint = Instantiate(floatingPointPrefab);
                floatingPoint.transform.SetParent(container.transform);
            }

            floatingPoint.transform.LookAt(Camera.main.transform);
            return floatingPoint;
        }

        private void ReturnFloatingPoint(GameObject floatingPoint)
        {
            floatingPoint.SetActive(false);
            pooledFloatingPoints.Add(floatingPoint);
        }
    }
}