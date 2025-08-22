using UnityEngine;

namespace Riten.CharacterTrail
{
    public struct Snapshot
    {
        public Mesh mesh;
        public Matrix4x4 matrix;
        public float duration;
        public float timeAlive;
        public MaterialPropertyBlock propertyBlock;
        public bool shouldFree;

        public Snapshot(Mesh mesh, Matrix4x4 matrix, float duration, MaterialPropertyBlock propertyBlock, bool shouldFree)
        {
            this.mesh = mesh;
            this.matrix = matrix;
            this.duration = duration;
            timeAlive = 0;
            this.shouldFree = shouldFree;
            this.propertyBlock = propertyBlock;
        }
    }
}
