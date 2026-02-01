using System;
using System.Collections.Generic;
using UnityEngine;

public static class PatPatModelManager
{
    public static PatPatModel? CreatePattableModelIfDoesntExists(GameObject characterRoot)
    {
        if (characterRoot == null) return null;

        var model = characterRoot.GetComponent<PatPatModel>();
        if (model != null && model.IsCreated)
        {
            return model;
        }

        Transform scout = characterRoot.transform.Find("Scout");
        if (scout == null) return null;

        var skinnedMeshRenderers = new List<SkinnedMeshRenderer>(scout.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        var meshRenderers = new List<MeshRenderer>(scout.GetComponentsInChildren<MeshRenderer>(true));


        if (skinnedMeshRenderers.Count == 0 && meshRenderers.Count == 0)
        {
            skinnedMeshRenderers.AddRange(characterRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            meshRenderers.AddRange(characterRoot.GetComponentsInChildren<MeshRenderer>(true));
            if (skinnedMeshRenderers.Count == 0)
            {
                return null;
            }
        }

        HashSet<Transform> uniqueBones = new HashSet<Transform>();
        foreach (var renderer in skinnedMeshRenderers)
        {
            if (renderer == null) continue;
            if (renderer.bones != null)
            {
                foreach (var b in renderer.bones) if (b != null) uniqueBones.Add(b);
            }
            if (renderer.rootBone != null)
            {
                uniqueBones.Add(renderer.rootBone);
            }
        }

        if (uniqueBones.Count == 0)
        {
            return null;
        }

        HashSet<Transform> boneSet = new HashSet<Transform>(uniqueBones);


        if (model == null)
        {
            model = characterRoot.AddComponent<PatPatModel>();
        }

        Transform visualRoot = new GameObject("SeparatedVisualModel").transform;
        visualRoot.SetParent(scout.parent, false);
        visualRoot.localScale = scout.localScale;

        Dictionary<Transform, Transform> original2Duplication = new Dictionary<Transform, Transform>(uniqueBones.Count);
        foreach (var original in uniqueBones)
        {
            if (original == null) continue;
            var duplicationGameObject = new GameObject(original.name + "_visual");
            var duplcationTransform = duplicationGameObject.transform;
            duplcationTransform.position = original.position;
            duplcationTransform.rotation = original.rotation;
            duplcationTransform.localScale = original.localScale;
            original2Duplication[original] = duplcationTransform;
        }

        foreach (var entry in original2Duplication)
        {
            var original = entry.Key;
            var duplication = entry.Value;
            if (original.parent != null && original2Duplication.ContainsKey(original.parent))
            {
                duplication.SetParent(original2Duplication[original.parent], true);
            }
            else
            {
                duplication.SetParent(visualRoot, true);
            }
        }

        List<PatPatModel.SkinnedMeshRendererBind> binds = new List<PatPatModel.SkinnedMeshRendererBind>(skinnedMeshRenderers.Count);

        foreach (var renderer in skinnedMeshRenderers)
        {
            if (renderer == null) continue;

            var bind = new PatPatModel.SkinnedMeshRendererBind();
            bind.renderer = renderer;

            var bones = renderer.bones ?? [];
            bind.origBones = new Transform[bones.Length];
            bind.duplicationBones = new Transform[bones.Length];

            for (int i = 0; i < bones.Length; i++)
            {
                bind.origBones[i] = bones[i];
                if (bones[i] != null && original2Duplication.ContainsKey(bones[i]))
                    bind.duplicationBones[i] = original2Duplication[bones[i]];
                else
                    bind.duplicationBones[i] = null;
            }

            bind.originalRoot = renderer.rootBone;
            bind.duplicationRoot = (renderer.rootBone != null && original2Duplication.ContainsKey(renderer.rootBone)) ? original2Duplication[renderer.rootBone] : null;

            renderer.bones = bind.duplicationBones;
            if (bind.duplicationRoot != null) renderer.rootBone = bind.duplicationRoot;

            binds.Add(bind);
        }

        List<PatPatModel.MeshRendererBind> meshBinds = new List<PatPatModel.MeshRendererBind>(meshRenderers.Count);
        foreach (var renderer in meshRenderers)
        {
            if (renderer == null) continue;
            var meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            var duplicationGameObject = new GameObject(meshFilter.gameObject.name + "_visual");
            var duplicationTransform = duplicationGameObject.transform;

            var duplicationFilter = duplicationGameObject.AddComponent<MeshFilter>();
            duplicationFilter.sharedMesh = meshFilter.sharedMesh;

            var duplicationRenderer = duplicationGameObject.AddComponent<MeshRenderer>();
            duplicationRenderer.sharedMaterials = renderer.sharedMaterials;

            duplicationRenderer.shadowCastingMode = renderer.shadowCastingMode;
            duplicationRenderer.receiveShadows = renderer.receiveShadows;
            duplicationRenderer.lightProbeUsage = renderer.lightProbeUsage;
            duplicationRenderer.reflectionProbeUsage = renderer.reflectionProbeUsage;
            duplicationRenderer.motionVectorGenerationMode = renderer.motionVectorGenerationMode;

            Transform visualParent = GetOrCreateVisualParent(renderer.transform.parent, visualRoot, original2Duplication);

            duplicationTransform.SetParent(visualParent, false);
            duplicationTransform.localPosition = renderer.transform.localPosition;
            duplicationTransform.localRotation = renderer.transform.localRotation;

            duplicationTransform.localScale = renderer.transform.localScale;

            bool originalActive = renderer.gameObject.activeInHierarchy;
            bool duplicationActive = duplicationRenderer.gameObject.activeSelf;

            if (!originalActive)
            {
                SetDuplicationVisible(false);
                SetOriginalVisible(true);
            } else
            {
                SetDuplicationVisible(true);
                SetOriginalVisible(false);
            }

            void SetDuplicationVisible(bool value)
            {
                duplicationRenderer.gameObject.SetActive(value);
            }

            void SetOriginalVisible(bool value)
            {
                renderer.forceRenderingOff = !value;
            }

            var bind = new PatPatModel.MeshRendererBind();
            bind.originalRenderer = renderer;
            bind.originalTransform = renderer.transform;
            bind.duplicationRenderer = duplicationRenderer;
            bind.duplicationTransform = duplicationTransform;

            meshBinds.Add(bind);

            if (!boneSet.Contains(renderer.transform))
            {
                original2Duplication[renderer.transform] = duplicationTransform;
            }
        }

        model.Setup(visualRoot, original2Duplication, binds, meshBinds, boneSet);
        return model;
    }

    static Transform GetOrCreateVisualParent(
        Transform original,
        Transform visualRoot,
        Dictionary<Transform, Transform> map)
    {
        if (map.TryGetValue(original, out var existing))
            return existing;

        if (original.parent == null || original.name == "Scout")
            return visualRoot;

        Transform parentVisual = GetOrCreateVisualParent(original.parent, visualRoot, map);

        var go = new GameObject(original.name + "_visual");
        var t = go.transform;
        t.SetParent(parentVisual, false);
        t.localPosition = original.localPosition;
        t.localRotation = original.localRotation;
        t.localScale = original.localScale;

        map[original] = t;
        return t;
    }


    public class PatPatModel : MonoBehaviour
    {
        internal bool IsCreated { get; private set; } = false;
        internal Transform? VisualRoot { get; private set; }
        internal Dictionary<Transform, Transform>? OriginalToDuplication { get; private set; }
        internal List<SkinnedMeshRendererBind>? SkinnedMeshBinds { get; private set; }
        internal List<MeshRendererBind>? MeshBinds { get; private set; }
        internal HashSet<Transform>? BoneSet { get; private set; }
        internal Transform? HeadVisualGroupDuplicationRoot { get; private set; }


        public Transform? GetVisualRoot() => VisualRoot;

        [System.Serializable]
        public class SkinnedMeshRendererBind
        {
            public SkinnedMeshRenderer? renderer;
            public Transform[]? origBones;
            public Transform[]? duplicationBones;
            public Transform? originalRoot;
            public Transform? duplicationRoot;
        }

        [System.Serializable]
        public class MeshRendererBind
        {
            public MeshRenderer? originalRenderer;
            public Transform? originalTransform;
            public MeshRenderer? duplicationRenderer;
            public Transform? duplicationTransform;
        }

        private float currentYScale = 1f;
        private Vector3 currentPivot = Vector3.zero;

        internal void Setup(Transform visualRoot, Dictionary<Transform, Transform> map, List<SkinnedMeshRendererBind> skinnedMeshBinds, List<MeshRendererBind> meshBinds, HashSet<Transform> boneSet)
        {
            VisualRoot = visualRoot;
            OriginalToDuplication = map;
            SkinnedMeshBinds = skinnedMeshBinds;
            MeshBinds = meshBinds;
            IsCreated = true;
            BoneSet = boneSet;
            HeadVisualGroupDuplicationRoot = visualRoot.Find("Hip_visual/Mid_visual/AimJoint_visual/Torso_visual/Head_visual");
        }

        public void UpdateScale(float progress, Character character)
        {
            SetScale(GetAnimationValue(progress), character);
        }

        public static float GetAnimationValue(float progress)
        {
            progress = Clamp01(progress);
            float easedProgress = 1f - MathF.Pow(1f - progress, 2);
            float range = PatPatConfig.PatWeight.Value / 1.5f;

            return (1f - range) + range * (1f - MathF.Sin(easedProgress * MathF.PI));
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private void SetScale(float scale, Character character)
        {
            currentYScale = Mathf.Clamp(scale, 0.0001f, 10f);
            if (character.data.isGrounded) currentPivot = character.data.groundPos;
            else currentPivot = character.refs.ragdoll.partDict[BodypartType.Hip].transform.position;
        }

        public void Reset()
        {
            currentYScale = 1f;
            currentPivot = Vector3.zero;
        }

        void LateUpdate()
        {
            if (!IsCreated || OriginalToDuplication == null) return;

            if (MeshBinds != null)
            {
                foreach (var bind in MeshBinds)
                {
                    SyncBind(bind);
                }
            }

            var duplicationSet = new HashSet<Transform>();
            foreach (var v in OriginalToDuplication.Values)
            {
                if (v != null) duplicationSet.Add(v);
            }

            var headGroup = HeadVisualGroupDuplicationRoot;

            foreach (var entry in OriginalToDuplication)
            {
                var original = entry.Key;
                var duplication = entry.Value;
                if (original == null || duplication == null) continue;

                bool parentIsDup = duplication.parent != null && duplicationSet.Contains(duplication.parent);
                bool isUnderHeadGroup = (headGroup != null) && (duplication == headGroup || duplication.IsChildOf(headGroup));

                if (currentYScale == 1f)
                {
                    duplication.position = original.position;
                    duplication.rotation = original.rotation;
                    duplication.localScale = original.localScale;
                }
                else
                {
                    if (isUnderHeadGroup)
                    {
                        if (duplication == headGroup)
                        {
                            Vector3 direction = original.position - currentPivot;
                            direction = new Vector3(direction.x, direction.y * currentYScale, direction.z);
                            duplication.position = currentPivot + direction;
                            duplication.rotation = original.rotation;
                            duplication.localScale = new Vector3(
                                original.localScale.x,
                                original.localScale.y * currentYScale,
                                original.localScale.z
                            );
                        }
                        else
                        {
                            duplication.localPosition = original.localPosition;
                            duplication.localRotation = original.localRotation;
                            duplication.localScale = original.localScale;
                        }
                    }
                    else
                    {
                        Vector3 direction = original.position - currentPivot;
                        direction = new Vector3(direction.x, direction.y * currentYScale, direction.z);
                        duplication.position = currentPivot + direction;

                        duplication.rotation = original.rotation;

                        if (!parentIsDup)
                        {
                            duplication.localScale = new Vector3(
                                original.localScale.x,
                                original.localScale.y * currentYScale,
                                original.localScale.z
                            );
                        }
                        else
                        {
                            duplication.localScale = original.localScale;
                        }
                    }
                }
            }
        }

        private void SyncBind(MeshRendererBind bind)
        {
            if (bind == null) return;
            if (bind.originalRenderer == null || bind.duplicationRenderer == null) return;

            GameObject original = bind.originalRenderer.gameObject;
            GameObject duplication = bind.duplicationRenderer.gameObject;

            if (original == null || duplication == null) return;

            bool originalActive = original.activeInHierarchy;
            bool duplicationActive = duplication.activeSelf;

            if (!originalActive)
            {
                SetDuplicationVisible(false);
                SetOriginalVisible(true);
                return;
            }

            SetDuplicationVisible(true);
            SetOriginalVisible(false);

            //

            void SetDuplicationVisible(bool value)
            {
                duplication.SetActive(value);
            }

            void SetOriginalVisible(bool value)
            {
                bind.originalRenderer.forceRenderingOff = !value;
            }
        }

    }
}
