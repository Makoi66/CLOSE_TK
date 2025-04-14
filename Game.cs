using CLOSE_TK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;



internal class Game: GameWindow
{
    private int width, height;

    private bool cursorGrabbed = true;
    public Vector2 lastPos;

    Camera camera;

    private int homeVAO, homeVBO, homeEBO, homeTextureVBO;
    private int homeNormalsVBO;
    private List<Vector3> homeVertices;
    private List<Vector2> homeTexCoords;
    private uint[] homeIndices;

    private int roofVAO, roofVBO, roofNormalsVBO, roofTexCoordsVBO, roofEBO;
    private List<Vector3> roofVertices;
    private List<Vector3> roofNormals;
    private List<Vector2> roofTexCoords;
    private List<uint> roofIndices;
    private int roofIndexCount;

    private int groundVAO, groundVBO, groundEBO, groundTextureID;
    private float[] groundVertices;
    private uint[] groundIndices;

    private int skyboxVAO, skyboxVBO, skyboxTextureID;
    private float[] skyboxVertices;

    private int sphereVAO, sphereVBO, sphereEBO;
    private List<Vector3> sphereVertices;
    private List<uint> sphereIndices;
    private int sphereIndexCount;


    private Shader shaderProgram;
    private Shader skyboxShaderProgram;

    private Shader unlitShader;
    private int unlitModelLoc, unlitViewLoc, unlitProjLoc, unlitColorLoc;


    private int modelLocation, skyboxSamplerLocation;
    private int viewLocation, skyboxViewLocation;
    private int projectionLocation, skyboxProjectionLocation;

    // Параметры цикла и освещения
    private float timeOfDay = 0.0f; // 0.0 = восход/полдень, PI = закат/полночь
    private float cycleSpeed = 0.5f; // Скорость смены дня/ночи (радианы в секунду)
    private float orbitRadius = 50.0f; // Насколько далеко солнце/луна
    private Vector3 sunPos, moonPos;   // Текущие позиции
    private Vector3 currentLightDir;   // Направление НА источник света
    private Vector3 currentLightColor; // Цвет источника
    private Vector3 currentAmbientColor; // Цвет фонового освещения
    private int skyboxBrightnessFactorLoc;
    private float sunAltitudeFactor;


    //Цвета для дня/ночи
    private readonly Vector3 sunColorDay = new Vector3(1.0f, 1.0f, 0.85f);
    private readonly Vector3 sunColorSunrise = new Vector3(1.0f, 0.55f, 0.35f);
    private readonly Vector3 sunColorSunset = new Vector3(1.0f, 0.75f, 0.35f);
    private readonly Vector3 moonColorNight = new Vector3(0.6f, 0.6f, 0.8f);

    private readonly Vector3 ambientDay = new Vector3(0.3f, 0.3f, 0.35f);
    private readonly Vector3 ambientSunrise = new Vector3(0.25f, 0.15f, 0.15f);
    private readonly Vector3 ambientSunset = new Vector3(0.3f, 0.15f, 0.18f);
    private readonly Vector3 ambientNight = new Vector3(0.08f, 0.08f, 0.15f);

    private readonly Vector3 skyColorDay = new Vector3(0.5f, 0.7f, 1.0f);
    private readonly Vector3 skyColorSunrise = new Vector3(0.9f, 0.55f, 0.45f);
    private readonly Vector3 skyColorSunset = new Vector3(0.95f, 0.45f, 0.5f);
    private readonly Vector3 skyColorNight = new Vector3(0.01f, 0.01f, 0.05f);

    private const float HorizonTransitionThreshold = 0.35f;
    private const float DayLightBoost = 1.5f;
    private const float MoonLightIntensity = 0.5f;

    //Локации для uniform'ов освещения в основном шейдере
    private int lightDirLoc, lightColorLoc, ambientColorLoc, viewPosLoc;

    private List<Vector3> homeNormals;


    private Vector3 activeLightPos;
    //Тени
    private int depthMapFBO;
    private int depthMapTexture;
    private Shader shadowShader;
    private Matrix4 lightSpaceMatrix;
    private int shadowMapWidth = 2048, shadowMapHeight = 2048;

