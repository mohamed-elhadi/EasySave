﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Diagnostics;

namespace EasySaveApp.model
{
    class Model
    {
 //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        //Declaration of all variables and properties
        public int checkdatabackup;
        private string serializeObj;
        public string backupListFile = System.Environment.CurrentDirectory + @"\Works\";
        public string stateFile = System.Environment.CurrentDirectory + @"\State\";
        public DataState DataState { get; set; }
        public string NameStateFile { get; set; }
        public string BackupNameState { get; set; }
        public string SourceDir { get; set; }
        public int nbfilesmax { get; set; }
        public int nbfiles { get; set; }
        public long size { get; set; }
        public float progs { get; set; }
        public string TargetDir { get; set; }
        public string SaveName { get; set; }
        public int Type { get; set; }
        public string SourceFile { get; set; }
        public string TypeString { get; set; }
        public long TotalSize { get; set; }
        public TimeSpan TimeTransfert { get; set; }
        public string userMenuInput { get; set; }
        public string MirrorDir { get; set; }

//---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public Model()
        {
            userMenuInput =  " ";

            if (!Directory.Exists(backupListFile)) //Check if the folder is created
            {
                DirectoryInfo di = Directory.CreateDirectory(backupListFile); //Function that creates the folder
            }
            backupListFile += @"backupList.json"; //Create a JSON file

            if (!Directory.Exists(stateFile))//Check if the folder is created
            {
                DirectoryInfo di = Directory.CreateDirectory(stateFile); //Function that creates the folder
            }
            stateFile += @"state.json"; //Create a JSON file


        }

        public void CompleteSave(string inputpathsave, string inputDestToSave, bool copyDir, bool verif) //Function for full folder backup
        {
            DataState = new DataState(NameStateFile);
            this.DataState.SaveState = true;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start(); //Starting the timed for the log file

            DirectoryInfo dir = new DirectoryInfo(inputpathsave);  // Get the subdirectories for the specified directory.

            if (!dir.Exists) //Check if the file is present
            {
                throw new DirectoryNotFoundException("ERROR 404 : Directory Not Found ! " + inputpathsave);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(inputDestToSave); // If the destination directory doesn't exist, create it.  

            FileInfo[] files = dir.GetFiles(); // Get the files in the directory and copy them to the new location.

            if (!verif) //  Check for the status file if it needs to reset the variables
            {
                TotalSize = 0;
                nbfilesmax = 0;
                size = 0;
                nbfiles = 0;
                progs = 0;

                foreach (FileInfo file in files) // Loop to allow calculation of files and folder size
                {
                    TotalSize += file.Length;
                    nbfilesmax++;
                }
                foreach(DirectoryInfo subdir in dirs) // Loop to allow calculation of subfiles and subfolder size
                {
                    FileInfo[] Maxfiles = subdir.GetFiles();
                    foreach(FileInfo file in Maxfiles)
                    {
                        TotalSize += file.Length;
                        nbfilesmax++;
                    }
                }

            }

            //Loop that allows to copy the files to make the backup
            foreach (FileInfo file in files) 
            {
                string tempPath = Path.Combine(inputDestToSave, file.Name);

                if(size > 0)
                {
                    progs = ((float)size / TotalSize) * 100;
                }

                //Systems which allows to insert the values ​​of each file in the report file.
                DataState.SourceFile = Path.Combine(inputpathsave, file.Name);
                DataState.TargetFile = tempPath;
                DataState.TotalSize = nbfilesmax;
                DataState.TotalFile = TotalSize;
                DataState.TotalSizeRest = TotalSize - size;
                DataState.FileRest = nbfilesmax - nbfiles;
                DataState.Progress = progs;

                UpdateStatefile(); //Call of the function to start the state file system

                file.CopyTo(tempPath, true); //Function that allows you to copy the file to its new folder.
                nbfiles++;
                size += file.Length;

            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copyDir)  
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(inputDestToSave, subdir.Name);
                    CompleteSave(subdir.FullName, tempPath, copyDir, true);
                }   
            }
            //System which allows the values ​​to be reset to 0 at the end of the backup
            DataState.TotalSize = TotalSize;
            DataState.SourceFile = null;
            DataState.TargetFile = null;
            DataState.TotalFile = 0;
            DataState.TotalSize = 0;
            DataState.TotalSizeRest = 0;
            DataState.FileRest = 0;
            DataState.Progress = 0;
            DataState.SaveState = false;

            UpdateStatefile(); //Call of the function to start the state file system

            stopwatch.Stop(); //Stop the stopwatch
            this.TimeTransfert = stopwatch.Elapsed; // Transfer of the chrono time to the variable
        }

