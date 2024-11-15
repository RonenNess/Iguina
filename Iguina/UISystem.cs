using Iguina.Defs;
using Iguina.Drivers;
using Iguina.Entities;
using Iguina.Utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;


namespace Iguina
{
    /// <summary>
    /// A GUI system instance.
    /// </summary>
    public class UISystem
    {
        /// <summary>
        /// Renderer implementation.
        /// </summary>
        public IRenderer Renderer { get; private set; } 

        /// <summary>
        /// Input provider implementation.
        /// </summary>
        public IInputProvider Input { get; private set; }

        /// <summary>
        /// Total elapsed time this system is running, in seconds.
        /// </summary>
        public double ElapsedTime { get; private set; }

        /// <summary>
        /// Last update frame delta time, in seconds.
        /// </summary>
        public float LastDeltaTime { get; private set; }

        /// <summary>
        /// If true, will render UI cursor (if defined in stylesheet).
        /// </summary>
        public bool ShowCursor = true;

        /// <summary>
        /// If true, will auto-focus entities the user interacts with.
        /// If false, entities will never be focused nor respond to keyboard interactions, unless you explicitly set the focused entity via code.
        /// </summary>
        public bool AutoFocusEntities = true;

        /// <summary>
        /// Default stylesheets to use for different entity types when no stylesheet is provided.
        /// </summary>
        public class _DefaultStylesheets
        {
            public StyleSheet? Panels;
            public StyleSheet? MessageBoxPanels;
            public StyleSheet? MessageBoxParagraphs;
            public StyleSheet? MessageBoxTitles;
            public StyleSheet? MessageBoxButtons;
            public StyleSheet? MessageBoxBackdrop;
            public StyleSheet? Paragraphs;
            public StyleSheet? Titles;
            public StyleSheet? Labels;
            public StyleSheet? Buttons;
            public StyleSheet? HorizontalLines;
            public StyleSheet? VerticalLines;
            public StyleSheet? CheckBoxes;
            public StyleSheet? RadioButtons;
            public StyleSheet? HorizontalSliders;
            public StyleSheet? HorizontalSlidersHandle;
            public StyleSheet? VerticalSliders;
            public StyleSheet? VerticalSlidersHandle;
            public StyleSheet? HorizontalColorSliders;
            public StyleSheet? HorizontalColorSlidersHandle;
            public StyleSheet? VerticalColorSliders;
            public StyleSheet? VerticalColorSlidersHandle;
            public StyleSheet? ListPanels;
            public StyleSheet? ListItems;
            public StyleSheet? DropDownPanels;
            public StyleSheet? DropDownItems;
            public StyleSheet? DropDownIcon;
            public StyleSheet? VerticalScrollbars;
            public StyleSheet? VerticalScrollbarsHandle;
            public StyleSheet? TextInput;
            public StyleSheet? NumericTextInput;
            public StyleSheet? NumericTextInputButton;
            public StyleSheet? HorizontalProgressBars;
            public StyleSheet? HorizontalProgressBarsFill;
            public StyleSheet? VerticalProgressBars;
            public StyleSheet? VerticalProgressBarsFill;
            public StyleSheet? ColorPickers;
            public StyleSheet? ColorPickersHandle;
        }
        public _DefaultStylesheets DefaultStylesheets = new();

        /// <summary>
        /// Default message box utils instance attached to this UI system.
        /// </summary>
        public MessageBoxUtils MessageBoxes { get; private set; }

        /// <summary>
        /// Currently-targeted entity (entity we point on with the cursor).
        /// </summary>
        public Entity? TargetedEntity { get; private set; }

        /// <summary>
        /// Get 'TargetEntity' but only if its an interactable entity.
        /// If its not, will return null instead.
        /// </summary>
        public Entity? InteractableTargetedEntity => (TargetedEntity != null && TargetedEntity.Interactable && !TargetedEntity.IgnoreInteractions) ? TargetedEntity : null;

        /// <summary>
        /// Currently focused entity, and the entity that will receive keyboard interactions.
        /// Will be set to the last entity interacted with.
        /// </summary>
        public Entity? FocusedEntity { get; set; }

