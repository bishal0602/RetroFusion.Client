
namespace SensorStream.MAUI.Services
{
    public interface ISensorService
    {
        IAccelerometer Accelerometer { get; }
        IOrientationSensor OrientationSensor { get; }
        IGyroscope Gyroscope { get; }

        void StartIfNotStarted();
        void StopIfStarted();
    }
}