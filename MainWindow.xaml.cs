using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Color = System.Drawing.Color;
using Path = System.IO.Path;

namespace QRCodeGeneratorApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Bitmap currentQRCode;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string content = ContentTextBox.Text.Trim();

                if (string.IsNullOrEmpty(content))
                {
                    MessageBox.Show("Please enter content to encode.", "Input Required",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get selected size
                int size = GetSelectedSize();

                // Get error correction level
                QRCodeGenerator.ECCLevel eccLevel = GetSelectedErrorCorrectionLevel();

                // Generate QR code
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, eccLevel);
                QRCode qrCode = new QRCode(qrCodeData);

                // Create bitmap with high quality settings
                currentQRCode = qrCode.GetGraphic(20, Color.Black, Color.White, true);

                // Resize to desired size with high quality
                Bitmap resizedQRCode = ResizeImage(currentQRCode, size, size);
                currentQRCode.Dispose();
                currentQRCode = resizedQRCode;

                // Convert to WPF image and display
                BitmapImage bitmapImage = ConvertToBitmapImage(currentQRCode);
                QRCodeImage.Source = bitmapImage;

                // Update status and enable save button
                StatusTextBlock.Text = $"QR Code generated successfully ({size}x{size})";
                SaveButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating QR code: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error generating QR code";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentQRCode == null)
            {
                MessageBox.Show("No QR code to save. Please generate a QR code first.",
                              "No QR Code", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp|All Files|*.*",
                FilterIndex = 1,
                DefaultExt = "png",
                FileName = "QRCode"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Determine format based on extension
                    string extension = Path.GetExtension(saveFileDialog.FileName).ToLower();
                    ImageFormat format = ImageFormat.Png;

                    switch (extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                            format = ImageFormat.Jpeg;
                            break;
                        case ".bmp":
                            format = ImageFormat.Bmp;
                            break;
                        default:
                            format = ImageFormat.Png;
                            break;
                    }

                    // For JPEG, create white background (JPEG doesn't support transparency)
                    if (format == ImageFormat.Jpeg)
                    {
                        using (Bitmap jpegBitmap = new Bitmap(currentQRCode.Width, currentQRCode.Height))
                        {
                            using (Graphics g = Graphics.FromImage(jpegBitmap))
                            {
                                g.Clear(Color.White);
                                g.DrawImage(currentQRCode, 0, 0);
                            }
                            jpegBitmap.Save(saveFileDialog.FileName, format);
                        }
                    }
                    else
                    {
                        currentQRCode.Save(saveFileDialog.FileName, format);
                    }

                    StatusTextBlock.Text = $"QR code saved to: {saveFileDialog.FileName}";

                    // Ask if user wants to open the saved file
                    var result = MessageBox.Show(
                        "QR code saved successfully! Would you like to open the file location?",
                        "Save Successful",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start("explorer.exe",
                            $"/select,\"{saveFileDialog.FileName}\"");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving QR code: {ex.Message}", "Save Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private int GetSelectedSize()
        {
            return SizeComboBox.SelectedIndex switch
            {
                0 => 200,
                1 => 300,
                2 => 400,
                3 => 500,
                _ => 400
            };
        }

        private QRCodeGenerator.ECCLevel GetSelectedErrorCorrectionLevel()
        {
            var selectedItem = (ComboBoxItem)ErrorCorrectionComboBox.SelectedItem;
            string tag = selectedItem.Tag.ToString();

            return tag switch
            {
                "L" => QRCodeGenerator.ECCLevel.L,
                "M" => QRCodeGenerator.ECCLevel.M,
                "Q" => QRCodeGenerator.ECCLevel.Q,
                "H" => QRCodeGenerator.ECCLevel.H,
                _ => QRCodeGenerator.ECCLevel.M
            };
        }

        private Bitmap ResizeImage(Bitmap original, int width, int height)
        {
            Bitmap resized = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.DrawImage(original, 0, 0, width, height);
            }
            return resized;
        }

        private BitmapImage ConvertToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            currentQRCode?.Dispose();
            base.OnClosed(e);
        }
    }
}