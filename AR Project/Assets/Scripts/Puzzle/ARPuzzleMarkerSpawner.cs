using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace PokemonAR.Puzzle
{
    public class ARPuzzleMarkerSpawner : MonoBehaviour
    {
        [System.Serializable]
        private struct NumberMarkerPrefab
        {
            public string markerName;
            public NumberObjectController numberPrefab;
            public int numberValue;
            public float spawnScaleMultiplier;
            public Vector3 localPositionOffset;
            public Vector3 localEulerOffset;
        }

        [Header("References")]
        [SerializeField] private ARTrackedImageManager trackedImageManager;
        [SerializeField] private PuzzleManager puzzleManager;

        [Header("Marker Mapping")]
        [SerializeField] private NumberMarkerPrefab[] markerPrefabs;
        [SerializeField] private bool hideWhenNotTracking = true;
        [Header("Spawn Tuning")]
        [SerializeField] private Vector3 localSpawnOffset = new Vector3(0f, 0.06f, 0f);
        [SerializeField] private Vector3 localSpawnScale = new Vector3(0.18f, 0.18f, 0.18f);
        [SerializeField] private bool autoNormalizeModelSize = true;
        [SerializeField] private float targetModelHeightMeters = 0.12f;
        [SerializeField] private float minAutoScaleFactor = 0.2f;
        [SerializeField] private float maxAutoScaleFactor = 30f;

        private readonly Dictionary<TrackableId, NumberObjectController> _spawnedByTrackable = new();

        private void OnEnable()
        {
            if (trackedImageManager != null)
            {
                trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
            }
        }

        private void OnDisable()
        {
            if (trackedImageManager != null)
                trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }

        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
        {
            foreach (ARTrackedImage added in args.added)
                HandleTrackedImage(added);

            foreach (ARTrackedImage updated in args.updated)
                HandleTrackedImage(updated);

            foreach (ARTrackedImage removed in args.removed)
                HandleRemovedImage(removed);
        }

        private void HandleTrackedImage(ARTrackedImage trackedImage)
        {
            if (trackedImage == null) return;

            if (!_spawnedByTrackable.TryGetValue(trackedImage.trackableId, out NumberObjectController spawned))
            {
                spawned = SpawnForMarker(trackedImage);
                if (spawned == null) return;

                _spawnedByTrackable[trackedImage.trackableId] = spawned;
                puzzleManager?.RegisterNumber(spawned);
            }

            spawned.transform.SetPositionAndRotation(trackedImage.transform.position, trackedImage.transform.rotation);
            bool visible = trackedImage.trackingState == TrackingState.Tracking || !hideWhenNotTracking;
            spawned.gameObject.SetActive(visible);
        }

        private void HandleRemovedImage(ARTrackedImage trackedImage)
        {
            if (trackedImage == null) return;
            if (!_spawnedByTrackable.TryGetValue(trackedImage.trackableId, out NumberObjectController spawned)) return;

            if (spawned != null)
            {
                puzzleManager?.UnregisterNumber(spawned);
                Destroy(spawned.gameObject);
            }

            _spawnedByTrackable.Remove(trackedImage.trackableId);
        }

        private NumberObjectController SpawnForMarker(ARTrackedImage trackedImage)
        {
            NumberMarkerPrefab? mapping = FindMapping(trackedImage.referenceImage.name);
            if (!mapping.HasValue || mapping.Value.numberPrefab == null)
            {
                Debug.LogWarning($"[ARPuzzleMarkerSpawner] No mapping found for marker '{trackedImage.referenceImage.name}'.");
                return null;
            }

            NumberObjectController spawned = Instantiate(
                mapping.Value.numberPrefab,
                trackedImage.transform.position,
                trackedImage.transform.rotation,
                trackedImage.transform);

            // Force predictable placement above marker regardless of prefab authoring offsets.
            spawned.transform.localPosition = localSpawnOffset + mapping.Value.localPositionOffset;
            spawned.transform.localRotation = Quaternion.Euler(mapping.Value.localEulerOffset);
            float scaleMultiplier = mapping.Value.spawnScaleMultiplier > 0f ? mapping.Value.spawnScaleMultiplier : 1f;
            Vector3 baseScale = localSpawnScale * scaleMultiplier;

            if (autoNormalizeModelSize)
            {
                float autoScale = CalculateAutoScaleFactor(spawned, targetModelHeightMeters);
                autoScale = Mathf.Clamp(autoScale, minAutoScaleFactor, maxAutoScaleFactor);
                baseScale *= autoScale;
            }

            spawned.transform.localScale = baseScale;
            spawned.SetNumberValue(mapping.Value.numberValue);
            spawned.EnsureInteractionCollider(true);
            return spawned;
        }

        private static float CalculateAutoScaleFactor(NumberObjectController spawned, float targetHeightMeters)
        {
            if (spawned == null || targetHeightMeters <= 0f) return 1f;

            Renderer[] renderers = spawned.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return 1f;

            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                combined.Encapsulate(renderers[i].bounds);

            float currentHeight = combined.size.y;
            if (currentHeight <= 0.0001f)
                currentHeight = Mathf.Max(combined.size.x, combined.size.z);
            if (currentHeight <= 0.0001f) return 1f;

            return targetHeightMeters / currentHeight;
        }

        private NumberMarkerPrefab? FindMapping(string markerName)
        {
            if (markerPrefabs == null) return null;

            string target = Normalize(markerName);
            for (int i = 0; i < markerPrefabs.Length; i++)
                if (Normalize(markerPrefabs[i].markerName) == target)
                    return markerPrefabs[i];

            return null;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return value.Trim().Replace(" ", "_").ToLowerInvariant();
        }
    }
}
