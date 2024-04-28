/*************************************************************************
* Description: Parses Stock Data From CSV Files and Stores it in Ram.  
* Author: Jason Neitzert
*************************************************************************/
using ExtensionMethods;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DividendStockAdvisor;

//
class InvestmentDatabase : IEnumerable<InvestmentDatabaseItem>
{

    /***************************** Class Enums ************************************/
    private enum DatabaseFields
    {
        AccountName,
        AccountNumber,
        AverageCostBasis,
        CostBasisTotal,
        CompanyName,
        CurrentValue,
        Description,
        DividendCoverageEPSNextYeasrByIAD, /* Earnings Per Share Next Year / Inidated Average Dividend */
        DividendCoverageEPSTTMByIAD, /* Earnings Per Share Trailing Twelve Months / Indicated Average Dividend */
        DividendGrowth5Year,
        DividendGrowthIADToPY, /* Indicated Annual Divident to Prior Year */
        DividendLastQuarter,
        DividendPayDate, /* Pay Date */
        DividendPerShare, /* Ampunt Per Share */
        DividendYield, /* Yield */
        EstimatedAnnualIncome,
        Exhange,
        ExDate,
        LastPrice,
        LastPriceChange,
        PercentOfAccount,
        Quantity,
        SecurityType,
        Symbol,
        TodaysGainLossDollar,
        TodaysGainLossPercent,
        TotalGainLossDollar,
        TotalGainLossPercent,
        Type,
        Unknown
    }

    /***************************** Private Class Variables *************************/
    public static readonly string _baseFileFolder = @"C:\DividendAdvisor\";
    private static readonly string _inputFolder = _baseFileFolder + @"input\";
    private static readonly string _dataFolder = _baseFileFolder + @"data\";
    private static readonly string _recentSoldItemsPath = _dataFolder + "RecentlySoldItems.csv";

    private List<InvestmentDatabaseItem> databaseItems = new List<InvestmentDatabaseItem>();
    private List<RecentlySoldItem> recentlySoldItems = new List<RecentlySoldItem>();

    public bool databaseReady { get; private set; } = true;

    /***************************** Public Class Functions **************************/

    /***************************************************************************
    * Description: Constructor
    ***************************************************************************/
    public InvestmentDatabase()
    {
        bool directoriesCreated = false;

        try
        {
            Directory.CreateDirectory(_baseFileFolder);
            Directory.CreateDirectory(_inputFolder);
            Directory.CreateDirectory(_dataFolder);

            directoriesCreated = true;
        }
        catch
        {
            Console.WriteLine("Failed To Create Directories for Program");
        }

        if (directoriesCreated && (databaseReady = ParseInvestmentData()))
        {
            CalculateCostBasisDividendRate();
        }


         
    }

    public InvestmentDatabaseItem? Find(Predicate<InvestmentDatabaseItem> match)
    {
        return databaseItems.Find(match);
    }


    /***************************************************************************
    * Description: Get Enumarator to Move through database
    * Return: IEnumerator<InvestmentDatabaseItem>
    ***************************************************************************/

    public IEnumerator<InvestmentDatabaseItem> GetEnumerator()
    {
        foreach (var item in databaseItems)
        {
            yield return item.Copy();
        }
    }

    /***************************************************************************
     * Description: Get Enumarator to Move through database
     * Return: IEnumerator<InvestmentDatabaseItem>
     ***************************************************************************/
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /***************************************************************************
    * Description: Parse Files to add their data to the database
    * Return: Void
    ***************************************************************************/
    public bool ParseInvestmentData()
    {
        /* Load Stock Data into database */
        ExcelHelper.convertXLSFilesToCSV(_inputFolder);
        List<string> fileList = Directory.GetFiles(_inputFolder).Where(s => s.EndsWith(".csv")).ToList();


        if (fileList.Count == 0)
        {
            Console.WriteLine("Failed to Find any Investment Data Files");
            return false;
        }

        foreach (string file in fileList)
        {
            List<List<string>>? csvData = ExcelHelper.ReadCSVFile(file);

            if (csvData == null)
                continue;


            List<DatabaseFields> fieldLookup = CreateFieldLookupList(csvData[0]);

            for (int i = 1; i < csvData.Count; i++)
            {
                PopulateDatabaseItem(csvData[i], fieldLookup);
            }

        }

        if (databaseItems.Count == 0)
        {
            Console.WriteLine("Failed to Parse any data from investment info files");
            return false;
        }

        databaseItems.Sort(InvestmentDatabaseItem.SortBySymbol);

        /* Load Recently Sold Items into database */
        List<List<string>>? recentlySoldData = ExcelHelper.ReadCSVFile(_recentSoldItemsPath);

        if (recentlySoldData != null)
        {
            foreach (var item in recentlySoldData)
            {
                RecentlySoldItem newItem = new(item[0], DateTime.Parse(item[1]));

                if (newItem.BuyingAllowedDate.CompareTo(DateTime.Today) < 0)
                {
                    var updateItem = databaseItems.Find(s => s.symbol == newItem.Symbol);
                    if (updateItem != null)
                    {
                        recentlySoldItems.Add(newItem);
                        updateItem.soldInLast30Days = true;
                    }
                }
            }
        }

        return true;
    }

