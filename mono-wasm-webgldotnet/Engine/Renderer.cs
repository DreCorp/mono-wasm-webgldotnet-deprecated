using System;
using System.Linq;
using System.Collections.Generic;
using WebGLDotNET;
using OpenToolkit.Mathematics;


namespace Engine
{
    public class Renderer
    {
        public bool drawLines = false;
        public bool updateAttributes = false;
        public bool lightUpdated = false;

        int canvasWidth = 100;
        int canvasHeight = 100;
        WebGLRenderingContext gl;
        ShaderManager sm;
        Scene currentScene;
        WebGLBuffer indexBuffer;
        float[] vertData; //data array of vertex positions
        float[] colData;  //data array of vertex colors
        ushort[] indiceData;
        float[] normalData;
        float[] textureData;

        List<float> verts = new List<float>();
        List<ushort> inds = new List<ushort>();
        List<float> colors = new List<float>();
        List<float> normals = new List<float>();
        List<float> texCoords = new List<float>();

        Matrix4 view;
        public Renderer(WebGLRenderingContext _mgl, int cw, int ch, Scene scene)
        {
            Console.WriteLine($"Initializing {this}");

            gl = _mgl;
            currentScene = scene;

            canvasWidth = cw;
            canvasHeight = ch;

            gl.ClearColor(
                currentScene.bgColor.X,
                currentScene.bgColor.Y,
                currentScene.bgColor.Z,
                1.0f);

            gl.Enable(WebGLRenderingContextBase.DEPTH_TEST);
            gl.Enable(WebGLRenderingContextBase.CULL_FACE);
            gl.CullFace(WebGLRenderingContextBase.BACK);

            gl.Disable(WebGLRenderingContextBase.DITHER);
            gl.Disable(WebGLRenderingContextBase.STENCIL_TEST);
            gl.Disable(WebGLRenderingContextBase.POLYGON_OFFSET_FILL);
            gl.Disable(WebGLRenderingContextBase.SAMPLE_ALPHA_TO_COVERAGE);
            gl.Disable(WebGLRenderingContextBase.SAMPLE_COVERAGE);
            gl.Disable(WebGLRenderingContextBase.SCISSOR_TEST);

            indexBuffer = gl.CreateBuffer();

            sm = new ShaderManager(gl);

            SetAttributes();
            SetLightUniforms();

            Console.WriteLine($"Finished {this} initialization");
        }

        public void SetAttributes()
        {
            verts.Clear();
            colors.Clear();
            inds.Clear();
            normals.Clear();
            texCoords.Clear();

            int vertCount = 0;

            foreach (Mesh m in currentScene.objects)
            {
                verts.AddRange(m.GetVerts().ToList());
                inds.AddRange(m.GetIndices(vertCount).ToList());
                colors.AddRange(m.GetColorData().ToList());
                normals.AddRange(m.GetNormals().ToList());
                texCoords.AddRange(m.GetTextureCoords().ToList());

                vertCount += m.VertCount;
            }

            vertData = verts.ToArray();
            colData = colors.ToArray();
            indiceData = inds.ToArray();
            normalData = normals.ToArray();
            textureData = texCoords.ToArray();

            if (sm.GetAttributeLocation(sm.mShader, "vPos") != -1)
            {
                gl.BindBuffer(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    sm.mShader.buffers["vPos"]);

                gl.BufferData(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    vertData,
                    WebGLRenderingContextBase.STATIC_DRAW);

                gl.VertexAttribPointer(
                    (uint)sm.GetAttributeLocation(sm.mShader, "vPos"),
                    3,
                    WebGLRenderingContextBase.FLOAT,
                    false,
                    3 * sizeof(float),
                    0);

                //gl.BindAttribLocation(sm.mShader.prog, 0, "vPos");
            }

            if (sm.GetAttributeLocation(sm.mShader, "vColor") != -1)
            {
                gl.BindBuffer(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    sm.mShader.buffers["vColor"]);

                gl.BufferData(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    colData,
                    WebGLRenderingContextBase.STATIC_DRAW);

                gl.VertexAttribPointer(
                    (uint)sm.GetAttributeLocation(sm.mShader, "vColor"),
                    3,
                    WebGLRenderingContextBase.FLOAT,
                    false,
                    3 * sizeof(float),
                    0);
            }

            if (sm.GetAttributeLocation(sm.mShader, "vNormal") != -1)
            {
                gl.BindBuffer(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    sm.mShader.buffers["vNormal"]);

                gl.BufferData(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    normalData,
                    WebGLRenderingContextBase.STATIC_DRAW);

                gl.VertexAttribPointer(
                    (uint)sm.GetAttributeLocation(sm.mShader, "vNormal"),
                    3,
                    WebGLRenderingContextBase.FLOAT,
                    false,
                    3 * sizeof(float),
                    0);
            }

            if (sm.GetAttributeLocation(sm.mShader, "texCoord") != -1)
            {
                gl.BindBuffer(
                    WebGLRenderingContext.ARRAY_BUFFER,
                    sm.mShader.buffers["texCoord"]);

                gl.BufferData(
                    WebGLRenderingContext.ARRAY_BUFFER,
                    textureData,
                    WebGLRenderingContext.STATIC_DRAW);

                gl.VertexAttribPointer(
                    (uint)sm.GetAttributeLocation(sm.mShader, "texCoord"),
                    2,
                    WebGLRenderingContext.FLOAT,
                    false,
                    2 * sizeof(float),
                    0);
            }

            gl.BindBuffer(
                WebGLRenderingContextBase.ELEMENT_ARRAY_BUFFER, indexBuffer);
            gl.BufferData(
                WebGLRenderingContextBase.ELEMENT_ARRAY_BUFFER,
                indiceData,
                WebGLRenderingContextBase.STATIC_DRAW);

            EnableVertexAttribArrays();

            if (updateAttributes) updateAttributes = false;
        }

