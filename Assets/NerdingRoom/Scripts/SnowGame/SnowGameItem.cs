using DG.Tweening;
using NerdingRoom.Scripts.Core.NRGameResources;
using NerdingRoom.Scripts.Core.NRHandles;
using NerdingRoom.Scripts.Core.Utility;
using UnityEngine;

namespace NerdingRoom.Scripts.SnowGame
{
    public class SnowGameItem : NRGameResourceItem
    {
        public SpriteRenderer Sprite;
        public BoxCollider2D BoundsBoxCollider;

        public float CollectionJumpHeight = 0.2f;

        public override void Reset()
        {
            if (collected)
            {
                collected = false;    
                transform.localScale = initialScale;
            }
        }
        
        public override void CollectResource(Vector3 targetLocation)
        {
            if (collected)
            {
                return;
            }
            TargetLocationOnceCollected = targetLocation;
            collected = true;
            // TODO: generalize this (inherit from base item class in SnowGame assembly, move this functionality there)
            var initialJump = transform.DOMove(transform.position + (Vector3.up + Vector3.right * Random.Range(- 0.5f, 0.5f)).normalized * CollectionJumpHeight, 0.5f)
                .SetEase(Ease.OutBack);
            initialJump.easeOvershootOrAmplitude = 10;
            DOTween.Sequence()
                .Append(initialJump)
                .Append(
                    transform.DOMove(TargetLocationOnceCollected, 0.5f)
                )
                    .Join(
                        transform.DOScale(transform.localScale * 0.5f, 0.5f)
                    )
                .OnComplete(
                    ()=>
                    {
                        NRGameResourceManager.Instance.Add(ResourceHandle.Get(), ResourceValue);
                        GameObjectPooler.Despawn(gameObject);
                    }
                )
                .Play();
                
        }

        public override Bounds GetBounds()
        {
            return BoundsBoxCollider.bounds;
        }
    }
}