        public void DifferentialSave(string pathA, string pathB, string pathC) // Function that allows you to make a differential backup
        {
            DataState = new DataState(NameStateFile); //Instattation of the method
            Stopwatch stopwatch = new Stopwatch(); // Instattation of the method
            stopwatch.Start(); //Starting the stopwatch

            DataState.SaveState = true;
            TotalSize = 0;
            nbfilesmax = 0;

            System.IO.DirectoryInfo dir1 = new System.IO.DirectoryInfo(pathA);
            System.IO.DirectoryInfo dir2 = new System.IO.DirectoryInfo(pathB);

            // Take a snapshot of the file system.  
            IEnumerable<System.IO.FileInfo> list1 = dir1.GetFiles("*.*", System.IO.SearchOption.AllDirectories);
            IEnumerable<System.IO.FileInfo> list2 = dir2.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

            //A custom file comparer defined below  
            FileCompare myFileCompare = new FileCompare();

            var queryList1Only = (from file in list1 select file).Except(list2, myFileCompare);
            size = 0;
            nbfiles = 0;
            progs = 0;

            foreach (var v in queryList1Only)
            {
                TotalSize += v.Length;
                nbfilesmax++;

            }

            //Loop that allows the backup of different files
            foreach (var v in queryList1Only)
            {
                string tempPath = Path.Combine(pathC, v.Name);
                //Systems which allows to insert the values ​​of each file in the report file.
                DataState.SourceFile = Path.Combine(pathA, v.Name);
                DataState.TargetFile = tempPath;
                DataState.TotalSize = nbfilesmax;
                DataState.TotalFile = TotalSize;
                DataState.TotalSizeRest = TotalSize - size;
                DataState.FileRest = nbfilesmax - nbfiles;
                DataState.Progress = progs;

                UpdateStatefile();//Call of the function to start the state file system
                v.CopyTo(tempPath, true); //Function that allows you to copy the file to its new folder.
                size += v.Length;
                nbfiles++;
            }

            //System which allows the values ​​to be reset to 0 at the end of the backup
            DataState.SourceFile = null;
            DataState.TargetFile = null;
            DataState.TotalFile = 0;
            DataState.TotalSize = 0;
            DataState.TotalSizeRest = 0;
            DataState.FileRest = 0;
            DataState.Progress = 0;
            DataState.SaveState = false;
            UpdateStatefile();//Call of the function to start the state file system

            stopwatch.Stop(); //Stop the stopwatch
            this.TimeTransfert = stopwatch.Elapsed; // Transfer of the chrono time to the variable
        }

        private void UpdateStatefile()//Function that updates the status file.
        {
            List<DataState> stateList = new List<DataState>();
            this.serializeObj = null;
            if (!File.Exists(stateFile)) //Checking if the file exists
            {
                File.Create(stateFile).Close();
            }

            string jsonString = File.ReadAllText(stateFile);  //Reading the json file

            if (jsonString.Length != 0) //Checking the contents of the json file is empty or not
            {
                DataState[] list = JsonConvert.DeserializeObject<DataState[]>(jsonString); //Derialization of the json file

                foreach (var obj in list) // Loop to allow filling of the JSON file
                {
                    if(obj.SaveName == this.NameStateFile) //Verification so that the name in the json is the same as that of the backup
                    {
                        obj.SourceFile = this.DataState.SourceFile;
                        obj.TargetFile = this.DataState.TargetFile;
                        obj.TotalFile = this.DataState.TotalFile;
                        obj.TotalSize = this.DataState.TotalSize;
                        obj.FileRest = this.DataState.FileRest;
                        obj.TotalSizeRest = this.DataState.TotalSizeRest;
                        obj.Progress = this.DataState.Progress;
                        obj.BackupDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                        obj.SaveState = this.DataState.SaveState;
                    }

                    stateList.Add(obj); //Allows you to prepare the objects for the json filling

                }

                this.serializeObj = JsonConvert.SerializeObject(stateList.ToArray(), Formatting.Indented) + Environment.NewLine; //Serialization for writing to json file

                File.WriteAllText(stateFile, this.serializeObj); //Function to write to JSON file
            }


        }

        public void UpdateLogFile(string savename, string sourcedir, string targetdir)//Function to allow modification of the log file
        {
            Stopwatch stopwatch = new Stopwatch();
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", TimeTransfert.Hours, TimeTransfert.Minutes, TimeTransfert.Seconds, TimeTransfert.Milliseconds / 10); //Formatting the stopwatch for better visibility in the file
            
            DataLogs datalogs = new DataLogs //Apply the retrieved values ​​to their classes
            {
                SaveName = savename,
                SourceDir = sourcedir,
                TargetDir = targetdir,
                BackupDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                TotalSize = TotalSize,
                TransactionTime = elapsedTime
            };

            string path = System.Environment.CurrentDirectory; //Allows you to retrieve the path of the program environment
            var directory = System.IO.Path.GetDirectoryName(path); // This file saves in the project: \EasySaveApp\bin

            string serializeObj = JsonConvert.SerializeObject(datalogs, Formatting.Indented) + Environment.NewLine; //Serialization for writing to json file
            File.AppendAllText(directory + @"DailyLogs_" + DateTime.Now.ToString("dd-MM-yyyy") + ".json", serializeObj); //Function to write to log file

            stopwatch.Reset(); // Reset of stopwatch
        }
       
