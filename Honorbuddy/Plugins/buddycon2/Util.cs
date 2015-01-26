using Styx.Common;
using System;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using Styx;
using System.Threading;
using System.Linq;
  
namespace BuddyCon2
{
    public class Util
    {
        public Util()
        {
        }
        public static void PostLog(string message)
        {
            try
            {
				if (Styx.StyxWoW.Me.Name.ToString() != "")
					message = message.Replace(Styx.StyxWoW.Me.Name.ToString(), "BOTNAME");
				if (BuddyConSettings2.Instance.apikey != "")
					message = message.Replace(BuddyConSettings2.Instance.apikey, "APIKEY");
				if (BuddyConSettings2.Instance.androidapi != "")
					message = message.Replace(BuddyConSettings2.Instance.androidapi, "ANDROIDKEY");
            }
            catch (Exception e)
            {
                Logging.Write(LogLevel.Verbose, e.Message);
            }
            if(BuddyConSettings2.Instance.debug) Logging.Write(LogLevel.Verbose, message);
        }
        public static void ShowToLog(string message)
        {
            try
            {
                if (Styx.StyxWoW.Me.Name.ToString() != "")
                    message = message.Replace(Styx.StyxWoW.Me.Name.ToString(), "BOTNAME");
                if (BuddyConSettings2.Instance.apikey != "")
                    message = message.Replace(BuddyConSettings2.Instance.apikey, "APIKEY");
                if (BuddyConSettings2.Instance.androidapi != "")
                    message = message.Replace(BuddyConSettings2.Instance.androidapi, "ANDROIDKEY");
            }
            catch (Exception e)
            {
                Logging.Write(LogLevel.Verbose, e.Message);
            }

            Logging.Write(message);
        }

        public static string MyDictionaryToJson(Dictionary<string, string> dict)
        {
            var entries = dict.Select(d => string.Format("\"{0}\": \"{1}\"", d.Key, d.Value));
            return "{" + string.Join(",", entries) + "}";
        }



        public static string SendNotification(string deviceId, string message)
        {
            string GoogleAppID = "AIzaSyCx6FACQbbOxXNKjTC2m6TePcrh_AwD2Hg";
            var SENDER_ID = "320209084966";
            var value = message;
            HttpWebRequest tRequest;
            Util.PostLog(string.Format("[bC2]: toAndroid start "));

            try
            {
                tRequest = (HttpWebRequest)WebRequest.Create("https://android.googleapis.com/gcm/send");
                tRequest.Method = "post";
                tRequest.ContentType = " application/x-www-form-urlencoded;charset=UTF-8";
                tRequest.Headers.Add(string.Format("Authorization: key={0}", GoogleAppID));

                tRequest.Headers.Add(string.Format("Sender: id={0}", SENDER_ID));

                string postData = "data.message=" + System.Uri.EscapeDataString(value) + "&data.time=" + System.DateTime.Now.ToString() + "&registration_id=" + deviceId + "";

                Console.WriteLine(postData);
                Byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                tRequest.ContentLength = byteArray.Length;

                Stream dataStream = tRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                HttpWebResponse tResponse = (HttpWebResponse)tRequest.GetResponse();
                
                dataStream = tResponse.GetResponseStream();

                StreamReader tReader = new StreamReader(dataStream);

                String sResponseFromServer = tReader.ReadToEnd();


                tReader.Close();
                dataStream.Close();
                tResponse.Close();
                Util.PostLog(string.Format("[bC2]: toAndroid{1}: {0} ", sResponseFromServer, (int)tResponse.StatusCode));
                if (sResponseFromServer.Contains("Error=NotRegistered"))
                    Util.ShowToLog(string.Format("[bC2]: NotRegisted Error for Android. Please restart the App and restart the Bot (or recompile the Plugin).", sResponseFromServer, (int)tResponse.StatusCode));
                return sResponseFromServer;
            }
            catch (WebException e)
            {
                Stream responseStream = e.Response.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);

                string responseString = responseReader.ReadToEnd();
                Util.ShowToLog(string.Format("[bC2]: Error: {0}", responseString));
                return "";
            }
            catch (Exception e)
            {
                Util.ShowToLog(string.Format("[bC2]: Error: {0}", e.Message));
                return "";
            }
        }

