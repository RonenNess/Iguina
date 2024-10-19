using Iguina.Defs;
using Iguina.Entities;


namespace Iguina.Utils
{
    /// <summary>
    /// Utilities to generate message boxes.
    /// </summary>
    public class MessageBoxUtils
    {
        // active ui system.
        UISystem _uiSystem;

        /// <summary>
        /// Create the message box utils.
        /// </summary>
        /// <param name="uiSystem">UI system to create message boxes for,</param>
        public MessageBoxUtils(UISystem uiSystem)
        {
            _uiSystem = uiSystem;
        }

        /// <summary>
        /// Button and action for a message box.
        /// </summary>
        public struct MessageBoxButtons
        {
            public string Text;
            public Action? Action;
            public Func<bool>? CancelableAction;
            public bool CloseMessageBox;
            public MessageBoxButtons(string text, Action? action = null, bool closeMessageBox = true)
            {
                Text = text;
                Action = action;
                CloseMessageBox = closeMessageBox;
            }
            public MessageBoxButtons(string text, Func<bool>? action = null, bool closeMessageBox = true)
            {
                Text = text;
                CancelableAction = action;
                CloseMessageBox = closeMessageBox;
            }
        }

        /// <summary>
        /// Message box options.
        /// </summary>
        public struct MessageBoxOptions
        {
            /// <summary>
            /// If true, message box will adjust height automatically.
            /// </summary>
            public bool AutoHeight;

            /// <summary>
            /// Message box size (note: Y is ignored is AutoHeight is true).
            /// </summary>
            public Point Size;

            /// <summary>
            /// If true, will make the message box draggable.
            /// </summary>
            public bool Draggable;

            /// <summary>
            /// If true, will add backdrop entity to block interaction with background entities.
            /// </summary>
            public bool AddBackdrop;

            public MessageBoxOptions(bool autoHeight, Point size, bool draggable, bool addBackdrop)
            {
                AutoHeight = autoHeight;
                Size = size;
                Draggable = draggable;
                AddBackdrop = addBackdrop;
            }
        }

        /// <summary>
        /// Default message box options to use when options are not provided.
        /// </summary>
        public MessageBoxOptions DefaultOptions = new MessageBoxOptions(true, new Point(700, 600), true, true);

        /// <summary>
        /// Contains all the created entities for a message box, returned to caller.
        /// </summary>
        public struct MessageBoxHandle
        {
            /// <summary>
            /// Backdrop entity (the part that hides the background).
            /// </summary>
            public Entity Backdrop;

            /// <summary>
            /// Message box main panel.
            /// </summary>
            public Panel MessageBoxPanel;

            /// <summary>
            /// An empty entity to contain entities above the bottom buttons.
            /// </summary>
            public Entity ContentContainer;

            /// <summary>
            /// Message box buttons.
            /// </summary>
            public Button[] Buttons;

            /// <summary>
            /// Close the message box.
            /// </summary>
            public void Close()
            {
                Backdrop.RemoveSelf();
                MessageBoxPanel.RemoveSelf();
            }
        }

