using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using ImageMagick;

namespace TestNextionDraw
{
    class Program
    {
        private static MemoryStream _drawCommands = new MemoryStream();
 
        private static void WriteCommand(string cmd)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(cmd + "AAA");
            bytes[bytes.Length - 1] = 255;
            bytes[bytes.Length - 2] = 255;
            bytes[bytes.Length - 3] = 255;

            _drawCommands.Write(bytes, 0, bytes.Length);
        }

        private static void Gif()
        {
            using (MagickImageCollection collection = new MagickImageCollection(@"D:\Projects\TestNextionDraw\test.gif"))
            {
                // This will remove the optimization and change the image to how it looks at that point
                // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                collection.Coalesce();

                int scaleDown = 8;
                int[,] previousFrame = new int[320 / scaleDown, 240 / scaleDown];

                var resize = new MagickGeometry() { FillArea = true, Height = 240 / scaleDown };
                var area = new MagickGeometry() { FillArea = true, Width = 320 / scaleDown, Height = 240 / scaleDown };

                int index = 0;

                // Resize each image in the collection to a width of 200. When zero is specified for the height
                // the height will be calculated with the aspect ratio.
                foreach (MagickImage image in collection)
                {
                    image.Resize(resize);
                    image.Crop(area, Gravity.Center);

                    var pixels = image.GetPixels();

                    for (int y = 0; y < image.Height; y++)
                    {
                        var evenFrame = index % 2 == 0;
                        var evenLine = y % 2 == 0;

                        if (evenFrame)
                        {
                            if (!evenLine)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (evenLine)
                            {
                                continue;
                            }
                        }

                        for (int x = 0; x < image.Width; x++)
                        {
                            var pixel = pixels[x, y].ToColor();

                            var r = Math.Min(31, Math.Round((pixel.R / 255d) * 32)); // 5 bit = 32 values
                            var g = Math.Min(63, Math.Round((pixel.G / 255d) * 64)); // 6 bit = 64 values
                            var b = Math.Min(31, Math.Round((pixel.B / 255d) * 32)); // 5 bit = 32 values

                            int rgb565 = ((byte)r << 11) | ((byte)g << 5) | (byte)b;

                            if (previousFrame[x, y] != rgb565)
                            {
                                WriteCommand(string.Format("fill {0},{1},{3},{3},{2}", x * scaleDown, y * scaleDown, rgb565, scaleDown));
                            }

                            previousFrame[x, y] = rgb565;
                        }

                    }

                    index++;

                }

            }

        }

        private static void Normal()
        {
            int totalSkip = 0;
            int scaleDown = 16;
            int[,] previousFrame = new int[320, 240];
            while (true)
            {
                using (MagickImage image = new MagickImage(@"D:\Temp\wimtest\pm 252.jpg"))
                {
                    var resize = new MagickGeometry() { FillArea = true, Width = 320 / scaleDown };
                    var area = new MagickGeometry() { FillArea = true, Width = 320 / scaleDown, Height = 240 / scaleDown };

                    image.Resize(resize);
                    image.Crop(area, Gravity.North | Gravity.East);
                    var pixels = image.GetPixels();

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var pixel = pixels[x, y].ToColor();

                            var r = (pixel.R / 255d) * 32; // 5 bit = 32 values
                            var g = (pixel.G / 255d) * 64; // 6 bit = 64 values
                            var b = (pixel.B / 255d) * 32; // 5 bit = 32 values

                            int rgb565 = ((byte)r << 11) | ((byte)g << 5) | (byte)b;

                            if (previousFrame[x, y] != rgb565)
                            {
                                WriteCommand(string.Format("fill {0},{1},{3},{3},{2}", x * scaleDown, y * scaleDown, rgb565, scaleDown));
                            }
                            else
                            {
                                totalSkip++;
                            }

                            int maxY = (y * scaleDown) + scaleDown;
                            int maxX = (x * scaleDown) + scaleDown;
                            for (int y2 = y * scaleDown; y2 < maxY; y2++)
                            {
                                for (int x2 = x * scaleDown; x2 < maxX; x2++)
                                {
                                    previousFrame[x, y] = rgb565;
                                }
                            }
                        }
                    }
                }

                if (scaleDown * 0.5d < 1)
                {
                    break;
                }

