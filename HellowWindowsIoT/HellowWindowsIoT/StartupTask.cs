using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;

// Add using statements to the GrovePi libraries
using GrovePi;
using GrovePi.Sensors;
using Windows.System.Threading;

namespace HellowWindowsIoT
{
    public sealed class StartupTask : IBackgroundTask
    {
        ThreadPoolTimer timer;
        BackgroundTaskDeferral deferral;
        ILed led;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();

            // Connect the LED to digital port 4
            led = DeviceFactory.Build.Led(Pin.DigitalPin4);

            // Create a timer that will 'tick' every one-second
            timer = ThreadPoolTimer.CreatePeriodicTimer(this.Timer_Tick, TimeSpan.FromSeconds(1));
        }

        private void Timer_Tick(ThreadPoolTimer timer)
        {
            try
            {
                led.ChangeState((led.CurrentState == SensorStatus.Off) ? SensorStatus.On : SensorStatus.Off);
            }
            catch (Exception ex)
            {
                // Swallow the exception
            }
        }
    }
}