        /// <summary>
        /// Show a message box.
        /// </summary>
        /// <param name="title">Message box title.</param>
        /// <param name="text">Message box text.</param>
        /// <param name="buttons">Message box options.</param>
        /// <param name="options">Message box options, or null to use defaults.</param>
        /// <returns>Newly created message box handle.</returns>
        MessageBoxHandle ShowMessageBox(string title, string text, MessageBoxButtons[] buttons, MessageBoxOptions? options = null)
        {
            // get default options
            options = options ?? DefaultOptions;

            // reset focused entity
            _uiSystem.FocusedEntity = null;

            // create the message box backdrop
            Entity backdrop = null!;
            if (options.Value.AddBackdrop)
            {
                backdrop = new Entity(_uiSystem, _uiSystem.DefaultStylesheets.MessageBoxBackdrop);
                backdrop.Size.SetPercents(100f, 100f);
                backdrop.Anchor = Anchor.Center;
                backdrop.Locked = true;
                backdrop.Identifier = "Message-Box-Backdrop";
                _uiSystem.Root.AddChild(backdrop);
            }

            // create the message box panel
            var panel = new Panel(_uiSystem, _uiSystem.DefaultStylesheets.MessageBoxPanels ?? _uiSystem.DefaultStylesheets.Panels);
            var size = options.Value.Size;
            panel.Size.SetPixels(size.X, size.Y);
            panel.AutoHeight = options.Value.AutoHeight;
            panel.Anchor = Anchor.Center;
            panel.Identifier = "Message-Box-Panel";

            // make draggable
            if (options.Value.Draggable)
            {
                panel.DraggableMode = DraggableMode.DraggableConfinedToScreen;
            }

            // add title
            panel.AddChild(new Title(_uiSystem, _uiSystem.DefaultStylesheets.MessageBoxTitles ?? _uiSystem.DefaultStylesheets.Titles ?? _uiSystem.DefaultStylesheets.Paragraphs, title));
            panel.AddChild(new HorizontalLine(_uiSystem));

            // add text
            panel.AddChild(new Paragraph(_uiSystem, _uiSystem.DefaultStylesheets.MessageBoxParagraphs ?? _uiSystem.DefaultStylesheets.Paragraphs, text));
            panel.AddChild(new RowsSpacer(_uiSystem));

            // empty container for optional content
            var contentContainer = panel.AddChild(new Entity(_uiSystem, null));
            contentContainer.Anchor = Anchor.AutoCenter;
            contentContainer.Size.X.SetPercents(100f);
            contentContainer.AutoHeight = true;
            contentContainer.Identifier = "Message-Box-Content";

            // add buttons
            var optionsPanel = panel.AddChild(new Panel(_uiSystem, null!));
            optionsPanel.AutoHeight = true;
            optionsPanel.AutoWidth = true;
            optionsPanel.Size.X.SetPercents(100f);
            optionsPanel.Anchor = Anchor.AutoCenter;
            optionsPanel.Identifier = "Message-Box-Options-Panel";
            var buttonWidth = MathF.Floor((float)size.X / buttons.Length) - 20;
            var buttonsList = new List<Button>();
            foreach (var option in buttons)
            {
                var button = optionsPanel.AddChild(new Button(_uiSystem, _uiSystem.DefaultStylesheets.MessageBoxButtons ?? _uiSystem.DefaultStylesheets.Buttons, option.Text));
                button.Anchor = Anchor.AutoInlineLTR;
                button.Size.X.SetPixels((int)buttonWidth);
                button.Events.OnClick = (ent) =>
                {
                    // invoke action that can't be cancelled
                    option.Action?.Invoke();

                    // invoke action that can be cancelled and if returned false, stop here
                    if (option.CancelableAction?.Invoke() == false)
                    {
                        return;
                    }

                    // should close message box?
                    if (option.CloseMessageBox)
                    {
                        panel.RemoveSelf();
                        backdrop?.RemoveSelf();
                    }
                };
                buttonsList.Add(button);
            }

            // add message box panel
            _uiSystem.Root.AddChild(panel);

            // return handle
            return new MessageBoxHandle()
            {
                Backdrop = backdrop,
                Buttons = buttonsList.ToArray(),
                ContentContainer = contentContainer,
                MessageBoxPanel = panel
            };
        }

        /// <summary>
        /// Show a message box with confirm / cancel buttons.
        /// </summary>
        /// <param name="title">Message box title.</param>
        /// <param name="text">Message box text.</param>
        /// <param name="onConfirm">Action for confirmation.</param>
        /// <param name="onCancel">Action for cancel.</param>
        /// <param name="confirmText">Text to show on confirm button.</param>
        /// <param name="cancelText">Text to show on cancel button.</param>
        /// <param name="options">Message box options, or null to use defaults.</param>
        /// <returns>Newly created message box handle.</returns>
        public MessageBoxHandle ShowConfirmMessageBox(string title, string text, Action? onConfirm = null, Action? onCancel = null, string confirmText = "Confirm", string cancelText = "Cancel", MessageBoxOptions? options = null)
        {
            return ShowMessageBox(title, text, new MessageBoxButtons[]
            {
                new MessageBoxButtons(confirmText, onConfirm),
                new MessageBoxButtons(cancelText, onCancel)
            }, options);
        }

