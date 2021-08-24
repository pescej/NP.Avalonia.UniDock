﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using NP.Concepts.Behaviors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NP.AvaloniaDock
{
    public class DockStackGroup : Control, IDockGroup, IDisposable
    {
        public StackGroup<IControl> _stackGroup = new StackGroup<IControl>();

        public bool ShowChildHeaders { get; } = true;

        public DockManager TheDockManager
        {
            get => DockAttachedProperties.GetTheDockManager(this);
            set => DockAttachedProperties.SetTheDockManager(this, value);
        }

        public Orientation TheOrientation
        {
            get => _stackGroup.TheOrientation;
            set => _stackGroup.TheOrientation = value;
        }
        #region NumberDockChildren Direct Avalonia Property
        public static readonly DirectProperty<DockStackGroup, int> NumberDockChildrenProperty =
            AvaloniaProperty.RegisterDirect<DockStackGroup, int>
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

        public IDockGroup? DockParent { get; set; }

        public IList<IDockGroup> DockChildren { get; } = new ObservableCollection<IDockGroup>();

        private IDockVisualItemGenerator TheDockVisualItemGenerator { get; } =
            new DockVisualItemGenerator();

        public event Action<IRemovable>? RemoveEvent;

        public void Remove()
        {
            RemoveEvent?.Invoke(this);
        }

        IDisposable? _behavior;
        IDisposable? _setDockGroupBehavior;

        public DockStackGroup()
        {
            AffectsMeasure<SimpleDockGroup>(NumberDockChildrenProperty);

            ((ISetLogicalParent)_stackGroup).SetParent(this);
            this.VisualChildren.Add(_stackGroup);
            this.LogicalChildren.Add(_stackGroup);

            _setDockGroupBehavior = new SetDockGroupBehavior(this, DockChildren!);
            _behavior = DockChildren?.AddDetailedBehavior(OnDockChildAdded, OnDockChildRemoved);
        }

        public void Dispose()
        {
            _setDockGroupBehavior?.Dispose();
            _setDockGroupBehavior = null;

            _behavior?.Dispose();
            _behavior = null!;
        }

        private void SetNumberDockChildren()
        {
            NumberDockChildren = DockChildren.Count;
        }

        private void OnDockChildAdded(IEnumerable<IDockGroup> groups, IDockGroup dockChild, int idx)
        {
            SetNumberDockChildren();

            IControl newVisualChildToInsert =
               TheDockVisualItemGenerator.Generate(dockChild);

            _stackGroup.Items.Insert(idx, newVisualChildToInsert);
        }

        private void OnDockChildRemoved(IEnumerable<IDockGroup> groups, IDockGroup dockChild, int idx)
        {
            dockChild.CleanSelfOnRemove();
            SetNumberDockChildren();

            _stackGroup.Items.RemoveAt(idx);
        }
    }
}