﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.Accessibility;
using static Interop;

namespace System.Windows.Forms.PropertyGridInternal;

internal abstract partial class GridEntry
{
    public class GridEntryAccessibleObject : AccessibleObject, IOwnedObject<GridEntry>
    {
        private readonly WeakReference<GridEntry> _owningGridEntry;

        private int[]? _runtimeId;

        private delegate void SelectDelegate(AccessibleSelection flags);

        public GridEntryAccessibleObject(GridEntry owner)
        {
            Debug.Assert(owner is not null, "GridEntryAccessibleObject must have a valid owner GridEntry");
            _owningGridEntry = new(owner);
        }

        private protected override string AutomationId => GetHashCode().ToString();

        public override Rectangle Bounds
            => PropertyGridView is not null && PropertyGridView.IsHandleCreated && this.TryGetOwnerAs(out GridEntry? owner)
                ? PropertyGridView.AccessibilityGetGridEntryBounds(owner)
                : Rectangle.Empty;

        public override string? DefaultAction
        {
            get
            {
                if (!this.TryGetOwnerAs(out GridEntry? owner) || !owner.Expandable)
                {
                    return base.DefaultAction;
                }
                else if (owner.Expanded)
                {
                    return SR.AccessibleActionCollapse;
                }
                else
                {
                    return SR.AccessibleActionExpand;
                }
            }
        }

        public override string Description
            => this.TryGetOwnerAs(out GridEntry? owner) ? owner.PropertyDescription : string.Empty;

        internal override ExpandCollapseState ExpandCollapseState
            => !this.TryGetOwnerAs(out GridEntry? owner) ? ExpandCollapseState.ExpandCollapseState_Collapsed : owner.Expandable
                ? owner.Expanded ? ExpandCollapseState.ExpandCollapseState_Expanded : ExpandCollapseState.ExpandCollapseState_Collapsed
                : ExpandCollapseState.ExpandCollapseState_LeafNode;

        public override string Help => this.TryGetOwnerAs(out GridEntry? owner) ? owner.PropertyDescription : string.Empty;

        public override string? Name => this.TryGetOwnerAs(out GridEntry? owner) ? owner.PropertyLabel : null;

        public override AccessibleObject? Parent
            => this.TryGetOwnerAs(out GridEntry? owner) ? owner.OwnerGridView?.AccessibilityObject : null;

        public override AccessibleRole Role => AccessibleRole.Cell;

        public override AccessibleStates State
        {
            get
            {
                if (PropertyGridView is null || !PropertyGridView.IsHandleCreated || !this.TryGetOwnerAs(out GridEntry? owner))
                {
                    return AccessibleStates.None;
                }

                AccessibleStates state = AccessibleStates.Selectable | AccessibleStates.Focusable;

                // Determine focus
                if (owner.HasFocus)
                {
                    state |= AccessibleStates.Focused;
                }

                // Determine selected
                Debug.Assert(Parent is not null, "GridEntry AO does not have a parent AO");
                var parent = (PropertyGridView.PropertyGridViewAccessibleObject)Parent;
                if (parent.GetSelected() == this)
                {
                    state |= AccessibleStates.Selected;
                }

                // Determine expanded/collapsed state
                if (owner.Expandable)
                {
                    if (owner.Expanded)
                    {
                        state |= AccessibleStates.Expanded;
                    }
                    else
                    {
                        state |= AccessibleStates.Collapsed;
                    }
                }

                // Determine readonly/editable state
                if (owner.ShouldRenderReadOnly)
                {
                    state |= AccessibleStates.ReadOnly;
                }

                // Determine password state
                if (owner.ShouldRenderPassword)
                {
                    state |= AccessibleStates.Protected;
                }

                Rectangle entryBounds = BoundingRectangle;
                Rectangle propertyGridViewBounds = PropertyGridView.GetToolNativeScreenRectangle();

                if (!entryBounds.IntersectsWith(propertyGridViewBounds))
                {
                    state |= AccessibleStates.Offscreen;
                }

                return state;
            }
        }

        public override string? Value
        {
            get => this.TryGetOwnerAs(out GridEntry? owner) ? owner.GetPropertyTextValue() : null;
            set
            {
                if (this.TryGetOwnerAs(out GridEntry? owner))
                {
                    owner.SetPropertyTextValue(value);
                }
            }
        }

