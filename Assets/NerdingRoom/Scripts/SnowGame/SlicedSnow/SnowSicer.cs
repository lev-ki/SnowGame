using System;
using System.Collections.Generic;
using MyBox;
using NerdingRoom.Scripts.Core.NRStats;
using UnityEngine;
using UnityEngine.Serialization;

namespace NerdingRoom.Scripts.SnowGame.SlicedSnow
{
    public class SnowSlicer : MonoBehaviour
    {
        private static readonly int Height = Shader.PropertyToID("_Height");
        public GameObject Root;
        public GameObject BaseLevel;
        public Transform SidesParent;
        
        public int GeneratedSlices = 15;
        public float WorldStep;

        public List<GameObject> GeneratedLevels;
        public List<MaterialPropertyBlock> MaterialPropertyBlocks;
        
        public CustomRenderTexture TargetRenderTexture;

        private void Awake()
        {
            Slice();
        }

        [ButtonMethod]
        private void Slice()
        {
            if (!TargetRenderTexture)
            {
                return;
            }
            TargetRenderTexture.Initialize();
            
            if (Root == null || BaseLevel == null)
            {
                return;
            }

            if (GeneratedLevels != null)
            {
                foreach (GameObject level in GeneratedLevels)
                {
                    GameObject.DestroyImmediate(level);
                }
                GeneratedLevels.Clear();
            }
            if (MaterialPropertyBlocks != null)
            {
                MaterialPropertyBlocks.Clear();
            }

            GeneratedLevels = new List<GameObject>();
            MaterialPropertyBlocks = new List<MaterialPropertyBlock>();

            for (int i = 0; i < GeneratedSlices; i++)
            {
                GameObject Level = Instantiate(BaseLevel, Root.transform);
                Level.transform.position = BaseLevel.transform.position + Vector3.up * WorldStep * (i);
                MeshRenderer renderer = Level.GetComponent<MeshRenderer>();
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetFloat(Height, 1.0f * i /  GeneratedSlices);
                renderer.SetPropertyBlock(block);
                GeneratedLevels.Add(Level);
                MaterialPropertyBlocks.Add(block);
            }

            SidesParent.localScale = new Vector3(1.0f, (GeneratedSlices - 1) * WorldStep, 1.0f);
        }
    }
}