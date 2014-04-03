using System.Windows.Media.Imaging;

namespace TaiChiLearning
{
    public interface IStreamManager
    {
        WriteableBitmap Bitmap { get; }
    }
}