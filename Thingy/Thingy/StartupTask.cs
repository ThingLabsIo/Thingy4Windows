using System;
using Windows.ApplicationModel.Background;

// Add using statements to the GrovePi libraries
using GrovePi;
using GrovePi.Sensors;
using GrovePi.I2CDevices;
using Windows.System.Threading;

namespace Thingy
{
    public sealed class StartupTask : IBackgroundTask
    {
        /**** DIGITAL SENSORS AND ACTUATORS ****/
        // Connect the button sensor to digital port 2
        IBuzzer buzzer;
        // Connect the button sensor to digital port 4
        IButtonSensor button;
        // Connect the Blue LED to digital port 5
        ILed blueLed;
        // Connect the Red LED to digital port 6
        ILed redLed;

        /**** ANALOG SENSORS ****/
        // Connect the light sensor to analog port 2
        ILightSensor lightSensor;

        /**** I2C Deices ****/
        // Connect the RGB display to one of the I2C ports
        IRgbLcdDisplay display;

        /**** Constants and Variables ****/
        // Decide an a level of ambient light at which the LED should
        // be in a completely off state (e.g. sensorValue == 700)
        const int ambientLightThreshold = 700;
        // Create a variable to track the current red LED brightness
        private int brightness;
        // Create a variable to track the current value from the Light Sensor
        private int actualAmbientLight;
        // Create a variable to track the state of the button
        private SensorStatus buttonState;
        // Create a timer to control the rateof sensor and actuator interactions
        private ThreadPoolTimer timer;
        // Create a deferral object to prevent the app from terminating
        private BackgroundTaskDeferral deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get the deferral instance
            deferral = taskInstance.GetDeferral();

            // Instantiate the sensors and actuators
            buzzer = DeviceFactory.Build.Buzzer(Pin.DigitalPin2);
            button = DeviceFactory.Build.ButtonSensor(Pin.DigitalPin4);
            blueLed = DeviceFactory.Build.Led(Pin.DigitalPin5);
            redLed = DeviceFactory.Build.Led(Pin.DigitalPin6);
            lightSensor = DeviceFactory.Build.LightSensor(Pin.AnalogPin2);
            display = DeviceFactory.Build.RgbLcdDisplay();

            buttonState = SensorStatus.Off;

            // The IO to the GrovePi sensors and actuators can generate a lot
            // of exceptions - wrap all GrovePi API calls in try/cath statements.
            try
            {
                // Set the RGB backlight to red and display a message
                display.SetBacklightRgb(255, 0, 0);
                display.SetText("The Thingy is getting started");
            }
            catch (Exception ex)
            {
                // On Error, Resume Next :)
            }

            // Start a timer to check the sensors and activate the actuators five times per second
            timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(200));
        }

        private void Timer_Tick(ThreadPoolTimer timer)
        {
            try
            {
                // Check the button state
                if (button.CurrentState != buttonState)
                {
                    // Capture the button state
                    buttonState = button.CurrentState;
                    // Change the state of the blue LED
                    blueLed.ChangeState(buttonState);
                    buzzer.ChangeState(buttonState);
                }

                // Capture the current value from the Light Sensor
                actualAmbientLight = lightSensor.SensorValue();

                // Log the amount of resistance the light sensor is providing.
                System.Diagnostics.Debug.WriteLine("R: " + lightSensor.Resistance());

                // If the actual light measurement is lower than the defined threshold
                // then define the LED brightness based on the delta between the actual
                // ambient light and the threshold value
                if (actualAmbientLight < ambientLightThreshold)
                {
                    // Use a range mapping method to conver the difference between the 
                    // actual ambient light and the threshold to a value between 0 and 255
                    // (the 8-bit range of the LED on D6 - a PWM pin). 
                    // If actual ambient light is low, the differnce between it and the threshold will be
                    // high resulting in a high brightness value.
                    brightness = Map(ambientLightThreshold - actualAmbientLight, 0, ambientLightThreshold, 0, 255);
                }
                else
                {
                    // If the actual ambient light value is above the threshold then 
                    // the LED should be completely off. Set the brightness to 0
                    brightness = 0;
                }

                // AnalogWrite uses Pulse Width Modulation (PWM) to 
                // control the brightness of the digital LED on pin D6.
                redLed.AnalogWrite(Convert.ToByte(brightness));

                // Use the brightness value to control the brightness of the RGB LCD backlight
                byte rgbVal = Convert.ToByte(brightness);
                display.SetBacklightRgb(rgbVal, rgbVal, 255);

                // Updae the RGB LCD with the light and sound levels
                display.SetText(String.Format("Thingy\nLight: {0}", actualAmbientLight));
            }
            catch (Exception ex)
            {
                // NOTE: There are frequent exceptions of the following:
                // WinRT information: Unexpected number of bytes was transferred. Expected: '. Actual: '.
                // This appears to be caused by the rapid frequency of writes to the GPIO
                // These are being swallowed here

                // If you want to see the exceptions uncomment the following:
                // System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        // This Map function is based on the Arduino Map function
        // http://www.arduino.cc/en/Reference/Map
        private int Map(int src, int in_min, int in_max, int out_min, int out_max)
        {
            return (src - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
    }
}