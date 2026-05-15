using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MyBox;
using NerdingRoom.Scripts.Core.NRGameResources;
using NerdingRoom.Scripts.Core.NRSessions;
using NerdingRoom.Scripts.Core.Utility;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NerdingRoom.Scripts.SnowGame
{
    public class SnowItemsGatherer : MonoBehaviour
    {
        public UIDataLayer UIDataLayer;
        public SnowShaderWriter MainLogic;
        public NRSessionManager SessionManager;

        public List<GameObject> AvailablePrefabs;
        
        public BoxCollider2D BoundsBoxCollider;
        public Bounds SpawnBounds;
        public List<NRGameResourceItem> SpawnedItems;

        private void Awake()
        {
            foreach (GameObject prefab in AvailablePrefabs)
            {
                GameObjectPooler.Preload(prefab, 100);
            }
            SessionManager.OnSessionStateEvent.AddListener(SessionStateChanged);
        }

        public IEnumerator SpawnWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnItems(40);
        }
        private void SessionStateChanged(ENRSessionState state)
        {
            if (state == ENRSessionState.Started)
            {
                StartCoroutine(SpawnWithDelay(1.1f));
                return;
            }

            if (state == ENRSessionState.Finished)
            {
                for (int i = SpawnedItems.Count - 1; i >= 0; i--)
                {
                    GameObjectPooler.Despawn(SpawnedItems[i].gameObject);
                    SpawnedItems.RemoveAt(i);
                }
            }
        }

        [ButtonMethod]
        public void GetBoundsFromCollider()
        {
            SpawnBounds = BoundsBoxCollider.bounds;
        }

        [ButtonMethod]
        public void SpawnItems_10()
        {
            SpawnItems(10);
        }
        
        public void SpawnItems(int count)
        {
            if (MainLogic == null)
            {
                return;
            }

            int leftToSpawn = count;
            int maxAttempts = count * 10;
            int attempt = 0;
            do
            {
                GameObject prefab = AvailablePrefabs.GetRandom();
                Vector3 spawnPos = new Vector3(
                        Random.Range(SpawnBounds.min.x, SpawnBounds.max.x),
                        Random.Range(SpawnBounds.min.y, SpawnBounds.max.y),
                        Random.Range(SpawnBounds.min.z, SpawnBounds.max.z)
                    );

                NRGameResourceItem item = prefab.GetComponent<NRGameResourceItem>();
                int arrayIndex = MainLogic.WorldPositionToArrayIndex(spawnPos, true, true, item.GetBounds().size.y / 2);
                if (arrayIndex >= 0)
                {
                    var spawnedItem = GameObjectPooler.Spawn(prefab, spawnPos, Quaternion.identity).GetComponent<NRGameResourceItem>();
                    spawnedItem.Reset();
                    SpawnedItems.Add(spawnedItem);
                    leftToSpawn--;
                }
            } while (leftToSpawn > 0 && attempt++ < maxAttempts);
        }

        private void Update()
        {
            if (MainLogic == null)
            {
                return;
            }

            for (int i = SpawnedItems.Count - 1; i >= 0 ; i--)
            {
                NRGameResourceItem item =  SpawnedItems[i];
                Vector3 pos = item.transform.position;
                pos.y = item.GetBounds().min.y;
                int arrayIndex = MainLogic.WorldPositionToArrayIndex(pos, true, false);
                if (arrayIndex >= 0)
                {
                    SpawnedItems.RemoveAt(i);
                    item.CollectResource(UIDataLayer.coinsCounterData.WorldLocation);
                }
            }
        }
    }
}