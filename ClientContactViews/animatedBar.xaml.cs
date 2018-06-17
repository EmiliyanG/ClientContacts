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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientContactViews
{
    /// <summary>
    /// Interaction logic for animatedBar.xaml
    /// </summary>
    public partial class AnimatedBar : UserControl
    {
        public AnimatedBar()
        {
            InitializeComponent();
            //actualWidth and actualHeight are set to 0 until Measure() and Arrange() are called
            //to get arround this wait until the control is loaded
            Loaded += delegate
            {
                // access ActualWidth and ActualHeight here
                ScreenGlintRect.Width = this.ActualWidth;
                ScreenGlintRect.Height = this.ActualHeight;
                var animation = new DoubleAnimation()
                {
                    Duration = new Duration(TimeSpan.FromSeconds(2)),
                    From = (-ActualWidth),
                    To = ActualWidth * 2
                };
                ScreenGlintRect.BeginAnimation(Canvas.LeftProperty, animation);
            };
        }
    }
}