        /// <summary>
        /// Show a message box with just OK button.
        /// </summary>
        /// <param name="title">Message box title.</param>
        /// <param name="text">Message box text.</param>
        /// <param name="onConfirm">Action for confirmation.</param>
        /// <param name="buttonText">Text to show on confirm button.</param>
        /// <param name="options">Message box options, or null to use defaults.</param>
        /// <returns>Newly created message box handle.</returns>
        public MessageBoxHandle ShowInfoMessageBox(string title, string text, Action? onConfirm = null, string buttonText = "OK", MessageBoxOptions? options = null)
        {
            return ShowMessageBox(title, text, new MessageBoxButtons[]
            {
                new MessageBoxButtons(buttonText, onConfirm)
            }, options);
        }

        /// <summary>
        /// Show a dialog to save file.
        /// This dialog will show a list with available files in folder and allow the user to pick either existing file name or a new file name. 
        /// </summary>
        /// <param name="title">Message box title.</param>
        /// <param name="text">Message box text.</param>
        /// <param name="onConfirm">Action for confirmation. Return true to close the files dialog, or false to keep files dialog opened.</param>
        /// <param name="onCancel">Action for cancel.</param>
        /// <param name="startingFolder">Folder to open dialog from (or null for working directory).</param>
        /// <param name="fileDialogOptions">Additional options for files dialog.</param>
        /// <param name="filesFilter">Optional filter to apply on files to determine if to present them or not.</param>
        /// <param name="confirmText">Text to show on confirm button.</param>
        /// <param name="cancelText">Text to show on cancel button.</param>
        /// <param name="rootLabel">If limited to not allow escaping starting folder, this is the label the dialog box will show as the root folder. If not set, will use starting folder name.</param>
        /// <param name="options">Message box options, or null to use defaults.</param>
        /// <returns>Newly created message box handle.</returns>
        public MessageBoxHandle ShowSaveFileDialog(string title, string text, Func<bool>? onConfirm = null, Action? onCancel = null, 
            string? startingFolder = null, Func<string, bool>? filesFilter = null, FileDialogOptions fileDialogOptions = DefaultSaveFileOptions, 
            string confirmText = "Save File", string cancelText = "Cancel", string? rootLabel = null, MessageBoxOptions? options = null)
        {
            // show confirmation dialog box
            var ret = ShowMessageBox(title, text, new MessageBoxButtons[]
            {
                new MessageBoxButtons(confirmText, onConfirm),
                new MessageBoxButtons(cancelText, onCancel)
            }, options);

            // get files dialog root folder and current folder
            string rootFolder = Path.GetFullPath(startingFolder ?? Directory.GetCurrentDirectory());
            string currFolder = rootFolder;

            // root folder name
            string rootFolderName = rootLabel ?? Path.GetFileName(currFolder) ?? ".";

            // create files / directories list
            ListBox filesList = new ListBox(_uiSystem);

            // create entity to show full path
            Paragraph fullPathLabel = new Paragraph(_uiSystem);
            fullPathLabel.Visible = fileDialogOptions.HasFlag(FileDialogOptions.ShowFullPath);

            // selected file name
            TextInput selectedFilename = new TextInput(_uiSystem);

            // rebuild files list
            void RebuildFilesList()
            {
                // update full path label
                bool canEscapeRoot = fileDialogOptions.HasFlag(FileDialogOptions.AllowEscapingRootFolder);
                fullPathLabel.Text = canEscapeRoot ? currFolder : (rootFolderName + currFolder.Substring(rootFolder.Length));

                // clear previous values
                filesList.Clear();

                // add folders data
                if (fileDialogOptions.HasFlag(FileDialogOptions.ShowFolders))
                {
                    // add going up folder item (..)
                    if (fileDialogOptions.HasFlag(FileDialogOptions.AllowGoingUpFolders) && 
                    (canEscapeRoot || (currFolder != rootFolder)))
                    {  
                        filesList.AddItem("..");
                    }

                    // add folders
                    var icon = _uiSystem.GetSystemIcon("folder");
                    var folders = Directory.GetDirectories(currFolder);
                    foreach (var folder in folders)
                    {
                        // apply filter
                        if (filesFilter?.Invoke(folder) == false)
                        {
                            continue;
                        }

                        // add folder
                        var folderName = Path.GetFileName(folder);
                        filesList.AddItem(folder, folderName);
                        if (icon != null)
                        {
                            filesList.SetItemLabel(folder, folderName, icon!, false);
                        }
                    }
                }

                // add files data
                if (fileDialogOptions.HasFlag(FileDialogOptions.ShowFiles))
                {
                    // add files
                    var icon = _uiSystem.GetSystemIcon("file");
                    var files = Directory.GetFiles(currFolder);
                    foreach (var file in files)
                    {
                        // apply filter
                        if (filesFilter?.Invoke(file) == false)
                        {
                            continue;
                        }

                        // add folder
                        var fileName = Path.GetFileName(file);
                        filesList.AddItem(file, fileName);
                        if (icon != null)
                        {
                            filesList.SetItemLabel(file, fileName, icon!, false);
                        }
                    }
                }
            }

            // build starting files
            RebuildFilesList();

            // add action for files list
            filesList.Events.OnValueChanged = (Entity ent) =>
            {
                // skip if not selected
                if (filesList.SelectedIndex == -1)
                {
                    return;
                }

                // go folder up
                if (filesList.SelectedText == "..")
                {
                    currFolder = Path.GetFullPath(Path.Combine(currFolder, ".."));
                    RebuildFilesList();
                    return;
                }

                // change folder
                if (Directory.Exists(filesList.SelectedValue))
                {
                    currFolder = filesList.SelectedValue;
                    RebuildFilesList();
                    return;
                }

                // set selected file
                selectedFilename.Value = filesList.SelectedText ?? selectedFilename.Value;
            };

            // add files list to message box
            ret.ContentContainer.AddChild(fullPathLabel);
            ret.ContentContainer.AddChild(filesList);
            ret.ContentContainer.AddChild(selectedFilename);

            // return message box handle
            return ret;
        }

