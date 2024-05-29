namespace SensorStream.MAUI.Services;

public class SensorService : ISensorService
{
    public IAccelerometer Accelerometer { get; } = Microsoft.Maui.Devices.Sensors.Accelerometer.Default;
    public IGyroscope Gyroscope { get; } = Microsoft.Maui.Devices.Sensors.Gyroscope.Default;
    public IOrientationSensor OrientationSensor { get; } = Microsoft.Maui.Devices.Sensors.OrientationSensor.Default;

    public SensorService()
    {

    }

    public void StartIfNotStarted()
    {
        if (!Accelerometer.IsMonitoring)
        {
            Accelerometer.Start(SensorSpeed.Game);
        }
        if (!Gyroscope.IsMonitoring)
        {
            Gyroscope.Start(SensorSpeed.Game);
        }
        if (!OrientationSensor.IsMonitoring)
        {
            OrientationSensor.Start(SensorSpeed.Game);
        }
    }


    public void StopIfStarted()
    {
        if (Accelerometer.IsMonitoring)
        {
            Microsoft.Maui.Devices.Sensors.Accelerometer.Stop();
        }
        if (Gyroscope.IsMonitoring)
        {
            Gyroscope.Stop();
        }
        if (OrientationSensor.IsMonitoring)
        {
            OrientationSensor.Stop();
        }
    }
}
