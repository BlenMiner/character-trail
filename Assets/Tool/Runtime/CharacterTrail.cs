using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Riten.CharacterTrail
{
    [ExecuteInEditMode]
    public class CharacterTrail : MonoBehaviour
    {
        private static readonly int ColorProp = Shader.PropertyToID("_BaseColor");

        [Header("Base Settings")]
        [SerializeField] private bool _autoFindChildren = true;
        [SerializeField] private Material _material;
        [SerializeField] private int _layer;
        [Header("Emit Settings")]
        [SerializeField] private float _duration = 1f;
        [SerializeField] private float _rateOverTime = -1;
        [SerializeField] private float _rateOverDistance = 1f;
        [Header("Render Settings")]
        [SerializeField] private Gradient _gradient;

        private readonly List<MeshFilter> _meshRenderers = new();
        private readonly List<SkinnedMeshRenderer> _skinnedMeshRenderers = new();
        private readonly List<Snapshot> _snapshots = new();

        readonly Queue<Mesh> _meshPool = new();
        readonly Queue<MaterialPropertyBlock> _materialPropertyBlockPool = new();

        private Vector3 _lastPosition;
        private float _lastTimeEmission;

        private Mesh GetMesh()
        {
            if (_meshPool.Count > 0)
                return _meshPool.Dequeue();
            return new Mesh();
        }

        public void FreeMesh(Mesh mesh)
        {
            _meshPool.Enqueue(mesh);
        }

        private MaterialPropertyBlock GetMaterialPropertyBlock()
        {
            if (_materialPropertyBlockPool.Count > 0)
                return _materialPropertyBlockPool.Dequeue();
            return new MaterialPropertyBlock();
        }

        public void FreeMaterialPropertyBlock(MaterialPropertyBlock materialPropertyBlock)
        {
            _materialPropertyBlockPool.Enqueue(materialPropertyBlock);
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;

            if (_autoFindChildren)
            {
                GetComponentsInChildren<SkinnedMeshRenderer>(_skinnedMeshRenderers);
                GetComponentsInChildren<MeshFilter>(_meshRenderers);
            }
        }

        public void Register(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            if (_skinnedMeshRenderers.Contains(skinnedMeshRenderer)) return;
            _skinnedMeshRenderers.Add(skinnedMeshRenderer);
        }

        public void Register(MeshFilter meshRenderer)
        {
            if (_meshRenderers.Contains(meshRenderer)) return;
            _meshRenderers.Add(meshRenderer);
        }

        public void Unregister(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            _skinnedMeshRenderers.Remove(skinnedMeshRenderer);
        }

        public void Unregister(MeshFilter meshRenderer)
        {
            _meshRenderers.Remove(meshRenderer);
        }

        public void Emit()
        {
            for (var i = 0; i < _skinnedMeshRenderers.Count; i++)
            {
                var skinnedMeshRenderer = _skinnedMeshRenderers[i];
                var mesh = GetMesh();
                mesh.Clear();
                skinnedMeshRenderer.BakeMesh(mesh, true);
                var propertyBlock = GetMaterialPropertyBlock();
                var snapshot = new Snapshot(mesh, skinnedMeshRenderer.transform.localToWorldMatrix, _duration,
                    propertyBlock, true);
                _snapshots.Add(snapshot);
            }

            for (var i = 0; i < _meshRenderers.Count; i++)
            {
                var meshRenderer = _meshRenderers[i];
                var propertyBlock = GetMaterialPropertyBlock();
                var snapshot = new Snapshot(meshRenderer.sharedMesh, meshRenderer.transform.localToWorldMatrix,
                    _duration, propertyBlock, false);
                _snapshots.Add(snapshot);
            }
        }

        private void Update()
        {
            if (_rateOverTime > 0)
            {
                _lastTimeEmission += Time.deltaTime;
                if (_lastTimeEmission >= _rateOverTime)
                {
                    Emit();
                    _lastTimeEmission = 0;
                }
            }

            if (_rateOverDistance > 0)
            {
                var distance = Vector3.Distance(_lastPosition, transform.position);
                if (distance >= _rateOverDistance)
                {
                    Emit();
                    _lastPosition = transform.position;
                }
            }

            HandleSnapshots();
        }

        private void HandleSnapshots()
        {
            for (int i = 0; i < _snapshots.Count; i++)
            {
                var snapshot = _snapshots[i];
                snapshot.timeAlive += Time.deltaTime;
                snapshot.propertyBlock.SetColor(ColorProp, _gradient.Evaluate(snapshot.timeAlive / snapshot.duration));

                int subMeshCount = snapshot.mesh.subMeshCount;
                for (int j = 0; j < subMeshCount; j++)
                {
                    Graphics.DrawMesh(snapshot.mesh, snapshot.matrix, _material, _layer, null, j,
                        snapshot.propertyBlock, ShadowCastingMode.On, true, null, LightProbeUsage.BlendProbes, null);
                }

                if (snapshot.timeAlive >= snapshot.duration)
                {
                    if (snapshot.shouldFree)
                        FreeMesh(snapshot.mesh);
                    FreeMaterialPropertyBlock(snapshot.propertyBlock);
                    _snapshots.RemoveAt(i--);
                }
                else _snapshots[i] = snapshot;
            }
        }
    }
}