    public void UpdateInvestmentItems(List<InvestmentDatabaseItem> updateItems)
    {
        int i = 0;

        foreach(var item in updateItems)
        {
            for (i = 0; i < databaseItems.Count; i++)
            {
                if (databaseItems[i].symbol == item.symbol)
                {
                    databaseItems.RemoveAt(i);
                    databaseItems.Add(item);
                }
            }
        }
    }
    
    public void AddRecentlySoldItems(List<RecentlySoldItem> recentlySoldItems)
    {
        foreach(var item in recentlySoldItems)
        {
            this.recentlySoldItems.Add(item);
        }

        List<List<string>> data = new List<List<string>>();

        foreach (var item in this.recentlySoldItems)
        {
            List<string> list = [item.Symbol, item.BuyingAllowedDate.ToString()];

            data.Add(list);
        }

        if (data.Count > 0)
        {
            /* Now write data to CSV file  */
            ExcelHelper.WriteCSVFile(_recentSoldItemsPath, data);
        }

    }

    /***************************** Private Class Functions **************************/
    /***************************************************************************
    * Description: Caclulate Dividend Rate Based on Cost Basis and store in database
    * Return: Void
    ***************************************************************************/
    private void CalculateCostBasisDividendRate()
    {
        foreach (var item in databaseItems)
        {
            if (item.AverageCostBasis > 0)
            {
                item.CostBasisDividendYield = item.EstimatedAnnualIncome / item.CostBasisTotal * 100;
            }
        }
    }

    /***************************************************************************
    * Description: Populate Data into a Database Item
    * Param: lineFields - list of lines of data to store into database
    * Param: fieldLookup - lookup of what each field in lineField contains
    * Return: Void
    ***************************************************************************/
    private void PopulateDatabaseItem(List<string> lineFields, List<DatabaseFields> fieldLookup)
    {
        int symbolFieldIndex = 0;


        
          /* Find symbol field index */
        for (symbolFieldIndex = 0; symbolFieldIndex < fieldLookup.Count;  symbolFieldIndex++)
        {
            if (fieldLookup[symbolFieldIndex] == DatabaseFields.Symbol)
            {
                break;
            }
        }

        /* Verify Symbol field was present */
        if (symbolFieldIndex >= fieldLookup.Count)
            return;

        /* Reject items */
        switch (lineFields[symbolFieldIndex])
        {
            case "FCASH**":
            case "Pending Activity":
                return;

            default:
                break;
        }

        /* See if any symbol field needs to be converted. Some reports list symbols in alternative format, so
         * convert them to ensure they all agree */
        if (lineFields[symbolFieldIndex].Contains('/') )
        {
            switch (lineFields[symbolFieldIndex])
            {
                case "GTLS/PB":
                    lineFields[symbolFieldIndex] = "GTLSPRB";
                    break;
                case "GEF/B":
                    lineFields[symbolFieldIndex] = "GEFB";
                    break;
                case "GTN/A":
                    lineFields[symbolFieldIndex] = "GTNA";
                    break;
                case "GRP/U":
                    lineFields[symbolFieldIndex] = "GRPU";
                    break;
                default:
                    Console.WriteLine("Need to Convert Symbol " + lineFields[symbolFieldIndex]);
                    break;
            }
        }
            

        /* see if a database item has matching symbol */
        var updateItem = databaseItems.Find(s=> s.symbol == lineFields[symbolFieldIndex]);

        /* create new item if one doesn't exist  */
        if (updateItem == null)
        {
            updateItem = new();
            databaseItems.Add(updateItem);
        }

        /* Save Fields I care about */
        SaveDataToDatabaseItem(fieldLookup, lineFields, updateItem);

    }

