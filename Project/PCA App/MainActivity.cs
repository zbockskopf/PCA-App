using Android.App;
using Android.Widget;
using Android.OS;
using Java.IO;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;


namespace PCAapp
{
    using System;
    using System.IO;
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


    [Activity(Label = "PCA App", MainLauncher = true)]
    public class MainActivity : Activity
    {


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            Button studentButton = FindViewById<Button>(Resource.Id.studentButton);
            studentButton.Click += Start_Student_Mode;

            Button professorButton = FindViewById<Button>(Resource.Id.profButton);
            professorButton.Click += Start_Prof_Mode;

           
        }

        
        private void Start_Student_Mode(object sender, EventArgs e)
        {

            SetContentView(Resource.Layout.TakePicture);
            Intent intent = new Intent(this, typeof(StudentMode));
            StartActivity(intent);

        }

        private void Start_Prof_Mode(object sender, EventArgs e)
        {
            SetContentView(Resource.Layout.ProfessorMode);
            Intent intent = new Intent(this, typeof(ProfessorMode));
            StartActivity(intent);

        }
    }

    
}

