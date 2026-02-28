using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using UniversalTasker.UI.ViewModels;

namespace UniversalTasker.UI.Services;

public static class ListBoxDragDropBehavior
{
    private static Point _startPoint;
    private static bool _isDragging;
    private static DropIndicatorAdorner? _adorner;

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ListBoxDragDropBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listBox) return;

        if ((bool)e.NewValue)
        {
            listBox.AllowDrop = true;
            listBox.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            listBox.PreviewMouseMove += OnPreviewMouseMove;
            listBox.DragOver += OnDragOver;
            listBox.Drop += OnDrop;
            listBox.DragLeave += OnDragLeave;
        }
        else
        {
            listBox.AllowDrop = false;
            listBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            listBox.PreviewMouseMove -= OnPreviewMouseMove;
            listBox.DragOver -= OnDragOver;
            listBox.Drop -= OnDrop;
            listBox.DragLeave -= OnDragLeave;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
        _isDragging = false;
    }

    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not ListBox listBox) return;

        var diff = _startPoint - e.GetPosition(null);

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        if (_isDragging) return;

        var item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
        if (item == null) return;

        var data = item.DataContext;
        if (data is not ActionViewModel) return;

        _isDragging = true;
        var dragData = new DataObject("ActionViewModel", data);
        DragDrop.DoDragDrop(listBox, dragData, DragDropEffects.Move);
        _isDragging = false;
        RemoveAdorner(listBox);
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (!e.Data.GetDataPresent("ActionViewModel"))
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;

        var targetItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
        if (targetItem == null)
        {
            RemoveAdorner(listBox);
            return;
        }

        var adornerLayer = AdornerLayer.GetAdornerLayer(listBox);
        if (adornerLayer == null) return;

        RemoveAdorner(listBox);

        var pos = e.GetPosition(targetItem);
        var insertAfter = pos.Y > targetItem.ActualHeight / 2;

        _adorner = new DropIndicatorAdorner(targetItem, insertAfter);
        adornerLayer.Add(_adorner);
    }

    private static void OnDragLeave(object sender, DragEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            RemoveAdorner(listBox);
        }
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (!e.Data.GetDataPresent("ActionViewModel")) return;

        RemoveAdorner(listBox);

        var draggedItem = e.Data.GetData("ActionViewModel") as ActionViewModel;
        if (draggedItem == null) return;

        var targetItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
        if (targetItem?.DataContext is not ActionViewModel targetData) return;

        var viewModel = listBox.DataContext as MainViewModel;
        if (viewModel == null)
        {
            // Try walking up the tree to find the DataContext
            var parent = VisualTreeHelper.GetParent(listBox);
            while (parent != null)
            {
                if (parent is FrameworkElement fe && fe.DataContext is MainViewModel mvm)
                {
                    viewModel = mvm;
                    break;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
        }
        if (viewModel == null) return;

        var oldIndex = viewModel.Actions.IndexOf(draggedItem);
        var newIndex = viewModel.Actions.IndexOf(targetData);

        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex) return;

        var pos = e.GetPosition(targetItem);
        if (pos.Y > targetItem.ActualHeight / 2 && newIndex < viewModel.Actions.Count - 1 && newIndex < oldIndex)
        {
            newIndex++;
        }
        else if (pos.Y <= targetItem.ActualHeight / 2 && newIndex > 0 && newIndex > oldIndex)
        {
            newIndex--;
        }

        viewModel.MoveActionByIndex(oldIndex, newIndex);
        e.Handled = true;
    }

    private static void RemoveAdorner(ListBox listBox)
    {
        if (_adorner == null) return;

        var adornerLayer = AdornerLayer.GetAdornerLayer(listBox);
        adornerLayer?.Remove(_adorner);
        _adorner = null;
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T target) return target;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}

internal class DropIndicatorAdorner : Adorner
{
    private readonly bool _insertAfter;
    private static readonly Pen IndicatorPen = new(Brushes.DodgerBlue, 2) { DashStyle = DashStyles.Solid };

    public DropIndicatorAdorner(UIElement adornedElement, bool insertAfter)
        : base(adornedElement)
    {
        _insertAfter = insertAfter;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var renderSize = AdornedElement.RenderSize;
        var y = _insertAfter ? renderSize.Height : 0;

        drawingContext.DrawLine(IndicatorPen,
            new Point(0, y),
            new Point(renderSize.Width, y));
    }
}
