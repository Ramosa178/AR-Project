using UnityEngine;

namespace PokemonAR.Collection
{
    public class PokeballBob : MonoBehaviour
    {
        [SerializeField] private float bobHeight = 0.05f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 _startPos;

        private void Start()
        {
            _startPos = transform.localPosition;
        }

        private void Update()
        {
            float newY = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = new Vector3(_startPos.x, newY, _startPos.z);
        }
    }
}