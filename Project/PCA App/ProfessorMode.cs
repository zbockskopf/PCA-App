using System.Linq;
using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Widget;
using Java.IO;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;

namespace PCAapp {  

    public static class ProfessorModeApp
    {
        public static File _file;
        public static File _dir;
        public static Bitmap bitmap;
        
    }

    [Activity(Label = "Professor Mode")]
    public class ProfessorMode : Activity
    {
        // Count of how many times picture taken.
        int picCounter = 0;

        // Count of areas
        int areaCounter = 0;

        //Restrict area selection count to that of the first picture
        int areaUpperLimit = 0;

        // Count of data sets
        int datasetCounter = 0;

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
        TextView selectAreaCounterLbl, numberOfPicturesLbl, pictureTitleLbl;

        // Current view
        string currentView;

        // Lists for dataset 
        List<String> Labels;
        /// <summary>
        /// Contains the areas that have been gray-scaled
        /// </summary>
        List<double> ImageTuple;
        /// <summary>
        /// Contains the lists of each image
        /// </summary>
        List<List<double>> DataSet; 

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (IsThereAnAppToTakePictures())
            {
                // Set our view from the "main" layout resource
                SetContentView(Resource.Layout.TakePicture);
                currentView = "areaSelect";
                CreateDirectoryForPictures();

                

                // Take a picture button
                captureButton = FindViewById<ImageButton>(Resource.Id.picButton);
                helpBtn = FindViewById<ImageButton>(Resource.Id.helpButton);
                helpBtn.Click += Help;
                pictureTitleLbl = FindViewById<TextView>(Resource.Id.pictureTitleLbl);
                pictureTitleLbl.Text = "Take a Picture";
                captureButton.SetImageResource(Resource.Drawable.cameraBtnImage);
                captureButton.Click += TakeAPicture;

                //// Crop, aka select area, button
                //cropButton = FindViewById<Button>(Resource.Id.cropButton);
                //cropButton.Enabled = false; // Can't crop a photo that doesn't exit yet
                //cropButton.Click += CropPic;

                //// Label data button

                //// Finish button for when picture taking and area selection is finished


                //Initialize The lists
                Labels = new List<String>();
                ImageTuple = new List<double>();
                DataSet = new List<List<double>>();
            }
        }        

        // Override the default behavior of the nav bar back button to return to the main menu.
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
                    picUri = Uri.FromFile(ProfessorModeApp._file);

                    // Make picture available in the gallery
                    Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
                    Uri contentUri = Uri.FromFile(ProfessorModeApp._file);
                    mediaScanIntent.SetData(contentUri);
                    SendBroadcast(mediaScanIntent);


                    captureButton.Enabled = false;
                    SetContentView(Resource.Layout.AreaSelection);

                    // Enable area selection button

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
                    ProfessorModeApp.bitmap = ProfessorModeApp._file.Path.LoadandResizeBitmap(width, height);
                    if (ProfessorModeApp.bitmap != null)
                    {
                        picView.SetImageBitmap(ProfessorModeApp.bitmap);
                        ProfessorModeApp.bitmap = null;
                    }




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

                    // Send GrayScale values to the Data Set
                    int gray = new int();
                    gray = Grayscale(avgColor);
                    ImageTuple.Add(gray);

                    // Sends the gray scales to the text file
                    //FileBs(Grayscale(avgColor));

                    // Update area selection counter
                    selectAreaCounterLbl.Text = "Number of crops: " + areaCounter.ToString();


