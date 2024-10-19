using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// Entity type that have 'Checked' property that can be checked / unchecked.
    /// </summary>
    public abstract class CheckedEntity : Entity
    {
        /// <summary>
        /// Set / get if this entity is in 'checked' state.
        /// Useful for buttons anc checkboxes, but can be set for any type of entity.
        /// </summary>
        public virtual bool Checked
        {
            get => _isChecked;
            set
            {
                // change checked state
                if (value != _isChecked)
                {
                    // uncheck siblings
                    if (ExclusiveSelection)
                    {
                        Parent?.IterateChildren((Entity entity) =>
                        {
                            var siblingAsCheckable = entity as CheckedEntity;
                            if ((siblingAsCheckable != null) && (siblingAsCheckable != this) && (siblingAsCheckable.ExclusiveSelection))
                            {
                                siblingAsCheckable.Checked = false;
                            }
                            return true;
                        });
                    }

                    // set value
                    _isChecked = value;

                    // invoke value change callbacks
                    Events.OnValueChanged?.Invoke(this);
                    UISystem.Events.OnValueChanged?.Invoke(this);

                    // invoke checked / unchecked callbacks
                    if (_isChecked)
                    {
                        Events.OnChecked?.Invoke(this);
                        UISystem.Events.OnChecked?.Invoke(this);
                    }
                    else
                    {
                        Events.OnUnchecked?.Invoke(this);
                        UISystem.Events.OnUnchecked?.Invoke(this);
                    }
                }
            }
        }

        /// <summary>
        /// If true, this entity will check / uncheck itself when clicked on.
        /// </summary>
        public bool ToggleCheckOnClick = false;

        /// <summary>
        /// If true, when this entity is checked, all its direct siblings with this property will be automatically unchecked.
        /// This is used for things like radio button where only one option can be checked at any given moment.
        /// </summary>
        public bool ExclusiveSelection = false;

        /// <summary>
        /// If false, it will be impossible to uncheck this entity once its checked by clicking on it.
        /// However, if the 'ExclusiveSelection' is set, you can still uncheck it by checking another sibling.
        /// </summary>
        public bool CanClickToUncheck = true;

        /// <inheritdoc/>
        public CheckedEntity(UISystem system, StyleSheet? stylesheet) : base(system, stylesheet)
        {
        }

        /// <summary>
        /// Toggle this entity checked state.
        /// </summary>
        public void ToggleCheckedState()
        {
            // disable unchecking
            if (!CanClickToUncheck && Checked)
            {
                return;
            }

            // toggle checked mode
            Checked = !Checked;
        }

        /// <inheritdoc/>
        internal override void DoInteractions(InputState inputState)
        {
            // call base class to trigger events
            base.DoInteractions(inputState);

            // check / uncheck
            if (ToggleCheckOnClick)
            {
                if (inputState.LeftMouseReleasedNow)
                {
                    ToggleCheckedState();
                }
            }
        }

        internal override void DoFocusedEntityInteractions(InputState inputState)
        {
            // call base class to trigger events
            base.DoFocusedEntityInteractions(inputState);

            // implement click via keyboard
            if (ToggleCheckOnClick)
            {
                if (inputState.KeyboardInteraction == Drivers.KeyboardInteractions.Select)
                {
                    ToggleCheckedState();
                }
            }
        }
    }
}
