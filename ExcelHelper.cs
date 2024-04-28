/*************************************************************************
* Description: Helper Functions For Excel and CSV files 
* Author: Jason Neitzert
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ExtensionMethods;

namespace DividendStockAdvisor;

static class ExcelHelper
{

    /******************************* Public Functions *************************/

    /***************************************************************************
    * Description: Read Data from CSV file and return parsed data.
    * Param: filename - Fullpath of filename to parse
    * Return: List of a list of strings in each line
    ***************************************************************************/
    static public List<List<string>>? ReadCSVFile(string filename)
    {
        List<List<string>>? list = null;
        StreamReader reader;     
        string? lineData;


        if (File.Exists(filename))
        {
            reader = new(filename);
            lineData = reader.ReadLine();

            if (lineData != null)
            {
                list = new List<List<string>>();

                while (lineData != null)
                {
                    List<string>? lineItems = ParseCSVLine(lineData);

                    if (lineItems == null)
                    {
                        break;
                    }

                    list.Add(lineItems);

                    lineData = reader.ReadLine();
                }

                if (list.Count == 0)
                {
                    list = null;
                }
            }

            reader.Close();
        }

        return list;
    }

    /***************************************************************************
    * Description: Write a new CSV file from a list of a list of strings
    * Details: The data to be written contains a list. Each item in list 
    *          represents a line of data in the CSV file.  That item is a
    *          list a strings that will be comma seperated in file.
    * Param: filename - Fullpath of filename to write
    * Param: data - list of a list of strings to write to file
    * Return: Void
    ***************************************************************************/
    static public void WriteCSVFile(string fileName, List<List<string>>? data)
    {
        if (data != null)
        {
            StreamWriter newCsvFile = File.CreateText(fileName);

            foreach (var line in data)
            {
                string writeLine = "";
                
                foreach(var item in line)
                {
                    /* if string contains commas, surround it by quotes, so
                       that anyone reading the CSV file knows the comma is not a 
                       new item */
                    if (item.Contains(","))
                    {
                        writeLine += "\"";
                    }

                    writeLine += item;

                    if (item.Contains(","))
                    {
                        writeLine += "\"";
                    }

                    /* Write Comma to start next item in current line. */
                    writeLine += ',';
                }
                /* remove final comma in line  */
                writeLine = writeLine.Remove(writeLine.Length - 1);
                newCsvFile.WriteLine(writeLine);
            }

            newCsvFile.Close();
        }
    }


    /***************************************************************************
    * Description: Convert all XLS files in a given directory to CVS
    * Param: directory - Directory to convert all files in
    * Return: void
    ***************************************************************************/
    public static void convertXLSFilesToCSV(string directory)
    {
        List<string> fileList = Directory.EnumerateFiles(directory).Where(s => s.EndsWith(".xls", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (string file in fileList)
        {
            ExcelHelper.XlsToCsv(file);
        }

    }


    /******************************* Private Functions *************************/

    /***************************************************************************
    * Description: Convert an XLS to a CSV File
    * Warning: Deletes original XLS File
    * Param: filename - Fullpath of filename to convert
    * Return: Void
    ***************************************************************************/
    static public void XlsToCsv(string file)
    {
        /* Take original XLS filename and covert it too file ending with CSV. 
           Also make sure all slashes go in correct direction for python parser */
        string csvfile = file.Remove(file.Length - 3).Replace('\\', '/');
        csvfile = $"'{csvfile}csv'";
        string xlsfile = $"'{file.Replace('\\', '/')}'";

        string pythonCommand = $"-c \"from xlsToCsv import getcsv; getcsv({xlsfile},{csvfile})\"";

        var runpythoncommand = new Process();

        runpythoncommand.StartInfo.FileName = "python";
        runpythoncommand.StartInfo.Arguments = pythonCommand;
        runpythoncommand.Start();
        runpythoncommand.WaitForExit();

        File.Delete(file);
    }

    /***************************************************************************
    * Description: Parse a line from a CSV file into a List of strings
    * Param: line - line from csv file to parse
    * Return: List<string> - list of strings parsed from line
    ***************************************************************************/
    private static List<string>? ParseCSVLine(string? line)
    {
        List<string>? parsedFields = null;

        if ((line is not null) && (line.Length > 0) && line.ContainsOtherThan(','))
        {
            parsedFields = new();

            string currentItem = "";
            bool foundQuote = false;

            foreach (var character in line)
            {
                /* Look for quotes in strings so they do not get added to parsed strings */
                if (character == '"')
                {
                    /* swap value of found quote, to show finding start quite and end quote */
                    foundQuote = !foundQuote;

                    /* Don't add quote to strings */
                }
                else if (character == ',' && !foundQuote)
                {
                    /* found comma delimiter and its not within a quote */
                    parsedFields.Add(currentItem);
                    currentItem = "";
                }
                else
                {
                    currentItem += character;
                }
            }
            /* after last item was parsed make sure to add it too list */
            parsedFields.Add(currentItem);

        }

        return parsedFields;
    }
}

