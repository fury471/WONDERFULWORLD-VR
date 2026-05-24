using UnityEngine;

#pragma warning disable 0649

namespace Wonderland.UI
{
    [DisallowMultipleComponent]
    public sealed class LocalizedNoticeBoardSurface : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField] private LocalizedNoticeBoardContent content;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private SpriteRenderer targetSpriteRenderer;
        [SerializeField] private int materialIndex;
        [SerializeField] private bool useMaterialPropertyBlock = true;

        private MaterialPropertyBlock propertyBlock;

        private void Reset()
        {
            targetRenderer = GetComponent<Renderer>();
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            UILanguageService.LanguageChanged += ApplyLanguage;
            ApplyLanguage(UILanguageService.GetCurrentOrDefault());
        }

        private void OnDisable()
        {
            UILanguageService.LanguageChanged -= ApplyLanguage;
        }

        public void SetContent(LocalizedNoticeBoardContent noticeContent)
        {
            content = noticeContent;
            ApplyLanguage(UILanguageService.GetCurrentOrDefault());
        }

        private void ResolveReferences()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetSpriteRenderer == null)
            {
                targetSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void ApplyLanguage(UILanguage language)
        {
            ResolveReferences();

            if (content == null)
            {
                return;
            }

            Sprite sprite = content.GetSprite(language);
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.sprite = sprite;
                targetSpriteRenderer.enabled = true;
                return;
            }

            if (targetRenderer == null)
            {
                return;
            }

            if (useMaterialPropertyBlock)
            {
                targetRenderer.GetPropertyBlock(propertyBlock, materialIndex);
                propertyBlock.SetTexture(BaseMapId, sprite.texture);
                propertyBlock.SetTexture(MainTexId, sprite.texture);
                targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
                return;
            }

            Material[] materials = targetRenderer.materials;
            if (materialIndex < 0 || materialIndex >= materials.Length || materials[materialIndex] == null)
            {
                return;
            }

            Material material = materials[materialIndex];
            if (material.HasProperty(BaseMapId))
            {
                material.SetTexture(BaseMapId, sprite.texture);
            }

            if (material.HasProperty(MainTexId))
            {
                material.SetTexture(MainTexId, sprite.texture);
            }
        }
    }
}

#pragma warning restore 0649