        public static void Log(string message)
        {
            /*
            if (BuddyConSettings2.Instance.debug)
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Util.ShowToLog(string.Format("[bC2]: Fehler: {0}", Styx.StyxWoW.Me.IsOutOfBounds));
                    w.Write("\r\nLog Entry : ");
                    w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                        DateTime.Now.ToLongDateString());
                    w.WriteLine("  :");
                    w.WriteLine("  :{0}", message);
                    w.WriteLine("Name  :{0}", Styx.StyxWoW.Me.Name.ToString());
                    w.WriteLine("isGhost  :{0}", Styx.StyxWoW.Me.IsGhost);
                    w.WriteLine("isOutof  :{0}", Styx.StyxWoW.Me.IsOutOfBounds);

                    w.WriteLine("-------------------------------");
                }
             */
        }


        public static Boolean sendToProwl(string header, string message, string name, string server)
        {
            if (BuddyConSettings2.Instance.prowlapi != "")
            {
                string[] sendParam = { "apikey", "application", "event", "description", "priority" };
                string[] sendVal = { BuddyConSettings2.Instance.prowlapi, "BuddyCon", header, message + "\nBot: " + name + " - " + server, "1" };
                string response = Util.HttpPost("https://api.prowlapp.com/publicapi/add", sendParam, sendVal);
                //Util.ShowToLog(string.Format("[bC]: Prowl {0} ", response));
                return response.Contains("success");
            }
            else
                return false;
        }
        public static string PostToFtp(string imagFilePath)
        {
            byte[] imageData;
            try
            {
                FileStream fileStream = File.OpenRead(imagFilePath);
                imageData = new byte[fileStream.Length];
                fileStream.Read(imageData, 0, imageData.Length);
                fileStream.Close();


                const int MAX_URI_LENGTH = 32766;
                string base64img = System.Convert.ToBase64String(imageData);
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < base64img.Length; i += MAX_URI_LENGTH)
                {
                    sb.Append(Uri.EscapeDataString(base64img.Substring(i, Math.Min(MAX_URI_LENGTH, base64img.Length - i))));
                }

                string uploadRequestString = "image=" + sb.ToString();
                string url = BuddyConSettings2.Instance.scripturl;
                if (!url.StartsWith("http://")) url = "http://" + url;
                Util.ShowToLog(string.Format("[bC]: url: {0}", url));

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ServicePoint.Expect100Continue = false;

                StreamWriter streamWriter = new StreamWriter(webRequest.GetRequestStream());
                streamWriter.Write(uploadRequestString);
                streamWriter.Close();

                WebResponse response = webRequest.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);

                string responseString = responseReader.ReadToEnd();

