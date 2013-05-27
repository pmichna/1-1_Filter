using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;

namespace _1_1_Filter_WPF
{
    public partial class MainWindow : Window
    {
        private PointCollection _pointCollection;
        int _width, _height, _rawStride;
        private byte[] _pixelData;
        private BitmapImage _bitmapImage;
        private BitmapSource _filteredBitmap;

        public MainWindow()
        {
            InitializeComponent();
            _pointCollection = new PointCollection();
            _polyline.Points = _pointCollection;
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == false)
                return;
            imgOriginal.Source = new BitmapImage(new Uri(dlg.FileName));
        }

        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point newPoint = new Point() { X = (int)e.GetPosition(canvas).X, Y = (int)e.GetPosition(canvas).Y };
            bool alreadyInserted = false;
            foreach (Point p in _pointCollection)
            {
                if (p.X > newPoint.X)
                {
                    _pointCollection.Insert(_pointCollection.IndexOf(p), newPoint);
                    alreadyInserted = true;
                    break;
                }
            }
            if (!alreadyInserted)
            {
                _pointCollection.Add(newPoint);
            }
        }

        private void btnClr_Click(object sender, RoutedEventArgs e)
        {
            _pointCollection.Clear();
        }

        private void testPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            short result;
            if (!Int16.TryParse(e.Text, out result))
            {
                e.Handled = true;
            }
        }

        private void btnInitY_Click(object sender, RoutedEventArgs e)
        {
            short initY;
            if (Int16.TryParse(txtInitY.Text, out initY) && initY >= 0 && initY <= 255)
            {
                Point newPoint = new Point() { X = 0, Y = 255 - initY };
                if (0 == _pointCollection.Count)
                {
                    _pointCollection.Add(newPoint);
                }
                if (0 == _pointCollection[0].X)
                {
                    _pointCollection[0] = newPoint;
                }
                else
                {
                    _pointCollection.Insert(0, newPoint);
                }
            }
            else
                MessageBox.Show("You have to enter a number in the range 0-255!");
        }

        private void btnFinalY_Click(object sender, RoutedEventArgs e)
        {
            short finalY;
            if (Int16.TryParse(txtFinalY.Text, out finalY) && finalY >= 0 && finalY <= 255)
            {
                Point newPoint = new Point() { X = 255, Y = 255 - finalY };
                if (0 == _pointCollection.Count || 255 != _pointCollection[_pointCollection.Count - 1].X)
                {
                    _pointCollection.Add(newPoint);
                }
                else
                {
                    _pointCollection[_pointCollection.Count - 1] = newPoint;
                }
            }
            else
                MessageBox.Show("You have to enter a number in the range 0-255!");
        }

        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_pointCollection[0].X != 0 || _pointCollection[_pointCollection.Count - 1].X != 255)
            {
                MessageBox.Show("You have to set the initial and final values of X!");
                return;
            }
            if (imgOriginal.Source == null)
            {
                MessageBox.Show("You have to load the image first!");
                return;
            }
            ImageSource ims = imgOriginal.Source;
            _bitmapImage = (BitmapImage)ims;
            _height = _bitmapImage.PixelHeight;
            _width = _bitmapImage.PixelWidth;
            _rawStride = (_width * _bitmapImage.Format.BitsPerPixel + 7) / 8;
            _pixelData = new byte[_rawStride * _height];
            _bitmapImage.CopyPixels(_pixelData, _rawStride, 0);
            int[] function = new int[256];
            for (int i = 0; i < 256; i++)
            {
                bool isKnown = false;
                foreach (Point p in _pointCollection)
                {
                    if (i == (int)p.X)
                    {
                        isKnown = true;
                        function[i] = (int)(255 - p.Y);
                        break;
                    }
                }
                if (isKnown)
                {
                    continue;
                }
                for (int j = 0; j < _pointCollection.Count; j++)
                {
                    if (_pointCollection[j].X < i && _pointCollection[j + 1].X >= i)
                    {
                        function[i] = 255 - ((int)_pointCollection[j].Y + ((int)_pointCollection[j + 1].Y - (int)_pointCollection[j].Y) * (i - (int)_pointCollection[j].X) / ((int)_pointCollection[j + 1].X - (int)_pointCollection[j].X));
                        break;
                    }
                }
            }
            for (int y = 0; y < _height; y++)
            {
                int yIndex = y * _rawStride;
                for (int x = 0; x < _rawStride; x+=3)
                {
                    if (x+yIndex+2 > _pixelData.Length-1)
                        break;
                    _pixelData[x + yIndex] = (byte)function[_pixelData[x + yIndex]];
                    _pixelData[x + yIndex + 1] = (byte)function[_pixelData[x + yIndex + 1]];
                    _pixelData[x + yIndex + 2] = (byte)function[_pixelData[x + yIndex + 2]];
                }
            }
            _filteredBitmap = BitmapSource.Create(_width, _height, 96, 96, _bitmapImage.Format, _bitmapImage.Palette, _pixelData, _rawStride);
            imgFiltered.Source = _filteredBitmap;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredBitmap == null)
            {
                MessageBox.Show("You have to filter the image first!");
                return;
            }
            SaveFileDialog sfg = new SaveFileDialog();
            sfg.Filter = "JPEG file (.jpg)|*.jpg";
            sfg.DefaultExt = ".jpg";
            if (sfg.ShowDialog() == null)
            {
                return;
            }
            FileStream stream = new FileStream(sfg.FileName, FileMode.Create);
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 100;
            encoder.Frames.Add(BitmapFrame.Create(_filteredBitmap));
            encoder.Save(stream);
            stream.Close();
        }
    }
}
