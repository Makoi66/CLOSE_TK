using CLOSE_TK;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.Marshalling;



internal class Game: GameWindow
{
    private int width, height;

    private int homeVAO, homeVBO, homeEBO, homeTextureVBO, homeTextureID;
    private List<Vector3> homeVertices;
    private List<Vector2> homeTexCoords;
    private uint[] homeIndices;

    private int groundVAO, groundVBO, groundEBO, groundTextureID;
    private float[] groundVertices;
    private uint[] groundIndices;

    private int skyboxVAO, skyboxVBO, skyboxTextureID;
    private float[] skyboxVertices;
    private uint[] skyboxIndices;

    private Shader shaderProgram;
    private Shader skyboxShaderProgram;


    private bool cursorGrabbed = true;
    public Vector2 lastPos;

    Camera camera;
    float yRot = 0f;


    private int modelLocation, skyboxSamplerLocation;
    private int viewLocation, skyboxViewLocation;
    private int projectionLocation, skyboxProjectionLocation;



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
        GL.Enable(EnableCap.TextureCubeMapSeamless);

        GL.ClearColor(0.1f, 0.1f, 0.15f, 1.0f);

        PrepareHomeData();
        PrepareGroundData();
        PrepareSkyboxData();

        homeTextureID = LoadTexture("../../../Textures/pineapples.jpg");
        groundTextureID = LoadTexture("../../../Textures/ground.jpg");
        skyboxTextureID = LoadCubemap(new List<string>
        {
            "../../../Textures/Skybox/right.jpg",
            "../../../Textures/Skybox/left.jpg",
            "../../../Textures/Skybox/top.jpg",
            "../../../Textures/Skybox/bottom.jpg",
            "../../../Textures/Skybox/front.jpg",
            "../../../Textures/Skybox/back.jpg"
        });

        SetupHomeBuffers();
        SetupGroundBuffers();
        SetupSkyboxBuffers();

        shaderProgram = new Shader("../../../Shaders/shader.vert",
            "../../../Shaders/shader.frag");
        skyboxShaderProgram = new Shader("../../../Shaders/skybox.vert",
            "../../../Shaders/skybox.frag");

        modelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
        viewLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
        projectionLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");

        skyboxSamplerLocation = GL.GetUniformLocation(skyboxShaderProgram.shaderHandle, "skybox");
        skyboxViewLocation = GL.GetUniformLocation(skyboxShaderProgram.shaderHandle, "view");
        skyboxProjectionLocation = GL.GetUniformLocation(skyboxShaderProgram.shaderHandle, "projection");
        GL.Uniform1(skyboxSamplerLocation, 0);

        GL.Enable(EnableCap.DepthTest);