        /// <summary>
        /// Additional options for files dialog boxes.
        /// </summary>
        [Flags]
        public enum FileDialogOptions
        {
            /// <summary>
            /// If set, will allow users to go up one folder.
            /// </summary>
            AllowGoingUpFolders = 1 << 0,

            /// <summary>
            /// If set, will allow users to 'escape' the first folder we started in.
            /// </summary>
            AllowEscapingRootFolder = 1 << 1,

            /// <summary>
            /// If set, selected filename must exist.
            /// </summary>
            FileMustExist = 1 << 2,

            /// <summary>
            /// If set, selected filename must not exist.
            /// </summary>
            FileMustNotExist = 1 << 3,

            /// <summary>
            /// If set, will show folders in dialog box.
            /// </summary>
            ShowFolders = 1 << 4,

            /// <summary>
            /// If set, will show files in dialog box.
            /// </summary>
            ShowFiles = 1 << 5,

            /// <summary>
            /// If set, will always show current full path above the files list.
            /// </summary>
            ShowFullPath = 1 << 6
        }

        /// <summary>
        /// Default flags for saving files dialog box.
        /// </summary>
        public const FileDialogOptions DefaultSaveFileOptions = FileDialogOptions.AllowGoingUpFolders | FileDialogOptions.ShowFolders | FileDialogOptions.ShowFiles | FileDialogOptions.ShowFullPath;
    }
}
