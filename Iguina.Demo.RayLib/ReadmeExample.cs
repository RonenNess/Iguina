using Iguina.Defs;
using Iguina.Demo.RayLib;
using Iguina.Entities;
using Raylib_cs;
using static Raylib_cs.Raylib;

static class ReadmeDemo
{
    public static void Start()
    {
        // get screen resolution for demo size
        int screenWidth = 800;
        int screenHeight = 600;

        // init window
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        InitWindow(screenWidth, screenHeight, "Iguina.Demo.RayLib");

        // start demo project and provide our renderer and input provider.
        var uiThemeFolder = "../../../../Iguina.Demo/Assets/DefaultTheme";
        var renderer = new RayLibRenderer(uiThemeFolder);
        var input = new RayLibInput();
        var uiSystem = new Iguina.UISystem(Path.Combine(uiThemeFolder, "system_style.json"), renderer, input);

        // create panel with hello message
        {
            var panel = new Panel(uiSystem);
            panel.Anchor = Anchor.Center;
            panel.Size.SetPixels(400, 400);
            uiSystem.Root.AddChild(panel);

            var paragraph = new Paragraph(uiSystem);
            paragraph.Text = "Hello World!";
            panel.AddChild(paragraph);
        }

        // Main game loop
        while (!WindowShouldClose())
        {
            // begin drawing
            BeginDrawing();
            ClearBackground(Raylib_cs.Color.DarkBlue);

            // update and draw ui
            BeginMode2D(new Camera2D() { Zoom = 1f });
            renderer.StartFrame();
            uiSystem.Update(GetFrameTime());
            uiSystem.Draw();
            renderer.EndFrame();
            EndMode2D();

            // end drawing
            EndDrawing();
        }

        CloseWindow();
        Environment.Exit(0);
    }
}