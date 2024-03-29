using OpenToolkit.Mathematics;

namespace Engine
{
    public class Quad : Mesh
    {
        float[] verts;
        float[] colorData;

        public Quad()
        {
            Setup(new Vector3(1f, 1f, 1f));
        }

        public Quad(Vector3 _color)
        {
            Setup(_color);
        }

        public void Setup(Vector3 _color)
        {
            VertCount = 4;
            IndiceCount = 6;
            ColorDataCount = 4;
            TextureCoordCount = 4;

            verts = new float[]
            {
                //CCW
                -0.5f, -0.5f, 0f,
                0.5f, -0.5f, 0f,
                0.5f, 0.5f, 0f,
                -0.5f, 0.5f,  0f,
            };

            color = _color;
        }

        public override float[] GetColorData()
        {
            colorData = new float[ColorDataCount * 3];
            for (int i = 0; i < ColorDataCount; i++)
            {
                colorData[i * 3] = color.X;
                colorData[i * 3 + 1] = color.Y;
                colorData[i * 3 + 2] = color.Z;
            }
            return colorData;
        }
        public override ushort[] GetIndices(int offset = 0)
        {
            ushort[] inds = new ushort[]
            {
                //CCW
               0,1,3,1,2,3
            };

            if (offset != 0)
            {
                for (int i = 0; i < inds.Length; i++)
                {
                    inds[i] += (ushort)offset;
                }
            }

            return inds;
        }

        public override float[] GetVerts()
        {
            return verts;
        }

        public override float[] GetTextureCoords()
        {
            return new float[]
            {
                0f,0f,
                1f,0f,
                1f,1f,
                0f,1f,
            };
        }
    }
}