    //Новые локации uniform'ов в основном шейдере
    private int lightSpaceMatrixLoc;
    private int shadowMapSamplerLoc;
    //Локация для шейдера теней
    private int shadowLightSpaceMatrixLoc;
    private int shadowModelLoc;


    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    private Season currentSeason = Season.Summer;
    private Dictionary<Season, int> groundTextureIDs;
    private Dictionary<Season, int[]> homeFaceTextureIDsDict;
    int curr_sesson = 0;
    int day_or_night = 0;

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
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.TextureCubeMapSeamless);

        groundTextureIDs = new Dictionary<Season, int>();
        homeFaceTextureIDsDict = new Dictionary<Season, int[]>();

        var sphereData = GeometryFactory.CreateSphereVertices(1.0f, 36, 18); // Радиус 1, детализация средняя
        sphereVertices = sphereData.vertices;
        sphereIndices = sphereData.indices;
        sphereIndexCount = sphereIndices.Count;
        SetupSphereBuffers();

        PrepareHomeData(); SetupHomeBuffers();

        PrepareRoofData(); SetupRoofBuffers();

        PrepareGroundData(); SetupGroundBuffers();

        PrepareSkyboxData(); SetupSkyboxBuffers();

        string groundSpringPath = "../../../Textures/Ground/ground_spring.jpg";
        string groundSummerPath = "../../../Textures/Ground/ground_summer.jpg"; // Твой старый ground.jpg?
        string groundAutumnPath = "../../../Textures/Ground/ground_autumn.jpg";
        string groundWinterPath = "../../../Textures/Ground/ground_winter.jpg";

        List<string> homeSpringPaths = new List<string> {
            "../../../Textures/Home/Spring/wall_right.jpg",
            "../../../Textures/Home/Spring/wall_back.jpg",
            "../../../Textures/Home/Spring/wall_left.jpg",
            "../../../Textures/Home/Spring/wall_front.jpg",
            "../../../Textures/Home/Spring/wall_top.jpg",
            "../../../Textures/Home/Spring/wall_bottom.jpg",
            "../../../Textures/Home/Spring/roof_left.jpg",
            "../../../Textures/Home/Spring/roof_right.jpg",
            "../../../Textures/Home/Spring/roof_front.jpg",
            "../../../Textures/Home/Spring/roof_back.jpg"
        };
        List<string> homeSummerPaths = new List<string> {
            "../../../Textures/Home/Summer/wall_right.jpg",
            "../../../Textures/Home/Summer/wall_left.jpg",
            "../../../Textures/Home/Summer/wall_back.jpg",
            "../../../Textures/Home/Summer/wall_front.jpg",
            "../../../Textures/Home/Summer/wall_top.jpg",
            "../../../Textures/Home/Summer/wall_bottom.jpg",
            "../../../Textures/Home/Summer/roof_left.jpg",
            "../../../Textures/Home/Summer/roof_right.jpg",
            "../../../Textures/Home/Summer/roof_front.jpg",
            "../../../Textures/Home/Summer/roof_back.jpg"
        };
        List<string> homeAutumnPaths = new List<string> {
            "../../../Textures/Home/Autumn/wall_front.jpg",
            "../../../Textures/Home/Autumn/wall_back.jpg",
            "../../../Textures/Home/Autumn/wall_left.jpg",
            "../../../Textures/Home/Autumn/wall_right.jpg",
            "../../../Textures/Home/Autumn/wall_top.jpg",
            "../../../Textures/Home/Autumn/wall_bottom.jpg",
            "../../../Textures/Home/Autumn/roof_left.jpg",
            "../../../Textures/Home/Autumn/roof_right.jpg",
            "../../../Textures/Home/Autumn/roof_front.jpg",
            "../../../Textures/Home/Autumn/roof_back.jpg"
        };
        List<string> homeWinterPaths = new List<string> {
            "../../../Textures/Home/Winter/wall_front.jpg",
            "../../../Textures/Home/Winter/wall_back.jpg",
            "../../../Textures/Home/Winter/wall_left.jpg",
            "../../../Textures/Home/Winter/wall_right.jpg",
            "../../../Textures/Home/Winter/wall_top.jpg",
            "../../../Textures/Home/Winter/wall_bottom.jpg",
            "../../../Textures/Home/Winter/roof_left.jpg",
            "../../../Textures/Home/Winter/roof_right.jpg",
            "../../../Textures/Home/Winter/roof_front.jpg",
            "../../../Textures/Home/Winter/roof_back.jpg"
        };

        Console.WriteLine("\n--- Loading Spring Textures ---");
        groundTextureIDs[Season.Spring] = LoadTexture(groundSpringPath); // Переименовали LoadTexture
        homeFaceTextureIDsDict[Season.Spring] = LoadHomeTextures(homeSpringPaths);

        Console.WriteLine("\n--- Loading Summer Textures ---");
        groundTextureIDs[Season.Summer] = LoadTexture(groundSummerPath);
        homeFaceTextureIDsDict[Season.Summer] = LoadHomeTextures(homeSummerPaths);

        Console.WriteLine("\n--- Loading Autumn Textures ---");
        groundTextureIDs[Season.Autumn] = LoadTexture(groundAutumnPath);
        homeFaceTextureIDsDict[Season.Autumn] = LoadHomeTextures(homeAutumnPaths);

        Console.WriteLine("\n--- Loading Winter Textures ---");
        groundTextureIDs[Season.Winter] = LoadTexture(groundWinterPath);
        homeFaceTextureIDsDict[Season.Winter] = LoadHomeTextures(homeWinterPaths);

        groundTextureID = LoadTexture("../../../Textures/ground.jpg");
        skyboxTextureID = LoadCubemap(new List<string>
        {
            "../../../Textures/Skybox/right.bmp",
            "../../../Textures/Skybox/left.bmp",
            "../../../Textures/Skybox/top.bmp",
            "../../../Textures/Skybox/bottom.bmp",
            "../../../Textures/Skybox/front.bmp",
            "../../../Textures/Skybox/back.bmp"
        });


        shaderProgram = new Shader("../../../Shaders/shader.vert",
            "../../../Shaders/shader.frag");
        skyboxShaderProgram = new Shader("../../../Shaders/skybox.vert",
            "../../../Shaders/skybox.frag");
        unlitShader = new Shader("../../../Shaders/unlit.vert",
            "../../../Shaders/unlit.frag");
        shadowShader = new Shader("../../../Shaders/shadow.vert",
            "../../../Shaders/shadow.frag");

        // Получаем локации для shadowShader
        shadowShader.UseShader();
        shadowLightSpaceMatrixLoc = GL.GetUniformLocation(
            shadowShader.shaderHandle, "lightSpaceMatrix");
        shadowModelLoc = GL.GetUniformLocation(shadowShader.shaderHandle, "model");

        // Создаем Framebuffer Object (FBO)
        depthMapFBO = GL.GenFramebuffer();

        // Создаем текстуру для карты глубины
        depthMapTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, depthMapTexture);

        // Создаем пустое изображение нужного размера с форматом глубины
        GL.TexImage2D(TextureTarget.Texture2D, 0,
            PixelInternalFormat.DepthComponent, shadowMapWidth,
            shadowMapHeight, 0, PixelFormat.DepthComponent, 
            PixelType.Float, IntPtr.Zero);

        // Устанавливаем параметры фильтрации
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);

        // Устанавливаем параметры обертывания
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

        // Устанавливаем цвет границы (белый = нет тени за пределами)
        float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };
        GL.TexParameter(TextureTarget.Texture2D,
            TextureParameterName.TextureBorderColor, borderColor);

        // Прикрепляем текстуру глубины к FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D,
            depthMapTexture, 0);

        // Говорим OpenGL, что мы не будем рендерить в цветовой буфер для этого FBO
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        // Отвязываем FBO, чтобы вернуться к рендеру в окно по умолчанию
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // Получаем локации для теней в основном шейдере
        shaderProgram.UseShader();
        lightSpaceMatrixLoc = GL.GetUniformLocation(
            shaderProgram.shaderHandle, "lightSpaceMatrix");
        shadowMapSamplerLoc = GL.GetUniformLocation(
            shaderProgram.shaderHandle, "shadowMap");
        
        // Указываем, что карта теней будет в текстурном юните 1 (0 уже занят основной текстурой)
        GL.Uniform1(shadowMapSamplerLoc, 1);


        modelLocation = GL.GetUniformLocation(
            shaderProgram.shaderHandle, "model");
        viewLocation = GL.GetUniformLocation(
            shaderProgram.shaderHandle, "view");
        projectionLocation = GL.GetUniformLocation(
            shaderProgram.shaderHandle, "projection");

        GL.Uniform1(GL.GetUniformLocation(
            shaderProgram.shaderHandle, "texture0"), 0);

        lightDirLoc = GL.GetUniformLocation(
            shaderProgram.shaderHandle, "lightDir");
        lightColorLoc = GL.GetUniformLocation(
            shaderProgram.shaderHandle, "lightColor");
        ambientColorLoc = GL.GetUniformLocation(
            shaderProgram.shaderHandle, "ambientColor");
        viewPosLoc = GL.GetUniformLocation(
            shaderProgram.shaderHandle, "viewPos");

        skyboxSamplerLocation = GL.GetUniformLocation(
            skyboxShaderProgram.shaderHandle, "skybox");
        skyboxViewLocation = GL.GetUniformLocation(
            skyboxShaderProgram.shaderHandle, "view");
        skyboxProjectionLocation = GL.GetUniformLocation(
            skyboxShaderProgram.shaderHandle, "projection");
        skyboxBrightnessFactorLoc = GL.GetUniformLocation(
            skyboxShaderProgram.shaderHandle, "brightnessFactor");

        GL.Uniform1(skyboxSamplerLocation, 0);

        unlitModelLoc = GL.GetUniformLocation(
            unlitShader.shaderHandle, "model");
        unlitViewLoc = GL.GetUniformLocation(
            unlitShader.shaderHandle, "view");
        unlitProjLoc = GL.GetUniformLocation(
            unlitShader.shaderHandle, "projection");
        unlitColorLoc = GL.GetUniformLocation(
            unlitShader.shaderHandle, "objectColor");

        camera = new Camera(width, height, new Vector3(-2.0f, 1.0f, -2.0f));
        CursorState = CursorState.Grabbed;
    }


    private void SetupSphereBuffers()
    {
        sphereVAO = GL.GenVertexArray();
        sphereVBO = GL.GenBuffer();
        sphereEBO = GL.GenBuffer();

        GL.BindVertexArray(sphereVAO);

        GL.BindBuffer(BufferTarget.ArrayBuffer, sphereVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, sphereVertices.Count
            * Vector3.SizeInBytes, sphereVertices.ToArray(),
            BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, sphereEBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, sphereIndices.Count
            * sizeof(uint), sphereIndices.ToArray(),
            BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float,
            false, Vector3.SizeInBytes, 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
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

        homeNormals = new List<Vector3>()
        {
            // Передняя Z+ (Индексы 0-5, Вершины 0-3)
            Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ,
            // Правая X+ (Индексы 6-11, Вершины 4-7)
            Vector3.UnitX, Vector3.UnitX, Vector3.UnitX, Vector3.UnitX,
            // Левая X- (Индексы 12-17, Вершины 8-11)
           -Vector3.UnitX,-Vector3.UnitX,-Vector3.UnitX,-Vector3.UnitX,
            // Задняя Z- (Индексы 18-23, Вершины 12-15)
           -Vector3.UnitZ,-Vector3.UnitZ,-Vector3.UnitZ,-Vector3.UnitZ,
            // Верхняя Y+ (Индексы 24-29, Вершины 16-19)
            Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY,
            // Нижняя Y- (Индексы 30-35, Вершины 20-23)
           -Vector3.UnitY,-Vector3.UnitY,-Vector3.UnitY,-Vector3.UnitY,
        };
    }

    private void AddRoofQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 normal, float layer, ref uint vertexOffset)
    {
        roofVertices.Add(v1);
        roofVertices.Add(v2);
        roofVertices.Add(v3);
        roofVertices.Add(v4);

        roofNormals.Add(normal);
        roofNormals.Add(normal);
        roofNormals.Add(normal);
        roofNormals.Add(normal);

        roofTexCoords.Add(new Vector2(0, 0));
        roofTexCoords.Add(new Vector2(1, 0));
        roofTexCoords.Add(new Vector2(1, 1));
        roofTexCoords.Add(new Vector2(0, 1));

        roofIndices.Add(vertexOffset + 0);
        roofIndices.Add(vertexOffset + 1);
        roofIndices.Add(vertexOffset + 2);
        roofIndices.Add(vertexOffset + 2);
        roofIndices.Add(vertexOffset + 3);
        roofIndices.Add(vertexOffset + 0);
        vertexOffset += 4;
    }

    private void AddRoofTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv1, Vector2 uv2, Vector2 uv3, float layer, ref uint vertexOffset)
    {
        Vector3 edge1 = v2 - v1;
        Vector3 edge2 = v3 - v1;
        Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
        if (Math.Abs(v1.X - v2.X) < 0.01f && normal.X > 0 && v1.X < 0) normal *= -1;
        if (Math.Abs(v1.X - v2.X) < 0.01f && normal.X < 0 && v1.X > 0) normal *= -1;


        roofVertices.Add(v1); roofVertices.Add(v2); roofVertices.Add(v3);
        roofNormals.Add(normal); roofNormals.Add(normal); roofNormals.Add(normal);
        roofTexCoords.Add(uv1); roofTexCoords.Add(uv2); roofTexCoords.Add(uv3);

        roofIndices.Add(vertexOffset + 0); roofIndices.Add(vertexOffset + 1); 
        roofIndices.Add(vertexOffset + 2);
        vertexOffset += 3;
    }

    private void PrepareRoofData()
    {
        roofVertices = new List<Vector3>();
        roofNormals = new List<Vector3>();
        roofTexCoords = new List<Vector2>();
        roofIndices = new List<uint>();

        float roofHeight = 0.5f;     // высота свода над основанием крыши
        float overhang = 0.15f;     // насколько крыша выступает за стены
        float baseY = 0.5f;         // Y-координата основания крыши (верх куба)
        float halfSize = 0.5f;      // половина размера куба

        // вершины свода крыши
        Vector3 ridgeLeft = new Vector3(-(halfSize + overhang), baseY + roofHeight, 0.0f);
        Vector3 ridgeRight = new Vector3((halfSize + overhang), baseY + roofHeight, 0.0f);

        // вершины основания крыши
        Vector3 baseFrontLeft = new Vector3(-(halfSize + overhang), baseY,
            (halfSize + overhang));
        Vector3 baseFrontRight = new Vector3((halfSize + overhang), baseY,
            (halfSize + overhang));
        Vector3 baseBackLeft = new Vector3(-(halfSize + overhang), baseY,
            -(halfSize + overhang));
        Vector3 baseBackRight = new Vector3((halfSize + overhang), baseY,
            -(halfSize + overhang));

        // добавляем 4 грани крыши
        uint vertexOffset = 0;

        // грань 1: Передний скат (+Z направление нормали примерно)
        Vector3 normalFront = Vector3.Normalize(new Vector3(0, halfSize + overhang, roofHeight)); // Нормаль ската
        AddRoofQuad(baseFrontLeft, baseFrontRight, ridgeRight, ridgeLeft,
            normalFront, 6.0f, ref vertexOffset); // Текстура 6

        // грань 2: Задний скат (-Z направление нормали примерно)
        Vector3 normalBack = Vector3.Normalize(new Vector3(0, halfSize + overhang, -roofHeight));
        AddRoofQuad(baseBackRight, baseBackLeft, ridgeLeft, ridgeRight,
            normalBack, 7.0f, ref vertexOffset); // Текстура 7

        // добавляем 2 торцевые грани крыши (треугольники)

        // торец 3: Левый (-X направление)
        AddRoofTriangle(baseBackLeft, baseFrontLeft, ridgeLeft, new Vector2(0, 0),
            new Vector2(1, 0), new Vector2(0.5f, 1), 8.0f, ref vertexOffset); // Текстура 8

        // торец 4: Правый (+X направление)
        AddRoofTriangle(baseFrontRight, baseBackRight, ridgeRight, new Vector2(0, 0),
            new Vector2(1, 0), new Vector2(0.5f, 1), 9.0f, ref vertexOffset); // Текстура 9

        roofIndexCount = roofIndices.Count; // обновляем количество индексов (4 * 6 = 24)
    }

    private void SetupRoofBuffers()
    {
        roofVAO = GL.GenVertexArray();
        roofVBO = GL.GenBuffer();
        roofNormalsVBO = GL.GenBuffer();
        roofTexCoordsVBO = GL.GenBuffer();
        roofEBO = GL.GenBuffer();

        GL.BindVertexArray(roofVAO);

        // VBO Вершины (location 0)
        GL.BindBuffer(BufferTarget.ArrayBuffer, roofVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, roofVertices.Count
            * Vector3.SizeInBytes, roofVertices.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float,
            false, Vector3.SizeInBytes, 0);
        GL.EnableVertexAttribArray(0);

        // VBO Текстурные координаты (location 1)
        GL.BindBuffer(BufferTarget.ArrayBuffer, roofTexCoordsVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, roofTexCoords.Count
            * Vector2.SizeInBytes, roofTexCoords.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float,
            false, Vector2.SizeInBytes, 0);
        GL.EnableVertexAttribArray(1);

        // VBO Нормали (location 2)
        GL.BindBuffer(BufferTarget.ArrayBuffer, roofNormalsVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, roofNormals.Count
            * Vector3.SizeInBytes, roofNormals.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float,
            false, Vector3.SizeInBytes, 0);
        GL.EnableVertexAttribArray(2);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, roofEBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, roofIndices.Count
            * sizeof(uint), roofIndices.ToArray(), BufferUsageHint.StaticDraw);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    private void PrepareGroundData()
    {
        float groundSize = 50.0f;
        float textureRepeat = 25.0f;

        groundVertices = new float[]{
            groundSize,  0.0f,  groundSize, 0.0f, 1.0f, 0.0f, textureRepeat, 0.0f,
            -groundSize, 0.0f,  groundSize, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f,
            -groundSize, 0.0f, -groundSize, 0.0f, 1.0f, 0.0f, 0.0f, textureRepeat,
            groundSize,  0.0f, -groundSize, 0.0f, 1.0f, 0.0f, textureRepeat, textureRepeat
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

            -1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

            -1.0f,  1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

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

        StbImage.stbi_set_flip_vertically_on_load(1);
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
        homeNormalsVBO = GL.GenBuffer();
        homeEBO = GL.GenBuffer();

        GL.BindVertexArray(homeVAO);

        //VBO Vertices
        GL.BindBuffer(BufferTarget.ArrayBuffer, homeVBO);
        GL.BufferData(BufferTarget.ArrayBuffer,
            homeVertices.Count * Vector3.SizeInBytes,
            homeVertices.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float,
            false, Vector3.SizeInBytes, 0);
        GL.EnableVertexAttribArray(0);

        //VBO Texture 
        GL.BindBuffer(BufferTarget.ArrayBuffer, homeTextureVBO);
        GL.BufferData(BufferTarget.ArrayBuffer,
            homeTexCoords.Count * Vector2.SizeInBytes,
            homeTexCoords.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float,
            false, Vector2.SizeInBytes, 0);
        GL.EnableVertexAttribArray(1);

        //VBO Normals
        GL.BindBuffer(BufferTarget.ArrayBuffer, homeNormalsVBO);
        GL.BufferData(BufferTarget.ArrayBuffer,
            homeNormals.Count * Vector3.SizeInBytes,
            homeNormals.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float,
            false, Vector3.SizeInBytes, 0);
        GL.EnableVertexAttribArray(2);

        //EBO Indices
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

        int stride = 8 * sizeof(float); // так как 8 значений у каждой вершины

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride,
            3 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride,
            6 * sizeof(float));
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

    private int[] LoadHomeTextures(List<string> paths)
    {
        int[] textureIDs = new int[paths.Count];
        Console.WriteLine($"Loading {paths.Count} individual home textures...");

        StbImage.stbi_set_flip_vertically_on_load(0);

        for (int i = 0; i < paths.Count; i++)
        {
            string path = paths[i];
            int textureHandle = 0;

            textureHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0); 
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);

            string absolutePath = Path.GetFullPath(path);
            Console.Write($" -> Loading texture {i}: {absolutePath}... ");

            // загрузка изображения
            using (Stream stream = File.OpenRead(absolutePath))
            {
                ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                    image.Width, image.Height, 0, PixelFormat.Rgba,
                    PixelType.UnsignedByte, image.Data);

                // генерируем мипмапы
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                // устанавливаем параметры
                GL.TexParameter(TextureTarget.Texture2D,
                    TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D,
                    TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D,
                    TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D,
                    TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                textureIDs[i] = textureHandle; // сохраняем успешный ID
                Console.WriteLine($"OK (ID: {textureHandle})");
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        Console.WriteLine("Finished loading home textures.");
        return textureIDs;
    }

    private int GetTextureIdSafe(int[] textureArray, int index)
    {
        if (textureArray != null && index >= 0 && index < textureArray.Length)
        {
            return textureArray[index];
        }
        Console.WriteLine($"Warning: invalid texture index {index}");
        return 0;
    }

    private void DrawCubeAndRoofFace(int indexOffset, int indexCount, int textureId)
    {
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.DrawElements(PrimitiveType.Triangles, indexCount,
            DrawElementsType.UnsignedInt, indexOffset * sizeof(uint));
    }


    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // Проход 1: Рендер Карты Теней
        // Рассчитываем матрицы  света
        float near_plane = 1.0f, far_plane = orbitRadius * 4.0f; // Дальность видимости для тени
        
        Matrix4 lightProjection = Matrix4.CreateOrthographicOffCenter(
            -25.0f, 25.0f, -25.0f, 25.0f, near_plane, far_plane); // ортографическая матрица проекции для направленного света
        Matrix4 lightView = Matrix4.LookAt(activeLightPos, Vector3.Zero, Vector3.UnitY); // матрица вида из позиции света
        lightSpaceMatrix = lightView * lightProjection; // единую матрицу преобразования в пространство света


        GL.Viewport(0, 0, shadowMapWidth, shadowMapHeight); // Устанавливаем размер viewport для FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO); // Активируем FBO
        GL.Clear(ClearBufferMask.DepthBufferBit); // Очищаем ТОЛЬКО буфер глубины

        
        shadowShader.UseShader(); // используем шейдер теней
        GL.UniformMatrix4(shadowLightSpaceMatrixLoc, false, ref lightSpaceMatrix); // передаем матрицу света


        // Рендерим объекты, отбрасывающие тень

        // Модель Дома
        Matrix4 homeModel = Matrix4.CreateTranslation(0f, 0.5f, 0f); // статичная модель дома
        GL.UniformMatrix4(shadowModelLoc, false, ref homeModel); // передаем модель в shadow shader

        GL.BindVertexArray(homeVAO);
        GL.DrawElements(PrimitiveType.Triangles, homeIndices.Length,
            DrawElementsType.UnsignedInt, 0); // куб

        GL.BindVertexArray(roofVAO);
        GL.DrawElements(PrimitiveType.Triangles, roofIndexCount,
            DrawElementsType.UnsignedInt, 0); // крыша

        // Рисуем Землю
        Matrix4 groundModelShadow = Matrix4.Identity;
        GL.UniformMatrix4(shadowModelLoc, false, ref groundModelShadow);
        GL.BindVertexArray(groundVAO);
        GL.DrawElements(PrimitiveType.Triangles, groundIndices.Length,
            DrawElementsType.UnsignedInt, 0);


        GL.BindVertexArray(0); // отвязываем VAO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0); // возвращаемся к рендеру в окно (из карты теней)


        // Проход 2: Основной Рендер Сцены
        GL.Viewport(0, 0, Size.X, Size.Y); // восстанавливаем viewport окна
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // очищаем буферы цвета и глубины окна

        Matrix4 view = camera.GetViewMatrix(); // получаем матрицу камеры
        Matrix4 projection = camera.GetProjectionMatrix();

        // Рендерим Дом и Землю (с тенями)
        shaderProgram.UseShader(); // Активируем основной шейдер с освещением и тенями

        // Передаем все необходимые uniform'ы: камеры, света, тени
        GL.UniformMatrix4(viewLocation, false, ref view);
        GL.UniformMatrix4(projectionLocation, false, ref projection);

        // Устанавливаем параметры освещения (вычислены в OnUpdateFrame)
        GL.Uniform3(lightDirLoc, ref currentLightDir);
        GL.Uniform3(lightColorLoc, ref currentLightColor);
        GL.Uniform3(ambientColorLoc, ref currentAmbientColor);
        GL.Uniform3(viewPosLoc, camera.Position);

        // Устанавливаем матрицу света и карту теней
        GL.UniformMatrix4(lightSpaceMatrixLoc, false, ref lightSpaceMatrix);
        GL.ActiveTexture(TextureUnit.Texture1); // активируем юнит 1
        GL.BindTexture(TextureTarget.Texture2D, depthMapTexture); // биндим карту теней к юниту 1
                                                                  // Sampler 'shadowMap' в шейдере должен использовать юнит 1 (установлено в OnLoad)

        GL.ActiveTexture(TextureUnit.Texture0); // активируем юнит 0 для основных текстур
                                                // Sampler 'texture0' в шейдере должен использовать юнит 0 (установлено в OnLoad)

        int[] currentHomeFaceIds = homeFaceTextureIDsDict[currentSeason];
        int currentGroundTexId = groundTextureIDs[currentSeason];

        // Рендерим КУБ Дома по граням
        GL.UniformMatrix4(modelLocation, false, ref homeModel); // устанавливаем модель дома
        GL.BindVertexArray(homeVAO); // биндим VAO куба
        DrawCubeAndRoofFace(0, 6, GetTextureIdSafe(currentHomeFaceIds, 0));   // Front
        DrawCubeAndRoofFace(6, 6, GetTextureIdSafe(currentHomeFaceIds, 1));   // Back
        DrawCubeAndRoofFace(12, 6, GetTextureIdSafe(currentHomeFaceIds, 2));  // Left
        DrawCubeAndRoofFace(18, 6, GetTextureIdSafe(currentHomeFaceIds, 3));  // Right
        DrawCubeAndRoofFace(24, 6, GetTextureIdSafe(currentHomeFaceIds, 4));  // Top
        DrawCubeAndRoofFace(30, 6, GetTextureIdSafe(currentHomeFaceIds, 5));  // Bottom

        // Рендерим КРЫШУ Дома по граням
        GL.BindVertexArray(roofVAO); // Биндим VAO крыши
        DrawCubeAndRoofFace(0, 6, GetTextureIdSafe(currentHomeFaceIds, 6));  // Front Slope
        DrawCubeAndRoofFace(6, 6, GetTextureIdSafe(currentHomeFaceIds, 7));  // Back Slope
        DrawCubeAndRoofFace(12, 3, GetTextureIdSafe(currentHomeFaceIds, 8)); // Left Gable
        DrawCubeAndRoofFace(15, 3, GetTextureIdSafe(currentHomeFaceIds, 9)); // Right Gable

        // Рендерим ЗЕМЛЮ
        GL.BindTexture(TextureTarget.Texture2D, currentGroundTexId); // биндим текстуру земли к юниту 0
        Matrix4 groundModel = Matrix4.Identity;
        GL.UniformMatrix4(modelLocation, false, ref groundModel);
        GL.BindVertexArray(groundVAO); // биндим VAO земли
        GL.DrawElements(PrimitiveType.Triangles, groundIndices.Length,
            DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0); // Отвязываем VAO

        // Рендерим Солнце и Луну (без теней)
        unlitShader.UseShader(); // активируем UNLIT шейдер
        GL.UniformMatrix4(unlitViewLoc, false, ref view);
        GL.UniformMatrix4(unlitProjLoc, false, ref projection);
        GL.BindVertexArray(sphereVAO);

        // Sun
        if (sunPos.Y >= -1.0f)
        {
            Matrix4 sunModel = Matrix4.CreateScale(2.0f) * Matrix4.CreateTranslation(sunPos);
            GL.UniformMatrix4(unlitModelLoc, false, ref sunModel);
            GL.Uniform3(unlitColorLoc, sunColorDay * 1.5f); // можно настроить яркость
            GL.DrawElements(PrimitiveType.Triangles, sphereIndexCount,
                DrawElementsType.UnsignedInt, 0);
        }
        // Moon
        if (moonPos.Y >= -1.0f)
        {
            Matrix4 moonModel = Matrix4.CreateScale(1.5f) * Matrix4.CreateTranslation(moonPos);
            GL.UniformMatrix4(unlitModelLoc, false, ref moonModel);
            GL.Uniform3(unlitColorLoc, moonColorNight * 1.5f); // можно настроить яркость
            GL.DrawElements(PrimitiveType.Triangles, sphereIndexCount,
                DrawElementsType.UnsignedInt, 0);
        }
        GL.BindVertexArray(0); // отвязываем VAO сферы

        // Рендерим Скайбокс
        GL.DepthFunc(DepthFunction.Lequal); // устанавливаем тест глубины для скайбокса
        skyboxShaderProgram.UseShader(); // активируем шейдер скайбокса
        Matrix4 skyboxViewMatrix = view.ClearTranslation(); // убираем смещение из матрицы вида
        GL.UniformMatrix4(skyboxViewLocation, false, ref skyboxViewMatrix);
        GL.UniformMatrix4(skyboxProjectionLocation, false, ref projection);
        GL.Uniform1(skyboxBrightnessFactorLoc, sunAltitudeFactor); // передаем яркость
        GL.BindVertexArray(skyboxVAO); // биндим VAO скайбокса
        GL.ActiveTexture(TextureUnit.Texture0); // активируем юнит 0
        GL.BindTexture(TextureTarget.TextureCubeMap, skyboxTextureID); // биндим кубмап к юниту 0
                                                                       // Sampler 'skybox' должен использовать юнит 0 (установлено в OnLoad)
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36); // рисуем скайбокс
        GL.BindVertexArray(0); // отвязываем VAO
        GL.DepthFunc(DepthFunction.Less); // возвращаем стандартный тест глубины

        Context.SwapBuffers(); // показываем отрисованный кадр
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        GL.DeleteBuffer(homeNormalsVBO);

        GL.DeleteBuffer(sphereVBO);
        GL.DeleteBuffer(sphereEBO);
        GL.DeleteVertexArray(sphereVAO);

        GL.DeleteVertexArray(homeVAO);
        GL.DeleteBuffer(homeVBO);
        GL.DeleteBuffer(homeEBO);
        GL.DeleteBuffer(homeTextureVBO);

        GL.DeleteVertexArray(groundVAO);
        GL.DeleteBuffer(groundVBO);
        GL.DeleteBuffer(groundEBO);
        GL.DeleteTexture(groundTextureID);

        GL.DeleteVertexArray(skyboxVAO);
        GL.DeleteBuffer(skyboxVBO);
        GL.DeleteTexture(skyboxTextureID);

        GL.DeleteFramebuffer(depthMapFBO);
        GL.DeleteTexture(depthMapTexture);

        GL.DeleteVertexArray(roofVAO);
        GL.DeleteBuffer(roofVBO);
        GL.DeleteBuffer(roofNormalsVBO);
        GL.DeleteBuffer(roofTexCoordsVBO);
        GL.DeleteBuffer(roofEBO);

        foreach (var kvp in groundTextureIDs)
        {
            GL.DeleteTexture(kvp.Value);
        }
        // Удаляем текстуры дома
        foreach (var kvp in homeFaceTextureIDsDict)
        {
            foreach (int texId in kvp.Value)
            {
                GL.DeleteTexture(texId);
            }
        }

        shaderProgram.DeleteShader();
        skyboxShaderProgram.DeleteShader();
        unlitShader.DeleteShader();
        shadowShader.DeleteShader();
    }


    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        if (!IsFocused) return;

        MouseState mouse = MouseState;
        KeyboardState input = KeyboardState;
        OnMouseButtonDown(mouse, args);
        OnFullScreenMode(input, mouse, args);
        if (input.IsKeyDown(Keys.Escape)) { Close(); return; }

        timeOfDay += (float)args.Time * cycleSpeed;

        if (timeOfDay >= 2.0f * MathF.PI) timeOfDay -= 2.0f * MathF.PI;

        sunPos = new Vector3(
            orbitRadius * MathF.Cos(timeOfDay), // координата X (r * cos)
            orbitRadius * MathF.Sin(timeOfDay), // координата Y (r * sin)
            0.0f                                // координата Z
        );

        moonPos = new Vector3(
            orbitRadius * MathF.Cos(timeOfDay + MathF.PI),
            orbitRadius * MathF.Sin(timeOfDay + MathF.PI),
            0.0f
        );

        sunAltitudeFactor = Math.Max(0.0f, sunPos.Y / orbitRadius); // высота солнца 0..1
        float dayFactor = MathF.Sin(MathHelper.DegreesToRadians(sunAltitudeFactor * 90.0f)); // интенсивность дня 0..1
        dayFactor = dayFactor * dayFactor; 

        Vector3 currentSkyColor;

        float horizonFactor = 1.0f - Math.Abs(MathF.Cos(timeOfDay)); // близость к горизонту 0..1
        horizonFactor = Math.Clamp(horizonFactor / HorizonTransitionThreshold, 0.0f, 1.0f); // нормализуем в зоне перехода
        horizonFactor = horizonFactor * horizonFactor * (3.0f - 2.0f * horizonFactor); // сглаживаем переход (smoothstep)

        if (sunPos.Y >= 0) // День
        {
            if (day_or_night == 1)
            {
                switch (curr_sesson % 4)
                {
                    case 0:
                        currentSeason = Season.Autumn;
                        break;
                    case 1:
                        currentSeason = Season.Winter;
                        break;
                    case 2:
                        currentSeason = Season.Spring;
                        break;
                    case 3:
                        currentSeason = Season.Summer;
                        break;
                }
                curr_sesson++;
                day_or_night = 0;
            }
            activeLightPos = sunPos; // источник света - солнце
            currentLightDir = Vector3.Normalize(-activeLightPos); // направление ОТ солнца

            // Смешиваем цвета дня и восхода/заката
            Vector3 transitionSunColor = Vector3.Lerp(sunColorSunrise,
                sunColorSunset, Math.Clamp(timeOfDay / MathF.PI, 0.0f, 1.0f)); // Плавный переход от восхода к закату
            Vector3 transitionAmbientColor = Vector3.Lerp(ambientSunrise,
                ambientSunset, Math.Clamp(timeOfDay / MathF.PI, 0.0f, 1.0f));
            Vector3 transitionSkyColor = Vector3.Lerp(skyColorSunrise,
                skyColorSunset, Math.Clamp(timeOfDay / MathF.PI, 0.0f, 1.0f));

            // Интерполируем между дневными и переходными цветами на основе близости к горизонту
            currentLightColor = Vector3.Lerp(sunColorDay, transitionSunColor, horizonFactor);
            currentAmbientColor = Vector3.Lerp(ambientDay, transitionAmbientColor, horizonFactor);
            currentSkyColor = Vector3.Lerp(skyColorDay, transitionSkyColor, horizonFactor);

            // Применяем общую интенсивность дня
            currentLightColor *= dayFactor * DayLightBoost;
        }
        else
        {
            day_or_night = 1;
            activeLightPos = moonPos;
            currentLightDir = Vector3.Normalize(-activeLightPos);

            // Лунный свет
            currentLightColor = moonColorNight * MoonLightIntensity;

            // Смешиваем цвета ночи и восхода/заката
            Vector3 transitionAmbientColor = Vector3.Lerp(ambientSunrise, ambientSunset, Math.Clamp((timeOfDay - MathF.PI) / MathF.PI, 0.0f, 1.0f)); // Фаза ночи 0..1
            Vector3 transitionSkyColor = Vector3.Lerp(skyColorSunrise, skyColorSunset, Math.Clamp((timeOfDay - MathF.PI) / MathF.PI, 0.0f, 1.0f));

            currentAmbientColor = Vector3.Lerp(ambientNight, transitionAmbientColor, horizonFactor);
            currentSkyColor = Vector3.Lerp(skyColorNight, transitionSkyColor, horizonFactor);
        }

        GL.ClearColor(currentSkyColor.X, currentSkyColor.Y, currentSkyColor.Z, 1.0f);

        if (cursorGrabbed)
        {
            camera.Update(input, mouse, args, out Vector2 newLastPos);
            lastPos = newLastPos;
        }
        else if (!cursorGrabbed) { lastPos = new Vector2(mouse.X, mouse.Y); }

        base.OnUpdateFrame(args);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        if (camera != null)
        {
            camera.UpdateScreenSize(e.Width, e.Height);
        }
        this.width = e.Width;
        this.height = e.Height;
    }


    private void OnMouseButtonDown(MouseState mouse, FrameEventArgs e)
    {
        if (this.IsFocused && mouse.IsButtonDown(MouseButton.Left) && !cursorGrabbed)
        {
            this.MousePosition = lastPos;
            this.CursorState = CursorState.Grabbed;
            cursorGrabbed = true;

            if (camera != null)
            {
                camera.firstMove = true;
            }
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
};

class GeometryFactory
{
    public static (List<Vector3> vertices, List<Vector2> texCoords, List<uint> indices) CreateSphereVertices(float radius, int sectorCount, int stackCount)
    {
        var vertices = new List<Vector3>();
        var texCoords = new List<Vector2>();
        var indices = new List<uint>();

        float x, y, z, xy; // координаты вершины
        float s, t; // текстурные координаты

        float sectorStep = 2 * MathF.PI / sectorCount; // угол одного сектора (полный круг / количество)
        float stackStep = MathF.PI / stackCount; // угол одного слоя (полукруг / количество)
        float sectorAngle, stackAngle; // текущие углы

        for (int i = 0; i <= stackCount; ++i)
        {
            // Начинаем с PI/2 (90 градусов, верхний полюс) и до -PI/2 (-90 градусов, нижний полюс)
            stackAngle = MathF.PI / 2 - i * stackStep; //
            xy = radius * MathF.Cos(stackAngle); // вычисляем проекцию радиуса на плоскость XY для текущего слоя (r * cos(угол_слоя))
            z = radius * MathF.Sin(stackAngle);  // координата Z для текущего слоя

            for (int j = 0; j <= sectorCount; ++j)
            {
                // Вычисляем угол текущего сектора (от 0 до 2*PI).
                sectorAngle = j * sectorStep;

                x = xy * MathF.Cos(sectorAngle); // x = r * cos(stackAngle) * cos(sectorAngle)
                y = xy * MathF.Sin(sectorAngle); // y = r * cos(stackAngle) * sin(sectorAngle)
                vertices.Add(new Vector3(x, y, z)); // добавляем вычисленную вершину в список 

                s = (float)j / sectorCount; // U (s) координата зависит от сектора (от 0 до 1 по горизонтали)
                t = (float)i / stackCount; // V (t) координата зависит от слоя (от 0 до 1 по вертикали)
                texCoords.Add(new Vector2(s, t)); // добавляем вычисленные текстурные координаты 
            }
        }

        // Генерация Индексов для Треугольников
        uint k1, k2; // индексы вершин в текущем (k1) и следующем (k2) слоях
        for (int i = 0; i < stackCount; ++i)
        {
            k1 = (uint)(i * (sectorCount + 1)); // индекс первой вершины текущего слоя
            k2 = (uint)(k1 + sectorCount + 1); // индекс первой вершины следующего слоя

            for (int j = 0; j < sectorCount; ++j, ++k1, ++k2)
            {
                // Треугольник 1: (вершина i, j) -> (вершина i+1, j) -> (вершина i, j+1)
                // Индексы: k1 -> k2 -> k1+1
                // Пропускаем первый слой (i=0), т.к. у верхнего полюса все вершины сливаются в одну точку
                if (i != 0)
                {
                    indices.Add(k1);
                    indices.Add(k2);
                    indices.Add(k1 + 1);
                }

                // Треугольник 2: (вершина i, j+1) -> (вершина i+1, j) -> (вершина i+1, j+1)
                // Индексы: k1+1 -> k2 -> k2+1
                // Пропускаем предпоследний слой (i = stackCount - 1), т.к. у нижнего полюса вершины следующего слоя (i+1) сливаются в одну точку
                if (i != (stackCount - 1))
                {
                    indices.Add(k1 + 1);
                    indices.Add(k2);
                    indices.Add(k2 + 1);
                }
            }
        }
        return (vertices, texCoords, indices);
    }
}

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