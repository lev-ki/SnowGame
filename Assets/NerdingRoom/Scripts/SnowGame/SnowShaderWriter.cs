using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using MyBox;
using NerdingRoom.Scripts.Core.NRGameResources;
using NerdingRoom.Scripts.Core.NRHandles;
using NerdingRoom.Scripts.Core.NRInput;
using NerdingRoom.Scripts.Core.NRSessions;
using NerdingRoom.Scripts.Core.NRStats;
using NerdingRoom.Scripts.Core.Utility;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace NerdingRoom.Scripts.SnowGame
{
    public class SnowShaderWriter : MonoBehaviour
    {
        public NRSessionManager SessionManager;
        
        public int randomSeed = 123123124;
        public SpriteRenderer targetSprite;
        public AnimationCurve animationCurve;
        public EdgeCollider2D edgeCollider2D;
        public ParticleSystem snowParticleSystem;
        public GameObject boundsMin;
        public GameObject boundsMax;

        public SnowGatheringParticleSpawner ParticleSpawner;
        
        [SerializeField, HideInInspector]
        private float[] points = new float[512];
        [SerializeField, HideInInspector]
        private Vector2[] points2d = new Vector2[128 + 2];

        [SerializeField]
        private float snowBaseValueMin = 0.25f;
        [SerializeField]
        private float snowBaseValueMax = 0.5f;
        [SerializeField]
        private float snowChangeStartVelocity = 0;
        [SerializeField]
        private float snowChangeAcceleration = 0.0001f;
        [FormerlySerializedAs("snowParticleValue")] [SerializeField]
        private float snowfallParticleValue = 0.0004f;
        [SerializeField]
        private float snowParticleSmoothingThreshold = 0.0002f;
        [SerializeField]
        private float snowDiffThreshold = 0.006f;
        [SerializeField]
        private int smoothingSpread = 8;
        [SerializeField]
        private float snowDiffCancelThreshold = 0.001f;
        [SerializeField]
        private float snowSmoothingStep = 0.0002f;
        [SerializeField]
        private int smoothingStepsPerFrame = 4;

        [SerializeField] private float gatheredSnowToParticlesRatio = 1000f;

        private HashSet<int> _fallingSnow = new HashSet<int>();

        private Dictionary<int, float> _snowAdjustmentsWithinTransaction;

        private float deltaX = 0.0f;
        
        private float[] tempPoints = new float[512];
        private bool initializing = false;
        private float initializationProgressNormalized = 0;
        
        private void Awake()
        {
            NRGameResourceManager.Instance.Register(Handles.Resource_Snow.Get(), 0);
            NRGameResourceManager.Instance.Register(Handles.Resource_Coins.Get(), 0);
            // NRGameResourceManager.Instance.Register(Handles.Resource_Snow.Get(), 0);
            
            points = new float[512];
            points2d = new Vector2[128 + 2];
            
            AddAllStats();
            SessionManager.OnSessionStateEvent.AddListener(OnSessionStateChanged);
        }

        private void OnSessionStateChanged(ENRSessionState state)
        {
            if (state == ENRSessionState.Started)
            {
                SetPoints();
                randomSeed++;
            }
        }

        private void AddAllStats()
        {
            var stats = NRStatSystem.Instance;

            stats.AddStat(new NRStat(Handles.SnowBaseValueMin.Get(), snowBaseValueMin));
            stats.AddStat(new NRStat(Handles.SnowBaseValueMax.Get(), snowBaseValueMax));

            stats.AddStat(new NRStat(Handles.SnowChangeStartVelocity.Get(), snowChangeStartVelocity));
            stats.AddStat(new NRStat(Handles.SnowChangeAcceleration.Get(), snowChangeAcceleration));

            stats.AddStat(new NRStat(Handles.SnowfallParticleValue.Get(), snowfallParticleValue));
            stats.AddStat(new NRStat(Handles.SnowParticleSmoothingThreshold.Get(), snowParticleSmoothingThreshold));

            stats.AddStat(new NRStat(Handles.SnowDiffThreshold.Get(), snowDiffThreshold));
            stats.AddStat(new NRStat(Handles.SnowDiffCancelThreshold.Get(), snowDiffCancelThreshold));
            stats.AddStat(new NRStat(Handles.SnowSmoothingStep.Get(), snowSmoothingStep));

            stats.AddStat(new NRStat(Handles.SmoothingSpread.Get(), smoothingSpread));
            stats.AddStat(new NRStat(Handles.SmoothingStepsPerFrame.Get(), smoothingStepsPerFrame));

            stats.AddStat(new NRStat(Handles.ClickAllowHold.Get(), 0));
            stats.AddStat(new NRStat(Handles.ClickAllowHover.Get(), 0));

            stats.AddStat(new NRStat(Handles.GatheredSnowToParticlesRatio.Get(), gatheredSnowToParticlesRatio));
        }

        private void Start()
        {
            float sizeX = boundsMax.transform.position.x - boundsMin.transform.position.x;
            deltaX = sizeX / points.Length;
        }

        private void OnEnable()
        {
            // points = new float[512];
            // points2d = new Vector2[128 + 2];
            // SetPoints();
        }

        [ContextMenu("SetPoints")]
        public void SetPointsDebug()
        {
            points = new float[512];
            points2d = new Vector2[128 + 2];
            SetPoints();
        }
        
        public void SetPoints()
        {
            if (!targetSprite)
            {
                return;
            }

            Random.InitState(randomSeed);

            float min = (float) NRStatSystem.Instance.GetDouble(Handles.SnowBaseValueMin.Get(), snowBaseValueMin);
            float max = (float) NRStatSystem.Instance.GetDouble(Handles.SnowBaseValueMax.Get(), snowBaseValueMax);
            float currentValue = Random.Range(min, max);

            float startVelocity = (float)NRStatSystem.Instance.GetDouble(
                Handles.SnowChangeStartVelocity.Get(), snowChangeStartVelocity
            );
            float acceleration = (float)NRStatSystem.Instance.GetDouble(
                Handles.SnowChangeAcceleration.Get(), snowChangeAcceleration
            );

            float currentChangeVelocity = Random.Range(-startVelocity, startVelocity);

            
            Array.Copy(points, tempPoints, points.Length);
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = currentValue;
                currentValue += currentChangeVelocity;
                if (currentValue < min || currentValue > max)
                {
                    currentChangeVelocity = 0;
                    currentValue = Mathf.Clamp(currentValue, min, max);
                }
                currentChangeVelocity += Random.Range(-acceleration, acceleration);
            }

            initializing = true;
            initializationProgressNormalized = 0;
            
            DOTween.To(()=> initializationProgressNormalized, x=> initializationProgressNormalized = x, 1.0f, 1f).SetEase(Ease.InOutQuad).SetUpdate(true)
                .OnComplete(() =>
                {
                    initializing = false;
                });
            UpdateVisualization();
        }

        private void UpdateVisualization()
        {
            float ratio = points.Length / (points2d.Length - 2);
            points2d[0] = new Vector2(-0.5f, -0.5f);
            points2d[^1] = new Vector2(0.5f, -0.5f);
            for (int i = 1; i < points2d.Length - 1; i++)
            {
                float t = Mathf.InverseLerp(1, points2d.Length - 2, i);
                points2d[i].x = t - 0.5f;
                points2d[i].y = points[(int)((i - 1) * ratio)] - 0.5f;
            }
            if (edgeCollider2D)
            {
                edgeCollider2D.SetPoints(new List<Vector2>(points2d));
            }

            targetSprite.sharedMaterial.SetFloatArray("_Segments", points);
            if (initializing)
            {
                targetSprite.sharedMaterial.SetFloatArray("_TempSegments", tempPoints);
                targetSprite.sharedMaterial.SetFloat("_InitializationProgress", initializationProgressNormalized);
            }
            else
            {
                targetSprite.sharedMaterial.SetFloat("_InitializationProgress", 1);
            }
        }
        private float ExpDecay(float a, float b, float expDecayFactor)
        {
            return b + (a - b) * expDecayFactor;
        }
        
        private void FixedUpdate()
        {
            if (initializing)
            {
                UpdateVisualization();
                return;
            }
            float diffThreshold = (float)NRStatSystem.Instance.GetDouble(
                Handles.SnowDiffThreshold.Get(), snowDiffThreshold
            );
            float diffCancelThreshold = (float)NRStatSystem.Instance.GetDouble(
                Handles.SnowDiffCancelThreshold.Get(), snowDiffCancelThreshold
            );
            int spread = (int)NRStatSystem.Instance.GetDouble(
                Handles.SmoothingSpread.Get(), smoothingSpread
            );

            for (int i = 1; i < points.Length - 1; i++)
            {
                float diffLeft = Mathf.Abs(points[i - 1] - points[i]);
                float diffRight = Mathf.Abs(points[i] - points[i + 1]);

                if (diffLeft >= diffThreshold && diffRight >= diffThreshold)
                {
                    for (int j = Mathf.Max(1, i - spread);
                         j < Mathf.Min(points.Length - 1, i + spread);
                         j++)
                    {
                        _fallingSnow.Add(j);
                    }
                }
                else if (_fallingSnow.Contains(i) &&
                         diffLeft <= diffCancelThreshold &&
                         diffRight <= diffCancelThreshold)
                {
                    _fallingSnow.Remove(i);
                }
            }

            float smoothingStep = (float)NRStatSystem.Instance.GetDouble(
                Handles.SnowSmoothingStep.Get(), snowSmoothingStep
            );
            int stepsPerFrame = (int)NRStatSystem.Instance.GetDouble(
                Handles.SmoothingStepsPerFrame.Get(), smoothingStepsPerFrame
            );

            for (int step = 0; step <= stepsPerFrame; step++)
            {
                foreach (int i in _fallingSnow.ToArray())
                {
                    float diffLeft = points[i - 1] - points[i];
                    float diffRight = points[i] - points[i + 1];

                    if (Mathf.Abs(diffLeft) >= Mathf.Abs(diffRight))
                    {
                        points[i - 1] -= Mathf.Sign(diffLeft) * smoothingStep;
                        points[i]     += Mathf.Sign(diffLeft) * smoothingStep;
                    }
                    else
                    {
                        points[i]     -= Mathf.Sign(diffRight) * smoothingStep;
                        points[i + 1] += Mathf.Sign(diffRight) * smoothingStep;
                    }
                }
            }

            UpdateVisualization();
        }

        public void GatherSnow(Vector3 point, int index, float baseCost, int spread, float reduction, float valueMult, bool tryUseTargetHeight = false)
        {
            const float minValue = 0.001f;
            double totalGatheredSnow = 0;
            if (tryUseTargetHeight)
            {
                float targetHeight = WorldHeightToValue(point.y);
                float targetValueAtCenter = Mathf.Max(minValue, targetHeight - baseCost);

                float finalValue = 0;
                if (targetValueAtCenter >= points[index])
                {
                    finalValue = Mathf.Max(points[index] - baseCost, minValue);
                }
                else
                {
                    finalValue = Mathf.Max(points[index] - baseCost, targetValueAtCenter);
                }
                totalGatheredSnow = points[index] - finalValue;
                points[index] = finalValue;
                
                for (int i = 1; i <= spread; i++)
                {
                    float localCost = baseCost * Mathf.Pow(reduction, i - 1);
                    float targetValue = Mathf.Max(minValue, targetHeight - localCost);

                    if (targetValue >= points[index - i])
                    {
                        finalValue = Mathf.Max(points[index - i] - localCost, minValue);
                    }
                    else
                    {
                        finalValue = Mathf.Max(points[index - i] - localCost, targetValue);
                    }
                    totalGatheredSnow += points[index - i] - finalValue;
                    points[index - i] = finalValue;
                    
                    if (targetValue >= points[index + i])
                    {
                        finalValue = Mathf.Max(points[index + i] - localCost, minValue);
                    }
                    else
                    {
                        finalValue = Mathf.Max(points[index + i] - localCost, targetValue);
                    }
                    totalGatheredSnow += points[index + i] - finalValue;
                    points[index + i] = finalValue;
                }
            }
            else
            {
                totalGatheredSnow = -AdjustSnowAtPoint_Immediate(index, -baseCost);
                for (int i = 1; i <= spread; i++)
                {
                    float localCost = baseCost * Mathf.Pow(reduction, i - 1);

                    totalGatheredSnow += -AdjustSnowAtPoint_Immediate(index - i, -localCost);
                    totalGatheredSnow += -AdjustSnowAtPoint_Immediate(index + i, -localCost);
                }
            }

            NRGameResourceManager.Instance.Add(Handles.Resource_Snow.Get(), (totalGatheredSnow * valueMult));

            Vector3 pos = ArrayIndexToWorldPosition(index);
            double ratio = NRStatSystem.Instance.GetDouble(Handles.GatheredSnowToParticlesRatio.Get());
            ParticleSpawner.EmitParticles(pos, deltaX * spread, (int) (totalGatheredSnow * ratio));
        }

        private void OnParticleCollision(GameObject other)
        {
            float particleValue = (float)NRStatSystem.Instance.GetDouble(
                Handles.SnowfallParticleValue.Get(), snowfallParticleValue
            );
            float smoothingThreshold = (float)NRStatSystem.Instance.GetDouble(
                Handles.SnowParticleSmoothingThreshold.Get(), snowParticleSmoothingThreshold
            );

            List<ParticleCollisionEvent> collisionEvents = new();
            snowParticleSystem.GetCollisionEvents(gameObject, collisionEvents);

            foreach (ParticleCollisionEvent collisionEvent in collisionEvents)
            {
                int arrayIndex = WorldPositionToArrayIndex(collisionEvent.intersection, true, false, 0.1f);
                if (arrayIndex == -1)
                {
                    return;
                }

                if (arrayIndex - 1 >= 0 &&
                    points[arrayIndex - 1] < points[arrayIndex] - smoothingThreshold)
                {
                    AdjustSnowAtPoint_Immediate(arrayIndex - 1, particleValue);
                }

                if (arrayIndex + 1 < points.Length &&
                    points[arrayIndex + 1] < points[arrayIndex] - smoothingThreshold)
                {
                    AdjustSnowAtPoint_Immediate(arrayIndex + 1, particleValue);
                }
                else
                {
                    AdjustSnowAtPoint_Immediate(arrayIndex, particleValue);
                }
            }

            UpdateVisualization();
        }

        public float AdjustSnowAtPoint_Immediate(int point, float inDelta)
        {
            if (point >= 0 && point < points.Length)
            {
                float delta = inDelta;
                float temp = points[point] + delta;
                points[point] = temp;
                if (temp < 0)
                {
                    delta -= temp;
                    points[point] = 0;
                }
                return delta;
            }

            return 0;
        }

        public int WorldPositionToArrayIndex(Vector3 worldPosition, bool checkY = false, bool below = true, float yTolerance = 0.01f)
        {
            var edgeColliderBounds = edgeCollider2D.bounds;
            float t = Mathf.InverseLerp(edgeColliderBounds.min.x, edgeColliderBounds.max.x, worldPosition.x);
            int index = Mathf.RoundToInt(t * (points.Length - 1));
            if (!checkY)
            {
                return index;
            }
            
            float height = boundsMax.transform.position.y - boundsMin.transform.position.y;
            if ((below && worldPosition.y - yTolerance < boundsMin.transform.position.y + points[index] * height)
                ||
                (!below && worldPosition.y + yTolerance > boundsMin.transform.position.y + points[index] * height))
            {
                return index;
            }

            return -1;
        }

        public float WorldHeightToValue(float inHeight)
        {
            return Mathf.InverseLerp(boundsMin.transform.position.y, boundsMax.transform.position.y, inHeight);
        }

        public Vector3 ArrayIndexToWorldPosition(int index)
        {
            float t = (index * 1.0f) /  (points.Length - 1);
            float posX = Mathf.Lerp(boundsMin.transform.position.x, boundsMax.transform.position.x, t);
            float height = boundsMax.transform.position.y - boundsMin.transform.position.y;
            float posY = boundsMin.transform.position.y + points[index] * height;
            return new Vector3(posX, posY, boundsMax.transform.position.z);
        }

        public int FindMaxIndexWithinRange(int start, int spread)
        {
            int chosenIndex = -1;
            for (int i = Mathf.Max(start - spread, 0); i < Mathf.Min(start + spread, points.Length); i++)
            {
                if (chosenIndex < 0)
                {
                    chosenIndex = i;
                    continue;
                }

                if (points[i] > points[chosenIndex])
                {
                    chosenIndex = i;
                }
            }

            return chosenIndex < 0  ? start : chosenIndex;
        }
    }
}