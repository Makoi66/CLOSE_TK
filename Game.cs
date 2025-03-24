using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;


internal class Game: GameWindow
{
    int width, height;
    public Game(int width, int height) : base
    (GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        this.CenterWindow(new Vector2i(width, height));
        this.height = height;
        this.width = width;
    }

    float[] vertices =
    {
        -0.5f,  0.5f,   0f,
        0.5f,   0.5f,   0f,
        0.5f,   -0.5f,  0f,
        -0.5f,  -0.5f,  0f
    };
        uint[] indices =
        {
        0, 1, 2,
        2, 3, 0
    };
    int EBO;
    int VAO;
    int VBO;
    Shader shaderProgram;

    protected override void OnLoad()
    {
        EBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length *
            sizeof(uint), indices, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

        VAO = GL.GenVertexArray();
        VBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float),
            vertices, BufferUsageHint.StaticDraw);
        GL.BindVertexArray(VAO);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
        GL.EnableVertexArrayAttrib(VAO, 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        shaderProgram = new Shader();
        shaderProgram.LoadShaders();
    }

    protected override void OnUnload()
    {
        GL.DeleteBuffer(VAO);
        GL.DeleteBuffer(VBO);
        GL.DeleteBuffer(EBO);

        shaderProgram.DeleteShader();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.ClearColor(0.0f, 0.99f, 0.66f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        shaderProgram.UseShader();
        //GL.BindVertexArray(VAO);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length,
            DrawElementsType.UnsignedInt, 0);

        Context.SwapBuffers();

        base.OnRenderFrame(args);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
        base.OnUpdateFrame(args);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        this.width = e.Width;
        this.height = e.Height;
    }

    //public static string LoadShaderSource(string filepath)
    //{
    //    string shaderSource = "";
    //    try
    //    {
    //        using (StreamReader reader = new StreamReader("../../../Shaders/" + filepath))
    //        {
    //            shaderSource = reader.ReadToEnd();
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        Console.WriteLine("Failed to load shader source file:" + e.Message);
    //    }
    //    return shaderSource;
    //}
};



public class Shader
{
    int shaderHandle;

    public void LoadShaders()
    {
        shaderHandle = GL.CreateProgram();
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, LoadShaderSource("shader.vert"));
        GL.CompileShader(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, LoadShaderSource("shader.frag"));
        GL.CompileShader(fragmentShader);

        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success1);
        if (success1 == 0)
        {
            string infoLog = GL.GetShaderInfoLog(vertexShader);
            Console.WriteLine(infoLog);
        }

        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int success2);
        if (success2 == 0)
        {
            string infoLog = GL.GetShaderInfoLog(fragmentShader);
            Console.WriteLine(infoLog);
        }

        GL.AttachShader(shaderHandle, vertexShader);
        GL.AttachShader(shaderHandle, fragmentShader);

        GL.LinkProgram(shaderHandle);

        //GL.DeleteProgram(shaderProgram);

    }

    public static string LoadShaderSource(string filepath)
    {
        string shaderSource = "";
        try
        {
            using (StreamReader reader = new StreamReader("../../../Shaders/" + filepath))
            {
                shaderSource = reader.ReadToEnd();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to load shader source file:" + e.Message);
        }
        return shaderSource;
    }

    public void UseShader()
    {
        GL.UseProgram(shaderHandle);
    }

    public void DeleteShader()
    {
        GL.DeleteProgram(shaderHandle);
    }
}