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

    private bool cursorGrabbed = true;
    public Vector2 lastPos;

    Camera camera;

    private int homeVAO, homeVBO, homeEBO, homeTextureVBO, homeTextureID;
    private int homeNormalsVBO;
    private List<Vector3> homeVertices;
    private List<Vector2> homeTexCoords;
    private uint[] homeIndices;
    private int[] homeFaceTextureIDs = new int[10];
    private List<string> faceTexturePaths;

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
    private float cycleSpeed = 0.1f; // Скорость смены дня/ночи (радианы в секунду)
    private float orbitRadius = 50.0f; // Насколько далеко солнце/луна
    private Vector3 sunPos, moonPos;   // Текущие позиции
    private Vector3 currentLightDir;   // Направление НА источник света
    private Vector3 currentLightColor; // Цвет источника
    private Vector3 currentAmbientColor; // Цвет фонового освещения
    private int skyboxBrightnessFactorLoc;
    private float sunAltitudeFactor;


    //Цвета для дня/ночи
    private readonly Vector3 sunColorDay = new Vector3(1.0f, 1.0f, 0.85f); // Яркий желтоватый
    private readonly Vector3 sunColorSunrise = new Vector3(1.0f, 0.55f, 0.35f); // Оранжево-красный
    private readonly Vector3 sunColorSunset = new Vector3(1.0f, 0.75f, 0.35f); // Красно-розовый
    private readonly Vector3 moonColorNight = new Vector3(0.6f, 0.6f, 0.8f); // Тусклый голубоватый

    private readonly Vector3 ambientDay = new Vector3(0.3f, 0.3f, 0.35f); // Светло-голубой эмбиент
    private readonly Vector3 ambientSunrise = new Vector3(0.25f, 0.15f, 0.15f); // Теплый красный эмбиент
    private readonly Vector3 ambientSunset = new Vector3(0.3f, 0.15f, 0.18f); // Теплый красноватый эмбиент
    private readonly Vector3 ambientNight = new Vector3(0.08f, 0.08f, 0.15f); // Темно-синий эмбиент

    private readonly Vector3 skyColorDay = new Vector3(0.5f, 0.7f, 1.0f); // Цвет неба днем
    private readonly Vector3 skyColorSunrise = new Vector3(0.9f, 0.55f, 0.45f); // Оранжевое небо
    private readonly Vector3 skyColorSunset = new Vector3(0.95f, 0.45f, 0.5f); // Красное небо
    private readonly Vector3 skyColorNight = new Vector3(0.01f, 0.01f, 0.05f); // Цвет неба ночью

    private const float HorizonTransitionThreshold = 0.35f;
    private const float DayLightBoost = 1.4f;
    private const float MoonLightIntensity = 0.8f;

    //Локации для uniform'ов освещения в основном шейдере
    private int lightDirLoc, lightColorLoc, ambientColorLoc, viewPosLoc;

    //Данные нормалей для куба
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

        // --- Генерируем геометрию сферы ---
        var sphereData = GeometryFactory.CreateSphereVertices(1.0f, 36, 18); // Радиус 1, детализация средняя
        sphereVertices = sphereData.vertices;
        sphereIndices = sphereData.indices;
        sphereIndexCount = sphereIndices.Count;
        SetupSphereBuffers(); // Настраиваем VAO/VBO/EBO для сферы

        PrepareHomeData();
        SetupHomeBuffers();

        PrepareRoofData();
        SetupRoofBuffers();

        PrepareGroundData();
        SetupGroundBuffers();

        PrepareSkyboxData();
        SetupSkyboxBuffers();

        homeFaceTextureIDs = LoadHomeTextures(new List<string>
        {
            "../../../Textures/Home/wall_front.jpg",  // 0: Front Face
            "../../../Textures/Home/wall_back.jpg",   // 1: Back Face
            "../../../Textures/Home/wall_left.jpg",   // 2: Left Face
            "../../../Textures/Home/wall_right.jpg",  // 3: Right Face
            "../../../Textures/Home/wall_top.jpg",    // 4: Top Face (основание крыши)
            "../../../Textures/Home/wall_bottom.jpg", // 5: Bottom Face
            "../../../Textures/Home/roof_front.jpg",  // 6: Roof Front
            "../../../Textures/Home/roof_back.jpg",   // 7: Roof Back
            "../../../Textures/Home/roof_left.jpg",   // 8: Roof Left
            "../../../Textures/Home/roof_right.jpg"   // 9: Roof Right
        });
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
        shadowLightSpaceMatrixLoc = GL.GetUniformLocation(shadowShader.shaderHandle,
            "lightSpaceMatrix");
        shadowModelLoc = GL.GetUniformLocation(shadowShader.shaderHandle, "model");

        // Создаем Framebuffer Object (FBO)
        depthMapFBO = GL.GenFramebuffer();

        // Создаем текстуру для карты глубины
        depthMapTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, depthMapTexture);

        // Создаем пустое изображение нужного размера с форматом глубины
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent,
                      shadowMapWidth, shadowMapHeight, 0, PixelFormat.DepthComponent,
                      PixelType.Float, IntPtr.Zero);

        // Устанавливаем параметры фильтрации (обычно Nearest для карт теней)
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMinFilter.Nearest);

        // Устанавливаем параметры обертывания (ClampToBorder лучше всего, чтобы за пределами карты не было теней)
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 
            (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToBorder);

        // Устанавливаем цвет границы (белый = нет тени за пределами)
        float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor,
            borderColor);

        // Прикрепляем текстуру глубины к FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthMapTexture, 0);

        // Говорим OpenGL, что мы не будем рендерить в цветовой буфер для этого FBO
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        // Проверяем, готов ли FBO
        var fboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (fboStatus != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine($"ERROR::FRAMEBUFFER:: Framebuffer is not complete! Status: {fboStatus}");
        }
        // Отвязываем FBO, чтобы вернуться к рендеру в окно по умолчанию
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // Получаем локации для теней в основном шейдере
        shaderProgram.UseShader();
        lightSpaceMatrixLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "lightSpaceMatrix");
        shadowMapSamplerLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "shadowMap");
        
        // Указываем, что карта теней будет в текстурном юните 1 (0 уже занят основной текстурой)
        GL.Uniform1(shadowMapSamplerLoc, 1);


        modelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
        viewLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
        projectionLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");
        GL.Uniform1(GL.GetUniformLocation(shaderProgram.shaderHandle, "texture0"), 0);

        lightDirLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "lightDir");
        lightColorLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "lightColor");
        ambientColorLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "ambientColor");
        viewPosLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "viewPos");

        skyboxSamplerLocation = GL.GetUniformLocation(skyboxShaderProgram.shaderHandle, "skybox");
        skyboxViewLocation = GL.GetUniformLocation(skyboxShaderProgram.shaderHandle, "view");
        skyboxProjectionLocation = GL.GetUniformLocation(skyboxShaderProgram.shaderHandle, "projection");
        skyboxBrightnessFactorLoc = GL.GetUniformLocation(skyboxShaderProgram.shaderHandle, "brightnessFactor");
        GL.Uniform1(skyboxSamplerLocation, 0);

        unlitModelLoc = GL.GetUniformLocation(unlitShader.shaderHandle, "model");
        unlitViewLoc = GL.GetUniformLocation(unlitShader.shaderHandle, "view");
        unlitProjLoc = GL.GetUniformLocation(unlitShader.shaderHandle, "projection");
        unlitColorLoc = GL.GetUniformLocation(unlitShader.shaderHandle, "objectColor");

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
            // Передняя Z+ (0-3)
            Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ,
            // Правая X+ (4-7)
            Vector3.UnitX, Vector3.UnitX, Vector3.UnitX, Vector3.UnitX,
            // Задняя Z- (8-11)
           -Vector3.UnitZ,-Vector3.UnitZ,-Vector3.UnitZ,-Vector3.UnitZ,
            // Левая X- (12-15)
           -Vector3.UnitX,-Vector3.UnitX,-Vector3.UnitX,-Vector3.UnitX,
            // Верхняя Y+ (16-19)
            Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY,
            // Нижняя Y- (20-23)
           -Vector3.UnitY,-Vector3.UnitY,-Vector3.UnitY,-Vector3.UnitY,
        };
    }

    private void AddRoofQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 normal, float layer, ref uint vertexOffset)
    {
        roofVertices.Add(v1); roofVertices.Add(v2); roofVertices.Add(v3); roofVertices.Add(v4);
        roofNormals.Add(normal); roofNormals.Add(normal); roofNormals.Add(normal); roofNormals.Add(normal);
        // UV для ската крыши (могут потребовать настройки)
        roofTexCoords.Add(new Vector2(0, 0)); roofTexCoords.Add(new Vector2(1, 0)); roofTexCoords.Add(new Vector2(1, 1)); roofTexCoords.Add(new Vector2(0, 1));
        // Не используем layer здесь, т.к. нет Texture Array
        // homeAllTexLayers.Add(layer); homeAllTexLayers.Add(layer); homeAllTexLayers.Add(layer); homeAllTexLayers.Add(layer);

        roofIndices.Add(vertexOffset + 0); roofIndices.Add(vertexOffset + 1); roofIndices.Add(vertexOffset + 2);
        roofIndices.Add(vertexOffset + 2); roofIndices.Add(vertexOffset + 3); roofIndices.Add(vertexOffset + 0);
        vertexOffset += 4;
    }

    private void AddRoofTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv1, Vector2 uv2, Vector2 uv3, float layer, ref uint vertexOffset)
    {
        Vector3 edge1 = v2 - v1;
        Vector3 edge2 = v3 - v1;
        Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
        // Убедимся, что нормаль торца направлена наружу (например, для левого торца X должен быть < 0)
        if (Math.Abs(v1.X - v2.X) < 0.01f && normal.X > 0 && v1.X < 0) normal *= -1; // Пример для левого торца
        if (Math.Abs(v1.X - v2.X) < 0.01f && normal.X < 0 && v1.X > 0) normal *= -1; // Пример для правого торца


        roofVertices.Add(v1); roofVertices.Add(v2); roofVertices.Add(v3);
        roofNormals.Add(normal); roofNormals.Add(normal); roofNormals.Add(normal);
        roofTexCoords.Add(uv1); roofTexCoords.Add(uv2); roofTexCoords.Add(uv3);
        // homeAllTexLayers.Add(layer); homeAllTexLayers.Add(layer); homeAllTexLayers.Add(layer);

        roofIndices.Add(vertexOffset + 0); roofIndices.Add(vertexOffset + 1); roofIndices.Add(vertexOffset + 2);
        vertexOffset += 3;
    }

    private void PrepareRoofData()
    {
        roofVertices = new List<Vector3>();
        roofNormals = new List<Vector3>();
        roofTexCoords = new List<Vector2>();
        roofIndices = new List<uint>();

        float roofHeight = 0.5f;     // Высота конька над основанием крыши
        float overhang = 0.15f;     // Насколько крыша выступает за стены
        float baseY = 0.5f;         // Y-координата основания крыши (верх куба)
        float halfSize = 0.5f;      // Половина размера куба

        // Вершины конька крыши
        Vector3 ridgeLeft = new Vector3(-(halfSize + overhang), baseY + roofHeight, 0.0f);
        Vector3 ridgeRight = new Vector3((halfSize + overhang), baseY + roofHeight, 0.0f);

        // Вершины основания крыши (с навесом)
        Vector3 baseFrontLeft = new Vector3(-(halfSize + overhang), baseY, (halfSize + overhang));
        Vector3 baseFrontRight = new Vector3((halfSize + overhang), baseY, (halfSize + overhang));
        Vector3 baseBackLeft = new Vector3(-(halfSize + overhang), baseY, -(halfSize + overhang));
        Vector3 baseBackRight = new Vector3((halfSize + overhang), baseY, -(halfSize + overhang));

        // --- Добавляем 4 грани крыши (прямоугольники) ---
        uint vertexOffset = 0;

        // Грань 1: Передний скат (+Z направление нормали примерно)
        Vector3 normalFront = Vector3.Normalize(new Vector3(0, halfSize + overhang, roofHeight)); // Нормаль ската
        AddRoofQuad(baseFrontLeft, baseFrontRight, ridgeRight, ridgeLeft, normalFront, 6.0f, ref vertexOffset); // Текстура 6

        // Грань 2: Задний скат (-Z направление нормали примерно)
        Vector3 normalBack = Vector3.Normalize(new Vector3(0, halfSize + overhang, -roofHeight));
        AddRoofQuad(baseBackRight, baseBackLeft, ridgeLeft, ridgeRight, normalBack, 7.0f, ref vertexOffset); // Текстура 7

        // --- Добавляем 2 торцевые грани крыши (треугольники) ---

        // Торец 3: Левый (-X направление)
        AddRoofTriangle(baseBackLeft, baseFrontLeft, ridgeLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1), 8.0f, ref vertexOffset); // Текстура 8

        // Торец 4: Правый (+X направление)
        AddRoofTriangle(baseFrontRight, baseBackRight, ridgeRight, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1), 9.0f, ref vertexOffset); // Текстура 9

        roofIndexCount = roofIndices.Count; // Обновляем количество индексов (4 * 6 = 24)
    }

    private void AddRoofTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        Vector3 edge1 = v2 - v1;
        Vector3 edge2 = v3 - v1;
        Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

        roofVertices.Add(v1); roofVertices.Add(v2); roofVertices.Add(v3);
        roofNormals.Add(normal); roofNormals.Add(normal); roofNormals.Add(normal);
        roofTexCoords.Add(uv1); roofTexCoords.Add(uv2); roofTexCoords.Add(uv3);
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
        GL.BufferData(BufferTarget.ArrayBuffer, roofVertices.Count * Vector3.SizeInBytes, roofVertices.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
        GL.EnableVertexAttribArray(0);

        // VBO Текстурные координаты (location 1)
        GL.BindBuffer(BufferTarget.ArrayBuffer, roofTexCoordsVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, roofTexCoords.Count * Vector2.SizeInBytes, roofTexCoords.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0);
        GL.EnableVertexAttribArray(1);

        // VBO Нормали (location 2)
        GL.BindBuffer(BufferTarget.ArrayBuffer, roofNormalsVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, roofNormals.Count * Vector3.SizeInBytes, roofNormals.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
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

        int stride = 8 * sizeof(float);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float)); // Смещение 3 float'а от начала
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
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

        StbImage.stbi_set_flip_vertically_on_load(1);

        for (int i = 0; i < paths.Count; i++)
        {
            string path = paths[i];
            int textureHandle = 0;

            textureHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0); 
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);

            // Проверка пути
            string absolutePath = Path.GetFullPath(path);
            Console.Write($" -> Loading texture {i}: {absolutePath}... ");

            // Загрузка изображения
            using (Stream stream = File.OpenRead(absolutePath))
            {
                ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                // Загружаем данные в GPU
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                    image.Width, image.Height, 0, PixelFormat.Rgba,
                    PixelType.UnsignedByte, image.Data);

                // Генерируем мипмапы
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                // Устанавливаем параметры (можно вынести за цикл, если они одинаковые)
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                textureIDs[i] = textureHandle; // Сохраняем успешный ID
                Console.WriteLine($"OK (ID: {textureHandle})");
            }
            GL.BindTexture(TextureTarget.Texture2D, 0); // Отвязываем текстуру после каждой попытки
        }

        // StbImage.stbi_set_flip_vertically_on_load(1); // Уже установлено в начале

        Console.WriteLine("Finished loading home textures.");
        return textureIDs;
    }

    private void DrawCubeFace(int indexOffset, int indexCount, int textureId)
    {
        if (textureId <= 0) return; // Не рисуем, если текстура не загрузилась
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        // Рисуем часть индексов из EBO куба
        GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, indexOffset * sizeof(uint));
    }

    private void DrawRoofFace(int vertexOffset, int vertexCount, int textureId)
    {
        if (textureId <= 0) return;
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        // Рисуем вершины крыши напрямую
        GL.DrawArrays(PrimitiveType.Triangles, vertexOffset, vertexCount);
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
        GL.DeleteTexture(homeTextureID);

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

        shaderProgram.DeleteShader();
        skyboxShaderProgram.DeleteShader();
        unlitShader.DeleteShader();
        shadowShader.DeleteShader();
    }

    private void DrawRoofFaceByIndex(int indexOffset, int indexCount, int textureId)
    {
        if (textureId <= 0) return;
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        // Рисуем часть индексов из EBO крыши
        GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, indexOffset * sizeof(uint));
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // --- 1. РЕНДЕР КАРТЫ ТЕНЕЙ (Depth Pass) ---
        // Рассчитываем матрицы вида и проекции для источника света
        float near_plane = 1.0f, far_plane = orbitRadius * 4.0f; // Дальность видимости для тени
                                                                 // Ортографическая проекция для направленного света
        Matrix4 lightProjection = Matrix4.CreateOrthographicOffCenter(-25.0f, 25.0f, -25.0f, 25.0f, near_plane, far_plane);
        // Матрица вида из позиции света (activeLightPos вычисляется в OnUpdateFrame)
        Matrix4 lightView = Matrix4.LookAt(activeLightPos, Vector3.Zero, Vector3.UnitY);
        lightSpaceMatrix = lightView * lightProjection; // Комбинированная матрица

        // Настраиваем рендер в текстуру глубины
        GL.Viewport(0, 0, shadowMapWidth, shadowMapHeight); // Устанавливаем размер viewport для FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO); // Активируем FBO
        GL.Clear(ClearBufferMask.DepthBufferBit); // Очищаем ТОЛЬКО буфер глубины

        // Используем шейдер для записи глубины
        shadowShader.UseShader();
        GL.UniformMatrix4(shadowLightSpaceMatrixLoc, false, ref lightSpaceMatrix); // Передаем матрицу света

        // --- Рендерим объекты, отбрасывающие тень ---

        // Модель Дома (без вращения, поднята)
        Matrix4 homeRotation = Matrix4.Identity;
        Matrix4 homeTranslation = Matrix4.CreateTranslation(0f, 0.5f, 0f);
        Matrix4 homeModel = homeTranslation * homeRotation; // Используем homeModel для обоих проходов
        GL.UniformMatrix4(shadowModelLoc, false, ref homeModel); // Передаем модель в shadow shader

        // Рисуем Куб Дома
        GL.BindVertexArray(homeVAO);
        GL.DrawElements(PrimitiveType.Triangles, homeIndices.Length, DrawElementsType.UnsignedInt, 0);

        // Рисуем Крышу Дома
        GL.BindVertexArray(roofVAO);
        GL.DrawElements(PrimitiveType.Triangles, roofIndexCount,
            DrawElementsType.UnsignedInt, 0); // Используем DrawArrays для крыши

        // Рисуем Землю
        Matrix4 groundModelShadow = Matrix4.Identity;
        GL.UniformMatrix4(shadowModelLoc, false, ref groundModelShadow);
        GL.BindVertexArray(groundVAO);
        GL.DrawElements(PrimitiveType.Triangles, groundIndices.Length, DrawElementsType.UnsignedInt, 0);

        // Отвязываем ресурсы FBO
        GL.BindVertexArray(0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0); // Возвращаемся к рендеру в окно

        // --- 2. ОСНОВНОЙ РЕНДЕР СЦЕНЫ ---
        // Восстанавливаем viewport окна
        GL.Viewport(0, 0, Size.X, Size.Y);
        // Очищаем буферы цвета и глубины окна
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Получаем матрицы камеры
        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetProjectionMatrix();

        // --- Сначала рендерим ОБЪЕКТЫ С ТЕНЯМИ (Дом и Земля) ---
        shaderProgram.UseShader(); // Активируем основной шейдер с освещением и тенями
                                   // Устанавливаем матрицы камеры
        GL.UniformMatrix4(viewLocation, false, ref view);
        GL.UniformMatrix4(projectionLocation, false, ref projection);
        // Устанавливаем параметры освещения (вычислены в OnUpdateFrame)
        GL.Uniform3(lightDirLoc, ref currentLightDir);
        GL.Uniform3(lightColorLoc, ref currentLightColor);
        GL.Uniform3(ambientColorLoc, ref currentAmbientColor);
        GL.Uniform3(viewPosLoc, camera.Position);
        // Устанавливаем матрицу света и КАРТУ ТЕНЕЙ
        GL.UniformMatrix4(lightSpaceMatrixLoc, false, ref lightSpaceMatrix);
        GL.ActiveTexture(TextureUnit.Texture1); // Активируем ТЕКСТУРНЫЙ ЮНИТ 1
        GL.BindTexture(TextureTarget.Texture2D, depthMapTexture); // Биндим КАРТУ ТЕНЕЙ к юниту 1
                                                                  // Sampler 'shadowMap' в шейдере должен использовать юнит 1 (установлено в OnLoad)

        // --- Рендерим КУБ Дома (по граням с разными текстурами) ---
        GL.ActiveTexture(TextureUnit.Texture0); // Активируем ТЕКСТУРНЫЙ ЮНИТ 0 для основных текстур
                                                // Sampler 'texture0' в шейдере должен использовать юнит 0 (установлено в OnLoad)
        GL.UniformMatrix4(modelLocation, false, ref homeModel); // Устанавливаем модель дома
        GL.BindVertexArray(homeVAO); // Биндим VAO куба
        DrawCubeFace(0, 6, homeFaceTextureIDs[0]);   // Front
        DrawCubeFace(6, 6, homeFaceTextureIDs[1]);   // Back
        DrawCubeFace(12, 6, homeFaceTextureIDs[2]);  // Left
        DrawCubeFace(18, 6, homeFaceTextureIDs[3]);  // Right
        DrawCubeFace(24, 6, homeFaceTextureIDs[4]);  // Top
        DrawCubeFace(30, 6, homeFaceTextureIDs[5]);  // Bottom

        // --- Рендерим КРЫШУ Дома (по граням с разными текстурами) ---
        // Модель та же, шейдер тот же, юнит 0 все еще активен
        GL.BindVertexArray(roofVAO); // Биндим VAO крыши
        DrawRoofFaceByIndex(0, 6, homeFaceTextureIDs[6]);  // Front Slope (6 индексов)
        DrawRoofFaceByIndex(6, 6, homeFaceTextureIDs[7]);  // Back Slope
        DrawRoofFaceByIndex(12, 3, homeFaceTextureIDs[8]); // Left Gable
        DrawRoofFaceByIndex(15, 3, homeFaceTextureIDs[9]);   // Roof Right

        // --- Рендерим ЗЕМЛЮ ---
        // Шейдер тот же, освещение то же, юнит 0 все еще активен, юнит 1 с тенью
        GL.BindTexture(TextureTarget.Texture2D, groundTextureID); // Биндим текстуру земли к юниту 0
        Matrix4 groundModel = Matrix4.Identity;
        GL.UniformMatrix4(modelLocation, false, ref groundModel);
        GL.BindVertexArray(groundVAO); // Биндим VAO земли
        GL.DrawElements(PrimitiveType.Triangles, groundIndices.Length, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0); // Отвязываем VAO

        // --- Теперь рендерим СОЛНЦЕ и ЛУНУ (без теней) ---
        unlitShader.UseShader(); // Активируем UNLIT шейдер
        GL.UniformMatrix4(unlitViewLoc, false, ref view);
        GL.UniformMatrix4(unlitProjLoc, false, ref projection);
        GL.BindVertexArray(sphereVAO);
        // Sun
        if (sunPos.Y >= -1.0f)
        {
            Matrix4 sunModel = Matrix4.CreateScale(2.0f) * Matrix4.CreateTranslation(sunPos);
            GL.UniformMatrix4(unlitModelLoc, false, ref sunModel);
            GL.Uniform3(unlitColorLoc, sunColorDay * 1.5f); // Можно настроить яркость
            GL.DrawElements(PrimitiveType.Triangles, sphereIndexCount, DrawElementsType.UnsignedInt, 0);
        }
        // Moon
        if (moonPos.Y >= -1.0f)
        {
            Matrix4 moonModel = Matrix4.CreateScale(1.5f) * Matrix4.CreateTranslation(moonPos);
            GL.UniformMatrix4(unlitModelLoc, false, ref moonModel);
            GL.Uniform3(unlitColorLoc, moonColorNight * 1.5f); // Можно настроить яркость
            GL.DrawElements(PrimitiveType.Triangles, sphereIndexCount, DrawElementsType.UnsignedInt, 0);
        }
        GL.BindVertexArray(0); // Отвязываем VAO сферы

        // --- Рендерим СКАЙБОКС (в самом конце) ---
        GL.DepthFunc(DepthFunction.Lequal); // Устанавливаем тест глубины для скайбокса
        skyboxShaderProgram.UseShader(); // Активируем шейдер скайбокса
        Matrix4 skyboxViewMatrix = view.ClearTranslation(); // Матрица вида без смещения
        GL.UniformMatrix4(skyboxViewLocation, false, ref skyboxViewMatrix);
        GL.UniformMatrix4(skyboxProjectionLocation, false, ref projection);
        GL.Uniform1(skyboxBrightnessFactorLoc, sunAltitudeFactor); // Передаем яркость
        GL.BindVertexArray(skyboxVAO); // Биндим VAO скайбокса
        GL.ActiveTexture(TextureUnit.Texture0); // Активируем юнит 0
        GL.BindTexture(TextureTarget.TextureCubeMap, skyboxTextureID); // Биндим кубмап к юниту 0
                                                                       // Sampler 'skybox' должен использовать юнит 0 (установлено в OnLoad)
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36); // Рисуем скайбокс
        GL.BindVertexArray(0); // Отвязываем VAO
        GL.DepthFunc(DepthFunction.Less); // Возвращаем стандартный тест глубины

        Context.SwapBuffers(); // Показываем отрисованный кадр
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


    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        if (!IsFocused) return;

        MouseState mouse = MouseState;
        KeyboardState input = KeyboardState;
        OnMouseButtonDown(mouse, args);
        OnFullScreenMode(input, mouse, args);
        if (input.IsKeyDown(Keys.Escape)){ Close(); return; }



        timeOfDay += (float)args.Time * cycleSpeed;

        if (timeOfDay >= 2.0f * MathF.PI) timeOfDay -= 2.0f * MathF.PI;

        sunPos = new Vector3(
            orbitRadius * MathF.Cos(timeOfDay),
            orbitRadius * MathF.Sin(timeOfDay), // Y - высота над горизонтом
            0.0f // Пусть движется в плоскости X-Y для простоты
        );

        moonPos = new Vector3(
            orbitRadius * MathF.Cos(timeOfDay + MathF.PI),
            orbitRadius * MathF.Sin(timeOfDay + MathF.PI),
            0.0f
        );

        sunAltitudeFactor = Math.Clamp(sunPos.Y / orbitRadius, 0.0f, 1.0f);
        Vector3 currentSkyColor;

        float dayLerpFactor = Math.Clamp(sunAltitudeFactor, 0.0f, 1.0f);
        float horizonProximity = Math.Abs(MathF.Cos(timeOfDay));

        float transitionMixFactor = Math.Clamp(1.0f - horizonProximity
            / (1.0f - HorizonTransitionThreshold), 0.0f, 1.0f);
        transitionMixFactor = transitionMixFactor * transitionMixFactor
            * (3.0f - 2.0f * transitionMixFactor);

        float dayIntensity;

        if (sunPos.Y >= 0) // День
        {
            activeLightPos = sunPos;
            currentLightDir = Vector3.Normalize(-activeLightPos);

            // Определяем цвет (восход или закат)
            Vector3 transitionSunColor = (timeOfDay < MathF.PI) ?
                sunColorSunrise : sunColorSunset;
            Vector3 transitionAmbientColor = (timeOfDay < MathF.PI) ?
                ambientSunrise : ambientSunset;
            Vector3 transitionSkyColor = (timeOfDay < MathF.PI) ?
                skyColorSunrise : skyColorSunset;

            // Смешиваем дневные цвета и цвета восхода/заката
            currentLightColor = Vector3.Lerp(sunColorDay,
                transitionSunColor, transitionMixFactor);
            currentAmbientColor = Vector3.Lerp(ambientDay,
                transitionAmbientColor, transitionMixFactor);
            currentSkyColor = Vector3.Lerp(skyColorDay,
                transitionSkyColor, transitionMixFactor);

            dayIntensity = MathF.Sin(MathHelper.DegreesToRadians(
                dayLerpFactor * 180.0f));
            currentLightColor *= dayIntensity * DayLightBoost;
        }
        else
        {
            activeLightPos = moonPos;
            currentLightDir = Vector3.Normalize(-activeLightPos);

            currentLightColor = moonColorNight * MoonLightIntensity;

            Vector3 transitionAmbientColor = (timeOfDay > MathF.PI) ?
                ambientSunset : ambientSunrise;
            Vector3 transitionSkyColor = (timeOfDay > MathF.PI) ?
                skyColorSunset : skyColorSunrise;

            currentAmbientColor = Vector3.Lerp(ambientNight,
                transitionAmbientColor, transitionMixFactor);
            currentSkyColor = Vector3.Lerp(skyColorNight,
                transitionSkyColor, transitionMixFactor);
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
};

