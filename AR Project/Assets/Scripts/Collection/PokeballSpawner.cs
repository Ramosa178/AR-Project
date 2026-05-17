using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace PokemonAR.Collection
{
    // Listens to ARTrackedImageManager and spawns/despawns pokeball prefabs
    // on top of each detected image marker.
    //
    // The trackedImageManager field is auto-resolved via GetComponent if not
    // assigned in the Inspector -- no manual wiring required.
    public class PokeballSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject pokeballPrefab;

        [Tooltip("Leave null -- auto-found via GetComponent on the same GameObject.")]
        [SerializeField] private ARTrackedImageManager trackedImageManager;

        private readonly Dictionary<TrackableId, GameObject> _spawnedBalls = new();

        private void Awake()
        {
            if (trackedImageManager == null)
                trackedImageManager = GetComponent<ARTrackedImageManager>();

            if (trackedImageManager == null)
                Debug.LogError("[PokeballSpawner] ARTrackedImageManager not found! " +
                               "Add it to the same GameObject (XR Origin) or assign it in the Inspector.");
        }

        private void OnEnable()
        {
            if (trackedImageManager != null)
                trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }

        private void OnDisable()
        {
            if (trackedImageManager != null)
                trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }

        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
        {
            foreach (ARTrackedImage added in args.added)
                HandleImage(added);

            foreach (ARTrackedImage updated in args.updated)
                HandleImage(updated);

            foreach (ARTrackedImage removed in args.removed)
                HandleRemoved(removed);
        }

        private void HandleImage(ARTrackedImage trackedImage)
        {
            if (trackedImage == null) return;

            bool isTracking = trackedImage.trackingState == TrackingState.Tracking;

            if (!_spawnedBalls.TryGetValue(trackedImage.trackableId, out GameObject ball))
            {
                // Only spawn when actively tracked
                if (!isTracking) return;

                ball = SpawnBall(trackedImage);
                if (ball == null) return;
                _spawnedBalls[trackedImage.trackableId] = ball;
            }

            // Show/hide based on tracking. Position is automatic via parenting.
            if (ball != null)
                ball.SetActive(isTracking);
        }

        private void HandleRemoved(ARTrackedImage trackedImage)
        {
            if (trackedImage == null) return;
            if (_spawnedBalls.TryGetValue(trackedImage.trackableId, out GameObject ball))
            {
                if (ball != null) Destroy(ball);
                _spawnedBalls.Remove(trackedImage.trackableId);
            }
        }

        private GameObject SpawnBall(ARTrackedImage trackedImage)
        {
            if (pokeballPrefab == null)
            {
                Debug.LogError("[PokeballSpawner] pokeballPrefab is not assigned!");
                return null;
            }

            // Parent ball to the tracked image transform -- it follows the marker automatically.
            // No manual position update needed in the updated loop.
            GameObject ball = Instantiate(
                pokeballPrefab,
                trackedImage.transform.position,
                Quaternion.identity,
                trackedImage.transform);

            ball.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            ball.transform.localRotation = Quaternion.identity;

            Debug.Log($"[PokeballSpawner] Spawned pokeball on marker: {trackedImage.referenceImage.name}");
            return ball;
        }
    }
}