                scaleDown /= 2;
            }
        }

        private static void NormalReparseMode(SerialPort port)
        {
            Action<string> send = (cmd) =>
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(cmd + "AAA");
                bytes[bytes.Length - 1] = 255;
                bytes[bytes.Length - 2] = 255;
                bytes[bytes.Length - 3] = 255;

                port.Write(bytes, 0, bytes.Length);
            };

            int scaleDown = 1;

            using (MagickImage image = new MagickImage(@"D:\Temp\wimtest\pm 252.jpg"))
            {
                var resize = new MagickGeometry() { FillArea = true, Width = 320 / scaleDown };
                var area = new MagickGeometry() { FillArea = true, Width = 320 / scaleDown, Height = 240 / scaleDown };

                image.Resize(resize);
                image.Crop(area, Gravity.North | Gravity.East);
                var pixels = image.GetPixels();

                var colorData = new MemoryStream();
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        var pixel = pixels[x, y].ToColor();

                        var r = (pixel.R / 255d) * 32; // 5 bit = 32 values
                        var g = (pixel.G / 255d) * 64; // 6 bit = 64 values
                        var b = (pixel.B / 255d) * 32; // 5 bit = 32 values

                        int rgb565 = ((byte)r << 11) | ((byte)g << 5) | (byte)b;
                        var buffer = BitConverter.GetBytes((ushort)rgb565);
                        colorData.Write(buffer, 0, buffer.Length);
                    }
                }

                ManualResetEvent waitForPaint = new ManualResetEvent(false);
                ManualResetEvent waitForBuffer = new ManualResetEvent(false);
                ManualResetEvent waitForDone = new ManualResetEvent(false);

                int bufferSize = 896;

                send("cls 0");
                send("bkcmd=0");
                send("x.val=0");
                send("y.val=0");
                send("scale.val=" + scaleDown);
                send("recmod=1");

                // clear current receive buffer
                port.DiscardInBuffer();

                // wait for our own return data signal
                port.DataReceived += (s, e) =>
                {
                    while (port.BytesToRead > 0)
                    {
                        var cmd = port.ReadByte();
                        if (cmd == 0x9e)
                        {
                            waitForPaint.Set();
                        }

                        if (cmd == 0x9f)
                        {
                            waitForDone.Set();
                        }
                    }
                };

                // send color data
                Task.Run(() =>
                {
                    colorData.Position = 0;
                    while (true)
                    {
                        var buffer = new byte[bufferSize];
                        var read = colorData.Read(buffer, 0, buffer.Length);

                        waitForPaint.Reset();
                        port.Write(buffer, 0, read);

                        if (read < buffer.Length)
                        {
                            return; // all data sent
                        }

                        waitForPaint.WaitOne(); // Nextion must return 0x9e

                    }
                });

                waitForDone.WaitOne();

            }

        }

        private static void GifReparseMode(SerialPort port)
        {
            Action<string> send = (cmd) =>
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(cmd + "AAA");
                bytes[bytes.Length - 1] = 255;
                bytes[bytes.Length - 2] = 255;
                bytes[bytes.Length - 3] = 255;

                port.Write(bytes, 0, bytes.Length);
            };

            MemoryStream colorData = new MemoryStream();
            int scaleDown = 8;

            using (MagickImageCollection collection = new MagickImageCollection(@"D:\Projects\TestNextionDraw\test.gif"))
            {
                // This will remove the optimization and change the image to how it looks at that point
                // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                collection.Coalesce();


                var resize = new MagickGeometry() { FillArea = true, Height = 240 / scaleDown };
                var area = new MagickGeometry() { FillArea = true, Width = 320 / scaleDown, Height = 240 / scaleDown };
                
                // Resize each image in the collection to a width of 200. When zero is specified for the height
                // the height will be calculated with the aspect ratio.
                foreach (MagickImage image in collection)
                {
                    image.Resize(resize);
                    image.Crop(area, Gravity.Center);

                    var pixels = image.GetPixels();

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var pixel = pixels[x, y].ToColor();

                            var r = Math.Min(31, Math.Round((pixel.R / 255d) * 32)); // 5 bit = 32 values
                            var g = Math.Min(63, Math.Round((pixel.G / 255d) * 64)); // 6 bit = 64 values
                            var b = Math.Min(31, Math.Round((pixel.B / 255d) * 32)); // 5 bit = 32 values

                            int rgb565 = ((byte)r << 11) | ((byte)g << 5) | (byte)b;
                            ushort color = (ushort)rgb565;

                            var buffer = BitConverter.GetBytes(color);
                            colorData.Write(buffer, 0, buffer.Length);
                        }
                    }

                }

            }

            ManualResetEvent waitForPaint = new ManualResetEvent(false);
            ManualResetEvent waitForBuffer = new ManualResetEvent(false);
            ManualResetEvent waitForDone = new ManualResetEvent(false);

            int bufferSize = 1000;

            send("cls 0");
            send("bkcmd=0");
            send("x.val=0");
            send("y.val=0");
            send("scale.val=" + scaleDown);
            send("recmod=1");

            // clear current receive buffer
            port.DiscardInBuffer();

            // wait for our own return data signal
            port.DataReceived += (s, e) =>
            {
                while (port.BytesToRead > 0)
                {
                    var cmd = port.ReadByte();
                    if (cmd == 0x9e)
                    {
                        waitForPaint.Set();
                    }

                    if (cmd == 0x9f)
                    {
                        waitForDone.Set();
                    }
                }
            };

            // send color data
            Task.Run(() =>
            {
                colorData.Position = 0;
                while (true)
                {
                    var buffer = new byte[bufferSize];
                    var read = colorData.Read(buffer, 0, buffer.Length);

                    waitForPaint.Reset();
                    port.Write(buffer, 0, read);

                    waitForPaint.WaitOne(); // Nextion must return 0x9e

                    if (read < buffer.Length) // back to start
                    {
                        colorData.Position = 0;
                    }
                }
            });
        }
        
        private static void GifReparseModeOpti(SerialPort port)
        {
            Action<string> send = (cmd) =>
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(cmd + "AAA");
                bytes[bytes.Length - 1] = 255;
                bytes[bytes.Length - 2] = 255;
                bytes[bytes.Length - 3] = 255;

                port.Write(bytes, 0, bytes.Length);
            };

            Action<Stream, ushort> writeData = (s, u) =>
            {
                var buffer = BitConverter.GetBytes(u);
                s.Write(buffer, 0, buffer.Length);
            };


            MemoryStream colorData = new MemoryStream();
            MemoryStream baseFrame = new MemoryStream();

            int scaleDown = 8;

            using (MagickImageCollection collection = new MagickImageCollection(@"D:\Projects\TestNextionDraw\test.gif"))
            {
                // This will remove the optimization and change the image to how it looks at that point
                // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                collection.Coalesce();


                int[,] previousFrame = new int[320 / scaleDown, 240 / scaleDown];

                var resize = new MagickGeometry() { FillArea = true, Height = 240 / scaleDown };
                var area = new MagickGeometry() { FillArea = true, Width = 320 / scaleDown, Height = 240 / scaleDown };

                //// first pass - paint every frame to get final image of last frame in previousFrame array
                foreach (MagickImage image in collection)
                {
                    image.Resize(resize);
                    image.Crop(area, Gravity.Center);

                    var pixels = image.GetPixels();

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var pixel = pixels[x, y].ToColor();

                            var r = Math.Min(31, Math.Round((pixel.R / 255d) * 32)); // 5 bit = 32 values
                            var g = Math.Min(63, Math.Round((pixel.G / 255d) * 64)); // 6 bit = 64 values
                            var b = Math.Min(31, Math.Round((pixel.B / 255d) * 32)); // 5 bit = 32 values

                            int rgb565 = ((byte)r << 11) | ((byte)g << 5) | (byte)b;
                            previousFrame[x, y] = rgb565;
                        }
                    }
                }

                // build base frame data
                for (int y = 0; y < area.Height; y++)
                {
                    for (int x = 0; x < area.Width; x++)
                    {
                        writeData(baseFrame, (ushort)previousFrame[x, y]);
                    }
                }

                //second pass - actually get pixel data as difference from previous frame
                //the first frame will also contain only difference
                foreach (MagickImage image in collection)
                {
                    var pixels = image.GetPixels();

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var pixel = pixels[x, y].ToColor();

                            var r = Math.Min(31, Math.Round((pixel.R / 255d) * 32)); // 5 bit = 32 values
                            var g = Math.Min(63, Math.Round((pixel.G / 255d) * 64)); // 6 bit = 64 values
                            var b = Math.Min(31, Math.Round((pixel.B / 255d) * 32)); // 5 bit = 32 values

                            int rgb565 = ((byte)r << 11) | ((byte)g << 5) | (byte)b;

                            if (previousFrame[x, y] != rgb565)
                            {
                                writeData(colorData, (ushort)x);
                                writeData(colorData, (ushort)y);
                                writeData(colorData, (ushort)rgb565);
                            }

                            previousFrame[x, y] = rgb565;

                        }
                    }

                }

            }

            ManualResetEvent waitForPaint = new ManualResetEvent(false);
            ManualResetEvent waitForBuffer = new ManualResetEvent(false);
            ManualResetEvent waitForDone = new ManualResetEvent(false);
            
            int bufferSize = 896;

            // clear current receive buffer
            port.DiscardInBuffer();

            // wait for our own return data signal
            port.DataReceived += (s, e) =>
            {
                while (port.BytesToRead > 0)
                {
                    var cmd = port.ReadByte();
                    if (cmd == 0x9e)
                    {
                        waitForPaint.Set();
                    }

                    if (cmd == 0x9f)
                    {
                        waitForDone.Set();
                    }
                }
            };

            send("cls 0");
            send("bkcmd=0");
            send("x.val=0");
            send("y.val=0");
            send("scale.val=" + scaleDown);
            send("tm0.en=1"); // base frame
            send("recmod=1");

            Task.Delay(250).Wait();

            // send base frame
            baseFrame.Position = 0;
            while (true)
            {
                var buffer = new byte[bufferSize];
                var read = baseFrame.Read(buffer, 0, buffer.Length);

                waitForPaint.Reset();
                port.Write(buffer, 0, read);

                waitForPaint.WaitOne(); // Nextion must return 0x9e

                if (read < buffer.Length) // back to start
                {
                    break;
                }
            }

            Task.Delay(250).Wait();

            send("DRAKJHSUYDGBNCJHGJKSHBDN"); // restart interactive mode
            send("tm0.en=0");
            send("x.val=0"); // re-sync position
            send("y.val=0");
            send("tm1.en=1"); // begin diff frame drawing
            send("recmod=1");

            Task.Delay(250).Wait();
            
            //// send diff frame
            Task.Run(() =>
            {
                colorData.Position = 0;
                while (true)
                {
                    var buffer = new byte[bufferSize];
                    var read = colorData.Read(buffer, 0, buffer.Length);

                    waitForPaint.Reset();
                    port.Write(buffer, 0, read);

                    waitForPaint.WaitOne(); // Nextion must return 0x9e

                    if (read < buffer.Length) // back to start
                    {
                        colorData.Position = 0;
                    }
                }
            });
        }

        private static void GifReparseModeOpti2(SerialPort port)
        {
            Action<string> send = (cmd) =>
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(cmd + "AAA");
                bytes[bytes.Length - 1] = 255;
                bytes[bytes.Length - 2] = 255;
                bytes[bytes.Length - 3] = 255;

                port.Write(bytes, 0, bytes.Length);
            };

            Action<Stream, ushort> writeData = (s, u) =>
            {
                var buffer = BitConverter.GetBytes(u);
                s.Write(buffer, 0, buffer.Length);
            };


            MemoryStream colorData = new MemoryStream();
            MemoryStream baseFrame = new MemoryStream();

            int scaleDown = 4;

            using (MagickImageCollection collection = new MagickImageCollection(@"D:\Projects\TestNextionDraw\test.gif"))
            {
                // This will remove the optimization and change the image to how it looks at that point
                // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                collection.Coalesce();


                int[,] previousFrame = new int[320 / scaleDown, 240 / scaleDown];

                var resize = new MagickGeometry() { FillArea = true, Height = 240 / scaleDown };
                var area = new MagickGeometry() { FillArea = true, Width = 320 / scaleDown, Height = 240 / scaleDown };

                //// first pass - paint every frame to get final image of last frame in previousFrame array
                foreach (MagickImage image in collection)
                {
                    image.Resize(resize);
                    image.Crop(area, Gravity.Center);

                    var pixels = image.GetPixels();

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var pixel = pixels[x, y].ToColor();

                            var r = Math.Min(31, Math.Round((pixel.R / 255d) * 32)); // 5 bit = 32 values
                            var g = Math.Min(63, Math.Round((pixel.G / 255d) * 64)); // 6 bit = 64 values
                            var b = Math.Min(31, Math.Round((pixel.B / 255d) * 32)); // 5 bit = 32 values

                            int rgb565 = ((byte)r << 11) | ((byte)g << 5) | (byte)b;
                            previousFrame[x, y] = rgb565;
                        }
                    }
                }

                // build base frame data
                for (int y = 0; y < area.Height; y++)
                {
                    for (int x = 0; x < area.Width; x++)
                    {
                        writeData(baseFrame, (ushort)previousFrame[x, y]);
                    }
                }

                int index = 0;

                //second pass - actually get pixel data as difference from previous frame
                //the first frame will also contain only difference
                foreach (MagickImage image in collection)
                {
                    var pixels = image.GetPixels();

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var evenFrame = index % 2 == 0;
                            var evenLine = y % 2 == 0;

                            if (evenFrame)
                            {
                                if (!evenLine)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (evenLine)
                                {
                                    continue;
                                }
                            }

                            var pixel = pixels[x, y].ToColor();

                            var r = Math.Min(31, Math.Round((pixel.R / 255d) * 32)); // 5 bit = 32 values
                            var g = Math.Min(63, Math.Round((pixel.G / 255d) * 64)); // 6 bit = 64 values
                            var b = Math.Min(31, Math.Round((pixel.B / 255d) * 32)); // 5 bit = 32 values

                            int rgb565 = ((byte)r << 11) | ((byte)g << 5) | (byte)b;

                            if (previousFrame[x, y] != rgb565)
                            {
                                writeData(colorData, (ushort)x);
                                writeData(colorData, (ushort)y);
                                writeData(colorData, (ushort)rgb565);
                            }

                            previousFrame[x, y] = rgb565;

                        }
                    }

                    index++;

                }

            }

            ManualResetEvent waitForPaint = new ManualResetEvent(false);
            ManualResetEvent waitForBuffer = new ManualResetEvent(false);
            ManualResetEvent waitForDone = new ManualResetEvent(false);

            int bufferSize = 896;

            // clear current receive buffer
            port.DiscardInBuffer();

            // wait for our own return data signal
            port.DataReceived += (s, e) =>
            {
                while (port.BytesToRead > 0)
                {
                    var cmd = port.ReadByte();
                    if (cmd == 0x9e)
                    {
                        waitForPaint.Set();
                    }

                    if (cmd == 0x9f)
                    {
                        waitForDone.Set();
                    }
                }
            };

            send("cls 0");
            send("bkcmd=0");
            send("x.val=0");
            send("y.val=0");
            send("scale.val=" + scaleDown);
            send("tm0.en=1"); // base frame
            send("recmod=1");

            Task.Delay(250).Wait();

            // send base frame
            baseFrame.Position = 0;
            while (true)
            {
                var buffer = new byte[bufferSize];
                var read = baseFrame.Read(buffer, 0, buffer.Length);

                waitForPaint.Reset();
                port.Write(buffer, 0, read);

                waitForPaint.WaitOne(); // Nextion must return 0x9e

                if (read < buffer.Length) // back to start
                {
                    break;
                }
            }

            Task.Delay(250).Wait();

            send("DRAKJHSUYDGBNCJHGJKSHBDN"); // restart interactive mode
            send("tm0.en=0");
            send("x.val=0"); // re-sync position
            send("y.val=0");
            send("tm1.en=1"); // begin diff frame drawing
            send("recmod=1");

            Task.Delay(250).Wait();

            //// send diff frame
            Task.Run(() =>
            {
                colorData.Position = 0;
                while (true)
                {
                    var buffer = new byte[bufferSize];
                    var read = colorData.Read(buffer, 0, buffer.Length);

                    waitForPaint.Reset();
                    port.Write(buffer, 0, read);

                    waitForPaint.WaitOne(); // Nextion must return 0x9e

                    if (read < buffer.Length) // back to start
                    {
                        colorData.Position = 0;
                    }
                }
            });
        }


        static void Main(string[] args)
        {
            //Normal();

            //Gif();

            var port = new SerialPort("COM6", 921600, Parity.None, 8, StopBits.One);
            port.WriteTimeout = 10000;
            port.Open();

            //NormalReparseMode(port);

            //GifReparseMode(port);

            //GifReparseModeOpti(port);

            GifReparseModeOpti2(port);

            Console.Read();

            //_drawCommands.Position = 0;

            //while (true)
            //{
            //    var command = new byte[256];
            //    var read = _drawCommands.Read(command, 0, command.Length);
            //    if (read < command.Length)
            //    {
            //        _drawCommands.Position = 0;

            //    }

            //    try
            //    {
            //        port.Write(command, 0, read);
            //    }
            //    catch (Exception)
            //    {
            //    }

            //    Console.WriteLine(_drawCommands.Position);
            //    Thread.Sleep(25);
            //}
        }
    }
}