    /***************************************************************************
    * Description: Populate Data into a Database Item
    * Param: lineFields - list of lines of data to store into database
    * Param: fieldLookup - lookup of what each field in lineField contains
    * Param: DatabaseIndex - index in database to populate
    * Return: Void
    ***************************************************************************/
    private void SaveDataToDatabaseItem(List<DatabaseFields> fieldLookup, List<string> lineFields, InvestmentDatabaseItem updateItem)
    {
        for (var index = 0; index<fieldLookup.Count; index++)
        {
            switch (fieldLookup[index])
            {
                case DatabaseFields.AccountName:
                {
                    updateItem.AccountName = lineFields[index];
                    break;
                }
                case DatabaseFields.AccountNumber:
                {
                    break;
                }
                case DatabaseFields.AverageCostBasis:
                {
                    lineFields[index] = lineFields[index].RemoveChar('$');
                    if ((lineFields[index] == "") || (lineFields[index] == "--"))
                        updateItem.AverageCostBasis = 0;
                    else
                        updateItem.AverageCostBasis = double.Parse(lineFields[index]);
                    break;
                }
                case DatabaseFields.CompanyName:
                {
                    updateItem.companyName = lineFields[index];
                    break;
                }
                case DatabaseFields.CostBasisTotal:
                {
                    lineFields[index] = lineFields[index].RemoveChar('$');
                    if ((lineFields[index] == "") || (lineFields[index] == "--"))
                        updateItem.CostBasisTotal = 0;
                    else
                        updateItem.CostBasisTotal = double.Parse(lineFields[index]);
                    break;
                }
                case DatabaseFields.CurrentValue:
                {
                    lineFields[index] = lineFields[index].RemoveChar('$');
                    updateItem.CurrentValue = double.Parse(lineFields[index]);
                    break;
                }
               case DatabaseFields.Description:
                {
                    updateItem.Description = lineFields[index];
                    break;
                }
                case DatabaseFields.DividendCoverageEPSNextYeasrByIAD:
                {
                    if (lineFields[index] != "")
                        updateItem.DividendCoverageEPSNextYRByIAD = double.Parse(lineFields[index]);
                    break;
                }
                case DatabaseFields.DividendCoverageEPSTTMByIAD:
                {
                    if (lineFields[index] != "")
                        updateItem.DividendCoverageEPSTTMByIAD = double.Parse(lineFields[index]);
                    break;
                }

                case DatabaseFields.DividendGrowth5Year:
                {
                    if (lineFields[index] != "")
                        updateItem.DividendGrowth5Yr = double.Parse(lineFields[index]);
                    break;
                }

                case DatabaseFields.DividendGrowthIADToPY:
                {
                    if (lineFields[index] != "")
                        updateItem.DividendGrowthIADToPriorYR = double.Parse(lineFields[index]);
                    break;
                }
                case DatabaseFields.DividendLastQuarter:
                {
                    if (lineFields[index] != "")
                        updateItem.DividendLastQuarter = double.Parse(lineFields[index]);
                    break;
                }

                case DatabaseFields.DividendPayDate:
                {
                    break;
                }

                case DatabaseFields.DividendPerShare:
                {
                    if ((lineFields[index] == "") || (lineFields[index] == "--"))
                        updateItem.Dividend = 0;
                    else
                    {

                        lineFields[index] = lineFields[index].RemoveChar('$');
                        updateItem.Dividend = double.Parse(lineFields[index]);
                    }
                    break;
                }

                case DatabaseFields.DividendYield:
                {
                    lineFields[index] = lineFields[index].RemoveChars("%");
                    if ((lineFields[index] == "") || (lineFields[index] == "--"))
                        updateItem.DividendYield = 0;
                    else
                        updateItem.DividendYield = double.Parse(lineFields[index]);
                    break;
                }

                case DatabaseFields.EstimatedAnnualIncome:
                {
                    if (lineFields[index] == "--")
                        updateItem.EstimatedAnnualIncome = 0;
                    else
                    {
                        lineFields[index] = lineFields[index].RemoveChars("$");
                        updateItem.EstimatedAnnualIncome = double.Parse(lineFields[index]);
                    }
                    break;
                }

                case DatabaseFields.ExDate:
                {
                    if (!((lineFields[index] == "") || (lineFields[index] == "--")))
                        updateItem.exDate = DateOnly.Parse(lineFields[index]);
                    break;
                }

                case DatabaseFields.Exhange:
                {
                    updateItem.Exchange = lineFields[index];
                    break;
                }

                case DatabaseFields.LastPrice:
                {
                    lineFields[index] = lineFields[index].RemoveChar('$');
                    if (lineFields[index] != "")
                        updateItem.LastPrice = double.Parse(lineFields[index]);
                    break;
                }

                case DatabaseFields.LastPriceChange:
                {
                    break;
                }

                case DatabaseFields.PercentOfAccount:
                {
                    break;
                }

                case DatabaseFields.Quantity:
                {
                    break;
                }

                case DatabaseFields.SecurityType:
                {
                    updateItem.securityType = lineFields[index];
                    break;
                }

                case DatabaseFields.Symbol:
                {
                    updateItem.symbol = lineFields[index];
                    break;
                }

                case DatabaseFields.TodaysGainLossDollar:
                case DatabaseFields.TodaysGainLossPercent:
                {
                    break;
                }

                case DatabaseFields.TotalGainLossDollar:
                {
                    lineFields[index] = lineFields[index].RemoveChars("$");
                    if ((lineFields[index] == "") || (lineFields[index] == "--"))
                        lineFields[index] = "0";
                    updateItem.TotalGainLossDollar = double.Parse(lineFields[index]);
                    break;
                }

                case DatabaseFields.TotalGainLossPercent:
                {
                    lineFields[index] = lineFields[index].RemoveChars("%+");
                    if ((lineFields[index] == "") || (lineFields[index] == "--"))
                        lineFields[index] = "0";
                    updateItem.TotalGainLossPercent = double.Parse(lineFields[index]);
                    break;
                }

                case DatabaseFields.Type:
                {
                    break;
                }

                default:
                    Console.WriteLine("Havent Handled " + fieldLookup[index]);
                    break;
                }
        }
    }

