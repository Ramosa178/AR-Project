using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace PokemonAR.Collection
{
    public class PokeballSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject pokeballPrefab;
        [SerializeField] private ARTrackedImageManager trackedImageManager;

        private Dictionary<TrackableId, GameObject> _spawnedBalls = new();

        private void OnEnable()
        {
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }

        private void OnDisable()
        {
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
        }

        private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            // New marker detected — spawn pokeball
            foreach (var trackedImage in args.added)
            {
                SpawnBall(trackedImage);
            }

            // Marker updated — move pokeball with marker
            foreach (var trackedImage in args.updated)
            {
                if (_spawnedBalls.TryGetValue(trackedImage.trackableId, out GameObject ball))
                {
                    bool isTracking = trackedImage.trackingState == TrackingState.Tracking;
                    ball.SetActive(isTracking);
                    if (isTracking)
                        ball.transform.position = trackedImage.transform.position;
                }
            }

            // Marker lost — hide pokeball
            // Marker lost — hide pokeball
            // Marker lost — hide pokeball
            foreach (var kvp in args.removed)
            {
                if (_spawnedBalls.TryGetValue(kvp.Key, out GameObject ball))
                {
                    Destroy(ball);
                    _spawnedBalls.Remove(kvp.Key);
                }
            }
        }

        private void SpawnBall(ARTrackedImage trackedImage)
        {
            if (pokeballPrefab == null) return;

            GameObject ball = Instantiate(pokeballPrefab, trackedImage.transform.position, Quaternion.identity);
            ball.transform.SetParent(trackedImage.transform);
            _spawnedBalls[trackedImage.trackableId] = ball;

            Debug.Log($"[PokeballSpawner] Spawned pokeball on marker: {trackedImage.referenceImage.name}");
        }
    }
}