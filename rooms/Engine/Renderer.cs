using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using WebGLDotNET;
using System.Numerics;


namespace Engine
{
    public class Renderer : IDisposable
    {
        public bool drawLines = false;
        public bool assocAttribs = false;

        public int canvasWidth = 100;
        public int canvasHeight = 100;
        WebGLRenderingContext gl;
        ShaderManager sm;
        Scene currentScene;

        public WebGLBuffer indexBuffer;


        float[] vertData; //data array of vertex positions
        float[] colData;  //data array of vertex colors
        ushort[] indiceData;
        float[] normalData;

        List<float> verts = new List<float>();
        List<ushort> inds = new List<ushort>();
        List<float> colors = new List<float>();
        List<float> normals = new List<float>();

        Matrix4x4 view;
        Matrix4x4 tempView;



        public Renderer(WebGLRenderingContext _mgl, int cw, int ch, Scene scene)
        {
            Console.WriteLine($"Initializing {this}");

            SceneManager.OnSceneChanged += OnSceneChanged;

            currentScene = scene;

            gl = _mgl;
            canvasWidth = cw;
            canvasHeight = ch;

            gl.ClearColor(currentScene.bgColor.X, currentScene.bgColor.Y, currentScene.bgColor.Z, 1.0f);
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

            AssociateAttribs();

            Console.WriteLine($"Finished {this} initialization");
        }

        public void AssociateAttribs()
        {
            verts.Clear();
            colors.Clear();
            inds.Clear();
            normals.Clear();

            int vertCount = 0;

            foreach (Mesh m in currentScene.objects)
            {
                verts.AddRange(m.GetVerts().ToList());
                inds.AddRange(m.GetIndices(vertCount).ToList());
                colors.AddRange(m.GetColorData().ToList());
                normals.AddRange(m.GetNormals().ToList());

                vertCount += m.VertCount;
            }

            vertData = verts.ToArray();
            colData = colors.ToArray();
            indiceData = inds.ToArray();
            normalData = normals.ToArray();

            if (sm.GetAttribute(sm.mShader, "vPos") != -1)
            {
                gl.BindBuffer(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    sm.mShader.buffers["vPos"]);

                gl.BufferData(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    vertData,
                    WebGLRenderingContextBase.STATIC_DRAW);

                gl.VertexAttribPointer(
                    (uint)sm.GetAttribute(sm.mShader, "vPos"),
                    3,
                    WebGLRenderingContextBase.FLOAT,
                    false,
                    3 * sizeof(float),
                    0);
            }

            if (sm.GetAttribute(sm.mShader, "vColor") != -1)
            {
                gl.BindBuffer(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    sm.mShader.buffers["vColor"]);

                gl.BufferData(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    colData,
                    WebGLRenderingContextBase.STATIC_DRAW);

                gl.VertexAttribPointer(
                    (uint)sm.GetAttribute(sm.mShader, "vColor"),
                    3,
                    WebGLRenderingContextBase.FLOAT,
                    false,
                    3 * sizeof(float),
                    0);
            }

            if (sm.GetAttribute(sm.mShader, "vNormal") != -1)
            {
                gl.BindBuffer(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    sm.mShader.buffers["vNormal"]);

                gl.BufferData(
                    WebGLRenderingContextBase.ARRAY_BUFFER,
                    normalData,
                    WebGLRenderingContextBase.STATIC_DRAW);

                gl.VertexAttribPointer(
                    (uint)sm.GetAttribute(sm.mShader, "vNormal"),
                    3,
                    WebGLRenderingContextBase.FLOAT,
                    false,
                    3 * sizeof(float),
                    0);
            }

            gl.BindBuffer(
                WebGLRenderingContextBase.ELEMENT_ARRAY_BUFFER, indexBuffer);
            gl.BufferData(
                WebGLRenderingContextBase.ELEMENT_ARRAY_BUFFER,
                indiceData,
                WebGLRenderingContextBase.STATIC_DRAW);
        }
        public void Update(float dTime)
        {
            if (assocAttribs)
            {
                AssociateAttribs();
                assocAttribs = false;
            }

            foreach (Mesh m in currentScene.objects)
            {
                m.CalculateModelMatrix();

                m.ViewProjectionMatrix = currentScene.cam.GetViewMatrix() *
                    Matrix4x4.CreatePerspectiveFieldOfView(1.3f, canvasWidth / (float)canvasHeight, 0.1f, 50.0f);

                m.ModelViewProjectionMatrix = m.modelMatrix * m.ViewProjectionMatrix;
            }
            //view = currentScene.cam.GetViewMatrix();

            tempView = currentScene.cam.GetViewMatrix();
            System.Numerics.Matrix4x4.Invert(tempView, out view);

            Render();
        }

        public void Render()
        {

            gl.Clear(WebGLRenderingContextBase.COLOR_BUFFER_BIT | WebGLRenderingContextBase.DEPTH_BUFFER_BIT);

            EnableVertexAttribArrays();

            int indiceat = 0;

            foreach (Mesh m in currentScene.objects)
            {
                gl.UniformMatrix4fv(
                    sm.GetUniform(sm.mShader, "modelview"),
                    false,
                    MathHelper.Mat4ToFloatArray(m.ModelViewProjectionMatrix));

                gl.UniformMatrix4fv(
                    sm.GetUniform(sm.mShader, "view"),
                    false,
                    MathHelper.Mat4ToFloatArray(view));


                gl.UniformMatrix4fv(
                    sm.GetUniform(sm.mShader, "model"),
                    false,
                    MathHelper.Mat4ToFloatArray(m.modelMatrix));


                gl.Uniform3fv(sm.GetUniform(sm.mShader, "lightDir"), currentScene.light.direction);
                gl.Uniform3fv(sm.GetUniform(sm.mShader, "lightColor"), currentScene.light.color);
                gl.Uniform1f(sm.GetUniform(sm.mShader, "lightAmbientIntens"), currentScene.light.ambientIntensity);
                gl.Uniform1f(sm.GetUniform(sm.mShader, "lightDiffuseIntens"), currentScene.light.diffuseIntensity);

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

        public void ChangeViewPortSize(int w, int h)
        {
            canvasWidth = w;
            canvasHeight = h;

            gl.Viewport(0, 0, w, h);
        }

        void OnSceneChanged(Scene newScene)
        {

        }

        public void Dispose()
        {
            Console.WriteLine($"Disposing of {this}...");
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                // Free any other managed objects here.
                //
                gl.Dispose();

            }

            disposed = true;
        }
    }
}