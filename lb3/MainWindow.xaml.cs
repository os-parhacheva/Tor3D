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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace lb3
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PerspectiveCamera cam;
        public MainWindow()
        {
            InitializeComponent();
            Title = "Simple 3D Scene in Code";

            // Make DockPanel content of window.
            DockPanel dock = new DockPanel();
            Content = dock;
            Viewport3D viewport = new Viewport3D();
            dock.Children.Add(viewport);

            drawTor(new Point3D(0, 0, 0), 1, 0.2, 40, 30, Colors.Tomato, viewport,true);
            drawTor(new Point3D(1.5, 0, 0), 1, 0.2, 40, 30, Colors.Blue, viewport,false);
            drawTor(new Point3D(3, 0, 0), 1, 0.2, 40, 30, Colors.Tomato, viewport, true);
            drawTor(new Point3D(4.5, 0, 0), 1, 0.2, 40, 30, Colors.Blue, viewport, false);

            // Create another ModelVisual3D for light
            ModelVisual3D modvis = new ModelVisual3D();
            modvis.Content = new AmbientLight(Colors.White);
            viewport.Children.Add(modvis);

            // Create another ModelVisual3D for light
            modvis = new ModelVisual3D();
            modvis.Content = new AmbientLight(Colors.White);
            viewport.Children.Add(modvis);

            // Create the camera
            cam = new PerspectiveCamera(new Point3D(0, -0.2, 4),
                         new Vector3D(0, 0, -1), new Vector3D(0, 1.5, 0), 45);
            viewport.Camera = cam;
            Trackball tr = new Trackball();
            tr.EventSource = this;
            cam.Transform = tr.Transform;

        }

        //метод возвращает точку по заданным радиусам и углам
        public Point3D getPositionTor(double R, double r, double A, double B, bool flag)
        {

            double sinB = Math.Sin(B * Math.PI / 180);
            double cosB = Math.Cos(B * Math.PI / 180);
            double sinA = Math.Sin(A * Math.PI / 180);
            double cosA = Math.Cos(A * Math.PI / 180);

            Point3D point = new Point3D();
            point.X = (R + r * cosA) * cosB;
            if (flag)
            {
                point.Y = r * sinA;
                point.Z = -(R + r * cosA) * sinB;
            }
            else
            {
                point.Y = -(R + r * cosA) * sinB;
                point.Z = r * sinA;
            } 
            


            return point;
        }
        //метод построения треугольного сегмента
        public static void drawTriangle(Point3D p0, Point3D p1, Point3D p2, Color color, Viewport3D viewport)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
          
            Material material = new DiffuseMaterial(new SolidColorBrush(color));

            GeometryModel3D geometry = new GeometryModel3D(mesh, material);
            ModelUIElement3D model = new ModelUIElement3D();
            model.Model = geometry;

            viewport.Children.Add(model);
        }

        public void drawTor(Point3D center, double R, double r, int N, int n, Color color, Viewport3D viewport, bool flag)
        {
            if (n < 2 || N < 2)
            {
                return;
            }

            Model3DGroup tor = new Model3DGroup();
            Point3D[,] points = new Point3D[N, n];

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    points[i, j] = getPositionTor(R, r, i * 360 / (N - 1), j * 360 / (n - 1), flag);
                    points[i, j] += (Vector3D)center;
                }
            }

            Point3D[] p = new Point3D[4];
            for (int i = 0; i < N - 1; i++)
            {
                for (int j = 0; j < n - 1; j++)
                {
                    p[0] = points[i, j];
                    p[1] = points[i + 1, j];
                    p[2] = points[i + 1, j + 1];
                    p[3] = points[i, j + 1];
                    drawTriangle(p[0], p[1], p[2], color, viewport);
                    drawTriangle(p[2], p[3], p[0], color, viewport);

                }
            }
        }



        public class Trackball
        {
            private FrameworkElement _eventSource;
            private Point _previousPosition2D;
            private Vector3D _previousPosition3D = new Vector3D(0, 0, 1);

            private Transform3DGroup _transform;
            private ScaleTransform3D _scale = new ScaleTransform3D(1, 1, 1);
            private AxisAngleRotation3D _rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);

            public Trackball()
            {

                _transform = new Transform3DGroup();
                _transform.Children.Add(_scale);
                _transform.Children.Add(new RotateTransform3D(_rotation));
            }


            //     A transform to move the camera or scene to the trackball's
            //     current orientation and scale.

            public Transform3D Transform
            {
                get { return _transform; }
            }

            #region Event Handling


            //     The FrameworkElement we listen to for mouse events.

            public FrameworkElement EventSource
            {
                get { return _eventSource; }

                set
                {
                    if (_eventSource != null)
                    {
                        _eventSource.MouseDown -= this.OnMouseDown;
                        _eventSource.MouseUp -= this.OnMouseUp;
                        _eventSource.MouseMove -= this.OnMouseMove;
                    }

                    _eventSource = value;

                    _eventSource.MouseDown += this.OnMouseDown;
                    _eventSource.MouseUp += this.OnMouseUp;
                    _eventSource.MouseMove += this.OnMouseMove;
                }
            }

            private void OnMouseDown(object sender, MouseEventArgs e)
            {
                Mouse.Capture(EventSource, CaptureMode.Element);
                _previousPosition2D = e.GetPosition(EventSource);
                _previousPosition3D = ProjectToTrackball(
                    EventSource.ActualWidth,
                    EventSource.ActualHeight,
                    _previousPosition2D);
            }

            private void OnMouseUp(object sender, MouseEventArgs e)
            {
                Mouse.Capture(EventSource, CaptureMode.None);
            }

            private void OnMouseMove(object sender, MouseEventArgs e)
            {
                Point currentPosition = e.GetPosition(EventSource);

                // Prefer tracking to zooming if both buttons are pressed.
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Track(currentPosition);
                }
                else if (e.RightButton == MouseButtonState.Pressed)
                {
                    Zoom(currentPosition);
                }

                _previousPosition2D = currentPosition;
            }

            #endregion Event Handling

            private void Track(Point currentPosition)
            {
                Vector3D currentPosition3D = ProjectToTrackball(
                    EventSource.ActualWidth, EventSource.ActualHeight, currentPosition);

                Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
                double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);
                Quaternion delta = new Quaternion(axis, -angle);

                // Get the current orientantion from the RotateTransform3D
                AxisAngleRotation3D r = _rotation;
                Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);

                // Compose the delta with the previous orientation
                q *= delta;

                // Write the new orientation back to the Rotation3D
                _rotation.Axis = q.Axis;
                _rotation.Angle = q.Angle;

                _previousPosition3D = currentPosition3D;
            }

            private Vector3D ProjectToTrackball(double width, double height, Point point)
            {
                double x = point.X / (width / 2);    // Scale so bounds map to [0,0] - [2,2]
                double y = point.Y / (height / 2);

                x = x - 1;                           // Translate 0,0 to the center
                y = 1 - y;                           // Flip so +Y is up instead of down

                double z2 = 1 - x * x - y * y;       // z^2 = 1 - x^2 - y^2
                double z = z2 > 0 ? Math.Sqrt(z2) : 0;

                return new Vector3D(x, y, z);
            }

            private void Zoom(Point currentPosition)
            {
                double yDelta = currentPosition.Y - _previousPosition2D.Y;

                double scale = Math.Exp(yDelta / 100);    // e^(yDelta/100) is fairly arbitrary.

                _scale.ScaleX *= scale;
                _scale.ScaleY *= scale;
                _scale.ScaleZ *= scale;
            }
        }
    }
}
