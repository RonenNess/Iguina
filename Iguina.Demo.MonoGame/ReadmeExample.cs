using Iguina.Defs;
using Iguina.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;

namespace Iguina.Demo.MonoGame
{
    /// <summary>
    /// Basic monogame example.
    /// </summary>
    public class IguinaMonoGameExample : Game
    {
        private GraphicsDeviceManager _graphics = null!;
        private SpriteBatch _spriteBatch = null!;
        UISystem _uiSystem = null!;

        public IguinaMonoGameExample()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = "Iguina Demo - MonoGame";
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // start demo project and provide our renderer and input provider.
            var uiThemeFolder = "../../../../Iguina.Demo/Assets/DefaultTheme";

            // create ui system
            var renderer = new MonoGameRenderer(Content, GraphicsDevice, _spriteBatch, uiThemeFolder);
            var input = new MonoGameInput();
            _uiSystem = new UISystem(Path.Combine(uiThemeFolder, "system_style.json"), renderer, input);

            // create panel with hello message
            {
                var panel = new Panel(_uiSystem);
                panel.Anchor = Defs.Anchor.Center;
                panel.Size.SetPixels(400, 400);
                _uiSystem.Root.AddChild(panel);

                var paragraph = new Paragraph(_uiSystem);
                paragraph.Text = "Hello World!";
                panel.AddChild(paragraph);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // update input and ui system
            var input = (_uiSystem.Input as MonoGameInput)!;
            input.StartFrame(gameTime);
            _uiSystem.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            input.EndFrame();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.CornflowerBlue);
            
            // render ui
            var renderer = (_uiSystem.Renderer as MonoGameRenderer)!;
            renderer.StartFrame();
            _uiSystem.Draw();
            renderer.EndFrame();

            base.Draw(gameTime);
        }
    }
}