        /// <summary>
        /// System-level stylesheet.
        /// Define properties like cursor graphics and general stuff.
        /// </summary>
        public SystemStyleSheet SystemStyleSheet = new SystemStyleSheet();

        /// <summary>
        /// If true, will debug-render entities.
        /// </summary>
        public bool DebugRenderEntities = false;

        /// <summary>
        /// Entity events you can register to.
        /// These events will trigger for any entity in the system.
        /// </summary>
        public EntityEvents Events;

        /// <summary>
        /// Scale all the texts in this UI system.
        /// </summary>
        public float TextsScale => SystemStyleSheet.TextScale;

        /// <summary>
        /// Root entity.
        /// All child entities should be added to this object.
        /// </summary>
        public Panel Root { get; private set; }

        // queue of scissor regions to cut-off rendering when overflow mode is hidden.
        internal Queue<Rectangle> _scissorRegionQueue = new();

        /// <summary>
        /// If set, will use this cursor properties regardless of interface state or entity the user points on.
        /// </summary>
        public CursorProperties? OverrideCursorProperties;

        /// <summary>
        /// When entities turn into interactive state (for example a button is clicked on), it will be locked in this state for at least this time, in seconds.
        /// This property is useful to make sure the interactive state is properly shown, even if user perform very rapid short clicks.
        /// </summary>
        /// <remarks>This property is especially important when there's interpolation on texture change, and switching to interactive state is not immediate.</remarks>
        internal float TimeToLockInteractiveState => SystemStyleSheet.TimeToLockInteractiveState;

        // actions to perform next update call
        ConcurrentBag<Action> _actionsToPerformNextUpdate = new();

        // store the thread id the UI system was created on
        int _ownerThreadId;

        /// <summary>
        /// If true and the UI system detect access from multiple threads, will assert in debug mode.
        /// If false will not validate thread ids at all.
        /// Regardless of this setting, will not assert in release mode.
        /// </summary>
        public bool ValidateThreadSafety = true;

        /// <summary>
        /// Create the UI system, with stylesheet, renderer and input manager provided by the host application.
        /// </summary>
        /// <remarks>
        /// For an example of renderer and input implementation via RayLib, check out the example project.
        /// </remarks>
        /// <param name="styleSheetFilePath">UI System stylesheet file path.</param>
        /// <param name="renderer">Renderer provider, to draw UI elements.</param>
        /// <param name="input">Input provider, to get mouse-like and keyboard input.</param>
        public UISystem(string styleSheetFilePath, IRenderer renderer, IInputProvider input) 
            : this(renderer, input)
        {
            try
            {
                SystemStyleSheet = JsonSerializer.Deserialize<SystemStyleSheet>(File.ReadAllText(styleSheetFilePath))!;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to read or deserialize UI system stylesheet!", e);
            }

            if (SystemStyleSheet.LoadDefaultStylesheets != null)
            {
                LoadDefaultStylesheets(SystemStyleSheet.LoadDefaultStylesheets, Path.GetDirectoryName(styleSheetFilePath) ?? string.Empty);
            }
        }

        /// <summary>
        /// Create the UI system, with stylesheet, renderer and input manager provided by the host application.
        /// </summary>
        /// <remarks>
        /// For an example of renderer and input implementation via RayLib, check out the example project.
        /// </remarks>
        /// <param name="styleSheet">UI System stylesheet.</param>
        /// <param name="stylesheetsFolder">If the UI stylesheet loads any additional stylesheet files, this will be the folder to load them from. If null, will use current working dir.</param>
        /// <param name="renderer">Renderer provider, to draw UI elements.</param>
        /// <param name="input">Input provider, to get mouse-like and keyboard input.</param>
        public UISystem(SystemStyleSheet styleSheet, string? stylesheetsFolder, IRenderer renderer, IInputProvider input) 
            : this(renderer, input)
        {
            SystemStyleSheet = styleSheet;
            if (SystemStyleSheet.LoadDefaultStylesheets != null)
            {
                LoadDefaultStylesheets(SystemStyleSheet.LoadDefaultStylesheets, stylesheetsFolder ?? string.Empty);
            }
        }

