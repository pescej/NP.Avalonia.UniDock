﻿// (c) Nick Polyak 2021 - http://awebpros.com/
// License: MIT License (https://opensource.org/licenses/MIT)
//
// short overview of copyright rules:
// 1. you can use this framework in any commercial or non-commercial 
//    product as long as you retain this copyright message
// 2. Do not blame the author of this software if something goes wrong. 
// 
// Also, please, mention this software in any documentation for the 
// products that use it.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using NP.Avalonia.Visuals;
using NP.Avalonia.Visuals.Behaviors;
using NP.Avalonia.Visuals.Controls;
using NP.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NP.Avalonia.UniDock
{
    public class FloatingWindow : CustomWindow
    {
        public SimpleDockGroup TheDockGroup { get; } = 
            new SimpleDockGroup();

        public DockManager? TheDockManager
        {
            get => DockAttachedProperties.GetTheDockManager(this);
            set => DockAttachedProperties.SetTheDockManager(this, value!);
        }

        static FloatingWindow()
        {
            DockAttachedProperties
                .TheDockManagerProperty
                .Changed
                .AddClassHandler<FloatingWindow>((dw, e) => dw.OnDockManagerChanged(e));
        }

        private void OnDockManagerChanged(AvaloniaPropertyChangedEventArgs e)
        {
            TheDockGroup.TheDockManager = TheDockManager;
        }

        public FloatingWindow()
        {
            Classes = new Classes(new[] { "PlainFloatingWindow" });
            HasCustomWindowFeatures = true;
            Content = TheDockGroup;

            TheDockGroup.HasNoChildrenEvent += TheDockGroup_HasNoChildrenEvent;

            this.Closing += FloatingWindow_Closing;
        }

        public FloatingWindow(DockManager dockManager) : this()
        {
            DockAttachedProperties.SetTheDockManager(this, dockManager);
        }

        private void TheDockGroup_HasNoChildrenEvent(SimpleDockGroup obj)
        {
            if ((TheDockGroup as IDockGroup).AutoDestroy)
            {
                this.Close();
            }
        }

        private void FloatingWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            TheDockManager = null;

            var allGroups = TheDockGroup.GetDockGroupSelfAndAncestors().Reverse().ToList();

            allGroups?.DoForEach(item => item.RemoveItselfFromParent());
        }

        protected Point2D? StartPointerPosition { get; set; }
        protected Point2D? StartWindowPosition { get; set; }


        #region PointerShift Styled Avalonia Property
        public Point2D PointerShift
        {
            get { return GetValue(PointerShiftProperty); }
            set { SetValue(PointerShiftProperty, value); }
        }

        public static readonly StyledProperty<Point2D> PointerShiftProperty =
            AvaloniaProperty.Register<CustomWindow, Point2D>
            (
                nameof(PointerShift)
            );
        #endregion PointerShift Styled Avalonia Property


        public void SetMovePtr()
        {
            this.Activated += CustomWindow_Activated!;
        }

        private void CustomWindow_Activated(object sender, EventArgs e)
        {
            this.Activated -= CustomWindow_Activated!;
            StartPointerPosition = CurrentScreenPointBehavior.CurrentScreenPointValue;
            StartWindowPosition = StartPointerPosition.Minus(new Point2D(60, 10));
            Position = StartWindowPosition.ToPixelPoint();

            SetDragOnMovePointer();
        }

        protected override void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            SetDragOnMovePointer(e);
        }

        private void SetDragOnMovePointer(PointerEventArgs e)
        {
            if (!e.GetCurrentPoint(HeaderControl).Properties.IsLeftButtonPressed)
            {
                return;
            }

            StartPointerPosition = GetCurrentPointInScreen(e);
            StartWindowPosition = this.Position.ToPoint2D();
            PointerShift = new Point2D();

            SetDragOnMovePointer();
        }

        private void SetDragOnMovePointer()
        {
            TheDockManager!.DraggedWindow = this;

            CurrentScreenPointBehavior.Capture(HeaderControl);

            if (HeaderControl != null)
            {
                HeaderControl.PointerMoved += OnPointerMoved!;

                HeaderControl.PointerReleased += OnPointerReleased!;
            }
        }

        public Point2D GetCurrentPointInScreen(PointerEventArgs e)
        {
            var result = HeaderControl.PointToScreen(e.GetPosition(HeaderControl));

            return result.ToPoint2D();
        }

        private void UpdatePosition(PointerEventArgs e)
        {
            PointerShift = GetCurrentPointInScreen(e).Minus(StartPointerPosition);

            this.Position = StartWindowPosition.Plus(PointerShift).ToPixelPoint();
        }

        protected void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (HeaderControl != null)
            {
                HeaderControl.PointerMoved -= OnPointerMoved!;

                HeaderControl.PointerReleased -= OnPointerReleased!;
            }

            UpdatePosition(e);

            TheDockManager?.CompleteDragDropAction();
        }

        protected void OnPointerMoved(object sender, PointerEventArgs e)
        {
            UpdatePosition(e);
        }

        private IEnumerable<ILeafDockObj> GetLeafGroups(DockManager dockManager)
        {
            return this.TheDockGroup
                        .GetDockGroupSelfAndDescendants(stopCondition:item => item is ILeafDockObj)
                        .OfType<ILeafDockObj>()
                        .Distinct()
                        .Where(g => ReferenceEquals(g.TheDockManager, dockManager));
        }

        public IEnumerable<DockItem> LeafItems
        {
            get
            {
                return GetLeafGroups(TheDockManager!).SelectMany(g => g.LeafItems);
            }
        }
    }
}