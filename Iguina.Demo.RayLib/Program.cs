using Iguina.Demo;
using Iguina.Demo.RayLib;
using Raylib_cs;
using static Raylib_cs.Raylib;

// uncomment this to play the example from the readme.md file
//ReadmeDemo.Start();

// get screen resolution for demo size
int screenWidth = Raylib_cs.Raylib.GetScreenWidth();
int screenHeight = Raylib_cs.Raylib.GetScreenHeight();

// init window
Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
InitWindow(screenWidth, screenHeight, "Iguina Demo - RayLib");
//SetWindowState(ConfigFlags.BorderlessWindowMode);
MaximizeWindow();
SetTargetFPS(0);

// start demo project and provide our renderer and input provider.
var uiThemeFolder = "../../../../Iguina.Demo/Assets/DefaultTheme";
var demo = new IguinaDemoStarter();
var renderer = new RayLibRenderer(uiThemeFolder);
var input = new RayLibInput();
demo.Start(renderer, input, uiThemeFolder);

// Main game loop
while (!WindowShouldClose())
{
    // begin drawing
    BeginDrawing();
    ClearBackground(Color.DarkBlue);

    // show / hide cursor
    if (IsWindowFocused()) { HideCursor(); }
    else { ShowCursor(); }

    // update and draw ui
    BeginMode2D(new Camera2D() { Zoom = 1f });
    renderer.StartFrame();
    demo.Update(GetFrameTime());
    demo.Draw();
    renderer.EndFrame();
    EndMode2D();

    // end drawing
    EndDrawing();
}

CloseWindow();
Environment.Exit(0);