        /// <summary>
        /// Create the UI system, with renderer and input manager provided by the host application.
        /// </summary>
        /// <remarks>
        /// For an example of renderer and input implementation via RayLib, check out the example project.
        /// </remarks>
        /// <param name="renderer">Renderer provider, to draw UI elements.</param>
        /// <param name="input">Input provider, to get mouse-like and keyboard input.</param>
        public UISystem(IRenderer renderer, IInputProvider input) 
        {
            // store renderer and input
            Renderer = renderer;
            Input = input;

            // create root entity
            Root = new Panel(this, new StyleSheet()) { Identifier = "Root" };

            // get owner thread id
            _ownerThreadId = Thread.CurrentThread.ManagedThreadId;

            // create message box utils
            MessageBoxes = new MessageBoxUtils(this);
        }

        /// <summary>
        /// Get system icon from UI system stylesheet.
        /// </summary>
        /// <param name="id">Icon id.</param>
        /// <returns>Icon settings, or null if icon is not defined in stylesheet.</returns>
        public IconTexture? GetSystemIcon(string id)
        {
            if (SystemStyleSheet.SystemIcons?.TryGetValue(id, out var icon) ?? false)
            {
                return icon;
            }
            return null;
        }

        /// <summary>
        /// Validate current thread id is the same thread that created the UI system.
        /// If false, raise assert in debug mode.
        /// </summary>
        internal void ValidateThreadId()
        {
            if (ValidateThreadSafety)
            {
                Debug.Assert(_ownerThreadId == Thread.CurrentThread.ManagedThreadId, "Iguina detected multi-thread access! To use the UI system from multiple threads, please use 'InvokeOnUIThread' (or if you sync threads yourself and think this validation is not needed, set 'ValidateThreadSafety' to false).");
            }
        }

        /// <summary>
        /// Load all default stylesheets from dictionary.
        /// </summary>
        /// <param name="stylesheetsToLoad">Stylesheets to load. Key = stylesheet entity name, Value = path to load from.</param>
        /// <param name="parentFolder">Folder to load stylesheet files from.</param>
        public void LoadDefaultStylesheets(Dictionary<string, string> stylesheetsToLoad, string parentFolder)
        {
            foreach (var pair in stylesheetsToLoad)
            {
                var entityStyleName = pair.Key;
                var path = pair.Value;

                var field = DefaultStylesheets.GetType().GetField(entityStyleName);
                if (field == null)
                {
                    throw new FormatException($"Error loading stylesheet for entity style id '{entityStyleName}': entity key not found under 'DefaultStylesheets'.");
                }

                var fullPath = Path.Combine(parentFolder, path);
                try
                {
                    var stylesheet = StyleSheet.LoadFromJsonFile(fullPath);
                    field.SetValue(DefaultStylesheets, stylesheet);
                }
                catch (FileNotFoundException)
                {
                    throw new FormatException($"Error loading stylesheet for entity style id '{entityStyleName}': stylesheet file '{fullPath}' not found!");
                }
            }
        }

        /// <summary>
        /// Add an action to call at the beginning of the next update call, on the UI thread.
        /// </summary>
        /// <param name="callback">Callback to execute.</param>
        public void InvokeOnUIThread(Action callback)
        {
            _actionsToPerformNextUpdate.Add(callback);
        }

