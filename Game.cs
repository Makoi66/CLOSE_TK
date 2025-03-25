using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using System.Runtime.InteropServices.Marshalling;


internal class Game: GameWindow
{
    private int width, height;
    private int VAO, VBO, EBO, textureVBO, textureID;
    private Shader shaderProgram;
    private uint[] indices;
    private float[] vertices;
    private float[] texCoords;

    public Game(int width, int height) : base
    (GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        this.CenterWindow(new Vector2i(width, height));
        this.height = height;
        this.width = width;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        vertices = new float[]
        {
            -0.5f,  0.5f,   0f,
            0.5f,   0.5f,   0f,
            0.5f,   -0.5f,  0f,
            -0.5f,  -0.5f,  0f
        };

        indices = new uint[]
        {
            0, 1, 2,
            2, 3, 0
        };

        texCoords = new float[]
        {
            0f, 1f,
            1f, 1f,
            1f, 0f,
            0f, 0f
        };

        //Texture Loading
        textureID = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, textureID);

        //Texture Parameters
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        //Load Image
        StbImage.stbi_set_flip_vertically_on_load(1);
        ImageResult boxTexture = ImageResult.FromStream(File.OpenRead(
            "../../../Textures/pineapples.jpg"), ColorComponents.RedGreenBlueAlpha);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
            boxTexture.Width, boxTexture.Height, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, boxTexture.Data);

        VAO = GL.GenVertexArray();
        VBO = GL.GenBuffer();
        EBO = GL.GenBuffer();
        textureVBO = GL.GenBuffer();

        GL.BindVertexArray(VAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float),
            vertices, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
        GL.EnableVertexArrayAttrib(VAO, 0);

        //Create Bind_Texture
        GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Length *
            sizeof(float), texCoords, BufferUsageHint.StaticDraw);
        
        //Point a slot number 1
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
        
        //Enable the slot
        GL.EnableVertexArrayAttrib(VAO, 1);


        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length *
            sizeof(uint), indices, BufferUsageHint.StaticDraw);
        

        GL.BindVertexArray(0);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        shaderProgram = new Shader();
        shaderProgram.LoadShaders();
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        GL.DeleteVertexArray(VAO);
        GL.DeleteBuffer(VBO);
        GL.DeleteBuffer(EBO);
        GL.DeleteBuffer(textureVBO);
        GL.DeleteTexture(textureID);

        shaderProgram.DeleteShader();
    }

    float yRot = 0f;

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.ClearColor(0.0f, 0.99f, 0.66f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        shaderProgram.UseShader();
        GL.BindTexture(TextureTarget.Texture2D, textureID);

        GL.BindVertexArray(VAO);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length,
            DrawElementsType.UnsignedInt, 0);

        Context.SwapBuffers();

        //Tranformation
        //Matrix4 model = Matrix4.Identity; 
        Matrix4 model = Matrix4.CreateRotationY(yRot % 90);
        Matrix4 translation = Matrix4.CreateTranslation(0f, 0f, -1f);
        model *= translation;
        Matrix4 view = Matrix4.Identity;
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(60.0f), width / height, 0.1f, 100.0f);

        //model = Matrix4.CreateTranslation(0f, 0f, -1f);

        int modelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
        int viewLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
        int projectionLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");


        GL.UniformMatrix4(modelLocation, true, ref model);
        GL.UniformMatrix4(viewLocation, true, ref view);
        GL.UniformMatrix4(projectionLocation, true, ref projection);

        
        yRot = yRot + 0.0005f;
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
};



public class Shader
{
    public int shaderHandle;

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
        GL.GetProgram(shaderHandle, GetProgramParameterName.LinkStatus, out int success3);
        if (success3 == 0)
        {
            string infoLog = GL.GetProgramInfoLog(shaderHandle);
            Console.WriteLine(infoLog);
        }

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