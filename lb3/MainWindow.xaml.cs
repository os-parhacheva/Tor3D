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
            Title = "Beachball Sphere";

            // Make DockPanel content of window.
            DockPanel dock = new DockPanel();
            Content = dock;
            Viewport3D viewport = new Viewport3D();
            dock.Children.Add(viewport);
          
            MeshGeometry3D mesh = GenerateSphere(new Point3D(0, 0, 0), 1, 20, 40);
            
            mesh.Freeze();

            // Define a brush for the sphere.
            Brush[] brushes = new Brush[4] { Brushes.White, Brushes.Blue,
                                             Brushes.Yellow, Brushes.Red};
            DrawingGroup drawgrp = new DrawingGroup();

            for (int i = 0; i < brushes.Length; i++)
            {
                RectangleGeometry rectgeo =
                    new RectangleGeometry(new Rect(10 * i, 0, 10, 60));

                GeometryDrawing geodraw =
                    new GeometryDrawing(brushes[i], null, rectgeo);

                drawgrp.Children.Add(geodraw);
            }
            DrawingBrush drawbrsh = new DrawingBrush(drawgrp);
            drawbrsh.Freeze();           

            GeometryModel3D geomod = new GeometryModel3D();
            geomod.Geometry = mesh;
            geomod.Material = new DiffuseMaterial(drawbrsh);
            geomod.BackMaterial = new DiffuseMaterial(Brushes.Crimson);

            ModelVisual3D modvis = new ModelVisual3D();
            modvis.Content = geomod;
            viewport.Children.Add(modvis);

            // Create another ModelVisual3D for light
            Model3DGroup modgrp = new Model3DGroup();
            modgrp.Children.Add(new AmbientLight(Color.FromRgb(128, 128, 128)));
            modgrp.Children.Add(
                 new DirectionalLight(Color.FromRgb(128, 128, 128),
                                          new Vector3D(2, -3, -1)));

            modvis = new ModelVisual3D();
            modvis.Content = modgrp;
            viewport.Children.Add(modvis);

            // Create the camera
            cam = new PerspectiveCamera(new Point3D(0, -0.2, 4),
                         new Vector3D(0, 0, -1), new Vector3D(0, 1.5, 0), 45);
            viewport.Camera = cam;
            Trackball tr = new Trackball();
            tr.EventSource = this;
            cam.Transform = tr.Transform;

        }


        MeshGeometry3D GenerateSphere(Point3D center, double radius, int slices, int stacks)
        {
            // Create the MeshGeometry3D.
            MeshGeometry3D mesh = new MeshGeometry3D();

            // Fill the Position, Normals, and TextureCoordinates collections.
            for (int stack = 0; stack <= stacks; stack++)
            {
                double phi = Math.PI / 2 - stack * Math.PI / stacks;
                double y = radius * Math.Sin(phi);
                double scale = -radius * Math.Cos(phi);

                for (int slice = 0; slice <= slices; slice++)
                {
                    double theta = slice * 2 * Math.PI / slices;
                    double x = scale * Math.Sin(theta);
                    double z = scale * Math.Cos(theta);

                    Vector3D normal = new Vector3D(x, y, z);
                    mesh.Normals.Add(normal);
                    mesh.Positions.Add(normal + center);
                    mesh.TextureCoordinates.Add(
                            new Point((double)slice / slices,
                                      (double)stack / stacks));
                }
            }

            // Fill the TriangleIndices collection.
            for (int stack = 0; stack < stacks/2 ; stack++)
            {
                for (int slice = 0; slice < slices ; slice++)
                {
                    int n = slices + 1; // Keep the line length down.

                    if (stack != 0)
                    {
                        mesh.TriangleIndices.Add((stack + 0) * n + slice);
                        mesh.TriangleIndices.Add((stack + 1) * n + slice);
                        mesh.TriangleIndices.Add((stack + 0) * n + slice + 1);
                    }
                    if (stack != stacks -1)
                    {
                        mesh.TriangleIndices.Add((stack + 0) * n + slice + 1);
                        mesh.TriangleIndices.Add((stack + 1) * n + slice);
                        mesh.TriangleIndices.Add((stack + 1) * n + slice + 1);
                    }
                }
            }
            return mesh;
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
