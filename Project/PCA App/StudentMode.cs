using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Java.IO;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Content.PM;
using Android.Graphics;
using Android.Provider;
using Android.Util;

using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;

namespace PCAapp
{
    public static class StudentModeApp
    {
        public static File _file;
        public static File _dir;
        public static Bitmap bitmap;
    }

    [Activity(Label = "Student Mode")]
    public class StudentMode : Activity
    {
        // Pic counter
        int count = 0;

        // Number of areas
        int areaCounter = 0;
        int areaLimit = 0;
        

        // Keep track of capture intent
        const int CAMERA_CAPTURE = 1;

        // Keep track of cropping intent
        const int PIC_CROP = 2;

        // Captured picture uri
        private Android.Net.Uri picUri;

        // Button declaration
        ImageView selectAreaImageView;
        ImageButton captureButton, helpBtn;
        Button cropButton, labelButton, finishButton;

        // TextView declaration
        TextView selectAreaCounterLbl, pictureTitleLabel;
        // Lists for Student Data 
        List<Double> ImageTuple;

        //%diff 
        double maxDist = 0;
        List<List<double>> psData0;
        List<List<double>> psData255;
        //Path String Stuff
        string path;
        string filePath;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (IsThereAnAppToTakePictures())
            {
                CreateDirectoryForPictures();

                // Set our view from the "main" layout resource
                SetContentView(Resource.Layout.TakePicture);

                //Creates the path, this will need to be user set later
                path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
                //filePath = System.IO.Path.Combine(path, "RefernceFile.txt");

                // Take a picture button
                helpBtn = FindViewById<ImageButton>(Resource.Id.helpButton);
                helpBtn.Click += Help;
                captureButton = FindViewById<ImageButton>(Resource.Id.picButton);
                captureButton.SetImageResource(Resource.Drawable.importFileImage);
                pictureTitleLabel = FindViewById<TextView>(Resource.Id.pictureTitleLbl);
                pictureTitleLabel.Text = "Import a Reference File";
                captureButton.Click += getFilePath;

                //Init data stuff
                ImageTuple = new List<double>();
            }
        }

        private void getFilePath(object sender, EventArgs e)
        {
            //User Prompt
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Please select desired reference file name(including .txt)");

            EditText input = new EditText(this);
            input.InputType = Android.Text.InputTypes.ClassText;
            builder.SetView(input);

            //only the postive button should do anything 
            builder.SetPositiveButton("OK", (see, ess) => {
                //get file path
                string name = "RefernceFile.txt";//Default value
                if (input.Text != "") name = input.Text;

                filePath = System.IO.Path.Combine(path, name);
                if (System.IO.File.Exists(filePath)) {
                    //If file exists on the path

                    // Only move on if positive button 
                    pictureTitleLabel = FindViewById<TextView>(Resource.Id.pictureTitleLbl);
                    pictureTitleLabel.Text = "Take a Picture";
                    captureButton.SetImageResource(Resource.Drawable.cameraBtnImage);
                    captureButton.Click += TakeAPicture;
                    captureButton.Click -= getFilePath;

                    //read reference file at user specified path
                    referenceRead(filePath);
                }
            });

            //this should just cancel
            builder.SetNegativeButton("Cancel", (afk, kfa) => {
                //captureButton.Click += getFilePath;
            });

            //show dialog 
            Dialog diaglog = builder.Create();
            diaglog.Show();        
        }        

        private void referenceRead(string filePath)
        {
            //Read Stuff into pca from reference file
            DataReader datareader = new DataReader(filePath);
            DataStructure.Labels = datareader.Labels;
            DataStructure.FeatureVectors = datareader.Vectors;
            DataStructure.FinalDataRealigned = datareader.FinalDataRealigned;
            areaLimit = datareader.DimensionCount;

            //create tuple of 0 and get its data point 
            //create pseudo data 0
            List < Double > pseudoDataZero = new List<double>();
            for (int i = 0; i < datareader.DimensionCount; i++) {
                pseudoDataZero.Add(0);
            }

            List<List<Double>> data = new List<List<double>>();
            data.Add(pseudoDataZero);
            
            //process pseudo data 0
            UserInput.Data = data;
            UserInput.maxDisProcess();
            psData0 = new List<List<double>>(UserInput.FinalDataRealinged);
            
            //create tuple of 255 and get its data point 
            //create pseudo data 255
            pseudoDataZero = new List<double>();
            for (int i = 0; i < datareader.DimensionCount; i++) {
                pseudoDataZero.Add(255);
            }

            data = new List<List<double>>();
            data.Add(pseudoDataZero);

            //process pseudo data 255
            UserInput.Data = data;
            UserInput.maxDisProcess();
            psData255 = new List<List<double>>(UserInput.FinalDataRealinged);
            
            maxDist = eDistance(psData0, psData255);
        }

