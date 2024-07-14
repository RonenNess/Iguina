using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace Iguina.Demo.MonoGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics = null!;
        private SpriteBatch _spriteBatch = null!;
        MonoGameRenderer _renderer = null!;
        MonoGameInput _input = null!;
        IguinaDemoStarter _demo = null!;

        public Game1()
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

            // create demo
            _demo = new IguinaDemoStarter();
            _renderer = new MonoGameRenderer(Content, GraphicsDevice, _spriteBatch, uiThemeFolder);
            _input = new MonoGameInput();
            _demo.Start(_renderer, _input, uiThemeFolder);

            // set maximized
            {
                int _ScreenWidth = GraphicsDevice.Adapter.CurrentDisplayMode.Width;
                int _ScreenHeight = GraphicsDevice.Adapter.CurrentDisplayMode.Height;
                _graphics.PreferredBackBufferWidth = (int)_ScreenWidth;
                _graphics.PreferredBackBufferHeight = (int)_ScreenHeight;
                IsMouseVisible = false;
                Window.AllowUserResizing = true;
                _graphics.IsFullScreen = false;
                _graphics.ApplyChanges();
                Window.AllowUserResizing = true;
                Window.IsBorderless = false;
                Window.Position = new Point(0, 0);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _input.StartFrame(gameTime);
            _demo.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            _input.EndFrame();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            _renderer.StartFrame();
            _demo.Draw();
            _renderer.EndFrame();

            base.Draw(gameTime);
        }
    }
}