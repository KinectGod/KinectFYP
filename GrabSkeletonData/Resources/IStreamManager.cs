using System.Windows.Media.Imaging;

namespace GrabSkeletonData
{
    public interface IStreamManager
    {
        WriteableBitmap Bitmap { get; }
    }
}