        /// <summary>
        /// Perform UI updates.
        /// Must be called every update frame in your game main loop.
        /// </summary>
        /// <param name="deltaTime">Delta time, since last update call, in seconds.</param>
        public void Update(float deltaTime)
        {
            // validate thread id
            ValidateThreadId();

            // update elapsed time
            LastDeltaTime = deltaTime;
            ElapsedTime += deltaTime;

            // set root to cover entire screen
            var screenBounds = Renderer.GetScreenBounds();
            Root.Size.SetPixels(screenBounds.Width, screenBounds.Height);

            // run methods to execute on update
            while (_actionsToPerformNextUpdate.TryTake(out Action? cb))
            {
                cb?.Invoke();
            }

            // update all entities
            Root._DoUpdate(deltaTime);

            // check if should lock target entity
            bool keepTargetEntity = (TargetedEntity != null) ? 
                (TargetedEntity.LockTargetedEntityOnSelf && TargetedEntity.IsCurrentlyVisible() && !TargetedEntity.IsCurrentlyLocked() && !TargetedEntity.IsCurrentlyDisabled()) 
                : false;

            // also lock target if mouse is held down and target entity is set to lock focus while mouse is down
            if (Input.IsMouseButtonDown(MouseButton.Left) && (TargetedEntity != null) && TargetedEntity.LockFocusWhileMouseDown)
            {
                keepTargetEntity = true;
            }

            // current mouse position
            var cp = Input.GetMousePosition();

            // find new entity we target
            if (!keepTargetEntity)
            {
                // reset target entity
                TargetedEntity = null;

                // iterate all entities to see which entity we point on
                List<Entity> entitiesToPostProcess = new List<Entity>();
                Root.Walk((Entity entity) =>
                {
                    // skip entities that are without interactions
                    // note: we don't want to skip locked or disabled entities because they can still 'block' other entities.
                    if (entity.IgnoreInteractions)
                    {
                        return true;
                    }

                    // check if entity can't get focused while mouse is down
                    if (!entity.CanGetFocusWhileMouseIsDown && Input.IsMouseButtonDown(MouseButton.Left))
                    {
                        return true;
                    }

                    // check if top most interactions
                    if (entity.TopMostInteractions)
                    {
                        entitiesToPostProcess.Add(entity);
                        return true;
                    }

                    // check if we point on this entity
                    if (entity.IsCurrentlyVisible() && entity.IsPointedOn(cp))
                    {
                        TargetedEntity = entity;
                    }

                    // continue iteration
                    return true;
                });

                // do top-most interactions
                foreach (var entity in entitiesToPostProcess)
                {
                    if (entity.IsCurrentlyVisible() && entity.IsPointedOn(cp))
                    {
                        TargetedEntity = entity;
                    }
                }
            }

            // calculate current input state
            var currInputState = new CurrentInputState()
            {
                MousePosition = cp,
                LeftMouseButton = Input.IsMouseButtonDown(MouseButton.Left),
                RightMouseButton = Input.IsMouseButtonDown(MouseButton.Right),
                WheelMouseButton = Input.IsMouseButtonDown(MouseButton.Wheel),
                MouseWheelChange = Input.GetMouseWheelChange(),
                TextInput = Input.GetTextInput(),
                TextInputCommands = Input.GetTextInputCommands(),
                KeyboardInteraction = Input.GetKeyboardInteraction()
            };
            var inputState = new InputState()
            {
                _Previous = _lastInputState,
                _Current = currInputState,
                ScreenBounds = Renderer.GetScreenBounds()
            };
            _lastInputState = currInputState;
            CurrentInputState = inputState;

            // do interactions with targeted entity
            // unless its locked or disabled
            if (TargetedEntity != null)
            {
                // pass interactions forward if needed to
                if (TargetedEntity.TransferInteractionsTo != null)
                {
                    TargetedEntity = TargetedEntity.TransferInteractionsTo;
                }

                // perform interactions and set focused entity
                if (!TargetedEntity.IsCurrentlyLocked() && !TargetedEntity.IsCurrentlyDisabled())
                {
                    TargetedEntity.DoInteractions(inputState);
                    if (AutoFocusEntities)
                    {
                        if (inputState.LeftMouseDown || inputState.RightMouseDown || inputState.WheelMouseDown)
                        {
                            FocusedEntity = TargetedEntity;
                        }
                    }
                }
            }
            // no entity we interact with?
            else
            {
                if (AutoFocusEntities)
                {
                    if (inputState.LeftMouseDown || inputState.RightMouseDown || inputState.WheelMouseDown)
                    {
                        FocusedEntity = null;
                    }
                }
            }
            
            // pass focus to other entity if needed, and also if focused entity is disabled / locked / invisible, remove focused
            if (FocusedEntity != null)
            {
                while (FocusedEntity.PassFocusTo != null)
                {
                    FocusedEntity = FocusedEntity.PassFocusTo;
                }

                if (!FocusedEntity.IsCurrentlyVisible() || FocusedEntity.IsCurrentlyDisabled() || FocusedEntity.IsCurrentlyLocked() || !FocusedEntity.Interactable)
                {
                    FocusedEntity = null;
                }
            }

            // perform special interactions on focused entity
            FocusedEntity?.DoFocusedEntityInteractions(inputState);

            // do post interactions
            Root.Walk((Entity entity) =>
            {
                if (entity.Interactable)
                {
                    entity.PostUpdate(inputState);
                }
                return true;
            });
        }

