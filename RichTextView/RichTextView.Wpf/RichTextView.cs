using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RichTextView.Wpf
{
    public class RichTextView : Control
    {
        private Grid _topGrid;

        public RichTextView()
        {
            _topGrid = new Grid();
        }

        public Row AddRow()
        {
            var row = new Row(_topGrid.RowDefinitions.Count);
            _rows.Add(row);
            _topGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            return row;
        }

        private List<Row> _rows = new List<Row>();
        public Row[] Rows { get { return _rows.ToArray(); } }

        public class Row
        {
            private Grid _grid;

            public Row(int index)
            {
                _grid = new Grid();
                _grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                Index = index;
            }

            public Row AddElement(int index, UIElement element)
            {
                while (_grid.ColumnDefinitions.Count <= index)
                {
                    _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                }
                element.SetValue(Grid.ColumnProperty, index);
                _grid.Children.Add(element);
                return this;
            }

            public UIElement[] Elements { get { return _grid.Children.OfType<UIElement>().OrderBy(e => (int)e.GetValue(Grid.ColumnProperty)).ToArray(); } }

            public int Index { get; private set; }
        }
    }
}