        internal override int Column => 0;

        internal override IRawElementProviderSimple.Interface? ContainingGrid => PropertyGridView?.AccessibilityObject;

        /// <summary>
        ///  Return the element that is the root node of this fragment of UI.
        /// </summary>
        internal override UiaCore.IRawElementProviderFragmentRoot? FragmentRoot
            => Parent as PropertyGridView.PropertyGridViewAccessibleObject;

        internal override int Row
        {
            get
            {
                if (Parent is not PropertyGridView.PropertyGridViewAccessibleObject parent
                    || !this.TryGetOwnerAs(out GridEntry? owner))
                {
                    return -1;
                }

                if (!parent.TryGetOwnerAs(out PropertyGridView? gridView))
                {
                    return -1;
                }

                GridEntryCollection? topLevelGridEntries = gridView.TopLevelGridEntries;
                if (topLevelGridEntries is null)
                {
                    return -1;
                }

                for (int i = 0; i < topLevelGridEntries.Count; i++)
                {
                    GridItem? topLevelGridEntry = topLevelGridEntries[i];
                    if (owner == topLevelGridEntry)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        internal override int[] RuntimeId => _runtimeId ??= new[]
        {
            // We need to provide a unique ID. Others are implementing this in the same manner. First item is static - 0x2a.
            // Second item can be anything, but it's good to supply HWND. Third and others are optional, but in case of
            // GridItem we need it, to make it unique. Grid items are not controls, they don't have hwnd - we use hwnd
            // of PropertyGridView.
            RuntimeIDFirstItem,
            this.TryGetOwnerAs(out GridEntry? owner)
                ? (int)(owner?.OwnerGridView?.InternalHandle ?? HWND.Null)
                : (int)HWND.Null,
            GetHashCode()
        };

        private PropertyGridView? PropertyGridView
        {
            get
            {
                var propertyGridViewAccessibleObject = Parent as PropertyGridView.PropertyGridViewAccessibleObject;
                if (propertyGridViewAccessibleObject is not null)
                {
                    propertyGridViewAccessibleObject.TryGetOwnerAs(out PropertyGridView? owner);
                    return owner;
                }

                return null;
            }
        }

        GridEntry? IOwnedObject<GridEntry>.Owner
            => _owningGridEntry.TryGetTarget(out GridEntry? target) ? target : null;

        public override void DoDefaultAction()
        {
            if (PropertyGridView is not null && PropertyGridView.IsHandleCreated && this.TryGetOwnerAs(out GridEntry? owner))
            {
                owner.OnOutlineClick(EventArgs.Empty);
            }
        }

        /// <summary>
        ///  Returns the currently focused child, if any.
        ///  Returns this if the object itself is focused.
        /// </summary>
        public override AccessibleObject? GetFocused()
            => this.TryGetOwnerAs(out GridEntry? owner) ? owner.HasFocus ? this : null : null;

        /// <summary>
        ///  Navigate to the next or previous grid entry.
        /// </summary>
        public override AccessibleObject? Navigate(AccessibleNavigation navdir)
        {
            if (Parent is not PropertyGridView.PropertyGridViewAccessibleObject parent
                || !this.TryGetOwnerAs(out GridEntry? owner))
            {
                return null;
            }

            switch (navdir)
            {
                case AccessibleNavigation.Down:
                case AccessibleNavigation.Right:
                case AccessibleNavigation.Next:
                    return parent.Next(owner);

                case AccessibleNavigation.Up:
                case AccessibleNavigation.Left:
                case AccessibleNavigation.Previous:
                    return parent.Previous(owner);

                case AccessibleNavigation.FirstChild:
                case AccessibleNavigation.LastChild:
                    // Fall through and return null,
                    // as this object has no children.
                    break;
            }

            return null;
        }

        public override void Select(AccessibleSelection flags)
        {
            if (PropertyGridView is null || !PropertyGridView.IsHandleCreated || !this.TryGetOwnerAs(out GridEntry? owner))
            {
                return;
            }

            // Make sure we're on the right thread.
            if (PropertyGridView.InvokeRequired)
            {
                PropertyGridView.Invoke(new SelectDelegate(Select), new object[] { flags });
                return;
            }

            // Focus the PropertyGridView window.
            if ((flags & AccessibleSelection.TakeFocus) == AccessibleSelection.TakeFocus)
            {
                bool focused = PropertyGridView.Focus();
            }

            // Select the grid entry.
            if ((flags & AccessibleSelection.TakeSelection) == AccessibleSelection.TakeSelection)
            {
                PropertyGridView.AccessibilitySelect(owner);
            }
        }

        internal override void Collapse()
        {
            if (this.TryGetOwnerAs(out GridEntry? owner) && owner.Expandable && owner.Expanded)
            {
                owner.Expanded = false;
            }
        }

        internal override void Expand()
        {
            if (this.TryGetOwnerAs(out GridEntry? owner) && owner.Expandable && owner.Expanded == false)
            {
                owner.Expanded = true;
            }
        }

        internal override IRawElementProviderFragment.Interface? FragmentNavigate(NavigateDirection direction)
        {
            switch (direction)
            {
                case NavigateDirection.NavigateDirection_Parent:
                    if (this.TryGetOwnerAs(out GridEntry? owner) && owner.ParentGridEntry is { } parentGridEntry)
                    {
                        return parentGridEntry is SingleSelectRootGridEntry
                            ? owner.OwnerGrid.GridViewAccessibleObject
                            : (IRawElementProviderFragment.Interface)parentGridEntry.AccessibilityObject;
                    }

                    return Parent;
                case NavigateDirection.NavigateDirection_PreviousSibling:
                    return Navigate(AccessibleNavigation.Previous);
                case NavigateDirection.NavigateDirection_NextSibling:
                    return Navigate(AccessibleNavigation.Next);
            }

            return base.FragmentNavigate(direction);
        }

        internal override VARIANT GetPropertyValue(UIA_PROPERTY_ID propertyID)
        {
            return propertyID switch
            {
                // The accessible hierarchy is changed so we cannot use Button type
                // for the grid items to not break automation logic that searches for the first
                // button in the PropertyGridView to show dialog/drop-down. In Level < 3 action
                // button is one of the first children of PropertyGridView.

                UIA_PROPERTY_ID.UIA_ControlTypePropertyId => (VARIANT)(int)UIA_CONTROLTYPE_ID.UIA_TreeItemControlTypeId,
                UIA_PROPERTY_ID.UIA_HasKeyboardFocusPropertyId => (VARIANT)(this.TryGetOwnerAs(out GridEntry? owner) && owner.HasFocus),
                UIA_PROPERTY_ID.UIA_IsEnabledPropertyId => VARIANT.True,
                UIA_PROPERTY_ID.UIA_IsKeyboardFocusablePropertyId => (VARIANT)State.HasFlag(AccessibleStates.Focusable),
                _ => base.GetPropertyValue(propertyID)
            };
        }

        internal override bool IsIAccessibleExSupported() => this.TryGetOwnerAs(out GridEntry? owner) && owner.Expandable;

        internal override bool IsPatternSupported(UIA_PATTERN_ID patternId)
        {
            switch (patternId)
            {
                case UIA_PATTERN_ID.UIA_InvokePatternId:
                case UIA_PATTERN_ID.UIA_LegacyIAccessiblePatternId:
                    return true;

                case UIA_PATTERN_ID.UIA_ExpandCollapsePatternId:
                    {
                        if (this.TryGetOwnerAs(out GridEntry? owner) && owner.Expandable)
                        {
                            return true;
                        }

                        break;
                    }

                case UIA_PATTERN_ID.UIA_GridItemPatternId:
                case UIA_PATTERN_ID.UIA_TableItemPatternId:
                    {
                        if (!this.TryGetOwnerAs(out GridEntry? owner) || owner.OwnerGrid is null || owner.OwnerGrid.SortedByCategories)
                        {
                            break;
                        }

                        // Only top level rows are grid items.
                        if (owner.ParentGridEntry is SingleSelectRootGridEntry)
                        {
                            return true;
                        }

                        break;
                    }
            }

            return base.IsPatternSupported(patternId);
        }

        internal override void SetFocus()
        {
            if (PropertyGridView is null || !PropertyGridView.IsHandleCreated)
            {
                return;
            }

            base.SetFocus();

            RaiseAutomationEvent(UIA_EVENT_ID.UIA_AutomationFocusChangedEventId);
        }
    }
}
