using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RichTextViewLib.Wpf
{
    public class RichTextView : UserControl
    {
        private Grid _topGrid;

        private bool _isSelecting;
        private Point _selectionStartPoint;
        private Point _selectionEndPoint;
        private UIElement _selectionStartElement;
        private UIElement _selectionEndElement;
        private Dictionary<Type, IRichTextElementExtension> _elementExtensions = new Dictionary<Type, IRichTextElementExtension>();

        public RichTextView()
        {
            _topGrid = new Grid { Background = Background };
            this.Content = _topGrid;
            this.MouseDown += RichTextView_MouseDown;
            this.MouseUp += RichTextView_MouseUp;
            SetElementExtension<TextBlock>(new TextBlockRichTextExtension());
            HighlightBackground = SystemColors.HighlightBrush;
            HighlightForeground = SystemColors.HighlightTextBrush;
        }

        public Brush HighlightBackground { get; set; }

        public Brush HighlightForeground { get; set; }

        public void SetElementExtension<T>(IRichTextElementExtension elementExtension) where T : UIElement
        {
            _elementExtensions[typeof(T)] = elementExtension;
        }

        void RichTextView_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            foreach(var prevSelectedRow in _selectedRows)
            {
                foreach (var prevSelectedElement in prevSelectedRow.SelectedElements)
                {
                    DeselectElement(prevSelectedElement);
                }
            }
            _selectedRows.Clear();
            
            _selectionStartPoint = e.GetPosition(_topGrid);
            var result = VisualTreeHelper.HitTest(_topGrid, e.GetPosition(_topGrid));
            if (result == null)
                return;
            var element = result.VisualHit as UIElement;
            if (element == null)
                return;
            var row = (int)element.GetValue(Row.RichTextViewRowProperty);
            if (row >= 0)
            {
                _isSelecting = true;
                this.MouseMove += RichTextView_MouseMove;
            }
            _selectionStartElement = element;
        }

        void RichTextView_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isSelecting)
            {
                return;
            }
            if(e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
            {
                OnMouseUp();
            }
            _selectionEndPoint = e.GetPosition(_topGrid);
            var diffX = _selectionEndPoint.X - _selectionStartPoint.X;
            var diffY = _selectionEndPoint.Y - _selectionStartPoint.Y;
            var diff = Math.Sqrt(diffX * diffX + diffY * diffY);
            if (diff <= 1)
            {
                return;
            }
            var result = VisualTreeHelper.HitTest(_topGrid, e.GetPosition(_topGrid));
            if (result == null)
                return;
            var element = result.VisualHit as UIElement;
            if (element == null)
            {
                return;
            }
            var row = (int)element.GetValue(Row.RichTextViewRowProperty);
            if (row >= 0)
                _selectionEndElement = element;

            HandleSelection();
        }

        void RichTextView_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OnMouseUp();
        }

        private void OnMouseUp()
        {
            this.MouseMove -= RichTextView_MouseMove;
            var plainTextBuilder = new StringBuilder();
            foreach (var row in _selectedRows)
            {
                if (row != _selectedRows.First())
                    plainTextBuilder.AppendLine();
                foreach (var element in row.SelectedElements)
                {
                    var elementType = element.GetType();
                    IRichTextElementExtension extension;
                    if (!_elementExtensions.TryGetValue(elementType, out extension))
                        continue;
                    plainTextBuilder.Append(extension.GetPlainText(element));
                }
            }
            var selectionPlainText = plainTextBuilder.ToString();
            SelectionPlainText = string.IsNullOrEmpty(selectionPlainText) ? null : selectionPlainText;
        }

        private void HandleSelection()
        {
            var startRowIndex = (int)_selectionStartElement.GetValue(Row.RichTextViewRowProperty);
            var endRowIndex = (int)_selectionEndElement.GetValue(Row.RichTextViewRowProperty);
            var startElementColIndex = (int)_selectionStartElement.GetValue(Grid.ColumnProperty);
            var endElementColIndex = (int)_selectionEndElement.GetValue(Grid.ColumnProperty);

            if(startRowIndex > endRowIndex || (startRowIndex == endRowIndex && startElementColIndex > endElementColIndex))
            {
                var tempRowIndex = startRowIndex;
                startRowIndex = endRowIndex;
                endRowIndex = tempRowIndex;

                var tempColIndex = startElementColIndex;
                startElementColIndex = endElementColIndex;
                endElementColIndex = tempColIndex;
            }

            var plainTextBuilder = new StringBuilder();

            var selectedRows = new List<Row>();
            var selectedElements = new List<UIElement>();

            var isAscRowIndex = startRowIndex <= endRowIndex;

            for (var rowIndex = startRowIndex; rowIndex <= endRowIndex; rowIndex++)
            {
                var row = _rows[rowIndex];
                var startElementIndex = 0;
                if (startRowIndex == rowIndex)
                {
                    startElementIndex = startElementColIndex;
                }
                var endElementIndex = row.Elements.Length - 1;
                if (endRowIndex == rowIndex)
                {
                    endElementIndex = endElementColIndex;
                }
                selectedRows.Add(row);

                foreach (var element in row.Elements)
                {
                    var isSelectable = (bool)element.GetValue(RichTextExtensionBase.IsSelectableProperty);
                    if (!isSelectable)
                        continue;
                    var elementIndex = (int)element.GetValue(Grid.ColumnProperty);
                    if ((startElementIndex <= elementIndex && elementIndex <= endElementIndex) || (startElementIndex >= elementIndex && elementIndex >= endElementIndex))
                    {
                        var elementType = element.GetType();
                        IRichTextElementExtension extension;
                        if (!_elementExtensions.TryGetValue(elementType, out extension))
                            continue;
                        selectedElements.Add(element);
                    }
                }
            }

            var deselectedElements = _selectedRows.SelectMany(r => r.SelectedElements).Except(selectedElements).ToArray();
            var newlySelectedElements = selectedElements.Except(_selectedRows.SelectMany(or => or.SelectedElements)).ToArray();

            foreach(var newlySelectedElement in newlySelectedElements)
            {
                SelectElement(newlySelectedElement);
            }

            foreach(var deselectedElement in deselectedElements)
            {
                DeselectElement(deselectedElement);
            }

            _selectedRows = selectedRows;
        }

        private void SelectElement(UIElement element)
        {
            element.SetValue(RichTextExtensionBase.IsSelectedProperty, true);
            var elementType = element.GetType();
            IRichTextElementExtension extension;
            if (!_elementExtensions.TryGetValue(elementType, out extension))
                return;
            extension.HandleSelection(element, HighlightForeground, HighlightBackground);
        }

        private void DeselectElement(UIElement element)
        {
            element.SetValue(RichTextExtensionBase.IsSelectedProperty, false);
            var elementType = element.GetType();
            IRichTextElementExtension extension;
            if (!_elementExtensions.TryGetValue(elementType, out extension))
                return;
            extension.HandleDeselection(element);
        }

        private string _selectionPlainText;
        public string SelectionPlainText
        {
            get { return _selectionPlainText; }
            private set
            {
                if (_selectionPlainText == value)
                    return;
                _selectionPlainText = value;
                SelectionChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler SelectionChanged = delegate { };

        

        public Row AddRow()
        {
            var row = new Row(_topGrid.RowDefinitions.Count);
            _rows.Add(row);
            _topGrid.Children.Add(row);
            _topGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            return row;
        }

        private List<Row> _rows = new List<Row>();
        public Row[] Rows { get { return _rows.ToArray(); } }

        private List<Row> _selectedRows = new List<Row>();

        public class Row : Grid
        {
            public Row(int rowIndex)
            {
                RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                SetValue(Grid.RowProperty, rowIndex);
            }

            public Row AddElement(int index, UIElement element, bool isSelectable = true)
            {
                while (ColumnDefinitions.Count <= index)
                {
                    ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                }
                element.SetValue(Grid.ColumnProperty, index);
                element.SetValue(RichTextViewRowProperty, RowIndex);
                element.SetValue(RichTextExtensionBase.IsSelectableProperty, isSelectable);
                Children.Add(element);
                UpdateElements();
                return this;
            }

            private void UpdateElements()
            {
                Elements = Children.OfType<UIElement>().OrderBy(e => (int)e.GetValue(Grid.ColumnProperty)).ToArray();
            }

            public UIElement[] Elements { get; private set; }

            public int RowIndex { get { return (int)GetValue(Grid.RowProperty); } }

            public static int GetRichTextViewRow(DependencyObject obj)
            {
                return (int)obj.GetValue(RichTextViewRowProperty);
            }

            public static void SetRichTextViewRow(DependencyObject obj, int value)
            {
                obj.SetValue(RichTextViewRowProperty, value);
            }

            public static readonly DependencyProperty RichTextViewRowProperty =
                DependencyProperty.RegisterAttached("RichTextViewRow", typeof(int), typeof(Row), new PropertyMetadata(-1));

            public UIElement[] SelectedElements { get { return Elements.Where(e => (bool)e.GetValue(RichTextExtensionBase.IsSelectedProperty)).ToArray(); } }

            public bool IsVisible
            {
                get { return this.Visibility == Visibility.Visible; }
                set
                {
                    if (IsVisible == value)
                        return;
                    this.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            public void Show()
            {
                IsVisible = true;
            }

            public void Hide()
            {
                IsVisible = false;
            }
        }

        public interface IRichTextElementExtension
        {
            string GetPlainText(UIElement element);

            string GetHtmlText(UIElement element);

            void HandleSelection(UIElement element, Brush highlightForeground, Brush highlighBackground);

            void HandleDeselection(UIElement element);
        }

        public abstract class RichTextExtension<T> : RichTextExtensionBase, IRichTextElementExtension where T : UIElement
        {
            public abstract string GetPlainText(T element);
            public abstract string GetHtmlText(T element);
            public abstract void HandleSelection(T element, Brush highlightForegroundBrush, Brush highlightBackgroundBrush);
            public abstract void HandleDeselection(T element);

            string IRichTextElementExtension.GetPlainText(UIElement element)
            {
                return GetPlainText((T)element);
            }

            string IRichTextElementExtension.GetHtmlText(UIElement element)
            {
                return GetHtmlText((T)element);
            }

            void IRichTextElementExtension.HandleSelection(UIElement element, Brush highlightForeground, Brush highlighBackground)
            {
                HandleSelection((T)element, highlightForeground, highlighBackground);
            }

            void IRichTextElementExtension.HandleDeselection(UIElement element)
            {
                HandleDeselection((T)element);
            }
        }

        public abstract class RichTextExtensionBase
        {
            protected void SetState<TState>(UIElement element, TState state)
            {
                element.SetValue(ElementStateProperty, state);
            }

            protected TState GetState<TState>(UIElement element)
            {
                return (TState)element.GetValue(ElementStateProperty);
            }

            private static object GetElementState(UIElement obj)
            {
                return (object)obj.GetValue(ElementStateProperty);
            }

            private static void SetElementState(UIElement obj, object value)
            {
                obj.SetValue(ElementStateProperty, value);
            }

            protected static readonly DependencyProperty ElementStateProperty =
                DependencyProperty.RegisterAttached("ElementState", typeof(object), typeof(RichTextExtensionBase), new PropertyMetadata(null));






            protected static Brush GetOriginalBackgroundBrush(UIElement obj)
            {
                return (Brush)obj.GetValue(OriginalBackgroundBrushProperty);
            }

            protected static void SetOriginalBackgroundBrush(UIElement obj, Brush value)
            {
                obj.SetValue(OriginalBackgroundBrushProperty, value);
            }

            public static readonly DependencyProperty OriginalBackgroundBrushProperty =
                DependencyProperty.RegisterAttached("OriginalBackgroundBrush", typeof(Brush), typeof(RichTextExtensionBase), new PropertyMetadata(null));






            protected static Brush GetOriginalForegroundBrush(UIElement obj)
            {
                return (Brush)obj.GetValue(OriginalForegroundBrushProperty);
            }

            protected static void SetOriginalForegroundBrush(UIElement obj, Brush value)
            {
                obj.SetValue(OriginalForegroundBrushProperty, value);
            }

            public static readonly DependencyProperty OriginalForegroundBrushProperty =
                DependencyProperty.RegisterAttached("OriginalForegroundBrush", typeof(Brush), typeof(RichTextExtensionBase), new PropertyMetadata(null));





            private static bool GetIsSelected(UIElement obj)
            {
                return (bool)obj.GetValue(IsSelectedProperty);
            }

            private static void SetIsSelected(UIElement obj, bool value)
            {
                obj.SetValue(IsSelectedProperty, value);
            }

            public static readonly DependencyProperty IsSelectedProperty =
                DependencyProperty.RegisterAttached("IsSelected", typeof(bool), typeof(RichTextExtensionBase), new PropertyMetadata(false));




            public static bool GetIsSelectable(DependencyObject obj)
            {
                return (bool)obj.GetValue(IsSelectableProperty);
            }

            public static void SetIsSelectable(DependencyObject obj, bool value)
            {
                obj.SetValue(IsSelectableProperty, value);
            }

            public static readonly DependencyProperty IsSelectableProperty =
                DependencyProperty.RegisterAttached("IsSelectable", typeof(bool), typeof(RichTextExtensionBase), new PropertyMetadata(true));


        }


        public class TextBlockRichTextExtension : RichTextExtension<TextBlock>
        {
            public override string GetPlainText(TextBlock element)
            {
                return element.Text;
            }

            public override string GetHtmlText(TextBlock element)
            {
                throw new NotImplementedException();
            }

            public override void HandleSelection(TextBlock element, Brush highlightForegroundBrush, Brush highlightBackgroundBrush)
            {
                SetOriginalBackgroundBrush(element, element.Background);
                SetOriginalForegroundBrush(element, element.Foreground);

                element.Foreground = highlightForegroundBrush;
                element.Background = highlightBackgroundBrush;
            }

            public override void HandleDeselection(TextBlock element)
            {
                element.Foreground = GetOriginalForegroundBrush(element);
                element.Background = GetOriginalBackgroundBrush(element);
            }
        }
    }
}