        public void AddSave(Backup backup) //Function that creates a backup job
        {
            List<Backup> backupList = new List<Backup>();
            this.serializeObj = null; 

            if (!File.Exists(backupListFile)) //Checking if the file exists
            {
                File.WriteAllText(backupListFile, this.serializeObj);
            }

            string jsonString = File.ReadAllText(backupListFile); //Reading the json file

            if (jsonString.Length != 0) //Checking the contents of the json file is empty or not
            {
                Backup[] list = JsonConvert.DeserializeObject<Backup[]>(jsonString); //Derialization of the json file
                foreach ( var obj in list) //Loop to add the information in the json
                {
                    backupList.Add(obj);
                }
            }
            backupList.Add(backup); //Allows you to prepare the objects for the json filling

            this.serializeObj = JsonConvert.SerializeObject(backupList.ToArray(), Formatting.Indented) + Environment.NewLine; //Serialization for writing to json file
            File.WriteAllText(backupListFile, this.serializeObj); // Writing to the json file

            DataState = new DataState(this.SaveName); //Class initiation

            DataState.BackupDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); //Adding the time in the variable
            AddState(); //Call of the function to add the backup in the report file.
        }

        public void AddState() //Function that allows you to add a backup job to the report file.
        {
            List<DataState> stateList = new List<DataState>();
            this.serializeObj = null;

            if (!File.Exists(stateFile)) //Checking if the file exists
            {
                File.Create(stateFile).Close();
            }

            string jsonString = File.ReadAllText(stateFile); //Reading the json file

            if (jsonString.Length != 0)
            {
                DataState[] list = JsonConvert.DeserializeObject<DataState[]>(jsonString); //Derialization of the json file
                foreach (var obj in list) //Loop to add the information in the json
                {
                    stateList.Add(obj);
                }
            }
            this.DataState.SaveState = false;
            stateList.Add(this.DataState); //Allows you to prepare the objects for the json filling

            this.serializeObj = JsonConvert.SerializeObject(stateList.ToArray(), Formatting.Indented) + Environment.NewLine; //Serialization for writing to json file
            File.WriteAllText(stateFile, this.serializeObj);// Writing to the json file


        }

        public void LoadSave(string backupname) //Function that allows you to load backup jobs
        {
            Backup backup = null;
            this.TotalSize = 0;
            BackupNameState = backupname;

            string jsonString = File.ReadAllText(backupListFile); //Reading the json file


            if (jsonString.Length != 0) //Checking the contents of the json file is empty or not
            {
                Backup[] list = JsonConvert.DeserializeObject<Backup[]>(jsonString);  //Derialization of the json file
                foreach (var obj in list)
                {
                    if(obj.SaveName == backupname) //Check to have the correct name of the backup
                    {
                        backup = new Backup(obj.SaveName, obj.SourceDir, obj.TargetDir, obj.Type, obj.MirrorDir); //Function that allows you to retrieve information about the backup
                    }
                }
            }

            if(backup.Type == 1) //If the type is 1, it means it's a full backup
            {
                NameStateFile = backup.SaveName;
                CompleteSave(backup.SourceDir, backup.TargetDir, true, false); //Calling the function to run the full backup
                UpdateLogFile(backup.SaveName, backup.SourceDir, backup.TargetDir); //Call of the function to start the modifications of the log file
                Console.WriteLine("Saved Successfull !"); //Satisfaction message display
            }
            else //If this is the wrong guy then, it means it's a differential backup
            {
                NameStateFile = backup.SaveName;
                DifferentialSave(backup.SourceDir, backup.MirrorDir, backup.TargetDir); //Calling the function to start the differential backup
                UpdateLogFile(backup.SaveName, backup.SourceDir, backup.TargetDir); //Call of the function to start the modifications of the log file
                Console.WriteLine("Saved Successfull !"); //Satisfaction message display
            }

        }

        public void CheckDataFile()  // Function that allows to count the number of backups in the json file of backup jobs
        {
            checkdatabackup = 0;

            if (File.Exists(backupListFile)) //Check on file exists
            {
                string jsonString = File.ReadAllText(backupListFile);//Reading the json file
                if (jsonString.Length != 0)//Checking the contents of the json file is empty or not
                {
                    Backup[] list = JsonConvert.DeserializeObject<Backup[]>(jsonString); //Derialization of the json file
                    checkdatabackup = list.Length; //Allows to count the number of backups
                }
            }
        }

    }

}