                return responseString;
            }
            catch (WebException e)
            {
                Stream responseStream = e.Response.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);

                string responseString = responseReader.ReadToEnd();
                Util.ShowToLog(string.Format("[bC]: Fehler: {0}", responseString));
                return "";
            }
            catch (Exception e)
            {
                Util.ShowToLog(string.Format("[bC]: Fehler: {0}", e.Message));
                return "";
            }

        }
        public static string PostToImgur(string imagFilePath, string apiKey)
        {
            byte[] imageData;
            try
            {
                //ScreenCapture sc = new ScreenCapture();
                //sc.CaptureWindowToFile(Styx.StyxWoW.Memory.WindowHandle, Styx.Helpers.GlobalSettings.Instance.PluginsPath + "\\test.png", ImageFormat.Png);
                Image img = Image.FromFile(imagFilePath);
                // Figure out the ratio
                double ratioX = (double)1280 / (double)img.Width;
                double ratioY = (double)1024 / (double)img.Height;
                // use whichever multiplier is smaller
                double ratio = ratioX < ratioY ? ratioX : ratioY;

                // now we can get the new height and width
                int newHeight = Convert.ToInt32(img.Height * ratio);
                int newWidth = Convert.ToInt32(img.Width * ratio);

                Bitmap bp = ResizeImage(img, newWidth, newHeight);
                bp.Save(Path.Combine(Path.Combine(Styx.Helpers.GlobalSettings.SettingsDirectory, "Settings"), string.Format("upload{0}.jpg", StyxWoW.Me.Name)), ImageFormat.Jpeg);

                FileStream fileStream = new FileStream(Path.Combine(Path.Combine(Styx.Helpers.GlobalSettings.SettingsDirectory, "Settings"), string.Format("upload{0}.jpg", StyxWoW.Me.Name)), FileMode.Open);//File.OpenRead(imagFilePath);
                imageData = new byte[fileStream.Length];
                fileStream.Read(imageData, 0, imageData.Length);
                fileStream.Close();


                const int MAX_URI_LENGTH = 32766;
                string base64img = System.Convert.ToBase64String(imageData);
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < base64img.Length; i += MAX_URI_LENGTH)
                {
                    sb.Append(Uri.EscapeDataString(base64img.Substring(i, Math.Min(MAX_URI_LENGTH, base64img.Length - i))));
                }

                string uploadRequestString = "image=" + sb.ToString() + "&key=" + apiKey;

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://api.imgur.com/2/upload.json");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ServicePoint.Expect100Continue = false;

                StreamWriter streamWriter = new StreamWriter(webRequest.GetRequestStream());
                streamWriter.Write(uploadRequestString);
                streamWriter.Close();

                WebResponse response = webRequest.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);

                string responseString = responseReader.ReadToEnd();

                return responseString;
            }
            catch (WebException e)
            {
                Stream responseStream = e.Response.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);

                string responseString = responseReader.ReadToEnd();
                Util.ShowToLog(string.Format("[bC]: Fehler: {0}", responseString));
                Util.ShowToLog(string.Format("[bC]: Fehler: {0}", e.Message));
                return "";
            }
            catch (Exception e)
            {
                Util.ShowToLog(string.Format("[bC]: Fehler: {0}", e.Message));
                return "";
            }

        }

        public static FileInfo GetLatestWritenFileFileInDirectory(DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null || !directoryInfo.Exists)
                return null;

            FileInfo[] files = directoryInfo.GetFiles();
            DateTime lastWrite = DateTime.MinValue;
            FileInfo lastWritenFile = null;

            foreach (FileInfo file in files)
            {
                if (file.LastWriteTime > lastWrite)
                {
                    lastWrite = file.LastWriteTime;
                    lastWritenFile = file;
                }
            }
            return lastWritenFile;
        }
        public static string HttpPost(string url, string[] paramName, string[] paramVal)
        {
            try
            {
                HttpWebRequest req = WebRequest.Create(new Uri(url))
                                     as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                StringBuilder paramz = new StringBuilder();
                for (int i = 0; i < paramName.Length; i++)
                {
                    paramz.Append(paramName[i]);
                    paramz.Append("=");
                    paramz.Append(System.Net.WebUtility.HtmlDecode(paramVal[i]));
                    paramz.Append("&");
                }

                // Encode the parameters as form data:
                byte[] formData =
                    UTF8Encoding.UTF8.GetBytes(paramz.ToString());
                req.ContentLength = formData.Length;

                // Send the request:
                using (Stream post = req.GetRequestStream())
                {
                    post.Write(formData, 0, formData.Length);
                }

                // Pick up the response:
                string result = null;
                using (HttpWebResponse resp = req.GetResponse()
                                              as HttpWebResponse)
                {
                    StreamReader reader =
                        new StreamReader(resp.GetResponseStream());
                    result = reader.ReadToEnd();
                }
                return result;
            }
            catch (WebException e)
            {
                Stream responseStream = e.Response.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);

                string responseString = responseReader.ReadToEnd();
                Util.PostLog(string.Format("[bC]: Fehler: {0}", responseString));
                return "";
            }

            catch (Exception e)
            {
                Util.ShowToLog(string.Format("[bC]: Fehler: {0}", e.Message));
                return "";
            }
        }
        /// <summary>
        /// Provides various image untilities, such as high quality resizing and the ability to save a JPEG.
        /// </summary>
        //public static class ImageUtilities
        //{
            /// <summary>
            /// A quick lookup for getting image encoders
            /// </summary>
            private static Dictionary<string, ImageCodecInfo> encoders = null;

            /// <summary>
            /// A quick lookup for getting image encoders
            /// </summary>
            public static Dictionary<string, ImageCodecInfo> Encoders
            {
                //get accessor that creates the dictionary on demand
                get
                {
                    //if the quick lookup isn't initialised, initialise it
                    if (encoders == null)
                    {
                        encoders = new Dictionary<string, ImageCodecInfo>();
                    }

                    //if there are no codecs, try loading them
                    if (encoders.Count == 0)
                    {
                        //get all the codecs
                        foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
                        {
                            //add each codec to the quick lookup
                            encoders.Add(codec.MimeType.ToLower(), codec);
                        }
                    }

                    //return the lookup
                    return encoders;
                }
            }

            /// <summary>
            /// Resize the image to the specified width and height.
            /// </summary>
            /// <param name="image">The image to resize.</param>
            /// <param name="width">The width to resize to.</param>
            /// <param name="height">The height to resize to.</param>
            /// <returns>The resized image.</returns>
            public static System.Drawing.Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
            {
                //a holder for the result
                Bitmap result = new Bitmap(width, height);
                // set the resolutions the same to avoid cropping due to resolution differences
                result.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                //use a graphics object to draw the resized image into the bitmap
                using (Graphics graphics = Graphics.FromImage(result))
                {
                    //set the resize quality modes to high quality
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    //draw the image into the target bitmap
                    graphics.DrawImage(image, 0, 0, result.Width, result.Height);
                }

                //return the resulting bitmap
                return result;
            }

            /// <summary> 
            /// Saves an image as a jpeg image, with the given quality 
            /// </summary> 
            /// <param name="path">Path to which the image would be saved.</param> 
            /// <param name="quality">An integer from 0 to 100, with 100 being the 
            /// highest quality</param> 
            /// <exception cref="ArgumentOutOfRangeException">
            /// An invalid value was entered for image quality.
            /// </exception>
            public static void SaveJpeg(string path, Image image, int quality)
            {
                //ensure the quality is within the correct range
                if ((quality < 0) || (quality > 100))
                {
                    //create the error message
                    string error = string.Format("Jpeg image quality must be between 0 and 100, with 100 being the highest quality.  A value of {0} was specified.", quality);
                    //throw a helpful exception
                    throw new ArgumentOutOfRangeException(error);
                }

                //create an encoder parameter for the image quality
                EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                //get the jpeg codec
                ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");

                //create a collection of all parameters that we will pass to the encoder
                EncoderParameters encoderParams = new EncoderParameters(1);
                //set the quality parameter for the codec
                encoderParams.Param[0] = qualityParam;
                //save the image using the codec and the parameters
                image.Save(path, jpegCodec, encoderParams);
            }

            /// <summary> 
            /// Returns the image codec with the given mime type 
            /// </summary> 
            public static ImageCodecInfo GetEncoderInfo(string mimeType)
            {
                //do a case insensitive search for the mime type
                string lookupKey = mimeType.ToLower();

                //the codec to return, default to null
                ImageCodecInfo foundCodec = null;

                //if we have the encoder, get it to return
                if (Encoders.ContainsKey(lookupKey))
                {
                    //pull the codec from the lookup
                    foundCodec = Encoders[lookupKey];
                }

                return foundCodec;
            }
            /// <summary>
            /// Provides functions to capture the entire screen, or a particular window, and save it to a file.
            /// </summary>
            public class ScreenCapture
            {
                /// <summary>
                /// Creates an Image object containing a screen shot of the entire desktop
                /// </summary>
                /// <returns></returns>
                public Image CaptureScreen()
                {
                    return CaptureWindow(User32.GetDesktopWindow());
                }
                /// <summary>
                /// Creates an Image object containing a screen shot of a specific window
                /// </summary>
                /// <param name="handle">The handle to the window. (In windows forms, this is obtained by the Handle property)</param>
                /// <returns></returns>
                public Image CaptureWindow(IntPtr handle)
                {
                    // get te hDC of the target window
                    IntPtr hdcSrc = User32.GetWindowDC(handle);
                    // get the size
                    User32.RECT windowRect = new User32.RECT();
                    User32.GetWindowRect(handle, ref windowRect);
                    int width = windowRect.right - windowRect.left;
                    int height = windowRect.bottom - windowRect.top;
                    // create a device context we can copy to
                    IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
                    // create a bitmap we can copy it to,
                    // using GetDeviceCaps to get the width/height
                    IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
                    // select the bitmap object
                    IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
                    // bitblt over
                    GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
                    // restore selection
                    GDI32.SelectObject(hdcDest, hOld);
                    // clean up 
                    GDI32.DeleteDC(hdcDest);
                    User32.ReleaseDC(handle, hdcSrc);
                    // get a .NET image object for it
                    Image img = Image.FromHbitmap(hBitmap);
                    // free up the Bitmap object
                    GDI32.DeleteObject(hBitmap);
                    return img;
                }
                /// <summary>
                /// Captures a screen shot of a specific window, and saves it to a file
                /// </summary>
                /// <param name="handle"></param>
                /// <param name="filename"></param>
                /// <param name="format"></param>
                public void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format)
                {
                    Image img = CaptureWindow(handle);
                    img.Save(filename, format);
                }
                /// <summary>
                /// Captures a screen shot of the entire desktop, and saves it to a file
                /// </summary>
                /// <param name="filename"></param>
                /// <param name="format"></param>
                public void CaptureScreenToFile(string filename, ImageFormat format)
                {
                    Image img = CaptureScreen();
                    img.Save(filename, format);
                }

                /// <summary>
                /// Helper class containing Gdi32 API functions
                /// </summary>
                private class GDI32
                {

                    public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
                    [DllImport("gdi32.dll")]
                    public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                        int nWidth, int nHeight, IntPtr hObjectSource,
                        int nXSrc, int nYSrc, int dwRop);
                    [DllImport("gdi32.dll")]
                    public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                        int nHeight);
                    [DllImport("gdi32.dll")]
                    public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
                    [DllImport("gdi32.dll")]
                    public static extern bool DeleteDC(IntPtr hDC);
                    [DllImport("gdi32.dll")]
                    public static extern bool DeleteObject(IntPtr hObject);
                    [DllImport("gdi32.dll")]
                    public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
                }

                /// <summary>
                /// Helper class containing User32 API functions
                /// </summary>
                private class User32
                {
                    [StructLayout(LayoutKind.Sequential)]
                    public struct RECT
                    {
                        public int left;
                        public int top;
                        public int right;
                        public int bottom;
                    }
                    [DllImport("user32.dll")]
                    public static extern IntPtr GetDesktopWindow();
                    [DllImport("user32.dll")]
                    public static extern IntPtr GetWindowDC(IntPtr hWnd);
                    [DllImport("user32.dll")]
                    public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
                    [DllImport("user32.dll")]
                    public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);
                }
            }
    }
}