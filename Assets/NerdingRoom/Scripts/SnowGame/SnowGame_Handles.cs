using NerdingRoom.Scripts.Core.NRHandles;

namespace NerdingRoom.Scripts.SnowGame
{
    static partial class Handles
    {
        [NRHandle("Stat.Snow.BaseValue.Min")] 
        public static NRHandleRef SnowBaseValueMin;

        [NRHandle("Stat.Snow.BaseValue.Max")] 
        public static NRHandleRef SnowBaseValueMax;

        [NRHandle("Stat.Snow.Change.StartVelocity")] 
        public static NRHandleRef SnowChangeStartVelocity;

        [NRHandle("Stat.Snow.Change.Acceleration")] 
        public static NRHandleRef SnowChangeAcceleration;

        [NRHandle("Stat.Snowfall.Particle.Value")] 
        public static NRHandleRef SnowfallParticleValue;
        
        [NRHandle("Stat.Snowfall.Particle.EmissionRate")]
        public static NRHandleRef SnowfallParticleEmissionRate;

        [NRHandle("Stat.Snow.Particle.SmoothingThreshold")] 
        public static NRHandleRef SnowParticleSmoothingThreshold;

        [NRHandle("Stat.Snow.Diff.Threshold")] 
        public static NRHandleRef SnowDiffThreshold;

        [NRHandle("Stat.Snow.Diff.CancelThreshold")] 
        public static NRHandleRef SnowDiffCancelThreshold;

        [NRHandle("Stat.Snow.Smoothing.Step")] 
        public static NRHandleRef SnowSmoothingStep;

        [NRHandle("Stat.Snow.Smoothing.Spread")] 
        public static NRHandleRef SmoothingSpread;

        [NRHandle("Stat.Snow.Smoothing.StepsPerFrame")] 
        public static NRHandleRef SmoothingStepsPerFrame;

        [NRHandle("Stat.Snow.Click.Cost")] 
        public static NRHandleRef ClickCost;

        [NRHandle("Stat.Snow.Click.ValueMult")] 
        public static NRHandleRef ClickValueMult;

        [NRHandle("Stat.Snow.Click.Spread")] 
        public static NRHandleRef ClickSpread;

        [NRHandle("Stat.Snow.Click.SpreadReductionStep")] 
        public static NRHandleRef ClickSpreadReductionStep;

        [NRHandle("Stat.Snow.Click.Timeout")] 
        public static NRHandleRef ClickTimeout;

        [NRHandle("Stat.Snow.Click.AllowHold")] 
        public static NRHandleRef ClickAllowHold;

        [NRHandle("Stat.Snow.Click.AllowHover")] 
        public static NRHandleRef ClickAllowHover;

        [NRHandle("Stat.Snow.Gathering.ParticleRatio")] 
        public static NRHandleRef GatheredSnowToParticlesRatio;    }
}