        private double eDistance(List<List<double>> i1, List<List<double>> i2)
        {
            //Eculdian distance = sqrt((x1-x2)^2 + (y1-y2)^2 + (z1-z2)^2)
            double xdiff = i1[0][0] - i2[0][0];
            double ydiff = i1[0][1] - i2[0][1];
            double zdiff = i1[0][2] - i2[0][2];
            
            xdiff = Math.Pow(xdiff, 2);
            ydiff = Math.Pow(ydiff, 2);
            zdiff = Math.Pow(zdiff, 2);
            
            double total = xdiff + ydiff + zdiff;
            total = Math.Sqrt(total);
            
            return total;
         }



        // Override the default behavior of the nav bar back button to return to the
        // main menu.
        public override void OnBackPressed()
        {
            SetContentView(Resource.Layout.Main);
            Intent intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == Result.Ok)
            {
                // User is returning from capturing an image using the camera
                if (requestCode == CAMERA_CAPTURE)
                {
                    picUri = Uri.FromFile(StudentModeApp._file);

                    // Make picture available in the gallery
                    Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
                    Uri contentUri = Uri.FromFile(StudentModeApp._file);
                    mediaScanIntent.SetData(contentUri);
                    SendBroadcast(mediaScanIntent);



                    captureButton.Enabled = false;
                    SetContentView(Resource.Layout.AreaSelection);

                    // Crop, aka select area, button
                    helpBtn = FindViewById<ImageButton>(Resource.Id.helpButton);
                    helpBtn.Click += Help;
                    cropButton = FindViewById<Button>(Resource.Id.cropButton);
                    selectAreaCounterLbl = FindViewById<TextView>(Resource.Id.selectAreaCounterLbl);
                    cropButton.Enabled = false;  // Can't crop a photo that doesn't exist yet
                    cropButton.Click += CropPic;
                    cropButton.Enabled = true;


                    // Display in ImageView
                    ImageView picView = FindViewById<ImageView>(Resource.Id.imageView1);
                    int height = Resources.DisplayMetrics.HeightPixels;
                    int width = Resources.DisplayMetrics.WidthPixels;
                    StudentModeApp.bitmap = StudentModeApp._file.Path.LoadandResizeBitmap(width, height);
                    if (StudentModeApp.bitmap != null)
                    {
                        picView.SetImageBitmap(StudentModeApp.bitmap);
                        StudentModeApp.bitmap = null;
                    }

                    // Enable area selection button

                    // Dispose of Java side bitmap
                    GC.Collect();
                }
                // User is returning from cropping the image
                else if (requestCode == PIC_CROP)
                {
                    // Get the returned data
                    Bundle extras = data.Extras;

                    // Get the cropped bitmap
                    Bitmap bm = (Android.Graphics.Bitmap)extras.GetParcelable("data");

                    // Calculate the average color of the bitmap
                    Android.Graphics.Color avgColor = CalculateAverageColor(bm);

                    //Add to the data structure
                    int gray = new int();
                    gray = Grayscale(avgColor);
                    ImageTuple.Add(gray);

                    // Update area selection counter
                    selectAreaCounterLbl.Text = "Number of crops: " + areaCounter.ToString() + " / " + areaLimit.ToString();

                    // User must select at least 3 areas from the picture

                    if (areaCounter == areaLimit)
                    {
                        selectAreaImageView = FindViewById<ImageView>(Resource.Id.imageView1);
                        selectAreaImageView.LayoutParameters.Height = 500;
                        selectAreaImageView.LayoutParameters.Width = 350;
                        labelButton = FindViewById<Button>(Resource.Id.labelButton);
                        labelButton.Click += LabelData;
                        labelButton.Visibility = Android.Views.ViewStates.Visible;
                        cropButton.Enabled = false;
                    }
                }
            }
        }

       
        // Convert RGB values to grayscale 
        public int Grayscale(Android.Graphics.Color avgCol)
        {
            var red = Android.Graphics.Color.GetRedComponent(avgCol);
            var green = Android.Graphics.Color.GetGreenComponent(avgCol);
            var blue = Android.Graphics.Color.GetBlueComponent(avgCol);

            return (int)(red * .299 + green * .587 + blue * .114);
        }

        //public void FileBs(List<int> List)
        //{
        //    // Creates a path to documents
        //    var path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
        //    int length = List.Count();
        //    var filePath = System.IO.Path.Combine(path, "Averages.txt");

        //    for (int i = 0; i < length; i++)
        //    {
        //        System.IO.File.AppendAllText(filePath, "Student: Picture " + count + " Value: " + List[i].ToString() + ",");
        //    }
        //}

        public void FileLabel(List<String> List)
        {
            // Creates a path to documents
            List<List<Double>> data = new List<List<double>>();
            data.Add(ImageTuple);

            //create path
            var path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            var filePath = System.IO.Path.Combine(path, "RefernceFile.txt");

            UserInput.Data = data;
            UserInput.Process();


            //Show Results
            // This displays an alert box to show the Results 
            AlertDialog.Builder alert = new AlertDialog.Builder(this);

            string alert1 = ("You were closest to " + DataStructure.Labels[UserInput.closestIndex] + "\nWith a distance of: " +
                    UserInput.closestDist + "\nPercent Difference: " + (UserInput.closestDist / maxDist));

            alert.SetMessage(alert1);
            Dialog dialog = alert.Create();

        }

        private void Help(object sender, EventArgs e)
        {
            AlertDialog.Builder helpAlert = new AlertDialog.Builder(this);
            
            helpAlert.SetTitle("Student Mode Help");
            helpAlert.SetMessage(
            "Taking a Picture:\n" +
            "\t1. Ensure protein tray is well lit.\n" +
            "\t2. Try to take the picture from a 90 degree angle above the tray. Avoid odd angles.\n" +
            "\t3. Avoid shadows on the tray.\n" +
            "\nArea Selection:\n" +
            "\t1. You must select the correct number of areas specified by the reference file. The number " +
            "of necessary areas will be shown by the area selection button.\n" +
            "\t2. All areas should be as close to the same size as possible.\n" +
            "\t3. Select areas in order starting from the upper left and moving to the right row by row.\n"
                        );
            
            Dialog helpDialog = helpAlert.Create();
            helpDialog.Show();
            
         }

        // Pretty self explanitory. Checks if the device has a camera for picture taking
        private bool IsThereAnAppToTakePictures()
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities =
                PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }

        // Creates a directory to store images if it doesn't already exist.
        /*** THIS FUNCTION MAY NOT BE NEEDED LATER ON AS IMAGES MIGHT NOT NEED TO BE SAVED TO THE DEVICE ***/
        private void CreateDirectoryForPictures()
        {
            StudentModeApp._dir = new File(
                Environment.GetExternalStoragePublicDirectory(
                    Environment.DirectoryPictures), "CameraAppDemo");
            if (!StudentModeApp._dir.Exists())
            {
                StudentModeApp._dir.Mkdirs();
            }
        }

        // Take a picture intent
        private void TakeAPicture(object sender, EventArgs eventArgs)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            StudentModeApp._file = new File(StudentModeApp._dir, String.Format("PCAPhoto_{0}.jpg", Guid.NewGuid()));
            intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(StudentModeApp._file));
            intent.PutExtra(MediaStore.ExtraSizeLimit, 1024);
            StartActivityForResult(intent, CAMERA_CAPTURE);
            count++;
            captureButton.Enabled = false; 
        }

        // Crop, aka area selection, intent
        private void CropPic(object sender, EventArgs eventArgs)
        {
            // Take care of exceptions
            try
            {
                // Call the standard crop action intent (the user device may not support it)
                Intent cropIntent = new Intent("com.android.camera.action.CROP");

                // Indicate image type and Uri
                cropIntent.SetDataAndType(picUri, "image/*");

                // Set crop properties
                cropIntent.PutExtra("crop", "true");

                // Indicate aspect of desired crop
                cropIntent.PutExtra("aspectX", 1);
                cropIntent.PutExtra("aspectY", 1);

                // Indicate output X and Y
                cropIntent.PutExtra("outputX", 256);
                cropIntent.PutExtra("outputY", 256);

                // Retrieve data on return
                cropIntent.PutExtra("return-data", true);

                // Start the activity - we handle returning in onActivityResult
                StartActivityForResult(cropIntent, PIC_CROP);

                areaCounter++;
            }
            // Respond to users whose devices do not support the crop action
            catch (ActivityNotFoundException anfe)
            {
                // Display an error message
                String errorMessage = "Whoops - your device doesn't support the crop action!";
                Toast toast = Toast.MakeText(this, errorMessage, ToastLength.Short);
                toast.Show();
            }
        }

        // Function to average pixel color of cropped images
        private static Android.Graphics.Color CalculateAverageColor(Bitmap bm)
        {
            /* This function adds the RGB values for each pixel in the bitmap and finds
             * the average.
             */
            int red = 0;
            int green = 0;
            int blue = 0;

            // Total number of pixels in image
            int pixCount = 0;

            int bitmapWidth = bm.Width;
            int bitmapHeight = bm.Height;

            for (int x = 0; x < bitmapWidth; x++)
            {
                for (int y = 0; y < bitmapHeight; y++)
                {
                    // Temporary variable to store the color of each pixel
                    int tmpColor = bm.GetPixel(x, y);

                    // Add RGB values for each pixel
                    red += Android.Graphics.Color.GetRedComponent(tmpColor);
                    green += Android.Graphics.Color.GetGreenComponent(tmpColor);
                    blue += Android.Graphics.Color.GetBlueComponent(tmpColor);

                    // Keep track of how many pixes processed
                    pixCount++;
                }
            }

            // Calculate Averages
            red /= pixCount;
            green /= pixCount;
            blue /= pixCount;

            return Android.Graphics.Color.Rgb(red, green, blue);
        }

        private void LabelData(object s, EventArgs e)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Label Data");

            EditText label = new EditText(this);

            builder.SetView(label);
            // Create empty event handlers, we will override them manually instead of letting the builder handle the clicks.
            builder.SetPositiveButton("Done", (EventHandler<DialogClickEventArgs>)null);
            builder.SetNegativeButton("Back", (EventHandler<DialogClickEventArgs>)null);
            var dialog = builder.Create();

            // Show the dialog. This is important to do before accessing the buttons.
            dialog.Show();

            // Get the buttons.
            var yesBtn = dialog.GetButton((int)DialogButtonType.Positive);
            var noBtn = dialog.GetButton((int)DialogButtonType.Negative);

            // Assign our handlers.
            yesBtn.Click += (sender, args) =>
            {
                // Don't dismiss dialog.
                // FileLabel(LabelList(label.Text.ToString()));

                labelButton.Visibility = Android.Views.ViewStates.Gone;
                cropButton.Text = "Finished";
                cropButton.Click -= CropPic;
                cropButton.Click += Finished;

                cropButton.Enabled = true;

                dialog.Dismiss();
            };
            noBtn.Click += (sender, args) =>
            {
                // Dismiss dialog.
                dialog.Dismiss();
            };
        }

        private void Finished(Object sender, EventArgs e)
        {
            //This is where we tie into the PCA core
            List<List<Double>> data = new List<List<double>>();
            data.Add(ImageTuple);

            UserInput.Data = data;
            UserInput.Process();
            //Show Results
            // This displays an alert box to show the Results 
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            string alert1 = ("You were closest to " + DataStructure.Labels[UserInput.closestIndex] + "\nWith a distance of: " + UserInput.closestDist + "\n% Distance: " + (UserInput.closestDist / maxDist) * 100);
            
            alert.SetMessage(alert1);
            Dialog dialog = alert.Create();
            dialog.Show();

            ///Temporarily commented out to test student mode
            ///We could leave it like this as to allow for the student to take multiple pictures and get results for each
            //SetContentView(Resource.Layout.Main);
            //Intent intent = new Intent(this, typeof(MainActivity));
            //StartActivity(intent);
            //This needs to be commented out or student mode auto closes the results screen, they can still hit back to exit 
        }
    }
}