                    // If first picture operate normally
                    if (picCounter == 1)
                    {
                        // Update area selection counter
                        //cropButton.Text = "Select Area (" + areaCounter + ")";
                        if (areaCounter == 3)
                        {
                            selectAreaImageView = FindViewById<ImageView>(Resource.Id.imageView1);
                            selectAreaImageView.LayoutParameters.Height = 500;
                            selectAreaImageView.LayoutParameters.Width = 350;
                            labelButton = FindViewById<Button>(Resource.Id.labelButton);
                            labelButton.Click += LabelData;
                            labelButton.Visibility = Android.Views.ViewStates.Visible;
                            labelButton.Enabled = true;
                        }
                        areaUpperLimit = areaCounter;
                    }
                    // For every subsequent picture restrict number of areas to select
                    else if (picCounter > 0)
                    {
                        // Update area selection counter with upper limit
                        selectAreaCounterLbl.Text = "Number of crops: "  + areaCounter + " / " + areaUpperLimit;
                        if (areaCounter == areaUpperLimit)
                        {

                            selectAreaImageView = FindViewById<ImageView>(Resource.Id.imageView1);
                            selectAreaImageView.LayoutParameters.Height = 500;
                            selectAreaImageView.LayoutParameters.Width = 350;
                            labelButton = FindViewById<Button>(Resource.Id.labelButton);
                            labelButton.Click += LabelData;
                            labelButton.Visibility = Android.Views.ViewStates.Visible;
                            labelButton.Enabled = true;
                            cropButton.Enabled = false;
                        }
                    }
                }
            }
        }

        // Convert RGB values to grayscale and put into list. 
        public int Grayscale(Color avgCol)
        {              
                var red = Color.GetRedComponent(avgCol);
                var green = Color.GetGreenComponent(avgCol);
                var blue = Color.GetBlueComponent(avgCol);

                return (int)(red * .3 + green * .59 + blue * .11);                  
        }

        public void FileLabel(List<String> List)
        {
            // Creates a path to documents
            var path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            int length = List.Count();
            var filePath = System.IO.Path.Combine(path, "Averages.txt");

            for (int i = 0; i < length; i++)
            {
                System.IO.File.AppendAllText(filePath, "Professor: Picture " + picCounter + " Value: " + List[i].ToString() + ",");
            }
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
            ProfessorModeApp._dir = new File(
                Environment.GetExternalStoragePublicDirectory(
                    Environment.DirectoryPictures), "CameraAppDemo");
            if (!ProfessorModeApp._dir.Exists())
            {
                ProfessorModeApp._dir.Mkdirs();
            }
        }

        // Take a picture intent
        private void TakeAPicture(object sender, EventArgs eventArgs)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            ProfessorModeApp._file = new File(ProfessorModeApp._dir, String.Format("PCAPhoto_{0}.jpg", Guid.NewGuid()));
            intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(ProfessorModeApp._file));
            intent.PutExtra(MediaStore.ExtraSizeLimit, 1024);
            StartActivityForResult(intent, CAMERA_CAPTURE);
            picCounter++;
            if(currentView == "takepicture")
            {
                numberOfPicturesLbl.Text = "Number of pictures: " + picCounter.ToString();

            }
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
        private static Color CalculateAverageColor(Bitmap bm)
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
                    red += Color.GetRedComponent(tmpColor);
                    green += Color.GetGreenComponent(tmpColor);
                    blue += Color.GetBlueComponent(tmpColor);

                    // Keep track of how many pixels processed
                    pixCount++;
                }
            }

            // Calculate Averages
            red /= pixCount;
            green /= pixCount;
            blue /= pixCount;
          
           return  Android.Graphics.Color.Rgb(red, green, blue);
        }

        private void LabelData(Object s, EventArgs e)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Label Data");

            EditText label = new EditText(this);

            builder.SetView(label);
            // Create empty event handlers, we will override them manually instead of letting the builder handling the clicks.
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
                //labelButton.Visibility = Android.Views.ViewStates.Gone;
                //cropButton.Text = "Finished";
                //cropButton.Click += Finished;
                SetContentView(Resource.Layout.TakePicture);
                captureButton = FindViewById<ImageButton>(Resource.Id.picButton);
                captureButton.Click += TakeAPicture;
                numberOfPicturesLbl = FindViewById<TextView>(Resource.Id.numberOfPicturesLbl);
                numberOfPicturesLbl.Text = "Number of pictures: " + picCounter.ToString();

                if (picCounter >= 3)
                {
                    finishButton = FindViewById<Button>(Resource.Id.finishButton);
                    finishButton.Visibility = Android.Views.ViewStates.Visible;
                    finishButton.Click += Finished;
                }

                areaCounter = 0;

                //Add Image Tuple to data set
                //adder is needed as adding to a list is adding a reference and so when image tuple would be cleared the data in dataset would be cleared
                List<double> adder = new List<double>(ImageTuple);              
                DataSet.Add(adder);               
                Labels.Add(label.Text.Clone().ToString());
                //Reset Image Tuple
                ImageTuple.Clear();

                // Finish button enabled if 3 or more pictures have been taken and 
                // 3 or more datasets have been collected
                if (picCounter >= 3 && datasetCounter >= 3) {
                    finishButton.Enabled = true;
                }
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
            //This is where we will tie in the PCA core
            ///Its undecided if we want to create a new class that is just the PCA core output to file output, probably best way to do it 
            DataStructure.Data = DataSet;
            DataStructure.Labels = Labels;
            DataStructure.Process();
            DataWriter dataWriter = new DataWriter();

            //Create the reference file here 
            //In new class pls 
            //User Prompt
            string path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            string filePath;

            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Please select desired reference file name(including .txt)");

            EditText input = new EditText(this);
            input.InputType = Android.Text.InputTypes.ClassText;
            builder.SetView(input);

            //only the postive button should do anything 
            builder.SetPositiveButton("OK", (see, ess) => {
                //get file path
                //get file path
                string name = "RefernceFile.txt";//Default value
                if (input.Text != "") name = input.Text;

                filePath = System.IO.Path.Combine(path, name);

                dataWriter.Filepath = filePath;
                //Dimension size 
                dataWriter.Dimensionsize = areaUpperLimit;
                dataWriter.NumberOfPics = Labels.Count;
                dataWriter.writeDimension();
                //Labels
                dataWriter.Labels = Labels;
                dataWriter.writeLabels();
                //using the final data from PCA forms the vectors. 
                dataWriter.FinalData = DataStructure.FinalDataRealigned;
                dataWriter.writeFinalDataRealigned();
                //Grab the vectors
                dataWriter.Vectors = DataStructure.FeatureVectors;
                dataWriter.writeVectors();

                //Clear the data structure afterwords 
                DataStructure.clear();
                ///Temporarily commented out for testing of student mode 

                SetContentView(Resource.Layout.Main);
                Intent intent = new Intent(this, typeof(MainActivity));
                StartActivity(intent);
            });

            //this should just cancel
            builder.SetNegativeButton("Cancel", (afk, kfa) => {
                
            });

            //show dialog 
            Dialog diaglog = builder.Create();
            diaglog.Show();


            
            
        }


        private void Help(Object sender, EventArgs e)
        {
            AlertDialog.Builder helpAlert = new AlertDialog.Builder(this);
            
            helpAlert.SetTitle("Professor Mode Help");
            helpAlert.SetMessage(
            "Constraints:\n" +
            "\t1. At least 3 pictures must be taken.\n" +
            "\t2. At least 3 areas must be selected from each picture taken.\n" +
            "\nTaking a Picture:\n" +
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
    }
}