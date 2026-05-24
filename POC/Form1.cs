using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace POC
{
    public partial class Form1 : Form
    {

        private List<PictureBox> pictureBoxes = new List<PictureBox>();
        private List<Tuple<Point, Point>> connectionSegments = new List<Tuple<Point, Point>>(); 
        private List<Tuple<Point, Point>> userLines = new List<Tuple<Point, Point>>(); 
        private PictureBox selectedPictureBox1 = null;
        private PictureBox selectedPictureBox2 = null;

        public Form1()
        {
            InitializeComponent();

            // Enable drag and drop for the form
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;

            // Add mouse event handlers for drawing lines
            this.MouseClick += Form1_MouseClick;
            this.Paint += Form1_Paint;
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            // Check if the data being dragged is a .png file
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                ((string[])e.Data.GetData(DataFormats.FileDrop))[0].EndsWith(".png"))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            // Get the file paths of the dropped files
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string file in files)
            {
                // Load the .png file into a PictureBox
                PictureBox pictureBox = new PictureBox();
                pictureBox.Image = Image.FromFile(file);
                pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;

                // Position the PictureBox on the form
                pictureBox.Location = this.PointToClient(new Point(e.X, e.Y));
                this.Controls.Add(pictureBox);

                // Subscribe to the Click event of pictureBox
                pictureBox.Click += pictureBox_Click;

                // Add the PictureBox to the list
                pictureBoxes.Add(pictureBox);
            }
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {
            PictureBox clickedPictureBox = sender as PictureBox;
            if (selectedPictureBox1 == null)
            {
                selectedPictureBox1 = clickedPictureBox;
            }
            else if (selectedPictureBox2 == null)
            {
                selectedPictureBox2 = clickedPictureBox;
                DrawUserLine(selectedPictureBox1, selectedPictureBox2);
                selectedPictureBox1 = null;
                selectedPictureBox2 = null;
            }

        }

        private void DrawUserLine(PictureBox startPictureBox, PictureBox endPictureBox)
        {
            Point startPoint = new Point(startPictureBox.Location.X + startPictureBox.Width / 2, startPictureBox.Location.Y + startPictureBox.Height / 2);
            Point endPoint = new Point(endPictureBox.Location.X + endPictureBox.Width / 2, endPictureBox.Location.Y + endPictureBox.Height / 2);
            userLines.Add(Tuple.Create(startPoint, endPoint));
            this.Invalidate(); // Trigger paint event to draw the lines
        }


        private void DrawOptimizedLine(PictureBox startPictureBox, PictureBox endPictureBox)
        {
            Point startPoint = new Point(startPictureBox.Location.X + startPictureBox.Width / 2, startPictureBox.Location.Y + startPictureBox.Height / 2);
            Point endPoint = new Point(endPictureBox.Location.X + endPictureBox.Width / 2, endPictureBox.Location.Y + endPictureBox.Height / 2);

            
            Point midPoint1 = new Point(startPoint.X, endPoint.Y); // Vertical then horizontal
            Point midPoint2 = new Point(endPoint.X, startPoint.Y); // Horizontal then vertical

            var path1 = new Tuple<Point, Point>[] { Tuple.Create(startPoint, midPoint1), Tuple.Create(midPoint1, endPoint) };
            var path2 = new Tuple<Point, Point>[] { Tuple.Create(startPoint, midPoint2), Tuple.Create(midPoint2, endPoint) };

           
            bool path1Valid = IsPathValid(path1);
            bool path2Valid = IsPathValid(path2);

            if (path1Valid && path2Valid)
            {
               
                connectionSegments.AddRange(path2);
            }
            else if (path1Valid)
            {
                connectionSegments.AddRange(path1);
            }
            else if (path2Valid)
            {
                connectionSegments.AddRange(path2);
            }
            else
            {
                
                Point intermediatePoint = new Point(midPoint2.X, midPoint1.Y); 
                var path3 = new Tuple<Point, Point>[]
                {
                    Tuple.Create(startPoint, midPoint2),
                    Tuple.Create(midPoint2, intermediatePoint),
                    Tuple.Create(intermediatePoint, endPoint)
                };
                connectionSegments.AddRange(path3);
            }

            this.Invalidate(); 
        }

        private bool IsPathValid(Tuple<Point, Point>[] path)
        {
            foreach (var segment in path)
            {
                foreach (var existingSegment in connectionSegments)
                {
                    if (DoLinesIntersect(segment.Item1, segment.Item2, existingSegment.Item1, existingSegment.Item2))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool DoLinesIntersect(Point p1, Point p2, Point q1, Point q2)
        {
            int o1 = Orientation(p1, p2, q1);
            int o2 = Orientation(p1, p2, q2);
            int o3 = Orientation(q1, q2, p1);
            int o4 = Orientation(q1, q2, p2);

            if (o1 != o2 && o3 != o4) return true;

            if (o1 == 0 && OnSegment(p1, q1, p2)) return true;
            if (o2 == 0 && OnSegment(p1, q2, p2)) return true;
            if (o3 == 0 && OnSegment(q1, p1, q2)) return true;
            if (o4 == 0 && OnSegment(q1, p2, q2)) return true;

            return false;
        }

        private int Orientation(Point p, Point q, Point r)
        {
            int val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
            if (val == 0) return 0;
            return (val > 0) ? 1 : 2;
        }

        private bool OnSegment(Point p, Point q, Point r)
        {
            if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                return true;
            return false;
        }



        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            foreach (PictureBox pictureBox in pictureBoxes)
            {
                if (pictureBox.Bounds.Contains(e.Location))
                {
                    pictureBox_Click(pictureBox, EventArgs.Empty);
                    break;
                }
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.Black, 2))
            {
                foreach (var segment in userLines)
                {
                    e.Graphics.DrawLine(pen, segment.Item1, segment.Item2);
                }
                foreach (var segment in connectionSegments)
                {
                    e.Graphics.DrawLine(pen, segment.Item1, segment.Item2);
                }
            }
        }

        private void btnReconnect_Click(object sender, EventArgs e)
        {
            // Clear existing lines
            connectionSegments.Clear();

            // Draw optimized lines based on the user-drawn lines
            foreach (var line in userLines)
            {
               
                OptimizeLine(line.Item1, line.Item2);
            }

            // Clear user-drawn lines
            userLines.Clear();

            this.Invalidate(); // 
        }


        private void OptimizeLine(Point startPoint, Point endPoint)
        {
            int offset = 15;  // This offset is added to avoid overlapping lines

            // Try both possible perpendicular paths
            Point midPoint1 = new Point(startPoint.X, endPoint.Y);
            Point midPoint2 = new Point(endPoint.X, startPoint.Y);

            // Adjust the points slightly if they are too close to avoid overlap
            Point adjustedMidPoint1 = new Point(midPoint1.X + offset, midPoint1.Y);
            Point adjustedMidPoint2 = new Point(midPoint2.X, midPoint2.Y + offset);

            var path1 = new Tuple<Point, Point>[] { Tuple.Create(startPoint, midPoint1), Tuple.Create(midPoint1, endPoint) };
            var path2 = new Tuple<Point, Point>[] { Tuple.Create(startPoint, midPoint2), Tuple.Create(midPoint2, endPoint) };
            var adjustedPath1 = new Tuple<Point, Point>[] { Tuple.Create(startPoint, adjustedMidPoint1), Tuple.Create(adjustedMidPoint1, endPoint) };
            var adjustedPath2 = new Tuple<Point, Point>[] { Tuple.Create(startPoint, adjustedMidPoint2), Tuple.Create(adjustedMidPoint2, endPoint) };

            // Determine the best path that doesn't overlap
            bool path1Valid = IsPathValid(path1);
            bool path2Valid = IsPathValid(path2);
            bool adjustedPath1Valid = IsPathValid(adjustedPath1);
            bool adjustedPath2Valid = IsPathValid(adjustedPath2);

            if (path1Valid)
            {
                connectionSegments.AddRange(path1);
            }
            else if (path2Valid)
            {
                connectionSegments.AddRange(path2);
            }
            else if (adjustedPath1Valid)
            {
                connectionSegments.AddRange(adjustedPath1);
            }
            else if (adjustedPath2Valid)
            {
                connectionSegments.AddRange(adjustedPath2);
            }
            else
            {
                // If none of the direct or adjusted paths are valid, create a more complex path
                Point intermediatePoint = new Point(adjustedMidPoint2.X, adjustedMidPoint1.Y);
                var complexPath = new Tuple<Point, Point>[]
                {
                    Tuple.Create(startPoint, adjustedMidPoint2),
                    Tuple.Create(adjustedMidPoint2, intermediatePoint),
                    Tuple.Create(intermediatePoint, endPoint)
                };
                connectionSegments.AddRange(complexPath);
            }
        }

    }
}
