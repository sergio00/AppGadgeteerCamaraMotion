using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;




namespace GadgeteerAppMotionDetect
{
    public partial class Program
    {
        //Objetos de interface gráfica GLIDE

        private GT.Timer timer;


        Bitmap mBitmap; //shared image buffer between camera and display
        DateTime mLastWarn = DateTime.MinValue;
        ArrayList posiciones = new ArrayList();
        ArrayList actualColores = new ArrayList();
        ArrayList lastColores = new ArrayList();
        static Bitmap currentBitmap, previousBitmap;
        Boolean block = false;
        
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {


            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");

            //Carga la ventana principal
            mBitmap = new Bitmap(camera.CurrentPictureResolution.Width, camera.CurrentPictureResolution.Height); //initialize buffer to camera view size
            camera.BitmapStreamed += camera_BitmapStreamed;
            

            startService();
            ethernetJ11D.NetworkDown += ethernetJ11D_NetworkDown;
            ethernetJ11D.NetworkUp += ethernetJ11D_NetworkUp;
            timer = new GT.Timer(600,GT.Timer.BehaviorType.RunOnce);
            timer.Tick += new GT.Timer.TickEventHandler(timer_Tick);


        }

        void ethernetJ11D_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            camera.StartStreaming(mBitmap);
            Debug.Print("Internet Up");
            //timer.Start();

        }

        void ethernetJ11D_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            camera.StopStreaming();
            Debug.Print("No Internet");
            timer.Stop();
        }


        private void timer_Tick(GT.Timer timer)
        {
            HttpRequest request = HttpHelper.CreateHttpGetRequest("http://api.thingspeak.com/update?api_key=4UOR8KYN0VL1AH7B&field1=5");
            request.ResponseReceived += request_ResponseReceived;
            request.SendRequest();
        }

        void request_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            Debug.Print("MENSAJE:" + response.StatusCode);


        }

        

        void camera_BitmapStreamed(GTM.GHIElectronics.Camera sender, Bitmap bitmap)
        {
            //320*240

            displayT35.SimpleGraphics.DisplayImage(bitmap, 0, 0);
            detectionMove(bitmap);
        }


        ArrayList FillColor(Bitmap bitmap)
        {
            int i, j;
            ArrayList temp = new ArrayList();
            int posX = 0, posY = 0;

            for (i = 0; i < 3; i++)
            {
                for (j = 0; j < 3; j++)
                {
                    var pixel = bitmap.GetPixel(posX, posY);
                    Color color = ColorUtility.ColorFromRGB(ColorUtility.GetRValue(pixel),
                                  ColorUtility.GetGValue(pixel), ColorUtility.GetBValue(pixel));
                    temp.Add(color);
                    posX += 159;
                }
                posY = +119;
                posX = 0;
            }
            return temp;
        }

        void detectionMove(Bitmap bitmap)
        {
            int cont = 0;

            if (currentBitmap != null)
            {
                previousBitmap = currentBitmap;
                lastColores = FillColor(previousBitmap);
            }

            if (previousBitmap != null)
            {

                for (int x = 0; x < 9; x++)
                {
                    Color oldColour = (Color)lastColores[x];
                    Color newColour = (Color)actualColores[x];
                    int deltaRed = System.Math.Abs(ColorUtility.GetRValue(oldColour) - ColorUtility.GetRValue(newColour));
                    int deltaGreen = System.Math.Abs(ColorUtility.GetGValue(oldColour) - ColorUtility.GetGValue(newColour));
                    int deltaBlue = System.Math.Abs(ColorUtility.GetBValue(oldColour) - ColorUtility.GetBValue(newColour));
                    int deltaTotal = deltaRed + deltaGreen + deltaBlue;
                    if (deltaTotal > 50)
                    {
                        cont++;
                    }
                }
                if (cont >= 1)
                {

                    if (block)
                    {
                        //mensaje de alerta
                        Debug.Print("Motion");
                        
                            timer.Start();
                       
                        //alerta = "MotionDetect";
                        block = false;

                    }

                }
                else
                {
                    //timer.Stop();
                    block = true;

                    Debug.Print("Nothing");
                }
            }
            currentBitmap = bitmap;
            actualColores = FillColor(currentBitmap);
        }


        void startService()
        {
            ethernetJ11D.NetworkInterface.Open();
            ethernetJ11D.NetworkInterface.EnableDhcp();
            ethernetJ11D.NetworkInterface.EnableDynamicDns();
            ethernetJ11D.UseThisNetworkInterface();
            Debug.Print(ethernetJ11D.NetworkInterface.IPAddress);

        }

    }
}
