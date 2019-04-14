using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Test
{
    abstract class Filters
    {
        protected abstract Color calculateNewPicelColor(Bitmap sourceImage, int x, int y);
        public int Clamp(int value, int min, int max) //цвет (3 его компонента) принимает значение от 0 до 255, чтобы при работе фильтра не выходить за рамки, мы используем эту функцию 
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
        public virtual Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / resultImage.Width * 100)); //будет сигнализировать элементу BackgroundWorker о текущем прогрессе
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPicelColor(sourceImage, i, j));
                }
            }

            return resultImage;
        }

    }
    class InvertFilter : Filters
    {
        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(255 - sourceColor.R, 255 - sourceColor.G, 255 - sourceColor.B);
            return resultColor;
        }
    }

    class MatrixFilter : Filters
    {
        protected float[,] kernel = null;
        protected MatrixFilter() { }
        public MatrixFilter(float[,] kernel)
        {
            this.kernel = kernel;
        }
        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0; //переменные типа float, в которых будут храниться цветовые компоненты результирующего цвета
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++) //два вложенных цикла, которые будут перебирать окрестность пикселя
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {   //х и y – координаты текущего пикселя
                    //Чтобы на граничных пикселях не выйти за границы изображения, используйте функцию Clamp.
                    //В переменных idX и idY хранятся координаты пикселей-соседей пикселя (x,y), с которым совмещается центр матрицы, 
                    // и для которого происходит вычисления цвета
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1); //l и k принимают значения от -radius до radius и означают положение элемента в матрице фильтра(ядре), если начало отсчета поместить в центр матрицы
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            }
            return Color.FromArgb(Clamp((int)resultR, 0, 255), Clamp((int)resultG, 0, 255), Clamp((int)resultB, 0, 255));
        }
    }

    class BlurFilter : MatrixFilter
    {
        public BlurFilter()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
                for (int j = 0; j < sizeY; j++)
                    kernel[i, j] = 1.0f / (float)(sizeX * sizeY);
        }
    }

    class GaussianFilter : MatrixFilter
    {
        public void createGaussianKernel(int radius, float sigma)
        {
            //определяем размер ядра
            int size = 2 * radius + 1;
            //создаем ядро фильтра
            kernel = new float[size, size];
            //коэф. нормировки ядра (степень размытия)
            float norm = 0;
            //рассчитываем ядро линейного фильтра
            for (int i = -radius; i <= radius; i++)
                for (int j = -radius; j <= radius; j++)
                {
                    kernel[i + radius, j + radius] = (float)(Math.Exp(-(i * i + j * j) / (sigma * sigma)));
                    norm += kernel[i + radius, j + radius];
                }
            //нормируем ядро
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    kernel[i, j] /= norm;

        }

        public GaussianFilter()
        {
            createGaussianKernel(3, 2);
        }
    }

    class GrayScaleFilter : Filters
    {
        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            double Intensity = 0.36 * sourceColor.R + 0.53 * sourceColor.G + 0.11 * sourceColor.B;
            int value = (int)Intensity;
            Color resultColor = Color.FromArgb(value, value, value);
            return resultColor;
        }
    }

    class Sepia : Filters
    {
        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            double Intensity = 0.36 * sourceColor.R + 0.53 * sourceColor.G + 0.11 * sourceColor.B;
            int value = (int)Intensity;
            float k = 20.0F;
            Color resultColor = Color.FromArgb(//value + 2 * k, (int)(value + 0.5 * k), value - 1 * k);
                Clamp((int)(Intensity + 2 * k), 0, 255), Clamp((int)(Intensity + 0.5 * k), 0, 255), Clamp((int)(Intensity - 1 * k), 0, 255));
            return resultColor;
        }
    }


    class Brightness : Filters
    {
        private int factor;
        public Brightness(int value)
        {
            factor = value;
        }
        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(Clamp((sourceColor.R + factor), 0, 255), Clamp((sourceColor.G + factor), 0, 255), Clamp((sourceColor.B + factor), 0, 255));
            return resultColor;
        }
    }

    class DoubleMatrixFilter : MatrixFilter
    {
        protected int[,] OY;
        protected int[,] OX;

        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusXOX = OX.GetLength(0) / 2;
            int radiusYOX = OX.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusYOX; l <= radiusYOX; l++) //сначала матрица по x
            {
                for (int k = -radiusXOX; k <= radiusXOX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * OX[k + radiusXOX, l + radiusYOX];
                    resultG += neighborColor.G * OX[k + radiusXOX, l + radiusYOX];
                    resultB += neighborColor.B * OX[k + radiusXOX, l + radiusYOX];
                }
            }
            Color newColor = Color.FromArgb(Clamp((int)(Math.Abs(resultR)), 0, 255), Clamp((int)(Math.Abs(resultG)), 0, 255), Clamp((int)(Math.Abs(resultB)), 0, 255));
            resultR = 0;
            resultG = 0;
            resultB = 0;
            int radiusXOY = OY.GetLength(0) / 2;
            int radiusYOY = OY.GetLength(1) / 2;
            for (int k = -radiusXOY; k <= radiusXOY; k++) //теперь матрица по y
            {
                for (int l = -radiusYOY; l <= radiusYOY; l++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * OY[k + radiusXOY, l + radiusYOY];
                    resultG += neighborColor.G * OY[k + radiusXOY, l + radiusYOY];
                    resultB += neighborColor.B * OY[k + radiusXOY, l + radiusYOY];
                }
            }
            Color secondColor = Color.FromArgb(Clamp((int)(Math.Abs(resultR)), 0, 255), Clamp((int)(Math.Abs(resultG)), 0, 255), Clamp((int)(Math.Abs(resultB)), 0, 255));
            int resR = (int)(Math.Sqrt(Math.Pow(newColor.R, 2)) + Math.Pow(secondColor.R, 2));
            int resG = (int)(Math.Sqrt(Math.Pow(newColor.G, 2)) + Math.Pow(secondColor.G, 2));
            int resB = (int)(Math.Sqrt(Math.Pow(newColor.B, 2)) + Math.Pow(secondColor.B, 2));
            return Color.FromArgb(Clamp(resR, 0, 255), Clamp(resG, 0, 255), Clamp(resB, 0, 255));
        }
    }

    class SobelFilter : DoubleMatrixFilter
    {
        public SobelFilter()
        {
            OY = new int[3, 3] { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } };
            OX = new int[3, 3] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
        }
    }

    class Sharpness : MatrixFilter
    {
        public Sharpness()
        {
            kernel = new float[3, 3] { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } };
        }
    }

    class SharrOperator : DoubleMatrixFilter
    {
        public SharrOperator()
        {
            OY = new int[3, 3] { { 3, 10, 3 }, { 0, 0, 0 }, { -3, -10, -3 } };
            OX = new int[3, 3] { { 3, 0, -3 }, { 10, 0, -10 }, { 3, 0, -3 } };
        }
    }

    class PruittOperator : DoubleMatrixFilter
    {
        public PruittOperator()
        {
            OY = new int[3, 3] { { -1, -1, -1 }, { 0, 0, 0 }, { 1, 1, 1 } };
            OX = new int[3, 3] { { -1, 0, 1 }, { -1, 0, 1 }, { -1, 0, 1 } };
        }
    }

    class Transfer : Filters
    {
        private int i;
        public Transfer(int how)
        {
            i = how;
        }
        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            int j = x + i;

            if (j >= sourceImage.Width)
            {
                Color resultColor = Color.FromArgb(0, 0, 0);
                return resultColor;
            }
            Color sourceColor = sourceImage.GetPixel(j, y);
            return sourceColor;
        }
    }

    class Rotate : Filters
    {
        private int k, l;
        private double radian;
        private double GetRadian(int degree)
        {
            return ((degree * Math.PI) / 180);
        }

        public Rotate(int degree)
        {
            radian = GetRadian(degree);
        }
        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            int xo = sourceImage.Width / 2;
            int yo = sourceImage.Height / 2;
            k = (int)((x - xo) * Math.Cos(radian) - (y - yo) * Math.Sin(radian) + xo);
            l = (int)((x - xo) * Math.Sin(radian) + (y - yo) * Math.Cos(radian) + yo);
            if ((k > (sourceImage.Width - 1)) || (k <= 0) || (l > (sourceImage.Height - 1)) || (l <= 0))
            {
                return Color.Black;
            }
            Color sourceColor = sourceImage.GetPixel(k, l);
            return sourceColor;
        }
    }
    //!
    class WavesOne : Filters
    {
        private int k;
        protected double GetRadian(double degree)
        {
            return ((degree * Math.PI) / 180);
        }

        protected virtual int GetX(int x, int y)
        {
            return (int)(x + 20 * Math.Sin(GetRadian(((2 * Math.PI * y) / 60))));
        }
        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            k = GetX(x, y);
            if ((k > (sourceImage.Width - 1)) | (k <= 0))
            {
                return Color.Black;
            }
            Color sourceColor = sourceImage.GetPixel(k, y);
            return sourceColor;
        }
    }
    //!
    class WavesTwo : WavesOne
    {
        protected override int GetX(int x, int y)
        {
            return (int)(x + 20 * Math.Sin(GetRadian(((2 * Math.PI * x) / 30))));
        }
    }

    class GetSmall : Filters
    {
        private int k, l;
        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            Random rand = new Random();
            k = (int)((x + rand.Next(1) - 0.5) * 10);
            l = (int)((y + rand.Next(1) - 0.5) * 10);
            if ((k > (sourceImage.Width - 1)) || (k <= 0) || (l > (sourceImage.Height - 1)) || (l <= 0))
            {
                return Color.Black;
            }
            Color sourceColor = sourceImage.GetPixel(k, l);
            return sourceColor;
        }
    }
    //!
    class Glass : Filters
    {
        private int k, l;
        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            Random rand = new Random();
            k = x + (int)((rand.Next(1) - 0.5) * 10);
            l = y + (int)((rand.Next(1) - 0.5) * 10);
            if ((k > (sourceImage.Width - 1)) || (k <= 0) || (l > (sourceImage.Height - 1)) || (l <= 0))
            {
                return Color.Black;
            }
            Color sourceColor = sourceImage.GetPixel(k, l);
            return sourceColor;
        }
    }
    //!
    class MotionBlur : MatrixFilter
    { //Doesn`t work
        private int size;//, SizeX, SizeY;
        public MotionBlur()
        {
            /*SizeX = sourceImage.Width;
            SizeY = sourceImage.Height;
            if (sourceImage.Width <= sourceImage.Height )
                size = sourceImage.Width;
            else size = sourceImage.Height;*/
            size = 5;
            kernel = new float[size, size];
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    if (i == j)
                        kernel[i, j] = 1 / size;
                    else kernel[i, j] = 0;
                }
        }
    }

        //Гипотезы
        class GrayWorld : Filters
        {
            public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
            {
                Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
                double MiddleR = 0, MiddleG = 0, MiddleB = 0;
                for (int i = 0; i < sourceImage.Width; i++)
                {
                    for (int j = 0; j < sourceImage.Height; j++)
                    {
                        //посчитаем средние яркости по всем каналам
                        Color processColor = sourceImage.GetPixel(i, j);
                        MiddleR += processColor.R;
                        MiddleG += processColor.G;
                        MiddleB += processColor.B;
                    }
                }
                //посчитаем средние яркости по всем каналам
                MiddleR /= (sourceImage.Width + sourceImage.Height);
                MiddleG /= (sourceImage.Width + sourceImage.Height);
                MiddleB /= (sourceImage.Width + sourceImage.Height);

                double Avg = (MiddleR + MiddleG + MiddleB) / 3;
                for (int i = 0; i < sourceImage.Width; i++)
                {
                    worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                    if (worker.CancellationPending)
                        return null;
                    for (int j = 0; j < sourceImage.Height; j++)
                    {
                        Color sourceColor = sourceImage.GetPixel(i, j);
                    Color resultColor = Color.FromArgb(Clamp((int)(sourceColor.R * Avg / MiddleR), 0, 255),
                                                        Clamp((int)(sourceColor.G * Avg / MiddleG), 0, 255),
                                                        Clamp((int)(sourceColor.B * Avg / MiddleB), 0, 255));
                        resultImage.SetPixel(i, j, resultColor);
                    }
                }
                return resultImage;
            }

          protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
            {
                Color sourceColor = sourceImage.GetPixel(x, y);
                Color resultColor = Color.FromArgb(255 - sourceColor.R, 255 - sourceColor.G, 255 - sourceColor.B);
                return resultColor;
            }
        }

  

}
    
   

    /*class YCbCr : Filters
    {
        protected override Color calculateNewPicelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            double Intensity = 0.36 * sourceColor.R + 0.53 * sourceColor.G + 0.11 * sourceColor.B;
            int value = (int)Intensity;
            Color resultColor = Color.FromArgb(value, value, value);
            return resultColor;
        }
    }*/