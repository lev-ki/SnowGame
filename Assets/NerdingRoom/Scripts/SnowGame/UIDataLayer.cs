using System.Collections.Generic;
using DG.Tweening;
using NerdingRoom.Scripts.Core.NRGameResources;
using NerdingRoom.Scripts.Core.NRHandles;
using NerdingRoom.Scripts.Core.NRSessions;
using NerdingRoom.Scripts.Core.NRUnlockables;
using NerdingRoom.Scripts.Core.Utility;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace NerdingRoom.Scripts.SnowGame
{
    [System.Serializable]
    public class ResourceDataSource
    {
        public bool display = true;
        public int resourceCount = 0;
        public LocalizedString counterName;
        public VisualElement CounterImage;
        public Vector3 WorldLocation;
        public GameObject particleAttractor;
    }

    public class UIDataLayer : MonoBehaviour 
    {
        [SerializeField]
        private VisualTreeAsset counterLabelAsset;

        public LocalizedString snowCounterLabel;
        
        public ResourceDataSource snowCounterData = new();
        public ResourceDataSource coinsCounterData = new();

        public NRSessionManager SessionManager;
        public NRUnlockablesRuntimeManager Unlockables;

        public bool UpgradeMenuEnabled = false;
        public float UpgradeMenuVisibility = 0.0f;

        private Vector2Int docReferenceResolution;
        
        private void Start()
        {
            GenerateUI();
            
            NRGameResourceManager.Instance.OnResourcesUpdated.AddListener(OnResourcesUpdated);
            SessionManager.OnSessionStateEvent.AddListener(OnSessionStateChanged);
        }

        private void LateUpdate()
        {
            CalculateWorldLocationForDataSource(snowCounterData);
            if (snowCounterData.particleAttractor)
            {
                snowCounterData.particleAttractor.transform.position = snowCounterData.WorldLocation;
            }
            CalculateWorldLocationForDataSource(coinsCounterData);
        }

        private void CalculateWorldLocationForDataSource(ResourceDataSource dataSource)
        {
            Vector2 elementScreenPos = dataSource.CounterImage.worldBound.center;
            Vector3 elementScreenPosInverted = new Vector3(elementScreenPos.x, docReferenceResolution.y - elementScreenPos.y, 0);
    
            float ratioX = docReferenceResolution.x == 0 ? 1 : (Screen.width  * 1.0f / docReferenceResolution.x);
            float ratioY = docReferenceResolution.y == 0 ? 1 : (Screen.height * 1.0f / docReferenceResolution.y);

            elementScreenPosInverted = new Vector3(ratioX * elementScreenPosInverted.x, ratioY * elementScreenPosInverted.y, transform.position.z);

            if (float.IsNaN(elementScreenPosInverted.x) || float.IsNaN(elementScreenPosInverted.y))
            {
                return;
            }
            dataSource.WorldLocation = ToolBox.Instance.MainCamera.ScreenToWorldPoint(elementScreenPosInverted);
        }

        private void OnResourcesUpdated()
        {
            snowCounterData.resourceCount = NRGameResourceManager.Instance.Get<int>(Handles.Resource_Snow.Get());
            coinsCounterData.resourceCount = NRGameResourceManager.Instance.Get<int>(Handles.Resource_Coins.Get());
            snowCounterData.WorldLocation = snowCounterData.CounterImage.LocalToWorld(Vector3.zero);
            coinsCounterData.WorldLocation = coinsCounterData.CounterImage.LocalToWorld(Vector3.zero);
            UIDocument doc = FindFirstObjectByType<UIDocument>();
            GenerateButtons(doc);
        }

        private void OnSessionStateChanged(ENRSessionState state)
        {
            switch (state)
            {
                case ENRSessionState.None:
                    break;
                case ENRSessionState.Started:
                {
                    DOTween.To(()=> UpgradeMenuVisibility, x=> UpgradeMenuVisibility = x, 0.0f, 1f).SetEase(Ease.InOutQuad).SetUpdate(true)
                        .OnComplete(() =>
                        {
                            UpgradeMenuEnabled = false;
                        });
                    break;
                }
                case ENRSessionState.Finished:
                {
                    UpgradeMenuVisibility = 0;
                    UpgradeMenuEnabled = true;
                    DOTween.To(()=> UpgradeMenuVisibility, x=> UpgradeMenuVisibility = x, 1.0f, 1f).SetEase(Ease.InOutQuad).SetUpdate(true);
                    break;
                }
                default: return;
            }
        }
        
        private void GenerateUI()
        {
            UIDocument doc = FindFirstObjectByType<UIDocument>();
            docReferenceResolution = doc.panelSettings.referenceResolution;
            doc.rootVisualElement.dataSource = this;
            
            VisualElement counters = doc.rootVisualElement.Q("Counters");
            
            if (counters != null)
            {
                counters.Clear();

                {
                    counterLabelAsset.CloneTree(counters, out int clonedCounterIndex, out _);
                    VisualElement child = counters[clonedCounterIndex];
                    child.dataSource = snowCounterData;
                    child.Q<Label>("ResourceLabel").SetBinding("text", snowCounterData.counterName);
                    snowCounterData.CounterImage = child.Q("Image");
                }
                {
                    counterLabelAsset.CloneTree(counters, out int clonedCounterIndex, out _);
                    VisualElement child = counters[clonedCounterIndex];
                    child.dataSource = coinsCounterData;
                    child.Q<Label>("ResourceLabel").SetBinding("text", coinsCounterData.counterName);
                    coinsCounterData.CounterImage = child.Q("Image");
                }
            }

            Button button = doc.rootVisualElement.Q<Button>("NextDayButton");
            button.clicked -= OnNextDayButtonPressed;
            button.clicked += OnNextDayButtonPressed;
            GenerateButtons(doc);
        }

        public void OnNextDayButtonPressed()
        {
            SessionManager.StartSession();
        }
        
        private void GenerateButtons(UIDocument doc)
        {
            VisualElement unblockableButtons = doc.rootVisualElement.Q("UnlockableButtons");
            if (unblockableButtons != null)
            {
                unblockableButtons.Clear();

                List<NRHandle> usedHandles = new();
                foreach (NRHandleRef handle in Unlockables.OuterLayer)
                {
                    NRUnlockablesGraphDefinition.NRGraphLink links = Unlockables.GraphDefinition.FindLink(handle);

                    foreach (NRHandleRef link in links.To)
                    {
                        if (usedHandles.Contains(link.Get()))
                        {
                            continue;
                        }
                        NRUnlockableDefinition def = Unlockables.GraphDefinition.FindDefByHandle(link);
                        if (def == null)
                        {
                            continue;
                        }
                        
                        EUnlockableState state = Unlockables.GetUnlockableState(link);
                        switch (state)
                        {
                            case EUnlockableState.PriceLocked:
                            {
                                Button shovelButton = new Button(PriceLocked);// { text = "Get Shovel" };
                                shovelButton.SetBinding("text", def.Name);
                                shovelButton.AddToClassList("locked-button");
                                unblockableButtons.Add(shovelButton);
                                usedHandles.Add(link.Get());
                                break;

                                void PriceLocked()
                                {
                                    Debug.Log($"Not enough cash! Stranger. No {def.Name.GetLocalizedString()} for you.");
                                }
                            }
                            case EUnlockableState.Unlockable:
                            {
                                Button shovelButton = new Button(Unlock);// { text = "Get Shovel" };
                                shovelButton.SetBinding("text", def.Name);
                                shovelButton.AddToClassList("unlockable-button");
                                unblockableButtons.Add(shovelButton);
                                usedHandles.Add(link.Get());
                                break;
                                
                                void Unlock()
                                {
                                    Unlockables.TryUnlockNode(link);
                                    GenerateUI();
                                }
                            }
                            default:
                                break;
                        }
                    }
                }
                //
                // Button allowHoldButton = new Button(OnAllowHoldButtonPressed) { text = "Hold!" };
                // allowHoldButton.AddToClassList("unlockable-button");
                // unblockableButtons.Add(allowHoldButton);
                //
                // Button allowHoverButton = new Button(OnAllowHoverButtonPressed) { text = "<b>Hover!!!</b>" };
                // allowHoverButton.AddToClassList("unlockable-button");
                // unblockableButtons.Add(allowHoverButton);
            }
        }
    }
}
