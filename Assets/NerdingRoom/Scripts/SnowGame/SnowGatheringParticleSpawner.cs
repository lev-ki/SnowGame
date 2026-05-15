using System;
using NerdingRoom.Scripts.Core.NRStats;
using UnityEngine;

namespace NerdingRoom.Scripts.SnowGame
{
    public class SnowGatheringParticleSpawner : MonoBehaviour
    {
        public ParticleSystem particleSystem;

        private void Start()
        {
            particleSystem = GetComponent<ParticleSystem>();
        }

        public void EmitParticles(Vector3 emitPosition, float emitRadius, int totalCount)
        {
            // ParticleSystem.EmitParams emitParams = new()
            // {
            //     position = emitPosition,
            //     applyShapeToPosition = true
            // };
            //
            var particleSystemShape = particleSystem.shape;
            particleSystemShape.radius = emitRadius;
            particleSystem.transform.position = emitPosition;

            particleSystem.Emit(totalCount);
        }
    }
}
