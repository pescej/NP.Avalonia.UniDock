﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.VisualTree;
using NP.Concepts.Behaviors;
using NP.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NP.AvaloniaDock
{
    public class SimpleDockGroup : Control, IDockGroup, IDisposable
    {
        #region NumberDockChildren Direct Avalonia Property
        public static readonly DirectProperty<SimpleDockGroup, int> NumberDockChildrenProperty =
            AvaloniaProperty.RegisterDirect<SimpleDockGroup, int>
            (
                nameof(NumberDockChildren),
                o => o.NumberDockChildren,
                (o, c) => o.NumberDockChildren = c
            );
        #endregion NumberDockChildren Direct Avalonia Property

        private int _numChildren = 0;
        public int NumberDockChildren
        {
            get => _numChildren;
            private set
            {
                SetAndRaise(NumberDockChildrenProperty, ref _numChildren, value);
            }
        }

        public event Action<IRemovable>? RemoveEvent;

        public void Remove()
        {
            RemoveEvent?.Invoke(this);
        }

        public bool ShowChildHeader => false;

        private IDockVisualItemGenerator TheDockVisualItemGenerator { get; } =
            new DockVisualItemGenerator();

        public bool ShowChildHeaders { get; } = false;

        public DockManager TheDockManager
        {
            get => DockAttachedProperties.GetTheDockManager(this);
            set => DockAttachedProperties.SetTheDockManager(this, value);
        }

        public IDockGroup? DockParent
        {
            get => null;
            set => throw new NotImplementedException();
        }

        [Content]
        public IDockGroup? TheChild
        {
            get => DockChildren?.FirstOrDefault();
            set
            {
                DockChildren.RemoveAllOneByOne();

                if (value != null)
                {
                    DockChildren.Add(value);
                }
            }
        }


        public IList<IDockGroup> DockChildren { get; } = 
            new ObservableCollection<IDockGroup>();


        private IDisposable? _addRemoveChildBehavior;
        private SetDockGroupBehavior? _setBehavior;
        public SimpleDockGroup()
        {
            AffectsMeasure<SimpleDockGroup>(NumberDockChildrenProperty);

            _setBehavior = new SetDockGroupBehavior(this, DockChildren!);

            _addRemoveChildBehavior = 
                DockChildren.AddBehavior(OnChildAdded, OnChildRemoved);

        }

        private void OnChildAdded(IDockGroup newChildToInsert)
        {
            // have to remove previous children before adding the new one.
            // Only one child is allowed
            var childrenToRemove = DockChildren.Except(newChildToInsert.ToCollection()).ToList();

            childrenToRemove.DoForEach(child => DockChildren.Remove(child));

            IControl newVisualChildToInsert =
                TheDockVisualItemGenerator.Generate(newChildToInsert);

            ((ISetLogicalParent)newVisualChildToInsert).SetParent(this);
            VisualChildren.Add(newVisualChildToInsert);
            LogicalChildren.Add(newVisualChildToInsert);

            NumberDockChildren = DockChildren?.Count() ?? 0;
        }

        private Control FindVisualChild(IDockGroup dockChild)
        {
            IControl control = dockChild;
            if (dockChild is ILeafDockObj leafDockChild)
            {
                control = leafDockChild.GetVisual();
            }

            return (Control) LogicalChildren.OfType<IControl>().FirstOrDefault(item => ReferenceEquals(item, control))!;
        }

        private void OnChildRemoved(IDockGroup childToRemove)
        {
            Control visualChildToRemove = FindVisualChild(childToRemove);

            ((ISetLogicalParent)visualChildToRemove).SetParent(null);
            VisualChildren.Remove(visualChildToRemove);
            LogicalChildren.Remove(visualChildToRemove);

            NumberDockChildren = DockChildren?.Count() ?? 0;
        }

        public void Dispose()
        {
            _setBehavior?.Dispose();
            _setBehavior = null;

            _addRemoveChildBehavior?.Dispose();
            _addRemoveChildBehavior = null;
        }
    }
}