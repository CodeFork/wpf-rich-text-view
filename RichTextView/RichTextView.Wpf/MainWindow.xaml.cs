using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RichTextViewLib.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            rtv.AddRow()
                .AddElement(0, new TextBlock { Text = "FirstTextRow1", Foreground = new SolidColorBrush(Colors.Blue), FontFamily = new FontFamily("Courier New"), Cursor = Cursors.IBeam })
                .AddElement(1, new TextBlock { Text = "SecTextFirstRow", Foreground = new SolidColorBrush(Colors.Red), FontFamily = new FontFamily("Courier New"), Cursor = Cursors.IBeam })
                .AddElement(2, new TextBlock { Text = " [100 lines]", Foreground = new SolidColorBrush(Colors.Green), FontFamily = new FontFamily("Courier New"), Cursor = Cursors.IBeam }, false);
            
            rtv.AddRow()
                .AddElement(0, new TextBlock { Text = "FirstTextSecondRow", Foreground = new SolidColorBrush(Colors.Blue), FontFamily = new FontFamily("Courier New"), Cursor = Cursors.IBeam })
                .AddElement(1, new TextBlock { Text = "SecTextRow2", Foreground = new SolidColorBrush(Colors.Red), FontFamily = new FontFamily("Courier New"), Cursor = Cursors.IBeam });

            rtv.AddRow()
                .AddElement(0, new TextBlock { Text = "FirstTextThirdRow", Foreground = new SolidColorBrush(Colors.Blue), FontFamily = new FontFamily("Courier New"), Cursor = Cursors.IBeam })
                .AddElement(1, new TextBlock { Text = "SecTextRow3", Foreground = new SolidColorBrush(Colors.Red), FontFamily = new FontFamily("Courier New"), Cursor = Cursors.IBeam });

            rtv.SelectionChanged += rtv_SelectionChanged;

            btn.Click += btn_Click;
        }

        void btn_Click(object sender, RoutedEventArgs e)
        {
            if (rtv.Rows[1].IsVisible)
                rtv.Rows[1].Hide();
            else
                rtv.Rows[1].Show();
        }

        void rtv_SelectionChanged(object sender, EventArgs e)
        {
            plainText.Text = rtv.SelectionPlainText;
        }
    }
}