        // last frame input state
        CurrentInputState _lastInputState;

        /// <summary>
        /// Get current input state.
        /// </summary>
        public InputState CurrentInputState { get; private set; }

        /// <summary>
        /// Add action to call after rendering entities.
        /// </summary>
        internal void RunAfterDrawingEntities(Action action)
        {
            _postDrawActions.Add(action);
        }
        List<Action> _postDrawActions = new List<Action>();

        /// <summary>
        /// Render the UI.
        /// Must be called every draw frame in your game rendering loop.
        /// </summary>
        /// <remarks>
        /// Clearing screen, setting render target, or using a 'camera' like object for zooming, is up to the host application.
        /// </remarks>
        public void Draw()
        {
            // validate thread id
            ValidateThreadId();

            // reset scissors queue
            _scissorRegionQueue.Clear();
            Renderer.ClearScissorRegion();

            // draw all entities
            var screenRect = Renderer.GetScreenBounds();
            var rootDrawResult = new Entity.DrawMethodResult()
            {
                BoundingRect = screenRect,
                InternalBoundingRect = screenRect
            };
            Root._DoDraw(rootDrawResult, null, false);

            // call post-draw actions
            if (_postDrawActions.Count > 0)
            {
                foreach (var action in _postDrawActions)
                {
                    action();
                }
                _postDrawActions.Clear();
            }

            // get which cursor to render
            CursorProperties? cursor = OverrideCursorProperties ?? SystemStyleSheet.CursorDefault;
            if (OverrideCursorProperties == null)
            {
                if (TargetedEntity?.IsPointedOn(Input.GetMousePosition(), true) ?? false)
                {
                    if (TargetedEntity.CursorStyle != null)
                    {
                        cursor = TargetedEntity.CursorStyle;
                    }
                    else if (TargetedEntity.IsCurrentlyDisabled())
                    {
                        cursor = SystemStyleSheet.CursorDisabled ?? SystemStyleSheet.CursorDefault;
                    }
                    else if (TargetedEntity.IsCurrentlyLocked())
                    {
                        cursor = SystemStyleSheet.CursorLocked ?? SystemStyleSheet.CursorDefault;
                    }
                    else if (TargetedEntity.Interactable)
                    {
                        cursor = SystemStyleSheet.CursorInteractable ?? SystemStyleSheet.CursorDefault;
                    }
                }
            }

            // debug draw stuff
            if (DebugRenderEntities)
            {
                Root.DebugDraw(true);
            }

            // render cursor
            if (ShowCursor && cursor != null)
            {
                var scale = cursor.Scale * SystemStyleSheet.CursorScale;
                var destRect = cursor.SourceRect;
                destRect.X = Input.GetMousePosition().X + (int)(cursor.Offset.X * scale);
                destRect.Y = Input.GetMousePosition().Y + (int)(cursor.Offset.Y * scale);
                destRect.Width = (int)(destRect.Width * scale);
                destRect.Height = (int)(destRect.Height * scale);
                Renderer.DrawTexture(cursor.EffectIdentifier, cursor.TextureId, destRect, cursor.SourceRect, cursor.FillColor);
            }
        }
    }
}
