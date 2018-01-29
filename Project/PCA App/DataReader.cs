using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace PCAapp {

    public class DataReader {
        //Privates
        int dimensionSize;
        int numberOfPics;
        List<String> labels;
        List<List<Double>> vectors;
        List<List<Double>> finalDataRaligned;
        
        //Publics
        public List<string> Labels {
            get { return labels; }
            //set { labels = value; }
        }

        public List<List<Double>> Vectors {
            get { return vectors; }
            //set { vectors = value; }
        }

        public List<List<Double>> FinalDataRealigned {
            get { return finalDataRaligned; }
            //set { finalDataRaligned = value; }
        }

        public int DimensionCount {
            get { return dimensionSize; }
        }

        public int NumberOfPics {
            get { return numberOfPics; }
        }

        //Constructors
        public DataReader(string path) {
            //Init
            dimensionSize = 0;
            numberOfPics = 0;
            labels = new List<string>();
            vectors = new List<List<double>>();
            finalDataRaligned = new List<List<double>>();

            //Read Data File
            readDataFile(path);
        }

        //Methods
        //Private 

        //Public
        private void readDataFile(string path) {
            //Open file
            string[] lines = File.ReadAllLines(path);

            int index = 0;
            dimensionSize = int.Parse(lines[index]);
            index++; //consume line

            numberOfPics = int.Parse(lines[index]);
            index++; //consume line
         
            //Grab Labels
            for (int i = 0; i < numberOfPics; i++) {
                labels.Add(lines[index]);
                index++;//consume line
            }
            
            //Grab the final data realigned
            string[] line;
            for (int i = 0; i < numberOfPics; i++) {
                List<double> numberLine = new List<double>(); //this list will be used to add to finalDataRealigned
                //Step over a line
                line = lines[index].Split(' ');
                for (int j = 0; j < line.Length; j++) {
                    double num = double.Parse(line[j]);
                    numberLine.Add(num);
                }
                finalDataRaligned.Add(numberLine);
                index++; // consume a line
            }

            //Grab the vectors
            for (int i = 0; i < 3; i++) {
                line = lines[index].Split(' ');
                List<double> numberLine = new List<double>(); //this list will be used to add to vectors
                for(int j = 0; j < line.Length; j++) {
                    double num = double.Parse(line[j]);
                    numberLine.Add(num);
                }
                vectors.Add(numberLine);
                index++; // consume a line
            }
        }
    }
}