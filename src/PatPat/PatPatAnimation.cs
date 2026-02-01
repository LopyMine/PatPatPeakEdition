using System;
using UnityEngine;

public class PatPatAnimation : MonoBehaviour
{
    private const float BillboardHeightOffset = 1.0f;

    private Mesh? _quadMesh;
    private Material? _billboardMaterial;
    private Texture2D[]? _frames;
    private float _scale = 0.65f;
    private Vector3 _anchorPosition;
    private readonly float Duration = 0.35f;
    private float _duration = 0.0f;
    public Character character;
    public PatPatModelManager.PatPatModel model;

    public static PatPatAnimation? Create(Character character, PatPatModelManager.PatPatModel model, Texture2D[] frames)
    {
        if (frames == null || frames.Length == 0)
        {
            return null;
        }

        var go = new GameObject("PatPatAnimation");
        var comp = go.AddComponent<PatPatAnimation>();
        comp._anchorPosition = character.Center + Vector3.up * BillboardHeightOffset;
        comp._frames = frames;
        comp._scale = 0.65f;
        comp.character = character;
        comp.model = model;
        comp.Initialize();
        return comp;
    }

    private void Initialize()
    {
        _quadMesh = new Mesh();
        _quadMesh.name = "PatPat_Texture_Quad";
        _quadMesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f)
        };
        _quadMesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        _quadMesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        _quadMesh.RecalculateNormals();

        var mf = gameObject.AddComponent<MeshFilter>();
        var mr = gameObject.AddComponent<MeshRenderer>();
        mf.mesh = _quadMesh;

        Shader shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
        _billboardMaterial = new Material(shader ?? Shader.Find("Standard"));
        ApplyMaterialAlphaSettings(_billboardMaterial);

        _billboardMaterial.mainTexture = _frames[0];

        mr.material = _billboardMaterial;

        transform.position = _anchorPosition;
        transform.localScale = new Vector3(-_scale, _scale, 1f);
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 dir = cam.transform.position - transform.position;
            if (dir.sqrMagnitude > 0.00001f) transform.rotation = Quaternion.LookRotation(dir, cam.transform.up);
        }
    }

    public void Reset()
    {
        _duration = 0f;
        UpdatePanelFrame(0.0F);
    }

    private void Update()
    {
        if (_duration == Duration)
        {
            OnDestroy();
            return;
        }

        _duration += Time.deltaTime;
        if (_duration >= Duration)
        {
            _duration = Duration;
        }

        _anchorPosition = character.Center + Vector3.up * BillboardHeightOffset;
        transform.position = _anchorPosition;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 dir = cam.transform.position - transform.position;
            if (dir.sqrMagnitude > 0.00001f) transform.rotation = Quaternion.LookRotation(dir, cam.transform.up);
        }

        transform.localScale = new Vector3(-_scale, _scale, 1f);

        float progress = (float) _duration / Duration;
        UpdatePanelFrame(progress);
        if (model != null) model.UpdateScale(progress, character);
    }


    private void UpdatePanelFrame(float animationProgress)
    {
        if (_billboardMaterial == null || _frames == null) return;

        int frame = Mathf.Clamp(
            Mathf.FloorToInt(animationProgress * _frames.Length),
            0,
            _frames.Length - 1
        );
        PatPatPeakMod.ModLogger.LogInfo(frame);
        Texture2D tex = _frames[frame];
        if (tex != null)
            _billboardMaterial.mainTexture = tex;
    }

    private void ApplyMaterialAlphaSettings(Material mat)
    {
        if (mat == null) return;
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;
    }

    private void OnDestroy()
    {
        PatPatPeakMod.PatPatManager.RemoveExpired(character.photonView.ViewID);
        if (_billboardMaterial != null) Destroy(_billboardMaterial);
        if (_quadMesh != null) Destroy(_quadMesh);
        Destroy(gameObject);
    }
}
