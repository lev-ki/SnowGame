using NerdingRoom.Scripts.Core.NRStats;
using UnityEngine;

namespace NerdingRoom.Scripts.SnowGame
{
    public class SnowFallController : MonoBehaviour
    {
        public ParticleSystem snowParticleSystem;

        public int UpdateInterval = 15;
        private int updateCounter = 0;

        private float currentEmissionRate = 0;
        private void Start()
        {
            currentEmissionRate = snowParticleSystem.emission.rateOverTime.constant;
            var stat = new NRStat(Handles.SnowfallParticleEmissionRate.Get(), currentEmissionRate);
            NRStatSystem.Instance.AddStat(stat);
        }

        private void FixedUpdate()
        {
            if (++updateCounter > UpdateInterval)
            {
                updateCounter = 0;

                float newSnowRateOverTime = (float)NRStatSystem.Instance.GetDouble(Handles.SnowfallParticleEmissionRate.Get());
                
                if (!Mathf.Approximately(newSnowRateOverTime, currentEmissionRate))
                {
                    currentEmissionRate = newSnowRateOverTime;
                    ParticleSystem.EmissionModule emissionModule = snowParticleSystem.emission;
                    emissionModule.rateOverTime = newSnowRateOverTime;
                }
            }
        }
    }
}