using System.Collections;
using UnityEngine;

namespace PokemonAR.Puzzle
{
    public class NumberObjectController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int numberValue = 1;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private string preferredColorProperty = "_BaseColor";
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color correctColor = Color.green;
        [SerializeField] private Color wrongColor = Color.red;

        public int NumberValue => numberValue;

        private bool _locked;
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);

            EnsureInteractionCollider(true);

            SetIdle();
        }

        public void SetNumberValue(int value) => numberValue = value;

        public bool CanInteract() => !_locked && gameObject.activeInHierarchy;

        public void SetIdle()
        {
            _locked = false;
            SetColor(idleColor);
        }

        public void ShowCorrect() => SetColor(correctColor);

        public IEnumerator FlashWrongAndReset(float flashDurationSeconds)
        {
            _locked = true;
            SetColor(wrongColor);
            yield return new WaitForSeconds(flashDurationSeconds);
            SetIdle();
        }

        private void SetColor(Color color)
        {
            if (targetRenderers == null) return;

            foreach (Renderer r in targetRenderers)
            {
                if (r == null) continue;

                r.GetPropertyBlock(_mpb);
                string prop = ResolveColorProperty(r.sharedMaterial);
                _mpb.SetColor(prop, color);
                r.SetPropertyBlock(_mpb);
            }
        }

        private string ResolveColorProperty(Material mat)
        {
            if (mat == null) return preferredColorProperty;
            if (mat.HasProperty(preferredColorProperty)) return preferredColorProperty;
            if (mat.HasProperty("_Color")) return "_Color";
            return preferredColorProperty;
        }

        public void EnsureInteractionCollider(bool forceRefit = false)
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);

            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
                box = gameObject.AddComponent<BoxCollider>();

            if (!forceRefit && box.size.sqrMagnitude > 0.0001f)
                return;

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                box.center = Vector3.zero;
                box.size = Vector3.one;
                return;
            }

            Renderer first = null;
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] != null)
                {
                    first = targetRenderers[i];
                    break;
                }
            }

            if (first == null)
            {
                box.center = Vector3.zero;
                box.size = Vector3.one;
                return;
            }

            Bounds bounds = first.bounds;
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] == null || targetRenderers[i] == first) continue;
                bounds.Encapsulate(targetRenderers[i].bounds);
            }

            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            Vector3 localExtents = transform.InverseTransformVector(bounds.extents);
            localExtents = new Vector3(Mathf.Abs(localExtents.x), Mathf.Abs(localExtents.y), Mathf.Abs(localExtents.z));
            Vector3 localSize = localExtents * 2f;

            if (localSize.x < 0.01f) localSize.x = 0.2f;
            if (localSize.y < 0.01f) localSize.y = 0.2f;
            if (localSize.z < 0.01f) localSize.z = 0.2f;

            box.center = localCenter;
            box.size = localSize;
        }
    }
}
