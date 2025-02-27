﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.Accessibility;
using static Interop;

namespace System.Windows.Forms;

public partial class TabPage
{
    internal sealed class TabPageAccessibleObject : ControlAccessibleObject
    {
        public TabPageAccessibleObject(TabPage owningTabPage) : base(owningTabPage) { }

        public override Rectangle Bounds
        {
            get
            {
                if (!this.IsOwnerHandleCreated(out TabPage? _))
                {
                    return Rectangle.Empty;
                }

                // The CHILDID_SELF constant returns to the id of the TabPage, which allows to use the native
                // "accLocation" method to get the "Bounds" property
                return SystemIAccessible.TryGetLocation(CHILDID_SELF);
            }
        }

        public override AccessibleStates State => SystemIAccessible.TryGetState(GetChildId());

        internal override UiaCore.IRawElementProviderFragmentRoot? FragmentRoot => OwningTabControl?.AccessibilityObject;

        private TabControl? OwningTabControl =>
            this.TryGetOwnerAs(out TabPage? owningTabPage) ? owningTabPage.ParentInternal as TabControl : null;

        public override AccessibleObject? GetChild(int index)
        {
            if (!this.IsOwnerHandleCreated(out TabPage? owningTabPage))
            {
                return null;
            }

            if (index < 0 || index > owningTabPage.Controls.Count - 1)
            {
                return null;
            }

            return owningTabPage.Controls[index].AccessibilityObject;
        }

        public override int GetChildCount()
            => this.IsOwnerHandleCreated(out TabPage? owningTabPage) ? owningTabPage.Controls.Count : -1;

        internal override IRawElementProviderFragment.Interface? FragmentNavigate(NavigateDirection direction)
        {
            if (!this.IsOwnerHandleCreated(out TabPage? _) || OwningTabControl is null)
            {
                return null;
            }

            return direction switch
            {
                NavigateDirection.NavigateDirection_Parent => OwningTabControl?.AccessibilityObject,
                NavigateDirection.NavigateDirection_NextSibling => GetNextSibling(),
                NavigateDirection.NavigateDirection_PreviousSibling => null,
                _ => base.FragmentNavigate(direction)
            };
        }

        internal override int GetChildId() => 0;

        internal override VARIANT GetPropertyValue(UIA_PROPERTY_ID propertyID)
            => propertyID switch
            {
                UIA_PROPERTY_ID.UIA_HasKeyboardFocusPropertyId => (VARIANT)(this.TryGetOwnerAs(out TabPage? owningTabPage) && owningTabPage.Focused),
                UIA_PROPERTY_ID.UIA_IsKeyboardFocusablePropertyId
                    // This is necessary for compatibility with MSAA proxy:
                    // IsKeyboardFocusable = true regardless the control is enabled/disabled.
                    => VARIANT.True,
                _ => base.GetPropertyValue(propertyID)
            };

        internal override bool IsPatternSupported(UIA_PATTERN_ID patternId)
            => patternId switch
            {
                UIA_PATTERN_ID.UIA_ValuePatternId => false,
                _ => base.IsPatternSupported(patternId)
            };

        private IRawElementProviderFragment.Interface? GetNextSibling()
        {
            if (!this.TryGetOwnerAs(out TabPage? owningTabPage) || OwningTabControl is null || owningTabPage != OwningTabControl.SelectedTab)
            {
                return null;
            }

            return OwningTabControl.TabPages.Count > 0 ? OwningTabControl.TabPages[0].TabAccessibilityObject : null;
        }
    }
}
