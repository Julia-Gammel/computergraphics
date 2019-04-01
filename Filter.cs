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
        public Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)(float)i / resultImage.Width * 100); //будет сигнализировать элементу BackgroundWorker о текущем прогрессе
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
                for (int j = 0; j < sizeX; j++)
                    kernel[i, j] = 1.0f / (float)(sizeX * sizeY); //ошибка тут
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

    class Sepia
    {

    }
}
