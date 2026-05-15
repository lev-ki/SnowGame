using System;
using DG.Tweening;
using NerdingRoom.Scripts.Core.NRInput;
using NerdingRoom.Scripts.Core.NRSessions;
using NerdingRoom.Scripts.Core.NRStats;
using NerdingRoom.Scripts.Core.Utility;
using UnityEngine;
using UnityEngine.Serialization;

namespace NerdingRoom.Scripts.SnowGame
{
    public class SnowGatheringCursor : MonoBehaviour
    {
        private static readonly int Opacity = Shader.PropertyToID("_Opacity");
        private static readonly int Radius = Shader.PropertyToID("_Radius");
        public NRSessionManager SessionManager;
        public SnowShaderWriter snowShaderWriter;
        public SpriteRenderer Inner;
        public SpriteRenderer Outer;

        [SerializeField]
        private float clickCost = 0.01f;
        private NRStat clickCostStat;
        [SerializeField]
        private float clickValueMult = 22f;
        private NRStat clickValueMultStat;
        [FormerlySerializedAs("clickSpread")] [SerializeField]
        private int baseClickSpread = 3;
        private NRStat clickSpreadStat;
        [SerializeField]
        private float clickSpreadReductionStep = 0.6f;
        private NRStat clickSpreadReductionStepStat;

        [SerializeField]
        private float clickTimeout = 1.2f;
        private NRStat clickTimeoutStat;
        
        [SerializeField] private float timerToRadiusMultiplier = 0.05f;
        
        private float clickHoldTimeout = 0.25f;
        private float clickTimer = 1;

        [SerializeField]
        private float baseInnerCircleRadius = 0.02f;
        private float innerCircleRadius = 0.0f;

        [SerializeField] private float cursorSmoothLerpT = 1;
        
        private float controlOpacityInner
        {
            set => Inner.sharedMaterial.SetFloat(Opacity, value);
            get => Inner.sharedMaterial.GetFloat(Opacity);
        }
        private float controlOpacityOuter
        {
            set => Outer.sharedMaterial.SetFloat(Opacity, value);
            get => Outer.sharedMaterial.GetFloat(Opacity);
        }
        
        private bool paused = false;
        
        private void Awake()
        {
            var stats = NRStatSystem.Instance;
            clickCostStat = stats.AddStat(new NRStat(Handles.ClickCost.Get(), clickCost));
            clickValueMultStat = stats.AddStat(new NRStat(Handles.ClickValueMult.Get(), clickValueMult));
            clickSpreadStat = stats.AddStat(new NRStat(Handles.ClickSpread.Get(), baseClickSpread));
            clickSpreadReductionStepStat = stats.AddStat(new NRStat(Handles.ClickSpreadReductionStep.Get(), clickSpreadReductionStep));
            
            clickTimeoutStat = stats.AddStat(new NRStat(Handles.ClickTimeout.Get(), clickTimeout));

            innerCircleRadius = (float)(baseInnerCircleRadius * clickSpreadStat.GetCalculatedValue() / baseClickSpread);
            Inner.sharedMaterial.SetFloat(Radius, innerCircleRadius);
            clickSpreadStat.OnValueUpdated.AddListener(OnClickSpreadUpdated);
            OnClickSpreadUpdated();
            
            SessionManager.OnSessionStateEvent.AddListener(OnSessionStateChanged);
        }

        private void OnSessionStateChanged(ENRSessionState state)
        {
            if (state == ENRSessionState.Finished)
            {
                paused = true;
                clickTimer = clickHoldTimeout;
                DOTween.To(()=> controlOpacityInner, x=> controlOpacityInner = x, 0.0f, 1f).SetEase(Ease.InOutQuad).SetUpdate(true);
                DOTween.To(()=> controlOpacityOuter, x=> controlOpacityOuter = x, 0.0f, 1f).SetEase(Ease.InOutQuad).SetUpdate(true);
            }

            if (state == ENRSessionState.Started)
            {
                paused = false;
                DOTween.To(()=> controlOpacityInner, x=> controlOpacityInner = x, 1.0f, 1f).SetEase(Ease.InOutQuad).SetUpdate(true);
            }
        }

        private void OnClickSpreadUpdated()
        {
            innerCircleRadius = (float)(baseInnerCircleRadius * clickSpreadStat.GetCalculatedValue() / baseClickSpread);
            Inner.sharedMaterial.SetFloat(Radius, innerCircleRadius);
        }

        private void Update()
        {
            if (paused)
            {
                return;
            }
            Vector3 cursorPos = CustomInputManager.Instance.CursorPosition;
            cursorPos.z = transform.position.z;
            Vector3 cursorWorldPos = ToolBox.Instance.MainCamera.ScreenToWorldPoint(cursorPos);
            int pointIndex = snowShaderWriter.WorldPositionToArrayIndex(cursorWorldPos);
            if (pointIndex >= 0)
            {
                Vector3 snowPos = snowShaderWriter.ArrayIndexToWorldPosition(pointIndex);
                if (snowPos.y > cursorWorldPos.y)
                {
                    if (transform.position.y > snowPos.y)
                    {
                        cursorWorldPos.y = Mathf.Lerp(transform.position.y, snowPos.y, Time.deltaTime * cursorSmoothLerpT);
                    }
                    else
                    {
                        cursorWorldPos.y = Mathf.Max(cursorWorldPos.y, snowPos.y);
                    }
                }
            }
            transform.position = cursorWorldPos;
            
            clickHoldTimeout = (float)clickTimeoutStat.GetCalculatedValue();
            bool clickThisFrame = false;
            {
                clickTimer -= Time.deltaTime; 
                Outer.sharedMaterial.SetFloat(Radius, innerCircleRadius + clickTimer / clickHoldTimeout * timerToRadiusMultiplier);
                Outer.sharedMaterial.SetFloat(Opacity, 1.0f - clickTimer / clickHoldTimeout);
                if (clickTimer <= 0)
                {
                    clickThisFrame = true;
                    clickTimer = clickHoldTimeout;
                }
            }
                
            if (clickThisFrame)
            {
                Ray ray = ToolBox.Instance.MainCamera.ScreenPointToRay(CustomInputManager.Instance.CursorPosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    int index = snowShaderWriter.WorldPositionToArrayIndex(hit.point, true, true, 0.1f);
                    if (index < 0)
                    {
                        return;
                    }

                    float baseCost = (float)clickCostStat.GetCalculatedValue();
                    float valueMult = (float)clickValueMultStat.GetCalculatedValue();
                    int spread = (int)clickSpreadStat.GetCalculatedValue();
                    float reduction = (float)clickSpreadReductionStepStat.GetCalculatedValue();

                    snowShaderWriter.GatherSnow(hit.point, index, baseCost, spread, reduction, valueMult, true);
                }
            }

        }
    }
}