        camera = new Camera(width, height, new Vector3(-2.0f, 1.0f, -2.0f));
        CursorState = CursorState.Grabbed;
    }

    private void PrepareHomeData()
    {
        homeVertices = new List<Vector3>()
        {   
            //Передняя грань (0-3)
            new Vector3(-0.5f,  -0.5f,   0.5f), //Нижний левый
            new Vector3( 0.5f,  -0.5f,   0.5f), //Нижний правый
            new Vector3( 0.5f,   0.5f,   0.5f), //Верхний правый
            new Vector3(-0.5f,   0.5f,   0.5f), //Верхний левый

            //Правая грань (4-7)
            new Vector3( 0.5f,  -0.5f,   0.5f), //Нижний левый
            new Vector3( 0.5f,  -0.5f,  -0.5f), //Нижний правый
            new Vector3( 0.5f,   0.5f,  -0.5f), //Верхний правый
            new Vector3( 0.5f,   0.5f,   0.5f), //Верхний левый

            //Левая грань (8-11)
            new Vector3(-0.5f,  -0.5f,  -0.5f), //Нижний левый
            new Vector3(-0.5f,  -0.5f,   0.5f), //Нижний правый
            new Vector3(-0.5f,   0.5f,   0.5f), //Верхний правый
            new Vector3(-0.5f,   0.5f,  -0.5f), //Верхний левый

            //Задняя грань (12-15)
            new Vector3( 0.5f,  -0.5f,  -0.5f), //Нижний левый
            new Vector3(-0.5f,  -0.5f,  -0.5f), //Нижний правый
            new Vector3(-0.5f,   0.5f,  -0.5f), //Верхний правый
            new Vector3( 0.5f,   0.5f,  -0.5f), //Верхний левый

            //Верхняя грань (16-19)
            new Vector3(-0.5f,   0.5f,   0.5f), //Передний левый
            new Vector3( 0.5f,   0.5f,   0.5f), //Передний правый
            new Vector3( 0.5f,   0.5f,  -0.5f), //Задний правый
            new Vector3(-0.5f,   0.5f,  -0.5f), //Задний левый
            
            //Нижняя грань (20-23)
            new Vector3(-0.5f,   -0.5f,   0.5f), //Передний левый
            new Vector3( 0.5f,   -0.5f,   0.5f), //Передний правый
            new Vector3( 0.5f,   -0.5f,  -0.5f), //Задний правый
            new Vector3(-0.5f,   -0.5f,  -0.5f), //Задний левый
        };

        homeTexCoords = new List<Vector2>()
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
        };

        homeIndices = new uint[]
{
            //Передняя
            0, 1, 2,
            2, 3, 0,

            //Правая
            4, 5, 6,
            6, 4, 7,

            //Левая
            8, 9, 10,
            10, 8, 11,

            //Задняя
            12, 13, 14,
            14, 12, 15,

            //Верхняя
            16, 17, 18,
            18, 16, 19,

            //Нижняя
            20, 21, 22,
            22, 20, 23
};
    }

    private void PrepareGroundData()
    {
        float groundSize = 50.0f; // Сделаем землю поменьше для начала
        float textureRepeat = 25.0f; // Повторение текстуры

        groundVertices = new float[]{
            groundSize,  0.0f,  groundSize, textureRepeat, 0.0f,
            -groundSize, 0.0f,  groundSize, 0.0f, 0.0f,
            -groundSize, 0.0f, -groundSize, 0.0f, textureRepeat,
            groundSize,  0.0f, -groundSize, textureRepeat, textureRepeat
        };

        groundIndices = new uint[]
        {
            0, 1, 2,
            0, 2, 3
        };
    }

    private void PrepareSkyboxData()
    {
        skyboxVertices = new float[]
        {
            1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
            // Left face (-X)
            -1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,
            // Top face (+Y)
            -1.0f,  1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,
            // Bottom face (-Y)
            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
            // Front face (+Z)
            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,
            // Back face (-Z)
            -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
        };
    }

    private int LoadTexture(string path)
    {
        int textureHandle = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);

        //Texture Parameters
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        StbImage.stbi_set_flip_vertically_on_load(1); //переворот текстуры
        ImageResult image = ImageResult.FromStream(File.OpenRead(
            path), ColorComponents.RedGreenBlueAlpha);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
            image.Width, image.Height, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, image.Data);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        GL.BindTexture(TextureTarget.Texture2D, 0);

        return textureHandle;
    }

    private void SetupHomeBuffers()
    {
        homeVAO = GL.GenVertexArray();
        homeVBO = GL.GenBuffer();
        homeTextureVBO = GL.GenBuffer();
        homeEBO = GL.GenBuffer();

        GL.BindVertexArray(homeVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, homeVBO);
        GL.BufferData(BufferTarget.ArrayBuffer,
            homeVertices.Count * Vector3.SizeInBytes,
            homeVertices.ToArray(), BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);

        GL.EnableVertexAttribArray(0);

        //Create Bind_Texture
        GL.BindBuffer(BufferTarget.ArrayBuffer, homeTextureVBO);
        GL.BufferData(BufferTarget.ArrayBuffer,
            homeTexCoords.Count * Vector2.SizeInBytes,
            homeTexCoords.ToArray(), BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0);
        GL.EnableVertexAttribArray(1);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, homeEBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer,
            homeIndices.Length * sizeof(uint),
            homeIndices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    private void SetupGroundBuffers()
    {
        groundVAO = GL.GenVertexArray();
        groundVBO = GL.GenBuffer();
        groundEBO = GL.GenBuffer();

        GL.BindVertexArray(groundVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, groundVBO);
        GL.BufferData(BufferTarget.ArrayBuffer,
            groundVertices.Length * sizeof(float),
            groundVertices, BufferUsageHint.StaticDraw);

        int stride = 5 * sizeof(float);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, groundEBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer,
            groundIndices.Length * sizeof(uint),
            groundIndices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(0);
    }

    private void SetupSkyboxBuffers()
    {
        skyboxVAO = GL.GenVertexArray();
        skyboxVBO = GL.GenBuffer();

        GL.BindVertexArray(skyboxVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, skyboxVBO);
        GL.BufferData(BufferTarget.ArrayBuffer,
            skyboxVertices.Length * sizeof(float), skyboxVertices,
            BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false,
            3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
    }

    private int LoadCubemap(List<string> facesPaths)
    {
        int textureID = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureCubeMap, textureID);

        StbImage.stbi_set_flip_vertically_on_load(0);

        for (int i = 0; i < facesPaths.Count; i++)
        {
            ImageResult image = ImageResult.FromStream(
                File.OpenRead(facesPaths[i]),
                ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, 
                PixelInternalFormat.Rgba, image.Width, image.Height, 0, 
                PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
        }

        StbImage.stbi_set_flip_vertically_on_load(1);

        GL.TexParameter(TextureTarget.TextureCubeMap, 
            TextureParameterName.TextureMinFilter, 
            (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap,
            TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap,
            TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap,
            TextureParameterName.TextureWrapR,
            (int)TextureWrapMode.ClampToEdge);

        GL.BindTexture(TextureTarget.TextureCubeMap, 0);

        return textureID;
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        GL.DeleteVertexArray(homeVAO);
        GL.DeleteBuffer(homeVBO);
        GL.DeleteBuffer(homeEBO);
        GL.DeleteBuffer(homeTextureVBO);
        GL.DeleteTexture(homeTextureID);

        GL.DeleteVertexArray(groundVAO);
        GL.DeleteBuffer(groundVBO);
        GL.DeleteBuffer(groundEBO);
        GL.DeleteTexture(groundTextureID);

        GL.DeleteVertexArray(skyboxVAO);
        GL.DeleteBuffer(skyboxVBO);
        GL.DeleteTexture(skyboxTextureID);

        shaderProgram.DeleteShader();
        skyboxShaderProgram.DeleteShader();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetProjectionMatrix();

        shaderProgram.UseShader();

        GL.UniformMatrix4(viewLocation, false, ref view);
        GL.UniformMatrix4(projectionLocation, false, ref projection);

        GL.BindVertexArray(groundVAO);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, groundTextureID);

        Matrix4 groundModel = Matrix4.Identity;
        GL.UniformMatrix4(modelLocation, false, ref groundModel);

        GL.DrawElements(PrimitiveType.Triangles,
            groundIndices.Length,
            DrawElementsType.UnsignedInt, 0);

        GL.BindVertexArray(homeVAO);

        GL.BindTexture(TextureTarget.Texture2D, homeTextureID);

        yRot += (float)args.Time * 0.5f;
        Matrix4 homeModel = Matrix4.CreateRotationY(yRot);
        Matrix4 homeTranslation = Matrix4.CreateTranslation(0f, 0.50001f, 0f);
        homeModel *= homeTranslation;

        GL.UniformMatrix4(modelLocation, false, ref homeModel);

        GL.DrawElements(PrimitiveType.Triangles, homeIndices.Length,
            DrawElementsType.UnsignedInt, 0);

        GL.BindVertexArray(0);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        //Skybox
        GL.DepthFunc(DepthFunction.Lequal);
        skyboxShaderProgram.UseShader();

        Matrix4 skyboxView = new Matrix4(new Matrix3(view));
        GL.UniformMatrix4(skyboxViewLocation, false, ref skyboxView);
        GL.UniformMatrix4(skyboxProjectionLocation, false, ref projection);

        GL.BindVertexArray(skyboxVAO);
        GL.ActiveTexture(TextureUnit.Texture0);

        GL.BindTexture(TextureTarget.TextureCubeMap, skyboxTextureID);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

        GL.BindVertexArray(0);
        GL.DepthFunc(DepthFunction.Less);

        Context.SwapBuffers();

    }


    private void OnMouseButtonDown(MouseState mouse, FrameEventArgs e)
    {
        if (this.IsFocused && mouse.IsButtonDown(MouseButton.Left) && !cursorGrabbed)
        {
            this.MousePosition = new Vector2(lastPos.X, lastPos.Y);
            this.CursorState = CursorState.Grabbed;
            cursorGrabbed = true;
        }
    }

    private void OnFullScreenMode(KeyboardState input, MouseState mouse, FrameEventArgs args)
    {
        if (input.IsKeyDown(Keys.LeftAlt) && input.IsKeyDown(Keys.Enter))
        {
            if (this.WindowState != WindowState.Fullscreen)
            {
                this.WindowState = WindowState.Fullscreen;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }
        if (input.IsKeyDown(Keys.LeftControl) && input.IsKeyDown(Keys.LeftAlt) &&
            input.IsKeyDown(Keys.LeftShift) && cursorGrabbed)
        {
            lastPos = new Vector2(mouse.X, mouse.Y);
            this.CursorState = CursorState.Normal;
            cursorGrabbed = false;
        }
    }


    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        MouseState mouse = MouseState;
        KeyboardState input = KeyboardState;

        OnMouseButtonDown(mouse, args);
        OnFullScreenMode(input, mouse, args);

        base.OnUpdateFrame(args);
        if (cursorGrabbed)
        {
            camera.Update(input, mouse, args);
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        //if (camera != null)
        //{
        //    camera.UpdateScreenSize(e.Width, e.Height);
        //}
        this.width = e.Width;
        this.height = e.Height;
    }
};



public class Shader
{
    public int shaderHandle;

    public Shader(string vertPath, string fragPath)
    {
        shaderHandle = GL.CreateProgram();
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, LoadShaderSource(vertPath));
        GL.CompileShader(vertexShader);
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success1);
        if (success1 == 0)
        {
            string infoLog = GL.GetShaderInfoLog(vertexShader);
            Console.WriteLine(infoLog);
        }

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, LoadShaderSource(fragPath));
        GL.CompileShader(fragmentShader);
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

        GL.DetachShader(shaderHandle, vertexShader);
        GL.DetachShader(shaderHandle, fragmentShader);
        GL.DeleteShader(fragmentShader);
        GL.DeleteShader(vertexShader);
    }


    public static string LoadShaderSource(string filepath)
    {
        string shaderSource = "";
        try
        {
            using (StreamReader reader = new StreamReader(filepath))
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