    /***************************************************************************
    * Description: Parse header from CSV file and Convert it too list of DatabaseFields
    * Param: lineFields - List of Field Headers
    * Return: Void
    ***************************************************************************/
    private List<DatabaseFields> CreateFieldLookupList(List<string> lineFields)
    {
        List<DatabaseFields> fieldList = new();

        foreach (var field in lineFields)
        {
            fieldList.Add(StringToDataBaseField(field));
        }

        return fieldList;
    }

    /***************************************************************************
    * Description: Get a DataBaseFields Enum item from a String
    * Param: field - string to lookup a DatabaseField
    * Return: Void
    ***************************************************************************/
    private DatabaseFields StringToDataBaseField(string field)
    {
        DatabaseFields fieldType = DatabaseFields.Unknown;

        fieldType = field switch
        {
            "Account Number" => DatabaseFields.AccountNumber,
            "Account Name" => DatabaseFields.AccountName,
            "Amount Per Share" => DatabaseFields.DividendPerShare,
            "Average Cost Basis" => DatabaseFields.AverageCostBasis,
            "Company Name" => DatabaseFields.CompanyName,
            "Cost Basis Total" => DatabaseFields.CostBasisTotal,
            "Current Value" => DatabaseFields.CurrentValue,
            "Description" => DatabaseFields.Description,
            "Dividend" => DatabaseFields.DividendPerShare,
            "Dividend Coverage (EPS Next Yr/IAD)" => DatabaseFields.DividendCoverageEPSNextYeasrByIAD,
            "Dividend Coverage (EPS TTM/IAD)" => DatabaseFields.DividendCoverageEPSTTMByIAD,
            "Dividend Growth Rate (5 Year Avg)" => DatabaseFields.DividendGrowth5Year,
            "Dividend Growth Rate (IAD to Prior Yr)" => DatabaseFields.DividendGrowthIADToPY,
            "Dividend Payout % (Last Quarter)" => DatabaseFields.DividendLastQuarter,
            "Dividend Yield" => DatabaseFields.DividendYield,
            "Est. Annual Income" => DatabaseFields.EstimatedAnnualIncome, 
            "Ex-Date" => DatabaseFields.ExDate,
            "Ex. Dividend Date (Upcoming)" => DatabaseFields.ExDate,
            "Exchange" => DatabaseFields.Exhange,
            "Last Price" => DatabaseFields.LastPrice,
            "Last Price Change" => DatabaseFields.LastPriceChange,
            "Pay Date" => DatabaseFields.DividendPayDate,
            "Percent Of Account" => DatabaseFields.PercentOfAccount,
            "Quantity" => DatabaseFields.Quantity,
            "Security Price" => DatabaseFields.LastPrice,
            "Security Type" => DatabaseFields.SecurityType,
            "Symbol" => DatabaseFields.Symbol,
            "Today's Gain/Loss Dollar" => DatabaseFields.TodaysGainLossDollar,
            "Today's Gain/Loss Percent" => DatabaseFields.TodaysGainLossPercent,
            "Total Gain/Loss Dollar" => DatabaseFields.TotalGainLossDollar,
            "Total Gain/Loss Percent" => DatabaseFields.TotalGainLossPercent,
            "Type" => DatabaseFields.Type,
            "Yield" => DatabaseFields.DividendYield,
            _ => DatabaseFields.Unknown,
        };

        if (fieldType == DatabaseFields.Unknown)
            Console.WriteLine($"Field: {field} is unknown ");

        return fieldType;
    }
}