        public void Update(float dTime)
        {
            if (updateAttributes)
            {
                SetAttributes();
            }

            if (lightUpdated)
            {
                SetLightUniforms();
            }

            foreach (Mesh m in currentScene.objects)
            {
                m.CalculateModelMatrix();

                m.ViewProjectionMatrix = currentScene.cam.GetViewMatrix() *
                    Matrix4.CreatePerspectiveFieldOfView(0.90f, canvasWidth / (float)canvasHeight, 0.1f, 50.0f);

                m.ModelViewProjectionMatrix = m.modelMatrix * m.ViewProjectionMatrix;
            }


            view = currentScene.cam.GetViewMatrix();

            Render();
        }

        public void Render()
        {

            gl.Clear(WebGLRenderingContextBase.COLOR_BUFFER_BIT | WebGLRenderingContextBase.DEPTH_BUFFER_BIT);

            int indiceat = 0;

            foreach (Mesh m in currentScene.objects)
            {

                if (sm.GetUniformLocation(sm.mShader, "modelview") != null)
                {
                    gl.UniformMatrix4fv(
                    sm.GetUniformLocation(sm.mShader, "modelview"),
                    false,
                    MathHelper.Mat4ToFloatArray(m.ModelViewProjectionMatrix));
                }

                if (sm.GetUniformLocation(sm.mShader, "view") != null)
                {
                    gl.UniformMatrix4fv(
                    sm.GetUniformLocation(sm.mShader, "view"),
                    false,
                    MathHelper.Mat4ToFloatArray(view));
                }

                if (sm.GetUniformLocation(sm.mShader, "model") != null)
                {
                    gl.UniformMatrix4fv(
                    sm.GetUniformLocation(sm.mShader, "model"),
                    false,
                    MathHelper.Mat4ToFloatArray(m.modelMatrix));
                }

                if (sm.GetUniformLocation(sm.mShader, "maintexture") != null)
                {
                    //gl.ActiveTexture(WebGLRenderingContextBase.TEXTURE0);
                    gl.BindTexture(WebGLRenderingContextBase.TEXTURE_2D, ContentManager.textures[m.textureId]);
                    gl.Uniform1i(sm.GetUniformLocation(sm.mShader, "maintexture"), 0);
                }

                if (!drawLines)
                {
                    gl.DrawElements(
                        WebGLRenderingContextBase.TRIANGLES,
                        m.IndiceCount,
                        WebGLRenderingContextBase.UNSIGNED_SHORT,
                        (uint)(indiceat * sizeof(ushort)));
                }
                else
                {
                    gl.DrawElements(
                        WebGLRenderingContextBase.LINES,
                        m.IndiceCount,
                        WebGLRenderingContextBase.UNSIGNED_SHORT,
                        (uint)(indiceat * sizeof(ushort)));
                }

                indiceat += m.IndiceCount;
            }
            //DisableVertexAttribArrays();

            gl.Flush();
            gl.Finish();
        }

        void EnableVertexAttribArrays()
        {
            for (int i = 0; i < sm.mShader.attribs.Count; i++)
            {
                gl.EnableVertexAttribArray(sm.mShader.attribs.Values.ElementAt(i).address);
            }
        }
        void DisableVertexAttribArrays()
        {
            for (int i = 0; i < sm.mShader.attribs.Count; i++)
            {
                gl.DisableVertexAttribArray(sm.mShader.attribs.Values.ElementAt(i).address);
            }
        }

        public void SetLightUniforms()
        {
            if (sm.GetUniformLocation(sm.mShader, "lightDir") != null)
            {
                gl.Uniform3fv(
                    sm.GetUniformLocation(sm.mShader, "lightDir"),
                    currentScene.light.direction);
            }

            if (sm.GetUniformLocation(sm.mShader, "lightColor") != null)
            {
                gl.Uniform3fv(
                    sm.GetUniformLocation(sm.mShader, "lightColor"),
                    currentScene.light.color);
            }

            if (sm.GetUniformLocation(sm.mShader, "lightAmbientIntens") != null)
            {
                gl.Uniform1f(
                    sm.GetUniformLocation(sm.mShader, "lightAmbientIntens"),
                    currentScene.light.ambientIntensity);
            }

            if (sm.GetUniformLocation(sm.mShader, "lightDiffuseIntens") != null)
            {
                gl.Uniform1f(
                    sm.GetUniformLocation(sm.mShader, "lightDiffuseIntens"),
                    currentScene.light.diffuseIntensity);
            }

            if (lightUpdated) lightUpdated = false;
        }

        public void ChangeViewPortSize(int w, int h)
        {
            canvasWidth = w;
            canvasHeight = h;

            gl.Viewport(0, 0, w, h);
        }
    }
}