class GeometryFactory
{
    public static (List<Vector3> vertices, List<Vector2> texCoords, List<uint> indices) CreateSphereVertices(float radius, int sectorCount, int stackCount)
    {
        var vertices = new List<Vector3>();
        var texCoords = new List<Vector2>();
        var indices = new List<uint>();

        float x, y, z, xy;                              // vertex position
        float s, t;                                     // vertex texCoord

        float sectorStep = 2 * MathF.PI / sectorCount;
        float stackStep = MathF.PI / stackCount;
        float sectorAngle, stackAngle;

        for (int i = 0; i <= stackCount; ++i)
        {
            stackAngle = MathF.PI / 2 - i * stackStep;
            xy = radius * MathF.Cos(stackAngle);
            z = radius * MathF.Sin(stackAngle);         

            for (int j = 0; j <= sectorCount; ++j)
            {
                sectorAngle = j * sectorStep;           // starting from 0 to 2pi

                // vertex position (x, y, z)
                x = xy * MathF.Cos(sectorAngle);        // r * cos(u) * cos(v)
                y = xy * MathF.Sin(sectorAngle);        // r * cos(u) * sin(v)
                vertices.Add(new Vector3(x, y, z));

                // vertex tex coord (s, t) range between [0, 1]
                s = (float)j / sectorCount;
                t = (float)i / stackCount;
                texCoords.Add(new Vector2(s, t));
            }
        }

        // generate CCW index list of sphere triangles
        uint k1, k2;
        for (int i = 0; i < stackCount; ++i)
        {
            k1 = (uint)(i * (sectorCount + 1)); // beginning of current stack
            k2 = (uint)(k1 + sectorCount + 1);  // beginning of next stack

            for (int j = 0; j < sectorCount; ++j, ++k1, ++k2)
            {
                if (i != 0)
                {
                    indices.Add(k1);
                    indices.Add(k2);
                    indices.Add(k1 + 1);
                }

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