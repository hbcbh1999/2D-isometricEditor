﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace _2Deditor
{
    public partial class Form1 : Form
    {    
        struct Editor
        {
            public Bitmap Map;
            public float MapPosX;
            public float MapPosY;
            public float MapSize;
            public string renderInfo;
            public void init()
            {
                Map = new Bitmap(64, 64);
                MapPosX = 300 / 2 - 32;
                MapPosY = 300 / 2 - 32;

                MapSize = 1;
            }
        }
        class LookBitmap
        {
            private Bitmap bmp;
            private Rectangle rect;
            private System.Drawing.Imaging.BitmapData bmpData;
            private IntPtr ptr;
            private int bytes;
            private byte[] rgbValues;

            public LookBitmap(Bitmap input,bool byValue)
            {
                if (byValue) bmp = new Bitmap(input);
                else bmp = input;
                rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,bmp.PixelFormat);
                ptr = bmpData.Scan0;
                bytes = Math.Abs(bmpData.Stride) * bmp.Height;
                rgbValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            }
            public Bitmap getBitmap()
            {
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
                bmp.UnlockBits(bmpData);
                return bmp;
            }
            public byte[] getRGB()
            {
                return rgbValues;
            }
        }
        class Texture{
            private bool enabled;
            private bool downwards;
            private bool endColor;
            private byte size;
            private Color[] colorsZ;
            public Texture(Color[] colors,bool endColor)
            {
                this.endColor = endColor;
                colorsZ = colors;
                size = (byte)(colorsZ.Length);
                if (endColor) size--;
            }

            public void setColor(byte[] arrayRGB, int offset,byte height,byte maxHeight,float shadow)
            {
                if (endColor && height + 1 == maxHeight)
                {
                    arrayRGB[offset + 0] = (byte)(colorsZ[size].B * shadow);
                    arrayRGB[offset + 1] = (byte)(colorsZ[size].G * shadow);
                    arrayRGB[offset + 2] = (byte)(colorsZ[size].R * shadow);
                    arrayRGB[offset + 3] = (byte)(colorsZ[size].A);
                }
                else
                {
                    while (height >= size) height -= size;
                    arrayRGB[offset + 0] = (byte)(colorsZ[height].B * shadow);
                    arrayRGB[offset + 1] = (byte)(colorsZ[height].G * shadow);
                    arrayRGB[offset + 2] = (byte)(colorsZ[height].R * shadow);
                    arrayRGB[offset + 3] = (byte)(colorsZ[height].A);
                }
            }

        }

        Texture[] textures;
        Bitmap heightMap;
        Editor height;
        Editor texture;
        Editor result;
        Point lastMouse;
        Size bitmapSize;
        byte addHeight = 128;
        byte[] shadowMap;
        int angle = 45;
        float gf = 0.5f;
        float gfadd = 0.01f;
        bool curTextureEdit;
        bool renderInTimer;
        byte editValue = 1;
        float neigung = 2;

        public Form1()
        {
            height.init();
            texture.init();
            result.init();

            textures = new Texture[] {
            new Texture(new Color[] {Color.FromArgb(80, 100, 50),Color.FromArgb(80, 105, 50),Color.FromArgb(80, 100, 50)},false),
            new Texture(new Color[] {Color.FromArgb(110, 100, 80)},false),
            new Texture(new Color[] {Color.FromArgb(80, 100, 50),Color.FromArgb(80, 110, 50)},true),
            new Texture(new Color[] {Color.FromArgb(150, 150, 150),Color.FromArgb(140, 140, 160),Color.FromArgb(140, 140, 150)},false),
            new Texture(new Color[] {Color.FromArgb(30, 70, 20),Color.FromArgb(40, 90, 30)},true),
            new Texture(new Color[] {Color.FromArgb(220, 220, 255),Color.FromArgb(150, 150, 255)},true),
            new Texture(new Color[] {Color.FromArgb(200, 200, 200),Color.FromArgb(200, 100, 50)},true),
            };

            InitializeComponent();
        }

       

        private Bitmap switchMode(Bitmap heightMap)
        {
            Stopwatch now = new Stopwatch();
            now.Start();
            Graphics g;
            bitmapSize.Width = (int)(heightMap.Width * 1.5f);
            bitmapSize.Height = (int)(heightMap.Height * 1.5f);

            Bitmap heightBM = new Bitmap((int)(bitmapSize.Width), (int)(bitmapSize.Height));
            g = Graphics.FromImage(heightBM);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.TranslateTransform(bitmapSize.Width / 2, bitmapSize.Height / 2);
            g.RotateTransform(angle);
            g.DrawImage(heightMap, new RectangleF(-heightMap.Width/2, -heightMap.Height / 2, heightMap.Width, heightMap.Height), new RectangleF(0, 0, heightMap.Width, heightMap.Width), GraphicsUnit.Pixel);
            g.ResetTransform();

            LookBitmap heightLB = new LookBitmap(heightBM, false);
            LookBitmap resultLB = new LookBitmap(new Bitmap((int)(bitmapSize.Width), (int)(bitmapSize.Height / 2)), false);
            byte[] heightRGB = heightLB.getRGB();
            byte[] resultRGB = resultLB.getRGB();

            

            int width = bitmapSize.Width;
            int offsetWidth = width * 4;
            for (int ix = 0; ix < bitmapSize.Width; ix++)
            {
                for (int iy = (int)((bitmapSize.Height - 1) / neigung); iy >= 0; iy--)
                {
                    int counterDest = (ix + iy * width) * 4;
                    int counterSrc = (int)((ix + iy * neigung * width) * 4);
                    if (heightRGB[counterSrc + 1] < heightRGB[counterSrc + 1 + offsetWidth])
                    {
                        resultRGB[counterDest + 1] = heightRGB[counterSrc + 1 + offsetWidth];
                        resultRGB[counterDest + 3] = heightRGB[counterSrc + 3 + offsetWidth];
                        resultRGB[counterDest + 0] = heightRGB[counterSrc + 0 + offsetWidth];
                    }
                    else
                    {
                        resultRGB[counterDest + 1] = heightRGB[counterSrc + 1];
                        resultRGB[counterDest + 3] = heightRGB[counterSrc + 3];
                        resultRGB[counterDest + 0] = heightRGB[counterSrc + 0];
                    }
                    //if (heightRGB[counterSrc + 1] != heightRGB[counterSrc + 1 + offsetWidth]) resultRGB[counterDest + 2] = 255;
                }
            }

            if (checkBoxShadow.Checked)
            {
                for (int ix = 0; ix < bitmapSize.Width; ix++)
                {
                    for (int iy = (int)((bitmapSize.Height - 1) / neigung); iy >= 0; iy--)
                    {
                        int counter = (ix + iy * width) * 4;
                        int i = 0;
                        int max = (resultRGB[counter + 1]);
                        while (iy + i < bitmapSize.Height && resultRGB[counter + 2] < max && resultRGB[counter + 1] <= max+1)
                        {
                            if (i > 0) resultRGB[counter + 2] = (byte)(max*1f);
                            i++;
                            max--;
                            counter += 4;
                        }

                    }
                }
            }
            Console.WriteLine(now.ElapsedMilliseconds);
            return resultLB.getBitmap();
        }
        private void renderHeight(Bitmap heightMap)
        {
            Stopwatch now = new Stopwatch();
            now.Start();
            if (heightMap == null) return;
            LookBitmap heightLB = new LookBitmap(heightMap, true);
            LookBitmap resultLB = new LookBitmap(new Bitmap(heightMap.Width, heightMap.Height), false);
            byte[] heightRGB = heightLB.getRGB();
            byte[] resultRGB = resultLB.getRGB();
            int renderPixel = 0;
            int width = heightMap.Width * 4;
            for (int ix = 1; ix < heightMap.Width - 1; ix++)
            {
                for (int iy = heightMap.Height - 2; iy >= 1; iy--)
                {
                    int counter = iy* width + ((ix) * 4);
                    byte thmp = (byte)((byte)(heightRGB[counter + 1] * 20)/2);
                    resultRGB[counter + 0] = thmp;
                    resultRGB[counter + 1] = (byte)(((byte)(heightRGB[counter + 1]/20))*40);
                    byte dd = 0;
                    if (heightRGB[counter + 1] > heightRGB[counter + 1 + 4]) dd++;
                    if (heightRGB[counter + 1] > heightRGB[counter + 1 - 4]) dd++;
                    if (heightRGB[counter + 1] > heightRGB[counter + 1 + width]) dd++;
                    if (heightRGB[counter + 1] > heightRGB[counter + 1 - width]) dd++;
                    if (dd > 0)
                    {
                        //resultRGB[counter + 0] = 100;

                        //shadowMap[counter / 4] = dd;
                    }
                    //else shadowMap[counter / 4] = 0;
                    resultRGB[counter + 3] = 255;
                    //renderPixel++;

                    //renderPixel++;
                }
            }

            this.height.renderInfo = ("renderPixels => " + renderPixel)+'\n'+ ("renderTime => " + now.ElapsedMilliseconds);
            this.height.Map = resultLB.getBitmap();
        }
        private void renderTexture(Bitmap heightMap)
        {
            Stopwatch now = new Stopwatch();
            now.Start();
            if (heightMap == null) return;
            LookBitmap heightLB = new LookBitmap(heightMap, false);
            LookBitmap resultLB = new LookBitmap(new Bitmap(heightMap.Width, heightMap.Height), false);
            byte[] heightRGB = heightLB.getRGB();
            byte[] resultRGB = resultLB.getRGB();
            int renderPixel = 0;
            int width = heightMap.Width * 4;
            for (int ix = 1; ix < heightMap.Width - 1; ix++)
            {
                for (int iy = heightMap.Height - 2; iy >= 1; iy--)
                {
                    int counter = iy * width + ((ix) * 4);

                    float h, s, v;
                    h = (byte)(heightRGB[counter + 0] * 30);
                    //s =  (255-(heightRGB[counter + 1]))/255f;

                    if (heightRGB[counter + 1] != heightRGB[counter + 1 + 4]) s = 0.7f;
                    else if (heightRGB[counter + 1] != heightRGB[counter + 1 - 4]) s = 0.7f;
                    else if (heightRGB[counter + 1] != heightRGB[counter + 1 + width]) s = 0.7f;
                    else if (heightRGB[counter + 1] != heightRGB[counter + 1 - width]) s = 0.7f;
                    else s = 1;

                    if (heightRGB[counter + 0] != heightRGB[counter + 0 + 4]) v = 0.7f;
                    else if (heightRGB[counter + 0] != heightRGB[counter + 0 - 4]) v = 0.7f;
                    else if (heightRGB[counter + 0] != heightRGB[counter + 0 + width]) v = 0.7f;
                    else if (heightRGB[counter + 0] != heightRGB[counter + 0 - width]) v = 0.7f;
                    else v = 1;

                    int pos = (int)(h / 256 * 6);
                    int x = (int)(h / 256 * (256 * 6));
                    int r = 0, g = 0, b = 0;
                    while (x > 255) x -= 255;
                    switch (pos)
                    {
                        case 0: r += 255; g += x; b += 0; break;
                        case 1: r += 255 - x; g += 255; b += 0; break;
                        case 2: r += 0; g += 255; b += x; break;
                        case 3: r += 0; g += 255 - x; b += 255; break;
                        case 4: r += x; g += 0; b += 255; break;
                        case 5: r += 255; g += 0; b += 255 - x; break;
                    }
                    float pro = (((s) / 1));
                    r = (int)(r * (pro) + ((255) * (1 - pro)));//r
                    g = (int)(g * (pro) + ((255) * (1 - pro)));//g
                    b = (int)(b * (pro) + ((255) * (1 - pro)));//b

                    pro = v / 1;
                    r = (int)(r *pro);//r
                    g = (int)(g * pro);//g
                    b = (int)(b * pro);//b

                    //else shadowMap[counter / 4] = 0;

                    //if (heightRGB[counter + 0] == 1) resultRGB[counter + 2] = 50;
                    resultRGB[counter + 0] = (byte)b;
                    resultRGB[counter + 1] = (byte)g;
                    resultRGB[counter + 2] = (byte)r;
                    resultRGB[counter + 3] = 255;
                    //renderPixel++;

                    //renderPixel++;
                }
            }

            this.texture.renderInfo = ("renderPixels => " + renderPixel) + '\n' + ("renderTime => " + now.ElapsedMilliseconds);
            this.texture.Map = resultLB.getBitmap();
        }
        private void renderResult(Bitmap inputMap)
        {
            Stopwatch now = new Stopwatch();
            now.Start();
            if (inputMap == null) return;
            LookBitmap inputLB = new LookBitmap(inputMap, false);
            LookBitmap resultLB = new LookBitmap(new Bitmap(inputMap.Width, inputMap.Height + addHeight),false);
            byte[] inputRGB = inputLB.getRGB();
            byte[] resultRGB = resultLB.getRGB();


            int renderPixel = 0;
            int width = inputMap.Width;
            for (int ix = 0; ix < inputMap.Width; ix++)
            {
                for (int iy = inputMap.Height - 1; iy >= 0; iy--) //Downwards
                {
                    int counter = (ix + iy * width) * 4;
                    for (byte i = inputRGB[counter + 1]; i > 0; i--) //Downwards
                    {
                        if ((iy + addHeight) - i >= 0)//save
                        {
                            int counter2 = counter - (width * i * 4) + width * addHeight * 4;//pos + curent height
                            if (resultRGB[counter2 + 3] == 0)
                            {
                                float shadow = 1f;
                                if (i < inputRGB[counter + 2]) shadow = 0.75f;
                                textures[inputRGB[counter]].setColor(resultRGB, counter2, (byte)(i - 1), inputRGB[counter + 1], shadow);

                                //resultRGB[counter2 + 3] = 255;
                                //if (i == inputRGB[counter + 1]) resultRGB[counter2] = 100;
                                //resultRGB[counter2 + 1] = (byte)(((byte)(i * 100)) / 4 + 30);

                                renderPixel++;
                            }
                            else
                            {
                                //renderPixel++;
                                break;
                            }
                            //renderPixel++;
                        }
                    }
                }
            }

            result.renderInfo = ("renderPixels => " + renderPixel) + '\n' + ("renderTime => " + now.ElapsedMilliseconds);
            result.Map = resultLB.getBitmap();
        }

        private void render(bool all)
        {
            //try
            //{

            Bitmap inputMap = switchMode(this.heightMap);
            bitmapSize = inputMap.Size;
            shadowMap = new byte[bitmapSize.Width * bitmapSize.Height];
            if (all)
            {
                if (curTextureEdit) renderTexture(this.heightMap);
                else renderHeight(this.heightMap);
                //renderTexture(this.heightMap);
                //renderHeight(this.heightMap);
                pBHeightMap.Refresh();
                pBEditorMap.Refresh();
            }
            //result.Map = inputMap;
            renderResult(inputMap);
            pBRender.Refresh();
            //}
            //catch { }
        }

        #region EventArgs

        private void pBHeightMap_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (height.MapSize < 1) g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            else g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            Rectangle dest = new Rectangle((int)height.MapPosX, (int)height.MapPosY, (int)(height.Map.Width * height.MapSize), (int)(height.Map.Height * height.MapSize));
            g.DrawImage(height.Map, dest, new RectangleF(0, 0, height.Map.Width, height.Map.Height), GraphicsUnit.Pixel);
            g.DrawRectangle(Pens.White, dest);
            g.DrawString(height.renderInfo, new Font("consolas", 11), new SolidBrush(Color.White), new Point(0, 0));
        }
        private void pBTextureMap_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (texture.MapSize < 1) g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            else g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            Editor curEdit;
            if (curTextureEdit) curEdit = texture;
            else curEdit = height;
            Rectangle dest = new Rectangle((int)texture.MapPosX, (int)texture.MapPosY, (int)(curEdit.Map.Width * texture.MapSize), (int)(curEdit.Map.Height * texture.MapSize));
            g.DrawImage(curEdit.Map, dest, new RectangleF(0, 0, curEdit.Map.Width, curEdit.Map.Height), GraphicsUnit.Pixel);
            g.DrawRectangle(Pens.White, dest);
            g.DrawString(curEdit.renderInfo, new Font("consolas", 11), new SolidBrush(Color.White), new Point(0, 0));

        }
        private void pBRender_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (result.MapSize < 1) g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            else g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            Rectangle dest = new Rectangle((int)result.MapPosX, (int)result.MapPosY, (int)(result.Map.Width * result.MapSize), (int)((result.Map.Height) * result.MapSize));
            g.DrawImage(result.Map, dest, new RectangleF(0, 0, result.Map.Width, result.Map.Height), GraphicsUnit.Pixel);
            g.DrawRectangle(Pens.White, dest);
            g.DrawString(result.renderInfo, new Font("consolas", 11), new SolidBrush(Color.White), new Point(0, 0));
        }

        private void pBHeightMap_MouseMove(object sender, MouseEventArgs e)
        {
            pBHeightMap.Focus();
            //if (e.X > heightMapPosX && e.Y > heightMapPosY)
            //{
            if (e.Button == MouseButtons.Left)
            {
                height.MapPosX -= (lastMouse.X - e.X);
                height.MapPosY -= (lastMouse.Y - e.Y);
                pBHeightMap.Refresh();
            }
            //}
            lastMouse = e.Location;
        }
        private void pBHeightMap_MouseWheel(object sender, MouseEventArgs e)
        {
            height.MapSize += (float)(height.MapSize * e.Delta) / 1000f;
            pBHeightMap.Refresh();
        }
        private void pBTextureMap_MouseMove(object sender, MouseEventArgs e)
        {
            pBEditorMap.Focus();
            //if (e.X > heightMapPosX && e.Y > heightMapPosY)
            //{
            if (e.Button == MouseButtons.Middle)
            {
                texture.MapPosX -= (lastMouse.X - e.X);
                texture.MapPosY -= (lastMouse.Y - e.Y);
                pBEditorMap.Refresh();
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (radioButtonEditM.Checked)
                {
                    texture.MapPosX -= (lastMouse.X - e.X);
                    texture.MapPosY -= (lastMouse.Y - e.Y);
                    pBEditorMap.Refresh();
                }
                else
                {
                    Graphics g = Graphics.FromImage(this.heightMap);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(0, editValue, listBoxTexture.SelectedIndex)), new RectangleF((lastMouse.X - texture.MapPosX) / texture.MapSize, (lastMouse.Y - texture.MapPosY) / texture.MapSize, 10, 10));
                    renderInTimer = true;
                }
            }
            //}
            lastMouse = e.Location;
        }
        private void pBTextureMap_MouseWheel(object sender, MouseEventArgs e)
        {
            texture.MapSize += (float)(height.MapSize * e.Delta) / 1000f;
            pBEditorMap.Refresh();
        }
        private void pBRender_MouseMove(object sender, MouseEventArgs e)
        {
            pBRender.Focus();
            //if (e.X > heightMapPosX && e.Y > heightMapPosY)
            //{
            if (e.Button == MouseButtons.Middle)
            {
                result.MapPosX -= (lastMouse.X - e.X);
                result.MapPosY -= (lastMouse.Y - e.Y);
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (radioButtonPreM.Checked)
                {
                    result.MapPosX -= (lastMouse.X - e.X);
                    result.MapPosY -= (lastMouse.Y - e.Y);
                }
                else if (radioButtonPreR.Checked)
                {
                    angle += (lastMouse.X - e.X);
                    lastMouse = e.Location;

                    render(true);
                }
                pBRender.Refresh();
            }
            //}
            lastMouse = e.Location;
        }
        private void pBRender_MouseWheel(object sender, MouseEventArgs e)
        {
            result.MapSize += (float)(height.MapSize * e.Delta) / 1000f;
            pBRender.Refresh();
        }

        private void bRotL_Click(object sender, EventArgs e)
        {
            angle -= 45;
            render(false);
        }
        private void bRotR_Click(object sender, EventArgs e)
        {
            angle += 45;
            render(false);
        }
        private void bRot_Click(object sender, EventArgs e)
        {
            angle = 0;
        }

        private void bClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void bNew_Click(object sender, EventArgs e)
        {
            heightMap = new Bitmap("../input/test.png");
            render(true);
            timer1.Enabled = true;
        }
        private void bSwitch_Click(object sender, EventArgs e)
        {
            curTextureEdit = !curTextureEdit;
            render(true);
        }
        private void bSave_Click(object sender, EventArgs e)
        {
            render(false);
            Bitmap save = new Bitmap(result.Map);
            result.Map.Save("../output/test.png", System.Drawing.Imaging.ImageFormat.Png);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //Console.WriteLine(angle);
            if (checkBoxPreAR.Checked)
            {
                angle += 1;
            }
            render(renderInTimer);
            renderInTimer = false;
            //gf += gfadd;
            //if (gf <= 1f) gfadd = 0.01f;
            //if (gf >= 10f) gfadd = -0.01f;
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                editValue = (byte)Convert.ToInt16(textBoxValue.Text);
                textBoxValue.Text = "" + editValue;
            }
            catch { textBoxValue.Text = "" + 0; }
        }

        #endregion
    }
}
