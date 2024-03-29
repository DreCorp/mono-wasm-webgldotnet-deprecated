using OpenToolkit.Mathematics;
using WebGLDotNET;

namespace Engine
{
    public abstract class Mesh
    {
        public Vector3 color { get; set; }
        public bool isTextured = false;
        public int textureId;
        public float[] normals;

        //public virtual int NormalCount { get { return normals.Length; } }

        public Vector3 Position = Vector3.Zero;
        public Vector3 Rotation = Vector3.Zero;
        public Vector3 Scale = Vector3.One;

        public virtual int VertCount { get; set; }
        public virtual int IndiceCount { get; set; }
        public virtual int ColorDataCount { get; set; }
        public virtual int TextureCoordCount { get; set; }


        public Matrix4 modelMatrix = Matrix4.Identity;
        public Matrix4 tempModelMatrix = Matrix4.Identity;
        public Matrix4 ViewProjectionMatrix = Matrix4.Identity;
        public Matrix4 ModelViewProjectionMatrix = Matrix4.Identity;

        public abstract float[] GetVerts();
        public abstract ushort[] GetIndices(int offset = 0);
        public abstract float[] GetColorData();
        public abstract float[] GetTextureCoords();


        public void CalculateModelMatrix()
        {
            modelMatrix = Matrix4.CreateScale(Scale)
           * Matrix4.CreateRotationX(Rotation.X)
           * Matrix4.CreateRotationY(Rotation.Y)
           * Matrix4.CreateRotationZ(Rotation.Z)
           * Matrix4.CreateTranslation(Position);
        }

        public virtual float[] GetNormals() { return normals; }

        public void CalculateNormals()
        {
            float[] f = GetVerts();

            Vector3[] _normals = new Vector3[f.Length / 3];

            Vector3[] verts = new Vector3[f.Length / 3];

            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = new Vector3(f[i * 3], f[i * 3 + 1], f[i * 3 + 2]);
            }

            ushort[] inds = GetIndices();

            //Console.WriteLine(IndiceCount);

            //compute normals for each face
            for (int i = 0; i < IndiceCount; i += 3)
            {
                Vector3 v1 = verts[inds[i]];//
                Vector3 v2 = verts[inds[i + 1]];
                Vector3 v3 = verts[inds[i + 2]];

                //normals are a product of two sides of the triangle
                _normals[inds[i]] += Vector3.Cross(v2 - v1, v3 - v1);
                _normals[inds[i + 1]] += Vector3.Cross(v2 - v1, v3 - v1);
                _normals[inds[i + 2]] += Vector3.Cross(v2 - v1, v3 - v1);
            }

            normals = new float[_normals.Length * 3];

            for (int i = 0; i < _normals.Length; i++)
            {
                _normals[i] = Vector3.Normalize(_normals[i]);
                normals[i * 3] = _normals[i].X;
                normals[i * 3 + 1] = _normals[i].Y;
                normals[i * 3 + 2] = _normals[i].Z;
            }
        }
    }
}