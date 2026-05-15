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
        
        public int GeneratedSlices = 15;
        public float WorldStep;

        public List<GameObject> GeneratedLevels;
        public List<MaterialPropertyBlock> MaterialPropertyBlocks;

        private void Awake()
        {
            Slice();
        }

        [ButtonMethod]
        private void Slice()
        {
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
                Level.transform.position = BaseLevel.transform.position + Vector3.up * WorldStep * (i + 1);
                MeshRenderer renderer = Level.GetComponent<MeshRenderer>();
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetFloat(Height, 1.0f * i /  GeneratedSlices);
                renderer.SetPropertyBlock(block);
                GeneratedLevels.Add(Level);
                MaterialPropertyBlocks.Add(block);
            }
        }
    }
}